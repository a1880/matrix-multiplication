#
#  akMiniZincSolver.py  - class to use MiniZinc solver for z3py
#
#  Axel Kemper  05-May-2025  1st draft
#
#  Make sure to adjust the paths of the MiniZinc executables
#

import json
import os
import subprocess
from util import (check, fatal, get_debug_level, o, o4,
                  load_json_line, load_json_with_trailing_commas,
                  pretty_num, timestamp, TimeReporter, wrap)
from z3 import (
    BoolVal,
    Goal,
    IntNumRef,
    IntVal,
    is_add,
    is_bool,
    is_const,
    is_eq,
    is_int,
    is_not,
    is_or,
    sat,
    Then,
    unsat,
    Z3_OP_ITE
)


class akMiniZincSolver:
    """
    Solver API provides methods for implementing commands:
    check, get-model, etc.
    """

    def __init__(self, solver, variables, threads: int, timeout_seconds: int, minizinc_backend: str = "cp-sat"):
        """
        solver:     the z3py Solver
        
        variables:  list of z3py variables to be referenced in the solution model
        """
        self._model = None
        self.solver = solver
        self.minizinc_backend = minizinc_backend
        self.variables = variables
        self.var2id = None
        self.id2var = None
        self.debug = (get_debug_level() > 3)
        if self.debug:
            o("akMiniZincSolver in debug mode")
        self.minizinc_home = "C:/ak/Tools/MiniZinc"
        self.minizinc_exe = self.minizinc_home + "/minizinc.exe"
        self.gecode_exe = self.minizinc_home + "/bin/fzn-gecode.exe"
        self.chuffed_exe = self.minizinc_home + "/bin/fzn-chuffed.exe"
        self.cp_sat_exe = self.minizinc_home + "/bin/fzn-cp-sat.exe"
        self.timeout = timeout_seconds
        self.threads = threads
        self.version = "???"
        self.minizinc_backend_description = "???"
        if minizinc_backend == "gecode":
            check(os.path.exists(self.gecode_exe),
                  f"gecode executable not found: {self.gecode_exe}")
        elif minizinc_backend == "chuffed":
            check(os.path.exists(self.chuffed_exe),
                  f"chuffed executable not found: {self.chuffed_exe}")
        elif minizinc_backend == "cp-sat":
            check(os.path.exists(self.cp_sat_exe),
                  f"OR Tools cp-sat executable not found: {self.cp_sat_exe}")
        else:
            check(False,
                  f"Expected MiniZinc backend to be 'gecode', 'chuffed' or 'cp_sat': {minizinc_backend}")
        msc = f"{self.minizinc_home}/share/minizinc/solvers/{minizinc_backend}.msc"
        if os.path.exists(msc):
            data = load_json_with_trailing_commas(msc)
            try:
                self.version = data['version']
                self.minizinc_backend_description = data['description']
                self.minizinc_backend_name = data['name']
                self.supports_treads = "-p" in data['stdFlags']
            except json.decoder.JSONDecodeError as e:
                o(f"Cannot decode MiniZinc backend configuration file for {minizinc_backend}")
                o(f"Error message: {e}")
        else:
            o(f"No MiniZinc backend configuration available for {minizinc_backend}")
            
    def check(self):
        """
        check whether the assertions in the given solver are consistent or not.
        """
        print(f"\nLaunching MiniZinc solver with backend {self.minizinc_backend} to find a solution")
        _ = TimeReporter(f"MiniZinc {self.minizinc_backend}")
        
        input_file_name = "akMiniZincSolver.mzn"
        output_file_name = "akMiniZincSolver.sol.txt"

        self.create_MiniZinc_file(input_file_name)

        check(os.path.exists(input_file_name),
              f"MiniZinc file not found: {input_file_name}")
        if self.run_minizinc_solver(input_file_name, output_file_name):
            self._model = akMiniZincSolver_model(output_file_name, self.var2id, self.id2var, self.minizinc_backend)
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
                    elif is_eq(e):
                        #  equality expression
                        check(len(e.children()) == 2, "Expected 2 children in eq()")
                        vv = self.collect_variables(e.children())
                        var2id.update(vv)
                    elif e.kind() == Z3_OP_ITE:
                        cc = e.children()
                        check(len(cc) == 3, "expected 3 children for ITE")
                        check(isinstance(cc[1], IntNumRef), "Child 1 expected as IntNumRef")
                        check(isinstance(cc[2], IntNumRef), "Child 2 expected as IntNumRef")
                        vv = self.collect_variables(e.children())
                        var2id.update(vv)                        
                    else:
                        print(e)
                        cc = e.children()
                        print(f"Unexpected e-op. Got '{e.kind()}' children {len(cc)}")
                        chno = 1
                        for child in cc:
                            print(f"child {chno}: {child}")
                            chno += 1
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
            elif is_eq(f):
                #  equality expression
                check(len(f.children()) == 2, "Expected 2 children in eq()")
                vv = self.collect_variables(f.children())
                var2id.update(vv)
            elif is_add(f):
                #  add expression
                vv = self.collect_variables(f.children())
                var2id.update(vv)
            elif f.kind() == Z3_OP_ITE:
                cc = f.children()
                check(len(cc) == 3, "expected 3 children for ITE")
                check(isinstance(cc[1], IntNumRef), "Child 1 expected as IntNumRef")
                check(isinstance(cc[2], IntNumRef), "Child 2 expected as IntNumRef")
                vv = self.collect_variables(f.children())
                var2id.update(vv)                        
            else:
                print(f)
                cc = f.children()
                print(f"Unexpected f-op. Got '{f.kind()}' children {len(cc)}")
                chno = 1
                for child in cc:
                    print(f"child {chno}: {child}")
                    chno += 1
                fatal("oops!")
        
        return var2id

    def create_MiniZinc_file(self, file_name):
        #  Credit to Simon Felix
        #  https://stackoverflow.com/a/39979431/1911064
        print(f"Writing MiniZinc mzn file '{file_name}'")
        goal = Goal()
        goal.add(self.solver.assertions())
        #  https://microsoft.github.io/z3guide/docs/strategies/summary/
        # applyResult = \
        #    Then(Tactic("simplify"),
        #        Tactic("bit-blast"),
        #        Tactic("tseitin-cnf")).apply(goal)
        # t = Then('simplify', 'bit-blast', 'aig', 'simplify', 'tseitin-cnf')
        # t = Then('simplify', 'symmetry-reduce', 'bit-blast', 'tseitin-cnf')
        # t = Then('simplify', 'bit-blast', 'tseitin-cnf')
        t = Then('simplify', 'tseitin-cnf')
        # t = Then('simplify', 'propagate-values', 'ctx-simplify', 'ctx-solver-simplify', 'tseitin-cnf')
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
        
        self.id2var = id2var
        self.var2id = var2id
        
        with open(file_name, "w", encoding='ascii') as mzn:
            lineCount = self.write_MiniZinc_header(mzn, id2var, len(result))
            lineCount += self.write_clauses(mzn, result)
            print(f"MiniZinc mzn file created. Lines {pretty_num(lineCount)}")

    def model(self):
        """
        Return a model for the last `check()`.
        """
        if self._model is None:
            print("model is not available")
            return None
        else:
            return self._model

    def run_minizinc_solver(self, input_file_name, output_file_name):
        #  minizinc  --json-stream --param-file-no-push C:/AK/Temp/mzn_JGSxXC.mpc
        """
        {
            "intermediate-solutions": true,
            "output-objective": true,
            "parallel": 5,
            "solver": "org.gecode.gecode@6.3.0",
            "time-limit": 22211000
        }
        
        "org.chuffed.chuffed@0.13.2"
        "cp-sat@9.12.4544"
        """
        mpc_file_name = "akMiniZincSolver.mpc"
        with open(mpc_file_name, "w", encoding='ascii') as mpc:
            mpc.write("{\n")
            mpc.write("    \"intermediate-solutions\": false,\n")
            mpc.write("    \"output-objective\": false,\n")
            if self.supports_treads:
                mpc.write(f"    \"parallel\": {self.threads},\n")
                mpc.write(f"    \"-p\": {self.threads},\n")
            if self.timeout > 0:
                mpc.write(f"    \"time-limit\": {self.timeout * 1000},\n")
            mpc.write(f"    \"solver\": \"{self.minizinc_backend}\"\n")
            mpc.write("}\n")
       
        # arg_verb = ""
        command = [self.minizinc_exe, "--json-stream", "--param-file-no-push", mpc_file_name]
        # command += ["--verbose"]
        command += ["-o", output_file_name, input_file_name]            

        with open(output_file_name, "w", encoding='ascii') as f:
            # Run the command and redirect the output to the file
            o(command)
            result = subprocess.run(command, stdout=f)

        o()
        if result.returncode == 0:
            print("MiniZinc solver executed successfully.")
        else:
            print(f"MiniZinc solver failed with return code {result.returncode}.")

        return result.returncode == 0

    def bracket(self, e):
        if e.startswith("x") and e.endswith("]") and (e.find("=") < 0) and (e.find(" ") < 0):
            """  subscripted x[] variable """
            return e
        elif len(e) == 1:
            """  single digit or one-character variable """
            return e
        else:
            return f"({e})"
        
    def expression(self, e):
        """ translate z3py expression to MiniZinc expression string """
        var2id = self.var2id
        if isinstance(e, IntNumRef):
            """ integer constant """
            return e.as_string()
        elif is_const(e):
            #  single not-inverted literal
            return f"x[{var2id[e]}]"
        elif is_not(e):
            x = self.expression(e.arg(0))
            return f"not {self.bracket(x)}"
        elif is_eq(e):
            cc = e.children()
            e0 = self.expression(cc[0])
            e1 = self.expression(cc[1])
            return f"{self.bracket(e0)} == {self.bracket(e1)}"
        elif is_add(e):
            sep = ""
            s = ""
            for child in e.children():
                s += f"{sep}{self.expression(child)}"
                sep = " + "
            return s
        elif is_or(e):
            sep = ""
            s = ""
            for child in e.children():
                s += f"{sep}{self.expression(child)}"
                sep = " \\/ "
            return s
        elif e.kind() == Z3_OP_ITE:
            cc = e.children()
            e_cond = self.expression(cc[0])
            e_yes = self.expression(cc[1])
            e_no = self.expression(cc[2])
            
            if (e_yes == "1") and (e_no == "0"):
                """ Booleans are coerced to 1 for true and 0 for false """
                return f"({e_cond})"
            else:
                return f"Ite({e_cond}, {e_yes}, {e_no})"            
        else:
            o(e)
            o(f"Unexpected expr-op: got '{e.kind()}' children {len(e.children())}")
            fatal("oops!")
            return("dummy")
                        
    def write_clauses(self, mzn, result):
        """ iterate through formulas and output clauses in MZN format """
        margin = len("constraint (")
        for f in result:
            if self.debug:
                o(str(f).replace("\n", " "))
            e = self.expression(f)
            s = f"constraint {e};\n"
            if (s.find("==") > 0) and (len(s) > 50):
                s = wrap(s, width=65, margin_width=margin, separator=" + ")
            mzn.write(s)
            
        mzn.write("\n")

        return len(result) + 1

    def write_MiniZinc_header(self, mzn, id2var, no_of_clauses):
        line_count = 2
        mzn.write("% MiniZinc mzn file\n")
        mzn.write(f"% Created by akMiniZinc.py MiniZinc interface: {timestamp()}\n")

        if self.debug:
            mzn.write("%\n")
            mzn.write("% Translation table between variable names and literal IDs\n")
            line_count += 2
            for k, v in id2var.items():
                name = v.decl().name()
                if not (('!' in name) or (name.startswith("_"))):
                    #  no auxiliary switching variables
                    mzn.write(f"% {name} <-> {k}\n")
                    line_count += 1
            mzn.write("%\n")
            mzn.write("\n")
            mzn.write(f"% p cnf {len(id2var)} {no_of_clauses}\n")
            line_count += 3
        else:
            mzn.write(f"% constraints: {no_of_clauses}\n")
            mzn.write("\n")
            line_count += 2
            
        mzn.write("%  decision variables as one big array\n")
        mzn.write(f"array[1..{len(id2var)}] of var bool: x;\n")
        mzn.write("\n")
        mzn.write("%  convenience function for ITE operation\n")
        mzn.write("function var int: Ite(var bool: cond, int: t, int: f) =\n");
        mzn.write("    if cond then t else f endif;\n")
        mzn.write("\n")
        
        return line_count + 7

class akMiniZincSolver_model:
    """
    Simplified z3py model class to make MiniZinc solution available
    to the caller
    """

    def __init__(self, file_name, var2id, id2var, minizinc_backend):
        #  a dictionary of z3py variables as keys is used to
        #  access the found solution values
        self._data = {}
        self.minizinc_backend = minizinc_backend
        #  a second dictionary helps to find the variables by name
        self.var2id = var2id
        self.id2var = id2var
        #  first and only action of the model is to digest the
        #  solution file provided by the solver
        self.read_solution(file_name)

    def __getitem__(self, key):
        if key in self._data:
            return self._data[key]
        else:
            raise KeyError(f"Key '{key}' not found")

    def __setitem__(self, key, value):
        self._data[key] = value

    def __str__(self):
        s = f"len {len(self._data)}\n"
        for k, v in enumerate(self._data):
            s += f"{k}: {v}\n"
        return s
    
    def copy_solution(self, dzn):
        check(dzn.startswith("x = "), "Inconsistent dzn start")
        check(dzn.endswith("];\n"), f"Inconsistent dzn end '...{dzn[-5:]}'")
        dzn = dzn[5:-3]
        a = dzn.split(", ")
        o4(f"dzn len {len(a)}")
        o4(a)
        id = 1
        for val in a:
            b = (val == "true")
            var = self.id2var.get(id)
            #  We have to wrap the int value into a suitable object
            #  Otherwise, the value consumer could not access it via as_long()
            if is_int(var):
                if b:
                    self[var] = IntVal(1)
                elif val == "false":
                    self[var] = IntVal(0)
                else:
                    self[var] = IntVal(int(val))
            elif is_bool(var):
                self[var] = BoolVal(b)
            else:
                print(f"var '{var}', {var.decl().name()}")
                raise RuntimeError(f"unexpected variable type {var}")
            id += 1
              
    def read_solution(self, file_name):
        if not os.path.exists(file_name):
            msg = f"MiniZinc solver solution file not found: {file_name}"
            raise RuntimeError(msg)

        try:
            data = load_json_line(file_name, "solution")
            
            if data is None:
                fatal("No json 'solution' section in {file_name}")
            else:
                check(data['type'] == "solution",
                      f"Invalid data type {data['type']}. \"solution\" expected")
                dzn = data['output']['dzn']
                self.copy_solution(dzn)
                
        except json.JSONDecodeError as e:
            print("JSON decoding error:", e)
                
        print("Solution file read complete.")
        
