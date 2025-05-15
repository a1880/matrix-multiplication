#
#  akSatSolver.py  - class to use SAT Solver for z3py
#
#  Axel Kemper  23-Mar-2025  1st draft
#

import os
import subprocess
import time

from z3 import (
    BoolVal,
    Goal,
    IntVal,
    is_bool,
    is_const,
    is_int,
    is_not,
    is_or,
    sat,
    Then,
    unsat
)

import exportAkbc
from util import (
    check,
    fatal,
    find_file_in_path,
    o,
    pretty_num,
    timestamp,
    TimeReporter
)


def write_dimacs_header(cnf, var2id, id2var, no_of_clauses):
    line_count = 0
    cnf.write("c DIMACS CNF file\n")
    cnf.write(f"c Created by akSatSolver.py SAT solver interface: {timestamp()}\n")

    cnf.write("c\n")
    cnf.write("c Translation table between variable names and literal IDs\n")
    line_count += 4
    for k, v in id2var.items():
        name = v.decl().name()
        if not (('!' in name) or (name.startswith("_"))):
            #  no auxiliary switching variables
            cnf.write(f"c {name} <-> {k}\n")
            line_count += 1
    cnf.write("c\n")
    cnf.write("\n")

    cnf.write(f"p cnf {len(var2id)} {no_of_clauses}\n")

    return line_count + 3


class akSatSolver:
    """
    Solver API provides methods for implementing some SMT 2.0 commands:
    check, get-model, etc.
    """

    def __init__(self, solver, variables, sat_solver, use_akbc, threads, debug):
        """
        solver:     the z3py Solver
        variables:  list of z3py variables to be referenced in the solution model
        sat_solver: name of SAT solver to be called via akBoole
        use_akbc:   translate solver assertions via akBool2cnf
        thread:     number of parallel threads
        debug:      activate debugging mode
        """
        self._model = None
        self.solver = solver
        self.sat_solver = sat_solver
        self.threads = threads
        self.variables = variables
        self.version = ""
        self.akBoole_exe = "akBoole.exe"  # expected to be on PATH
        self.use_akbc = use_akbc
        self.akBool2cnf_exe = "akBool2cnf.exe"  # expected to be on PATH
        self.debug = debug

    def check(self):
        """
        check whether the assertions in the given solver are consistent or not.
        """
        t = TimeReporter(f"SAT solver {self.sat_solver} run")
        print(f"\nLaunching SAT solver {self.sat_solver} to find a solution")
        started = time.time()

        input_file_name = "akSatSolver.dimacs.txt"
        output_file_name = "akSatSolver.sol.txt"
        akbc_file_name = "akSatSolver.akbc.txt"
        akbc_log_file_name = "akSatSolver.akbc.log.txt"

        if self.use_akbc:
            print(f"Creating DIMACS CNF file via akBool2cnf")
            exportAkbc.export_akbc(self.solver, akbc_file_name)
            exportAkbc.run_akbc(akbc_file_name, input_file_name, akbc_log_file_name, self.debug)
        else:
            print(f"Creating DIMACS CNF file")
            self.create_dimacs(input_file_name)

        check(os.path.exists(input_file_name),
              f"SAT file not found: {input_file_name}")
        print(f"\nLaunching SAT solver {self.sat_solver} to find a solution")
        if self.run_sat_solver(input_file_name, output_file_name):
            self._model = akSatSolver_model(output_file_name, self.variables, self.sat_solver)
            ret = sat
        else:
            ret = unsat

        return ret

    def collect_variables(self, result):
        """ iterate through formulae in result and populate dictionary of variables """
        var2id = {}

        for f in result:
            #  f is a BoolRef            
            if is_or(f):
                for e in f.children():
                    if is_not(e):
                        if self.debug:
                            check(e.num_args() == 1, "unexpected len(Args) != 1")
                            check(is_const(e.arg(0)), "arg is no constant")
                        v = e.arg(0)
                        if not (v in var2id):
                            var2id[v] = 0
                    elif is_const(e):
                        if not (e in var2id):
                            var2id[e] = 0
                    else:
                        print(e)
                        print(f"NOT or VAR formula expected, Got '{e.kind()}' children {len(e.children())}")
                        fatal("oops!")

            elif is_not(f):
                #  a clause with just one literal
                v = f.arg(0)
                if not (v in var2id):
                    var2id[v] = 0
                if self.debug:
                    check(is_const(v), "arg is no constant")
            elif is_const(f):
                #  single non-inverted literal
                if not (f in var2id):
                    var2id[f] = 0
            else:
                print(f)
                print(f"OR, NOT or VAR formula expected, Got '{f.kind()}' children {len(f.children())}")
                fatal("oops!")

        return var2id

    def write_clauses(self, cnf, result, var2id):
        """ iterate through formulas and output cNF clauses in DIMACS format """
        for f in result:
            if self.debug:
                o(str(f).replace("\n", " "))
            if is_or(f):
                for e in f.children():
                    if is_not(e):
                        id = -var2id[e.arg(0)]
                    else:
                        id = var2id[e]
                    cnf.write(f"{id} ")
            elif is_not(f):
                cnf.write(f"-{var2id[f.arg(0)]} ")
            elif is_const(f):
                #  single not-interved literal
                cnf.write(f"{var2id[f]} ")
            else:
                o(f)
                o(f"OR, NOT or VAR formula expected: got '{f.kind()}' children {len(f.children())}")
                fatal("oops!")

            cnf.write("0\n")
        cnf.write("\n")

        return len(result) + 1

    def create_dimacs(self, dimacs_file_name):
        #  Credit to Simon Felix
        #  https://stackoverflow.com/a/39979431/1911064
        print(f"Writing CNF/Dimacs file '{dimacs_file_name}'")
        goal = Goal()
        goal.add(self.solver.assertions())
        #  https://microsoft.github.io/z3guide/docs/strategies/summary/
        # applyResult = \
        #    Then(Tactic("simplify"),
        #        Tactic("bit-blast"),
        #        Tactic("tseitin-cnf")).apply(goal)
        # t = Then('simplify', 'bit-blast', 'aig', 'simplify', 'tseitin-cnf')
        # t = Then('simplify', 'symmetry-reduce', 'bit-blast', 'tseitin-cnf')
        t = Then('simplify', 'bit-blast', 'tseitin-cnf')
        applyResult = t(goal)
        check(len(applyResult) == 1, "Unexpected number of subgoals")

        #  dictionary maps from BoolExpr to int
        result = applyResult[0]  # result is a Goal
        var2id = self.collect_variables(result)

        #  assign numbers to variables
        id = 0
        id2var = {}
        for key in var2id.keys():
            var2id[key] = (id := id + 1)
            id2var[id] = key

        with open(dimacs_file_name, "w", encoding='ascii') as cnf:
            lineCount = write_dimacs_header(cnf, var2id, id2var, len(result))
            lineCount += self.write_clauses(cnf, result, var2id)
            print(f"CNF/Dimacs file created. Lines {pretty_num(lineCount)}")

    def model(self):
        """
        Return a model for the last `check()`.
        """
        if self._model is None:
            o("model is not available")
            return None
        else:
            return self._model

    def run_sat_solver(self, input_file_name, output_file_name):
        exe = find_file_in_path("akBoole.exe", "$PATH")
        check(exe is not None, "akBoole.exe not found!")
        command = \
            [exe,
             "-ss", self.sat_solver,
             "-s", "1",
             "-vvvv" if self.debug else "-vv",
             "-i", f"\"{input_file_name}\"",
             "-mt", str(self.threads),
             "--log", f"\"{output_file_name}.log.txt\"",
             "-o", f"\"{output_file_name}\""]

        o("")
        o("Running SAT solver via akBoole:")
        if self.debug:
            print(command)

        #  make sure that akBoole output comes after the
        #  output stored so far
        print("", flush=True)
        print("", flush=True)

        result = subprocess.run(command)

        o()
        if result.returncode == 0:
            o(f"{self.sat_solver} solver executed successfully.")
        else:
            o(f"{self.sat_solver} solver failed with return code {result.returncode}.")

        return result.returncode == 0


class akSatSolver_model:
    """
    Simplified z3py model class to make SAT Solver solution available
    to the caller
    """

    def __init__(self, file_name, variables, sat_solver):
        #  a dictionary of z3py variables as keys is used to
        #  access the found solution values
        self._data = {}
        self.sat_solver = sat_solver
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

    def process_line(self, line, lineCount):
        """
        interpret a solution line and store variable+value in dictionary
        """
        s = line[:-1].strip()
        a = s.split()
        if (len(a) == 3) and (a[1] == "="):
            var_name = a[0]
            val = a[2]
        else:
            msg = f"Unexpected line {lineCount}: '{s}'. Should have three fields"
            raise ValueError(msg)
        var = self.name2var.get(var_name)
        #  We have to wrap the int value into a suitable object
        #  Otherwise, the value consumer could not access it via as_long()
        if var is None:
            check('!' in var_name, "Expected var to be switching variable with '!'")
        elif is_int(var):
            fatal("SAT Solver cannot deal with Int variables")
            self[var] = IntVal(int(val))
        elif is_bool(var):
            self[var] = BoolVal(val == "1")
        else:
            o(f"var '{var}', line {lineCount}: '{line}'")
            o(f"var {var.kind()} '{var}', line {lineCount}: '{line}', {var.decl().name()}, ")
            raise RuntimeError(f"unexpected variable type {var}")

    def read_solution(self, file_name):
        lineCount = 0
        if not os.path.exists(file_name):
            msg = f"SAT solver solution file not found: {file_name}"
            raise RuntimeError(msg)

        start_ok = False
        with open(file_name, "r", encoding='ascii') as file:
            for line in file:
                lineCount = lineCount + 1
                if start_ok:
                    if line.endswith(" = 0\n") or \
                            line.endswith(" = 1\n"):
                        self.process_line(line, lineCount)
                elif line == "Solution 1:\n":
                    start_ok = True

        check(start_ok, "No solution section found in file!")

        print(f"Solution file read complete. Lines: {lineCount}")
