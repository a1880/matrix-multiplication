"""
[]--------------------------------------------------------------[]
|                                                                |
|  akBrentUp  -  Solve Brent's equations for matrix              |
|                multiplication.                                 |
|                Input is a mod 2 solution in Bini format.       |
|                Output is a solution with +1/-1 coefficients.   |
|                                                                |
|  Axel Kemper  24-Apr-2025                                      |
|                                                                |
[]--------------------------------------------------------------[]

usage: python -OO akBrentup.py [options] bini_scheme_file

find matrix multiplication algorithm

positional arguments:
  bini                  matrix multiplication problem in Bini form [default: s2x2x2_07.bini.mod2.txt]

options:
  -h, --help            show this help message and exit
  -L, --lift            lifting method to be used: direct, hensel, groebner [default: direct]
  -s, --seed SEED       seed for random numbers [-1 = clock]
  -t, --threads THREADS
                        number of parallel threads
  -T, --timeout TIMEOUT
                        timeout in seconds [0 = no timeout]
  -v, --verbose
  -S, --solver [SOLVER]
                        solver to be used: cvc5, minizinc, yices, z3 or sat_xxx [default: sat_clasp]
  --akbc [AKBC]         Translate assertions via akBool2cnf

contact: Axel@KemperZone.de

"""
import os

from z3 import z3

from Events import Events
from akMiniZincSolver import akMiniZincSolver
from akSmtSolver import akSmtSolver
from BiniScheme import BiniScheme, debugLevel
from config import Config
from HenselLifter import HenselLifter
from GroebnerLifter import GroebnerLifter
from MatMultDim import MatMultDim
import sys
from util import (
    check,
    define_ctrl_break_handler,
    fatal,
    find_file_in_path,
    finish,
    get_debug_level,
    o,
    o4,
    path_add,
    pretty_num,
    TimeReporter,
    Watch
)
from yacas import create_yacas_file, int_value

events: Events = Events()

""" count assertions for reporting/statistics """
assertionIdx: int = 0

""" dynamically derived from mmDim """
product_index_digits: int = 2

""" To keep track of all variables created   """
""" These are required for akSmtSolver.model()  """
variables: list[z3.BoolRef] = []

#  Literals are created on demand
#  Dictionary name: Bool
literals: dict[str, z3.BoolRef] = {}
ref_counts: dict[str, int] = {}

#  Xor dyads are created on demand
#  Dictionary name: Bool
dyads: dict[str, z3.BoolRef] = {}

def test_case() ->None:
    test = 555
    all_CPUs = str(os.cpu_count())

    if test == 1:
        t = ['--help']
    elif test == 115:
        t = ["-t", all_CPUs,
             "-S", "z3",
             "-vv",  #  4; default is 2
             "1x1x5_05/s1x1x5_05.bini.mod2.txt"]
    elif test == 1151:
        t = ["-t", all_CPUs,
             "--lift", "groebner",
             "-vv",  #  4; default is 2
             "1x1x5_05/s1x1x5_05.bini.mod2.txt"]
    elif test == 212:
        t = ["-t", "1",
             "-S", "minizinc_cp-sat",
             "-vv",  #  4; default is 2
             "s2x1x2_04/s2x1x2_04.bini.mod2.txt"]
    elif test == 222:
        t = ["-t", all_CPUs,
             "-S", "minizinc_cp-sat",
             "-vv",  #  4; default is 2
             "s2x2x2_07/s2x2x2_07.bini.mod2.txt"]
    elif test == 2221:
        t = ["-t", all_CPUs,
             "--lift", "groebner",
             "-vv",  #  4; default is 2
             "s2x2x2_07/s2x2x2_07.bini.mod2.txt"]
    elif test == 456:
        t = ["-t", all_CPUs,
             "-S", "z3",
             # "-vv",  #  4; default is 2
             "4x5x6_90/s4x5x6_90.bini.mod2.txt"]
    elif test == 457:
        t = ["-t", all_CPUs,
             "-S", "z3",
             # "-vv",  #  4; default is 2
             "4x5x7_104/s4x5x7_104.bini.mod2.txt"]
    elif test == 466:
        t = ["-t", all_CPUs,
             "-S", "z3",
             # "-vv",  #  4; default is 2
             "4x6x6_106/s4x6x6_106.bini.mod2.txt"]
    elif test == 467:
        t = ["-t", all_CPUs,
             "-S", "z3",
             # "-vv",  #  4; default is 2
             "4x6x7_123/s4x6x7_123.bini.mod2.txt"]
    elif test == 477:
        t = ["-t", all_CPUs,
             "-S", "z3",
             # "-vv",  #  4; default is 2
             "4x7x7_144/s4x7x7_144.bini.mod2.txt"]
    elif test == 555:
        t = ["-t", all_CPUs,
             # "-S", "z3",
             # "-S", "minizinc_cp-sat",
             "-S", "yices",
             # "-v",  #  3; default is 2
             "s5x5x5_93/s5x5x5_93.bini.mod2.txt"]
    elif test == 556:
        t = ["-t", all_CPUs,
             "-S", "z3",
             # "-vv",  #  4; default is 2
             "5x5x6_110/s5x5x6_110.bini.mod2.txt"]
    elif test == 557:
        t = ["-t", all_CPUs,
             "-S", "z3",
             # "-vv",  #  4; default is 2
             "5x5x7_127/s5x5x7_127.bini.mod2.txt"]
    elif test == 566:
        t = ["-t", all_CPUs,
             "-S", "z3",
             # "-vv",  #  4; default is 2
             "5x6x6_130/s5x6x6_130.bini.mod2.txt"]
    elif test == 567:
        t = ["-t", all_CPUs,
             "-S", "z3",
             # "-vv",  #  4; default is 2
             "5x6x7_150/s5x6x7_150.bini.mod2.txt"]
    elif test == 577:
        t = ["-t", all_CPUs,
             "-S", "z3",
             # "-vv",  #  4; default is 2
             "5x7x7_176/s5x7x7_176.bini.mod2.txt"]
    elif test == 666:
        t = ["-t", all_CPUs,
             # "-S", "minizinc_cp-sat",
             # "-S", "yices",
             "-S", "z3",
             "s6x6x6_153/s6x6x6_153.bini.mod2.txt"]
    elif test == 667:
        t = ["-t", all_CPUs,
             "-S", "z3",
             # "-vv",  #  4; default is 2
             "6x6x7_183/s6x6x7_183.bini.mod2.txt"]
    else:
        fatal(f"Invalid test {test}")

    sys.argv = ['akBrentUp.py'] + t


def add(solver: z3.Solver, predicates) -> None:
    """ add assertion to solver
        predicates can be a single z3.BoolRef or
        multiple ones  """
    global assertionIdx
    try:
        events.register("assertion")
        o4(f"assert {str(predicates)}")
        solver.add(predicates)
        if hasattr(predicates, "__len__"):
            assertionIdx += len(predicates)
        else:
            assertionIdx += 1
    except z3.Z3Exception as ex:
        o(f"Z3Exception: {ex}")
        o(predicates)
        fatal("Z3 error")


def And(a: z3.BoolRef, b: z3.BoolRef) -> z3.BoolRef:
    """
    Create a Z3 AND expression
    :param a: Bool variable
    :param b: Bool variable
    :return: (a && b) as Boolean expression
    """
    events.register("And2() expression")
    return z3.And(a, b)


def Bool(name: str) -> z3.BoolRef:
    """ create a new z3 Bool variable """
    events.register("Bool variable")
    _v = z3.Bool(name)
    variables.append(_v)
    return z3.Bool(name)


def cardinality(solver, lits: list[z3.BoolRef]) -> None:
    """
    add a cardinality assertion to the solver.
    The count of true literals is constrained.
    
    For an even number of literals, the number of
    true and false literals must be the same to
    fulfil an even equation of Brent's equations.
    The number of '+' and '-' summands must match
    to get 0 as sum.
    
    For an odd number of literals, the number of
    true literals must be one larger than the number if
    false literals. Otherwise, the sum would not be 1.
    
    For an empty list of literals, nothing as to be
    done. There are no signs to be decided upon.
    """
    n = len(lits)
    if n < 1:
        """ ignore empty list """
        pass
    elif n == 1:
        add(solver, lits[0])
        events.register("cardinality odd  1 of 1")
    elif n == 2:
        add(solver, Xor(lits[0], lits[1]))
        events.register("cardinality even 1 of 2")
    else:
        """
        It did not help to use special expressions for 2-of-3
        and 2-of-4
        """
        #  rounded-up integer division
        no_of_trues = (n + 1) // 2
        add(solver, z3.Sum(lits) == no_of_trues)
        if n % 2 == 0:
            events.register(f"cardinality even {no_of_trues} of {n}")
        else:
            events.register(f"cardinality odd  {no_of_trues} of {n}")


def configure_z3(cfg: Config) -> None:
    """ Set Z3 parameters according to cfg Config """
    if z3.z3_debug():
        o(f"Debug mode! DebugLevel {get_debug_level()}")
    else:
        """  python -O """
        o("Release mode")

    #  cf.  https://microsoft.github.io/z3guide/programming/Parameters
    if cfg.timeout > 0:
        z3.set_param("timeout", cfg.timeout * 1000)  # milliseconds
    else:
        o("No timeout limit specified")
    z3.set_param("verbose", 0 if get_debug_level() < 4 else 1)
    if cfg.threads > 1:
        z3.set_param("sat.threads", cfg.threads)
    # z3.set_param("sat.phase", "always_false")
    # z3.set_param("sat.random_seed", random.randrange(10000));
    #  keep seed static to have consistent runtimes
    z3.set_param("sat.random_seed", cfg.seed)
    

def define_constraints(solver: z3.Solver, mmDim: MatMultDim, bini: BiniScheme) -> None:
    """
    Constraints are defined as Z3 solver assertions
    
    :param solver: The z3 solver
    :param cfg: The Config object with configuration parameters
    :param mmDim: Problem dimensions
    :param bini: Bini scheme with mod 2 solution
    :return: none
    """
    _ = TimeReporter("Constraint definition")
    
    """  create equations  """
    equations = 0
    o("Defining constraints")
    for a_row, a_col in mmDim.AIndices:
        for b_row, b_col in mmDim.BIndices:
            for c_row, c_col in mmDim.CIndices:
                equations += 1
                triples = []
                for product in mmDim.Products:
                    if is_non_zero_triple(bini, a_row, a_col, b_row, b_col, c_row, c_col, product):
                        """
                        for mod2 1*1*1 triple, we have to find the '+'/'-' signs
                        
                        A "triple" is a product of three variables.
                        To get the resulting sign, we are using Xor()
                        """
                        fg = dyad(a_row, a_col, b_row, b_col, product)
                        d = literal_var("d", c_row, c_col, product)                        
                        triple = Xor(fg, d)
                        triples.append(triple)

                """
                Brent's rule for odd and even equations:
                Odd equations have 1 as sum on the right-hand-side.
                Even equations has 0 as sum.
                """
                odd = (a_row == c_row) and (a_col == b_row) and (b_col == c_col)
                check((len(triples) % 2 == 1) == odd, "inconsistent triples")
                
                seq = f"{idx(a_row, a_col)}{idx(b_row, b_col)}{idx(c_row, c_col)}"
                if len(triples) > 0:
                    s = "odd" if odd else "even"
                    cardinality(solver, triples)
                else:
                    s = "odd" if odd else "even"
                    events.register("cardinality even 0 of 0")
                o4(f"{seq}  {s}  {len(triples)} triples")
                    
    o(f"Equations created: {pretty_num(equations)}")
    eval_literal_statistics(mmDim, bini)
        

def dyad(a_row: int, a_col: int, b_row: int, b_col: int, product: int) -> z3.BoolRef:
    """
    A "dyad" is a product of two variables.
    Here, we are using Xor() to get the product.
    For +1/-1 variables, this calculates the resulting polarity.
    true  = +1
    false = -1
    """
    lit = f"_d{idx(a_row, a_col)}{idx(b_row, b_col)}{product+1:02d}"
    if lit in dyads:
        fg = dyads[lit]
    else:
        f = literal_var("f", a_row, a_col, product)
        g = literal_var("g", b_row, b_col, product)
        fg = Xor(f, g)
        dyads[lit] = fg
    return fg


def eval_literal_statistics(mmDim: MatMultDim, bini: BiniScheme) -> None:
    if get_debug_level() >= 3:
        ref_cnt_digits = len(str(max(ref_counts.values())))
        for fgd in mmDim.FGD:
            for row, col in mmDim.indices_fgd(fgd):
                for product in mmDim.Products:
                    name = mmDim.FGD_names[fgd]
                    lit = literal(name, row, col, product)
                    if lit in ref_counts.keys():
                        cnt = str(ref_counts[lit]).zfill(ref_cnt_digits)
                    else:
                        cnt = str(0).zfill(ref_cnt_digits)
                    events.register(f"{name} coefficient referenced {cnt}x")
        for a_row, a_col in mmDim.AIndices:
            for b_row, b_col in mmDim.BIndices:
                for c_row, c_col in mmDim.CIndices:
                    for product in mmDim.Products:
                        if is_non_zero_triple(bini, a_row, a_col, b_row, b_col, c_row, c_col, product):
                            f = literal("f", a_row, a_col, product)
                            f_cnt = str(ref_counts[f]).zfill(ref_cnt_digits)
                            g = literal("g", b_row, b_col, product)
                            g_cnt = str(ref_counts[g]).zfill(ref_cnt_digits)
                            d = literal("d", c_row, c_col, product)
                            d_cnt = str(ref_counts[d]).zfill(ref_cnt_digits)
                            pattern = f"{f_cnt}_{g_cnt}_{d_cnt}"
                            events.register(f"Reference counts pattern {pattern}")
    
    
def idx(row: int, col: int):
    return f"{row+1}{col+1}"


def is_non_zero_triple(bini: BiniScheme, 
                       a_row: int, a_col: int, 
                       b_row: int, b_col: int, 
                       c_row: int, c_col: int, 
                       product: int):
    """  use 'and'  to benefit from expression short circuiting """
    # o(f"{idx(a_row, a_col)}{idx(b_row, b_col)}{idx(c_row, c_col)}_{product+1}")
    return \
           (bini.a(a_row, a_col, product) != 0) and \
           (bini.b(b_row, b_col, product) != 0) and \
           (bini.c(c_row, c_col, product) != 0) 
    

def literal(name: str, row: int, col: int, product: int):
    lit = f"{name}{idx(row, col)}_{str(product+1).zfill(product_index_digits)}"
    return lit


def literal_var(name: str, row: int, col: int, product: int):
    lit = literal(name, row, col, product)
    if lit not in literals:
        """ create new literal as z3.Bool() variable """
        ll = Bool(lit)
        literals[lit] = ll
        ref_counts[lit] = 1
    else:
        """ re-use existing literal """
        ll = literals[lit]
        ref_counts[lit] = ref_counts[lit] + 1
    return ll


def Not(a: z3.BoolRef):
    """
    Create a Z3 NOT expression
    :param a: Bool variable
    :return: (!a) as Boolean expression
    """
    events.register("Not() expression")
    return z3.Not(a)


def pack_solution(model: z3.Model, mmDim: MatMultDim):
    """
    The packed solution is a dictionary.
    Keys are the literal names.
    Values are the int values +1/-1
    """
    pack_names = ["a", "b", "c"]
    p = {}
    
    if get_debug_level() > 3:
        o("Model")
        o(model)
        o("Literals")
        o(literals)
    
    for fgd in mmDim.FGD:
        for row, col in mmDim.indices_fgd(fgd):
            for k in mmDim.Products:
                lit = literal(mmDim.FGD_names[fgd], row, col, k)
                if lit in literals:
                    var = literals[lit]
                    val = int_value(model, var)
                    """  we use a, b, c names rather than f, g, d 
                         for the packed solution """
                    lit = literal(pack_names[fgd], row, col, k)
                    p[lit] = val if val == 1 else -1    
    return p

    
def solve_and_report(solver, cfg: Config, mmDim: MatMultDim):
    """
    Start solver and write results to Yacas script file

    param: solver: The solver
    param: cfg: the configuration
    param: mmDim: Problem dimensions
    return: return status (0 = ok, 1 = no solution)
    """

    watch = Watch()

    o()
    o(f"{cfg.solver} solver started ...")
    o()

    if get_debug_level() > 3:
        o(f"Assertions: {pretty_num(assertionIdx)}")
        o()

    timeReporter = TimeReporter("Solver run")

    res = solver.check()

    elapsed = timeReporter.elapsed

    del timeReporter

    o()
    if res == z3.sat:
        o("Solution found!")
        m = solver.model()
        if get_debug_level() > 4:
            o("Model")
            o(m)

        timeReporter = TimeReporter("Yacas script file creation")
        solution = pack_solution(m, mmDim)
        if get_debug_level() > 3:
            o("Solution")
            o(solution)
        
        if create_yacas_file(solver, mmDim, solution, str(watch), cfg):
            o("Solution verified. OK!\n")
        else:
            o("Solution found to be invalid!\n")

        del timeReporter

        o()
        o(f"Solution time: {elapsed}")
        o()

        return 0
    else:
        o("No solution found. Sorry!")
        return 1


def validate_environment(python_home: str) -> None:
    """Check if libraries and tools are as expected"""
    # show_environment()

    # check(os.path.isdir(z3_home), f"z3 home directory '{z3_home} not found!")
    check(os.path.isdir(python_home), f"Python home directory '{python_home} not found!")
    # check(os.path.isfile(z3_home + r"\bin\z3.exe"), "z3.exe not found in z3 home directory '{z3Home}'")
    check(os.path.isfile(python_home + r"\python.exe"), f"python.exe not found in python home directory '{python_home}'")

    dll = find_file_in_path("libz3.dll", "$PATH")
    check(dll is not None, "libz3.dll not found on PATH")
    o(f"libz3.dll found as '{dll}'")
    o(f"python: {python_home}")

    show_path = False
    if show_path:
        o("path:")
        for p in os.environ['PATH'].split(';'):
            o(f"  {p}")
        o()

    show_pythonpath = False
    if show_pythonpath:
        o("pythonpath:")
        for p in os.environ['PYTHONPATH'].split(';'):
            o(f"  {p}")
        o()
    o()


def Xor(a: z3.BoolRef, b: z3.BoolRef) -> z3.BoolRef:
    """
    Create a Z3 xor expression
    :param a: Bool variable
    :param b: Bool variable
    :return: (a != b) as Boolean expression
    """
    events.register("Xor2() expression")
    return z3.Xor(a, b)


def main():
    """
    Use main() to avoid global variables
    """
    global product_index_digits
    
    o("[]--------------------------------------------------------------[]")
    o("|                                                                |")
    o("|  akBrentUp  -  Solve Brent's equations for matrix              |")
    o("|                multiplication.                                 |")
    o("|                Input is a mod 2 solution in Bini format.       |")
    o("|                Output is a solution with +1/-1 coefficients.   |")
    o("|                                                                |")
    o("|  Axel Kemper   05-May-2025                                     |")
    o("|                                                                |")
    o("[]--------------------------------------------------------------[]")
    o()

    """  default reaction is finish()  """
    define_ctrl_break_handler()
    
    python_home: str = r"C:\AK\Python\Python313"

    path_add(f"{python_home}\\Lib\\site-packages\\z3\\lib", "PATH")

    """ Check if libraries and tools are as expected """
    validate_environment(python_home)

    """ Get commandline parameters via argparse """

    if (len(sys.argv) == 2) and (sys.argv[1] == "test"):
        test_case();

    cfg = Config()

    o(f"debugLevel {get_debug_level()}")

    timeReporter = TimeReporter("Prepare solving process")

    configure_z3(cfg)

    solver = z3.Solver()

    bini = BiniScheme()
    bini.read(cfg.bini_input_file)
    
    """ Translate problem dimensions into ranges and parameters """
    mmDim = MatMultDim(bini.a_rows, bini.a_cols, bini.b_cols, bini.no_of_products)
    product_index_digits = len(str(mmDim.noOfProducts))
    
    if cfg.lift == "hensel":
        lifter = HenselLifter(mmDim, bini)
        status = lifter.lift()
        
        del timeReporter
    elif cfg.lift == "groebner":
        lifter = GroebnerLifter(mmDim, bini)
        status = lifter.lift()        
    else:
        check(cfg.lift == "direct", \
              f"Invalid lift option '{cfg.lift}'. " + \
               "Supported: direct, hensel, groebner")
        define_constraints(solver, mmDim, bini)
        if cfg.solver != "z3":
            if cfg.solver.startswith("sat_"):
                # solver = \
                #    akSatSolver(solver,
                #                variables,
                #                sat_solver=cfg.solver[4:],
                #                use_akbc=cfg.akbc,
                #                threads=cfg.threads,
                #                debug=get_debug_level() > 3)
                solver = None
                fatal("SAT solving not supported yet!")
            elif cfg.solver == "minizinc":
                solver = \
                    akMiniZincSolver(solver,
                                     variables,
                                     threads=cfg.threads,
                                     timeout_seconds=cfg.timeout)
            elif cfg.solver.startswith("minizinc_"):
                arr = cfg.solver.split("_")
                solver = \
                    akMiniZincSolver(solver,
                                     variables,
                                     threads=cfg.threads,
                                     timeout_seconds=cfg.timeout,
                                     minizinc_backend=arr[1])                
            else:
                solver = \
                    akSmtSolver(solver,
                                variables,
                                cfg.solver,
                                cfg.threads)

        del timeReporter

        status = solve_and_report(solver, cfg, mmDim)
    
    events.report("akBrentUp statistics")

    finish(status)

"""
MAIN program  starts here
"""
if __name__ == "__main__":
    main()
