import os
import random

from z3 import (z3, Bools, And)

from Array2D import Array2D
from Events import Events
from MatMultDim import MatMultDim
from akSatSolver import akSatSolver
from akSmtSolver import akSmtSolver
from config import Config
from util import (
    check,
    debugLevel,
    define_ctrl_break_handler,
    fatal,
    find_file_in_path,
    finish,
    o,
    path_add,
    set_debug_level,
    show_environment,
    TimeReporter,
    Watch
)
from yacas import create_yacas_file

events = Events()

assertionIdx = 0


def add(solver, predicates):
    """add assertion to solver"""
    global assertionIdx
    try:
        events.register("add assertion")
        solver.add(predicates)
        if hasattr(predicates, "__len__"):
            assertionIdx += len(predicates)
        else:
            assertionIdx += 1
    except z3.Z3Exception as ex:
        print(f"Z3Exception: {ex}")
        print(predicates)
        fatal("Z3 error")


def And(a, b):
    events.register("And")
    return z3.And(a, b)


def And3(a, b, c):
    events.register("And3")
    return z3.And(a, b, c)


#  To keep track of all variables created
#  These are required for akSmtSolver.model()
variables = []


def Bool(name):
    events.register("Bool")
    _v = z3.Bool(name)
    variables.append(_v)
    return z3.Bool(name)


def configure_z3(cfg):
    """Set Z3 parameters according to cfg Config"""
    if z3.z3_debug():
        print(f"Debug mode! DebugLevel {debugLevel}")
    else:
        print("Release mode")

    #  cf.  https://microsoft.github.io/z3guide/programming/Parameters
    if cfg.timeout > 0:
        z3.set_param("timeout", cfg.timeout * 1000)  # milliseconds
    else:
        print("No timeout limit specified")
    z3.set_param("verbose", 0 if cfg.debugLevel < 4 else 1)
    if cfg.threads > 1:
        z3.set_param("sat.threads", cfg.threads)
    z3.set_param("sat.phase", "always_false")
    # z3.set_param("sat.random_seed", random.randrange(10000));
    #  keep seed static to have consistent runtimes
    z3.set_param("sat.random_seed", cfg.seed)


def create2d(name, indices, products):
    """construct 2D array of z3 Bool variables"""
    arr = [
        [Bool(literal(name, row, col, product)) for product in products]
        for row, col in indices
    ]
    events.register("Bool", times=len(products) * len(indices))
    return Array2D.from_array(arr)


def define_constraints(solver, cfg, mmDim, u, v, w, U, V, W):
    """
    Constraints are defined as Z3 solver assertions
    
    :param solver: The z3 solver
    :param cfg: The Config object with configuration parameters
    :param mmDim: MatMultDim problem dimensions
    :param u: Array of [A] coefficient decision variables
    :param v: Array of [B] coefficient decision variables
    :param w: Array of [C] coefficient decision variables
    :param U: index range to enumerate array u
    :param V: index range to enumerate array v
    :param W: index range to enumerate array w
    :return: none
    """
    timeReporter = TimeReporter("Constraint definition")

    """  create equations  """
    equations = 0
    even_cnt = 0
    odd_cnt = 0
    for ra, ca in mmDim.AIndices:
        i = ra * mmDim.aCols + ca
        for rb, cb in mmDim.BIndices:
            j = rb * mmDim.bCols + cb
            # dyads = [And(u[i, r], v[j, r]) for r in mmDim.Products]
            for rc, cc in mmDim.CIndices:
                delta = (ra == rc) and (ca == rb) and (cb == cc)
                k = rc * mmDim.cCols + cc
                triples = [And3(u[i, r], v[j, r], w[k, r]) for r in mmDim.Products]
                # triples = [And(dyads[r], w[k, r]) for r in mmDim.Products]
                if delta:
                    add(solver, odd(triples))
                    odd_cnt += 1
                else:
                    add(solver, even(triples))
                    even_cnt += 1
                equations += 1

    o(f"Equations created: {equations} ({even_cnt} even, {odd_cnt} odd)")
    expected = mmDim.aElements * mmDim.bElements * mmDim.cElements
    check(
        equations == expected,
        f"Inconsistent number of equations {equations} != {expected}",
    )

    """
    Fix odd one odd triple to 1*1*1 for the first products
    """

    #  the first cfg.fix triple tuples of a randomly shuffled list
    odd_ra_ca_cb = shuffle_list(
        [(ra, ca, cb) for (ra, ca) in mmDim.AIndices for cb in range(mmDim.bCols)]
    )[: cfg.fix]

    for r, t in enumerate(odd_ra_ca_cb):
        ra, ca, cb = t
        #  F[ra, ca, r] = 1
        add(solver, u[ra * mmDim.aCols + ca, r])
        #  G[ca, cb, r] = 1
        add(solver, v[ca * mmDim.bCols + cb, r])
        #  D[ra, cb, r] = 1
        add(solver, w[ra * mmDim.cCols + cb, r])

    """
    %  Break product permutation symmetry
    constraint forall(r in Products where r > 1) (
      lex_greater([u[i, r-1] | i in U] ++ [v[j, r-1] | j in V],
              [u[i,   r] | i in U] ++ [v[j,   r] | j in V])
    );
    """

    useGreater = False
    if useGreater:
        print("Constraint: sort u+v vectors lexicographically")
        for r in mmDim.Products:
            if r > 0:
                lits1 = [u[i, r - 1] for i in U] + [v[j, r - 1] for j in V]
                lits2 = [u[i, r] for i in U] + [v[j, r] for j in V]
                add(solver, greater(lits1, lits2))

    """
    %  Each element of [C] has to comprise atleast ACols summands
    constraint forall(k in W) (
      ARows <= count([w[k, r] | r in Products])
    );
    """

    useAtleast = False
    if useAtleast:
        for k in W:
            add(solver, z3.AtLeast(*[w[k, r] for r in mmDim.Products], mmDim.aRows))

    """
    %  Each pair of [C] elements has to be different in atleast 2 summands
    constraint forall(k1 in W, k2 in W where k2 > k1) (
      2 <= count([w[k1, r] != w[k2, r] | r in Products])
    );
    """

    useAtleast2 = False
    if useAtleast2:
        #  slow solution
        for k1 in W:
            for k2 in W:
                if k2 > k1:
                    add(
                        solver,
                        z3.AtLeast(
                            *[Xor(w[k1, r], w[k2, r]) for r in mmDim.Products], 2
                        ),
                    )

    """
    %  Each element of [A] has to be present in atlast one product
    constraint forall(i in U) (
      exists([u[i, r] | r in Products])
    );
    """
    for i in U:
        exists(solver, [u[i, r] for r in mmDim.Products])

    """
    %  Each element of [B] has to be present in atlast one product
    constraint forall(j in V) (
      exists([v[j, r] | r in Products])
    );
    """
    for j in V:
        exists(solver, [v[j, r] for r in mmDim.Products])

    """
    %  Each product of elements [A]*[B] has to be present in one products
    %  constraint forall(i in U, j in V where ValidOperands[i, j]) (
    %    exists([ u[i, r] /\\ v[j, r] | r in Products])
    %    #  added extra backslash to avoid warning
    %  );
    """

    """2D lookup to find operands which occur in matrix multiplication"""
    ValidOperands = mmDim.validOperands
    for i in U:
        for j in V:
            if ValidOperands[i, j]:
                exists(solver, [And(u[i, r], v[j, r]) for r in mmDim.Products])

    """
    %  Delimit the number of terms per product
    %  constraint forall(r in Products) (
    %    MaxTermsPerProduct >=
    %        (count([u[i, r] | i in U]) + count([v[j, r] | j in V]))
    %  );
    """

    """
    %  Delimit the number of summands pro product
    %  constraint forall(k in W) (
    %    MaxProductsPerSum >= count([w[k, r] | r in Products])
    %  );
    """


def define_variables(mmDim):
    """
    Create array index ranges and define decision variables

    param: mmDim: MatMultDim problem dimensions
    return: none
    """

    """Dimensions of decision variable arrays u, v, w"""
    U = range(mmDim.aRows * mmDim.aCols)
    V = range(mmDim.bRows * mmDim.bCols)
    W = range(mmDim.cRows * mmDim.cCols)

    """
    Create decision variable arrays
    We are using "f", "g" and "d" as variable name prefixes
    to recognize variables during debugging
    """
    u = create2d("f", mmDim.AIndices, mmDim.Products)
    v = create2d("g", mmDim.BIndices, mmDim.Products)
    w = create2d("d", mmDim.CIndices, mmDim.Products)

    return u, v, w, U, V, W


def even(lits):
    """
    parity function
    break up long input lists in two smaller lists
    
    param: lits: list of Boolean literal variables
    return: none
    """
    ret = None
    length = len(lits)
    check(length > 0, "Even() needs argument(s)")
    events.register(f"even{length}")
    if length == 1:
        ret = Not(lits[0])
    elif length == 2:
        ret = Xnor(lits[0], lits[1])
    elif length == 3:
        ret = Xnor(lits[0], Xor(lits[1], lits[2]))
    elif length == 4:
        ret = Xnor(Xor(lits[0], lits[1]), Xor(lits[2], lits[3]))
    else:
        aux = freshBool()

        cut_len = 3
        ret = And(even(lits[:cut_len] + [aux]), even([aux] + lits[cut_len:]))
    return ret


def exists(solver, predicates):
    """syntactic sugar to illustrate that one or more predicates have to hold true"""
    add(solver, Or1(predicates))


freshBoolIdx = 0


def freshBool():
    """create a z3 Bool variable"""
    global freshBoolIdx
    events.register(f"fresh Bool")
    freshBoolIdx = freshBoolIdx + 1
    #  add a '_' to exclude this variable from
    #  the translation table in the cNF/Dimacs file
    return Bool(f"_fb{freshBoolIdx}")


def greater(lits1, lits2):
    """
    Create a Boolean expression which is true
    iff literals "lits1" are greater than literals "lits2"
    
    param: lits1: first list of Bool literal variables
    param: lits2: second list of Bool literal variables
    return: none
    """
    length = len(lits1)

    check(length == len(lits2), "Compare lists have to have matching lengths")

    if length == 1:
        gt = And(lits1[0], Not(lits2[0]))
    else:
        msb1 = lits1[0]
        msb2n = Not(lits2[0])
        msb_greater = And(msb1, msb2n)
        #
        #  msb1   msb2   msb_greater   msb_eq
        #   0      0        0             1
        #   0      1        0             0
        #   1      0        1             0 (don't care!)
        #   1      1        0             1
        #
        #  The don't care allows a simplification of msb_eq:
        msb_eq = Or(msb1, msb2n)
        rest_greater = greater(lits1[1:], lits2[1:])
        gt = Or(msb_greater, And(msb_eq, rest_greater))

    return gt


def literal(name, row, col, product):
    """variable name with 1-based row and column and trailing product char"""
    return f"{name}{row + 1}{col + 1}{productChar(product)}"


def Not(a):
    events.register("Not")
    return z3.Not(a)


def odd(lits):
    """
    parity function
    break up long input lists in two smaller lists

    param: lits: list of Bool literal variables
    return: none
    """
    ret = None
    length = len(lits)
    check(length > 0, "Odd needs argument(s)")
    events.register(f"odd{length}")
    if length == 1:
        ret = lits[0]
    elif length == 2:
        ret = Xor(lits[0], lits[1])
    elif length == 3:
        ret = Xor(lits[0], Xor(lits[1], lits[2]))
    elif length == 4:
        """this symmetric form is much faster than the chained forms"""
        ret = Xor(Xor(lits[0], lits[1]), Xor(lits[2], lits[3]))
    else:
        aux = freshBool()

        cut_len = 3
        ret = And(odd(lits[:cut_len] + [aux]), odd([Not(aux)] + lits[cut_len:]))
    return ret


def Or1(a):
    events.register("Or1")
    return z3.Or(a)


def Or(a, b):
    events.register("Or")
    return z3.Or(a, b)


def productChar(k):
    """translate product number to product char"""
    return "abcdefghijklmnopqrstuvwxyz"[k]


def shuffle_list(lst):
    """Shuffle a list using the Fisher-Yates algorithm"""
    for i in range(len(lst) - 1, 0, -1):
        j = random.randint(0, i)
        lst[i], lst[j] = lst[j], lst[i]
    return lst


def solve_and_report(solver, mmDim, u, v, w, cfg):
    """
    Start z3 or SMT2 solver and write results to Yacas script file

    param: solver: The z3 or SMT2 solver
    param: mmDim: MatMultDim problem dimensions
    param: u: Array2D of [A] coefficient variables
    param: v: Array2D of [B] coefficient variables
    param: w: Array2D of [C] coefficient variables
    param: cfg: the configuration 
    return: return status (0 = ok, 1 = no solution)
    """

    watch = Watch()

    print()
    print(f"{cfg.solver} solver started ...")
    print()

    if debugLevel > 0:
        print(f"Assertions: {assertionIdx}")
        print()

    timeReporter = TimeReporter("Solver run")

    res = solver.check()

    elapsed = timeReporter.elapsed

    del timeReporter

    print()
    if res == z3.sat:
        print("Solution found!")
        m = solver.model()
        # print(m)

        timeReporter = TimeReporter("Yacas script file creation")

        if create_yacas_file(solver, m, mmDim, u, v, w, str(watch), cfg):
            print("Solution verified. OK!\n")
        else:
            print("Solution found to be invalid!\n")

        del timeReporter

        print()
        print(f"Solution time: {elapsed}")
        print()

        return 0
    else:
        print("No solution found. Sorry!")
        return 1


def validate_environment(z3_home, python_home):
    """Check if libraries and tools are as expected"""
    # show_environment()

    check(os.path.isdir(z3_home), f"z3 home directory '{z3_home} not found!")
    check(os.path.isdir(python_home), f"Python home directory '{python_home} not found!")
    check(os.path.isfile(z3_home + r"\bin\z3.exe"), "z3.exe not found in z3 home directory '{z3Home}'")
    check(os.path.isfile(python_home + r"\python.exe"), f"python.exe not found in python home directory '{python_home}'")

    dll = find_file_in_path("libz3.dll", "$PATH")
    check(dll is not None, "libz3.dll not found on PATH")
    print(f"libz3.dll found as '{dll}'")
    print(f"python: {python_home}")

    show_path = False
    if show_path:
        print(f"path:")
        for p in os.environ['PATH'].split(';'):
            print(f"  {p}")
        print()

    show_pythonpath = False
    if show_pythonpath:
        print(f"pythonpath:")
        for p in os.environ['PYTHONPATH'].split(';'):
            print(f"  {p}")
        print()


def Xor(a, b):
    """
    Create a Z3 xor expression
    :param a: Bool variable
    :param b: Bool variable
    :return: (a != b) as Boolean expression
    """
    events.register("Xor")
    return z3.Xor(a, b)


def Xnor(a, b):
    """
    Create a Z3 Xnor expression.
    This is a stripped-down version of Xor(a,b) from the z3py API
    
    param: a: Bool variable
    param: b: Bool variable
    return: (a == b) as Boolean expression
    """
    ctx = a.ctx
    events.register("Xnor")
    return z3.BoolRef(z3.Z3_mk_eq(ctx.ref(), a.as_ast(), b.as_ast()), ctx)


def main():
    """
    Use main() to avoid global variables
    """
    o("[]--------------------------------------------------------------[]")
    o("|                                                                |")
    o("|  akBrentWithSbr  -  Solve Brent's equations for matrix         |")
    o("|                     multiplication with symmetry breaking      |")
    o("|                                                                |")
    o("|  Axel Kemper  24-Mar-2025                                      |")
    o("|                                                                |")
    o("[]--------------------------------------------------------------[]")
    o()

    #  default reaction is finish()
    define_ctrl_break_handler()

    z3_home = r"K:\AK\Tools\z3-4.14.1-x64-win"
    python_home = r"K:\AK\Tools\Python313"

    path_add(f"{z3_home}\\bin", "PATH")
    path_add(f"{z3_home}\\bin\\python", "PYTHONPATH")

    """Check if libraries and tools are as expected"""
    validate_environment(z3_home, python_home)

    """Get commandline parameters via argparse"""

    cfg = Config()

    o(f"debugLevel {cfg.debugLevel}")

    watch = Watch()
    timeReporter = TimeReporter("Prepare solving process")

    """Translate problem dimensions into ranges and parameters"""
    mmDim = MatMultDim.from_problem(cfg.problem)

    configure_z3(cfg)

    """Decision variables are kept in three 2D arrays
       with suitable ranges to enumerate them"""
    u, v, w, U, V, W = define_variables(mmDim)

    solver = z3.Solver()

    define_constraints(solver, cfg, mmDim, u, v, w, U, V, W)

    if cfg.solver != "z3":
        if cfg.solver.startswith("sat_"):
            solver = \
                akSatSolver(solver,
                            variables,
                            sat_solver=cfg.solver[4:],
                            use_akbc=cfg.akbc,
                            threads=cfg.threads,
                            debug=cfg.debugLevel > 3)
        else:
            solver = \
                akSmtSolver(solver,
                            variables,
                            cfg.solver)

    del timeReporter

    status = solve_and_report(solver, mmDim, u, v, w, cfg)

    events.report("Event statistics")

    finish(status)


if __name__ == "__main__":
    main()
