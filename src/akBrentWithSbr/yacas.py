"""

yacas.py

Routines to create a Yacas script

"""

from z3 import z3

from util import (close_output, debugLevel, o, open_output, output_file_name, timestamp)


def create_yacas_file(solver, model, dim, u, v, w, elapsed, cfg):
    ret_ok = True
    open_output(f"s{dim.toString}.{cfg.solver}.yacas.txt")
    print(f"Creating Yacas script file {output_file_name()}")

    o("#")
    o(f"# Yacas script {output_file_name()} created {timestamp()}")
    o("#")
    o(f"# Matrix multiplication method for {dim}")
    o("#")
    o(f"# Intermediate products: {dim.noOfProducts}")
    o("#")
    if __debug__:
        o(f"# Debug mode! DebugLevel {debugLevel}")
    else:
        o("# Release mode")
    if cfg.solver == "z3":
        o(f"# Solver: {z3.get_full_version()}")
        threads = z3.get_param("sat.threads")
        o(f"# Parallel threads: {threads}")
    else:
        o(f"# Solver: {cfg.solver} {solver.version}")
        o(f"# Parallel threads: {cfg.threads}")

    o("#")
    o(f"# Solution time: {elapsed}")
    o("#")

    p_len = len(str(dim.noOfProducts))
    for product in dim.Products:
        st = f"P{str(product + 1).zfill(p_len)} := "
        st += sum_of_coefficients(model, "a", product, u, dim.aRows, dim.aCols)
        st += " * "
        st += sum_of_coefficients(model, "b", product, v, dim.bRows, dim.bCols)
        st += ";"
        o(st)

    o("")

    for p, q in indices(dim.cRows, dim.cCols):
        st = f"c{p + 1}{q + 1} := "
        st += sum_of_products(model, dim, p, q, w)
        st += ";"
        o(st)

    o("")

    for p, q in indices(dim.cRows, dim.cCols):
        st = f"Simplify(c{p + 1}{q + 1} - ("
        st += falk_sum(dim.aCols, p, q)
        st += "));"
        o(st)

    o("")
    if verify_solution(model, dim, u, v, w) and try_solution(model, dim, u, v, w):
        equations = dim.aElements * dim.bElements * dim.cElements
        o("# Algorithm verified OK! " + f"Fulfills all {equations} Brent's equations")
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


def indices(rows, cols):
    """return 0..rows-1, 0..cols-1  as iterable of index tuples"""
    return [(row, col) for row in range(rows) for col in range(cols)]


def int_value(model, var):
    """retrieve value of Bool variable as 0 or 1"""
    return int(z3.is_true(model[var]))


def literal0(name, row, col):
    """variable name with 1-based row and column"""
    return f"{name}{row + 1}{col + 1}"


def sum_of_coefficients(model, name, product, a, rows, cols):
    """
    For Yacas output: an arithmetic sum of matrix elements
    Put in round brackets (..) if more than one summand is present
    This is just the mod2 version!
    """
    soc = ""
    cnt = 0

    for row, col in indices(rows, cols):
        idx = row * cols + col
        var = a[idx, product]
        val = int_value(model, var)
        if val == 1:
            soc += f" + {literal0(name, row, col)}"
            cnt += 1

    if soc.startswith(" + "):
        soc = soc[3:]
    if (cnt > 1) or soc.startswith(" "):
        soc = f"({soc})"

    return soc


def sum_of_products(model, dim, row, col, w):
    """
    for Yacas output: a sum of intermediate products
    to compute a [c] matrix element
    This is just the mod2 version!
    """
    sop = ""
    p_len = len(str(dim.noOfProducts))
    cnt = 0

    for product in dim.Products:
        idx = row * dim.cCols + col
        var = w[idx, product]
        val = int_value(model, var)
        if val == 1:
            cnt += 1
            sign = "+" if cnt > 1 else " "
            sop += f" {sign} P{str(product + 1).zfill(p_len)}"
        else:
            es = ""
            sop += f"    {es.rjust(p_len)}"

    return sop[2:].rstrip()


def try_solution(_model, dim, u, v, w):
    """cannot try mod2 solution"""
    cnt_err = 0
    return cnt_err == 0


def verify_solution(model, dim, u, v, w):
    """check if Brent's equations are actually properly fulfilled"""
    cnt_ok = 0
    cnt_err = 0
    for i, j in indices(dim.aRows, dim.aCols):
        for m, n in indices(dim.bRows, dim.bCols):
            for p, q in indices(dim.cRows, dim.cCols):
                delta = 1 if (i == p) and (j == m) and (n == q) else 0
                eq_sum = 0
                for product in dim.Products:
                    f = int_value(model, u[i * dim.aCols + j, product])
                    g = int_value(model, v[m * dim.bCols + n, product])
                    d = int_value(model, w[p * dim.cCols + q, product])
                    eq_sum += f * g * d

                if delta == (eq_sum % 2):
                    cnt_ok += 1
                else:
                    cnt_err += 1

    if cnt_err > 0:
        o("#")
        o("# Algorithm does not fulfill all of Brent's equations mod2:")
        o(f"# Fulfilled:     {cnt_ok}")
        o(f"# Not fulfilled: {cnt_err}")
    return cnt_err == 0
