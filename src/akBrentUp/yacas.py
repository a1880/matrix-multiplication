"""

yacas.py

Routines to create a Yacas script

"""

from ast import MatMult
from z3 import z3
from config import Config
from Events import Events
from MatMultDim import MatMultDim
from PrimeNumbers import PrimeNumbers
import random
from SignFlipper import beautify_literals
from util import (close_output, exists_dir, fatal, get_debug_level, 
                  mkdir, o, o4, open_output, output_file_name, pretty_num, timestamp)

product_index_digits = 2

events = Events()

def apply_solution(a, b, solution, dim: MatMultDim):
    """  calculate products  """
    products = []
    for k in dim.Products:
        aSum = sum_of_coefficient_values("a", k, solution, dim.AIndices, a)
        bSum = sum_of_coefficient_values("b", k, solution, dim.BIndices, b)
        products.append(aSum * bSum)
    c = [[sum_of_product_values(solution, dim, c_row, c_col, products) \
          for c_col in range(dim.cCols)] for c_row in range(dim.cRows)]
    return c

        
def compare_matrix_products(axb, c, dim):
    cnt_err = 0
    for row, col in dim.CIndices:
        if axb[row][col] != c[row][col]:
            cnt_err += 1
    return cnt_err    


def create_prime_matrix(rows, cols, prime_iterator):
    return [[next(prime_iterator) * randomSign() for _ in range(cols)] for _ in range(rows)]


def create_yacas_file(solver, dim: MatMultDim, solution: list, elapsed: str, cfg: Config):
    global product_index_digits
    
    ret_ok = True
    
    if not exists_dir(f"./{dim.toString}"):
      mkdir(f"./{dim.toString}")
    open_output(f"{dim.toString}/s{dim.toString}.{cfg.solver}.yacas.txt")
    print(f"Creating Yacas script file {output_file_name()}")

    o("#")
    o(f"# Yacas script {output_file_name()} created {timestamp()}")
    o("#")
    o(f"# Matrix multiplication method for {dim}")
    o("#")
    o(f"# Intermediate products: {dim.noOfProducts}")
    o("#")
    if __debug__:
        o(f"# Debug mode! DebugLevel {get_debug_level()}")
    else:
        o("# Release mode")
    if cfg.solver == "z3":
        o(f"# Solver: {z3.get_full_version()}")
        threads = z3.get_param("sat.threads")
        o(f"# Parallel threads: {threads}")
    else:
        backend = getattr(solver, "minizinc_backend", "")
        descr = getattr(solver, "minizinc_backend_description", "")
        name = getattr(solver, "minizinc_backend_name", "")
        supports_threads = getattr(solver, "supports_threads", False)
        
        if backend == "":
            o(f"# Solver: {cfg.solver} {solver.version}")
            o(f"# Parallel threads: {cfg.threads}")
        else:
            o(f"# Solver: MiniZinc {name} {solver.version}")
            o(f"#         {descr}")
            if supports_threads:
                o(f"# Parallel threads: {cfg.threads}")
            else:
                o("# Parallel threads: 1  (multi-threading not supported)")                           

    o("#")
    o(f"# Solution time: {elapsed}")
    o("#")

    beautify_literals(solution, dim)
    
    p_len = len(str(dim.noOfProducts))
    product_index_digits = p_len
    
    for product in dim.Products:
        st = f"P{str(product + 1).zfill(p_len)} := "
        st += sum_of_coefficients("a", product, solution, dim, dim.MatF)
        st += " * "
        st += sum_of_coefficients("b", product, solution, dim, dim.MatG)
        st += ";"
        o(st)

    o()

    for p, q in dim.CIndices:
        st = f"c{p + 1}{q + 1} := "
        st += sum_of_products(solution, dim, p, q)
        st += ";"
        o(st)

    o()

    events.report("Operations statistics", "# ")
    
    o()
    
    for p, q in dim.CIndices:
        st = f"Simplify(c{p + 1}{q + 1} - ("
        st += falk_sum(dim.aCols, p, q)
        st += "));"
        o(st)

    o()
    if verify_solution(solution, dim) and try_solution(solution, dim):
        equations = dim.aElements * dim.bElements * dim.cElements
        o("# Algorithm verified OK! " + f"Fulfills all {pretty_num(equations)} Brent's equations")
        o("")
    else:
        ret_ok = False

    o("#")
    o(f"# End of {dim.toString} solution file {output_file_name()}")
    o("#")
    o("")

    print(f"Yacas script file {output_file_name()} created")
    print("")

    close_output()

    return ret_ok


def falk_sum(cols, row, col):
    """return the schoolbook formula for c[row, col]"""
    st = ""
    for c in range(cols):
        st += f" + a{row + 1}{c + 1}*b{c + 1}{col + 1}"

    return st[3:]


def int_value(model, var):
    """retrieve value of Bool variable as 0 or 1"""
    return int(z3.is_true(model[var]))


def literal0(name, row, col):
    """variable name with 1-based row and column"""
    return f"{name}{row + 1}{col + 1}"


def literal(name, row, col, product):
    """variable name with 1-based row and column"""
    return f"{name}{row + 1}{col + 1}_{str(product + 1).zfill(product_index_digits)}"


def multiply_row_col(a, b, row, col, dim):
    """ vector product of row in a[] times col in b[] """
    s = 0
    for k in range(dim.aCols):
        s += a[row][k] * b[k][col]
    return s


def multiply_classic(a, b, dim):
    """  perform classical matrix multiplication  """
    c = [[multiply_row_col(a, b, c_row, c_col, dim) \
          for c_col in range(dim.cCols)] for c_row in range(dim.cRows)]
    if get_debug_level() > 4:
        show_matrix(a, dim.AIndices, "matrix a")
        show_matrix(b, dim.BIndices, "matrix b")
        show_matrix(c, dim.CIndices, "matrix c = a * b")
    return c


def randomSign():
    return 1 if random.randint(0, 100) > 50 else -1


def show_matrix(mat: list[list[int]], indices: list[int, int], purpose: str):
    o(f"# {purpose}")
    o(f"# {len(purpose)*'='}")
    s = ""
    slen = max([len(str(mat[row][col])) for row, col in indices]) + 1
    for row, col in indices:
        if col == 0:
            if s != "":
                o(s)
            s = "# "
        s += f"{str(mat[row][col]).rjust(slen)}"
    o(s)
    o()
             

def sum_of_coefficients(name: str, product: int, solution, mmDim: MatMultDim, fgd: int):
    """
    For Yacas output: an arithmetic sum of matrix elements
    Put in round brackets (..) if more than one summand is present
    This is just the mod2 version!
    """
    soc = ""
    cnt = 0
        
    for row, col in mmDim.indices_fgd(fgd):
        lit = literal(name, row, col, product)
        if lit in solution:
            val = solution[lit]
            if val == 1:
                if soc != "":
                    events.register("add operation")                
                soc += f" + {literal0(name, row, col)}"
                cnt += 1
            elif val == -1:
                if soc != "":
                    events.register("subtract operation")
                else:
                    events.register("multiply by -1 operation")
                soc += f" - {literal0(name, row, col)}"
                cnt += 1
            else:
                fatal("oops!")
            
    if soc.startswith(" + "):
        soc = soc[3:]
    if (cnt > 1) or soc.startswith(" "):
        soc = f"({soc})"

    return soc


def sum_of_coefficient_values(name: str, product: int, solution: list, 
                              indices: list[int, int], mat: list):
    """
    Calculate the sum of coefficients
    """
    soc = 0
        
    for row, col in indices:
        lit = literal(name, row, col, product)
        if lit in solution:
            val = solution[lit]
            soc += val * mat[row][col]

    return soc


def sum_of_products(solution, dim, row, col):
    """
    for Yacas output: a sum of intermediate products
    to compute a [c] matrix element
    """
    sop = ""
    p_len = product_index_digits
    cnt = 0

    for product in dim.Products:
        lit = literal("c", row, col, product)
        if lit in solution:
            val = solution[lit]
            if val == 1:
                cnt += 1
                sign = "+" if cnt > 1 else " "
                if cnt > 1:
                    events.register("add operation")
                sop += f" {sign} P{str(product + 1).zfill(p_len)}"
            elif val == -1:
                cnt += 1
                if cnt > 1:
                    events.register("subtract operation")
                else:
                    events.register("multiply by -1 operation")
                sign = "-"
                sop += f" {sign} P{str(product + 1).zfill(p_len)}"                
        else:
            es = ""
            sop += f"    {es.rjust(p_len)}"

    return sop[1:].rstrip()


def sum_of_product_values(solution, dim, row, col, products):
    """
    a sum of intermediate products
    to compute a [c] matrix element
    """
    sop = 0

    for product in dim.Products:
        lit = literal("c", row, col, product)
        if lit in solution:
            sop += solution[lit] * products[product]

    return sop


def try_solution(solution, dim):
    o4("# Experimental validation of matrix multiplication algorithm")
    prime_iterator = PrimeNumbers(100)
    o4(f"# Create two matrices {dim.aRows}x{dim.aCols} {dim.bRows}x{dim.bCols} of prime numbers")
    a = create_prime_matrix(dim.aRows, dim.aCols, prime_iterator)
    b = create_prime_matrix(dim.bRows, dim.bCols, prime_iterator)
    o4("# Apply solution to multiply matrices")
    axb = apply_solution(a, b, solution, dim)
    o4("# Multiply the same matrices in the classic schoolbook way")
    c = multiply_classic(a, b, dim)
    
    o4("# Compare resulting product matrices and count differences")
    cnt_err = compare_matrix_products(axb, c, dim)
    
    if cnt_err == 0:
        o4("# No differences found! Solution validated!")
        o("# Test multiplication was correct. OK!")
    else:
        o(f"# Differences found: {cnt_err}")
        o("# Solution is invalid!")
        
    return cnt_err == 0


def verify_solution(solution, dim):
    """check if Brent's equations are actually properly fulfilled"""
    cnt_ok = 0
    cnt_err = 0
    for i, j in dim.AIndices:
        for m, n in dim.BIndices:
            for p, q in dim.CIndices:
                delta = 1 if (i == p) and (j == m) and (n == q) else 0
                eq_sum = 0
                for product in dim.Products:
                    f = solution.get(literal("a", i, j, product), 0)
                    g = solution.get(literal("b", m, n, product), 0)
                    d = solution.get(literal("c", p, q, product), 0)
                    eq_sum += f * g * d

                if delta == eq_sum:
                    cnt_ok += 1
                else:
                    cnt_err += 1

    if cnt_err > 0:
        o("#")
        o("# Algorithm does not fulfill all of Brent's equations:")
        o(f"# Fulfilled:     {pretty_num(cnt_ok)}")
        o(f"# Not fulfilled: {pretty_num(cnt_err)}")
    return cnt_err == 0
