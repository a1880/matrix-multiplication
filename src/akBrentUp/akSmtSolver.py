#
#  akSmtSolver.py  - class to use SmtSolver like yices or cvc5 as SMT2 solver for z3py
#
#  Axel Kemper  20-Mar-2025  1st draft
#
#  Make sure to adjust the paths of the SMT2 solver executables
#

import os
import subprocess
from util import (check, pretty_num, TimeReporter)
from z3 import (BoolVal, IntVal, is_bool, is_int, sat, unsat)


class akSmtSolver:
    """
    Solver API provides methods for implementing some SMT 2.0 commands:
    check, get-model, etc.
    """

    def __init__(self, solver, variables, smt_solver: str, threads: int):
        """
        solver:     the z3py Solver
        
        variables:  list of z3py variables to be referenced in the solution model
        """
        self._model = None
        self.solver = solver
        self.smt_solver = smt_solver
        self.variables = variables
        self.threads = threads
        self.yices_exe = "C:/AK/Tools/yices-2.6.5/bin/yices-smt2.exe"
        self.cvc5_exe = "C:/AK/Tools/cvc5-Win64-x86_64-static/bin/cvc5.exe"
        if smt_solver == "yices":
            check(os.path.exists(self.yices_exe),
                  f"yices2 executable not found: {self.yices_exe}")
            self.version = "2.6.5"
        elif smt_solver == "cvc5":
            check(os.path.exists(self.cvc5_exe),
                  f"cvc5 executable not found: {self.cvc5_exe}")
            self.version = "1.2.1"
        else:
            check(False,
                  "Expected smt_solver to be 'yices' or 'cvc5': {smt_solver}")

    def check(self):
        """
        check whether the assertions in the given solver are consistent or not.
        """
        print(f"\nLaunching SMT2 solver {self.smt_solver} to find a solution")
        timeReporter = TimeReporter("Writing SMT2 file")
        expr = self.solver.to_smt2()
        a = expr.split('\n')
        a = ["(set-logic QF_LIA)"] + a[2:-1] + ["(get-model)"]

        input_file_name = "akSmtSolver.smt2.txt"
        output_file_name = "akSmtSolver.sol.txt"

        with open(input_file_name, 'w', encoding='ascii') as file:
            # Write joined lines to the file
            file.write("\n".join(a))

        check(os.path.exists(input_file_name),
              f"SMT2 file not found: {input_file_name}")
        del timeReporter

        if (self.smt_solver == "yices") or (self.threads < 2):
            timeReporter = TimeReporter(f"Running SMT2 solver {self.smt_solver} with {self.threads} threads")
        else:
            timeReporter = TimeReporter(f"Running SMT2 solver {self.smt_solver}, single-threaded")
        if self.run_smt_solver(input_file_name, output_file_name):
            self._model = akSmtSolver_model(output_file_name, self.variables, self.smt_solver)
            ret = sat
        else:
            ret = unsat
        del timeReporter

        return ret

    def model(self):
        """
        Return a model for the last `check()`.
        """
        if self._model is None:
            print("model is not available")
            return None
        else:
            return self._model

    def run_smt_solver(self, input_file_name, output_file_name):
        if self.smt_solver == "yices":
            command = [self.yices_exe, "--verbosity=1", f"--nthreads={self.threads}", input_file_name]
        else:
            command = [self.cvc5_exe, "--verbose", "--produce-models", input_file_name]

        with open(output_file_name, "w", encoding='ascii') as f:
            # Run the command and redirect the output to the file
            # result = subprocess.run(command, stdout=f, stderr=subprocess.STDOUT)
            result = subprocess.run(command, stdout=f)

        print()
        if result.returncode == 0:
            print(f"{self.smt_solver} solver executed successfully.")
        else:
            print(f"{self.smt_solver} solver failed with return code {result.returncode}.")

        return result.returncode == 0


class akSmtSolver_model:
    """
    Simplified z3py model class to make cvc5/yices2 solution available
    to the caller
    """

    def __init__(self, file_name, variables, smt_solver):
        #  a dictionary of z3py variables as keys is used to
        #  access the found solution values
        self._data = {}
        self.smt_solver = smt_solver
        #  a second dictionary helps to find the variables by name
        self.name2var = {var.decl().name(): var for var in variables}
        #  first and only action of the model is to digest the
        #  solution file provided by the SMT2 solver
        self.read_solution(file_name)

    def __getitem__(self, key):
        if key in self._data:
            return self._data[key]
        else:
            raise KeyError(f"Key '{key}' not found")

    def __setitem__(self, key, value):
        self._data[key] = value

    def process_line(self, line, true_line_count):
        """
        interpret a solution line and store variable+value in dictionary
        """
        s = line.strip()[3:-1]
        a = s.split()
        if (len(a) == 5) and line.startswith("(define"):
            #  long solution line format
            var_name = a[1]
            val = a[4]
        elif len(a) == 2:
            #  short yices2 solution line format
            var_name = a[0]
            val = a[1]
        else:
            msg = f"Unexpected line {true_line_count}: '{s}'. Should have two or five fields"
            raise ValueError(msg)
        var = self.name2var.get(var_name)
        #  We have to wrap the int value into a suitable object
        #  Otherwise, the value consumer could not access it via as_long()
        if is_int(var):
            self[var] = IntVal(int(val))
        elif is_bool(var):
            self[var] = BoolVal(val == "true")
        else:
            print(f"var '{var}', line {true_line_count}: '{line}', {var.decl().name()}, ")
            raise RuntimeError(f"unexpected variable type {var}")

    def read_solution(self, file_name):
        line_count = 0
        true_line_count = 0
        if not os.path.exists(file_name):
            msg = f"SMT2 solver solution file not found: {file_name}"
            raise RuntimeError(msg)

        with open(file_name, "r", encoding='ascii') as file:
            for line in file:
                if not (line.startswith("=") or line.startswith("|") or
                        (line == "(\n") or (line == ")\n")):
                    line_count += 1
                    true_line_count += 1
                    if line_count == 1:
                        if line != "sat\n":
                            print(f"line = '{line}'")
                            raise ValueError(f"sat expected in first nonheader-line of {self.smt_solver} solution")
                    else:
                        self.process_line(line, true_line_count)

        print(f"Solution file read complete. Lines: {pretty_num(true_line_count)}")
        
