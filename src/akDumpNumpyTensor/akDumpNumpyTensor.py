
"""
akDumpNumpyTensor.py  -  Tool to write a NumPy tensor in a file in matrix multiplication
                         tensor format.
                         The source data was provided by DeepMind AlphaEvolve

@techreport{alphaevolve,
      author={Novikov, Alexander and Vu, Ngân and Eisenberger, Marvin and Dupont, Emilien and Huang, Po-Sen and Wagner, Adam Zsolt and Shirobokov, Sergey and Kozlovskii, Borislav and Ruiz, Francisco J. R. and Mehrabian, Abbas and Kumar, M. Pawan and See, Abigail and Chaudhuri, Swarat and Holland, George and Davies, Alex and Nowozin, Sebastian and Kohli, Pushmeet and Balog, Matej},
      title={Alpha{E}volve: A coding agent for scientific and algorithmic discovery},
      year={2025},
      institution={{Google DeepMind}},
      month={05},
      url={https://storage.googleapis.com/deepmind-media/DeepMind.com/Blog/alphaevolve-a-gemini-powered-coding-agent-for-designing-advanced-algorithms/AlphaEvolve.pdf},
}

Axel Kemper  16-May-2025

"""

import datetime
from MatMultTensor import MatMultTensor
import numpy as np

# Source:
# AlphaEvolve: A coding agent for scientific and algorithmic discovery
# https://storage.googleapis.com/deepmind-media/DeepMind.com/Blog/alphaevolve-a-gemini-powered-coding-agent-for-designing-advanced-algorithms/AlphaEvolve.pdf

""" output file """
wrf = None
wrf_line_count = 0

""" central tensor object """
mmt: MatMultTensor = None


def comment(s: str = ""):
    w(f"# {s}")


def get_complex_term_verbatim(fgd: int, product: int) -> str:
    s = ""    
    eps = 0.00001
    name = mmt.ABC_names[fgd]

    assert mmt.data_type in { np.complex64,  np.complex128 }, "oops!"

    for row, col in mmt.indices_fgd(fgd):
        v = mmt.data(fgd, row, col, product)

        if abs(v) > eps:
            lit = literal(name, row, col)
            sv = str(v)
            s += f" + {sv}*{lit}"

    return f"({s})"


def get_complex_term(fgd: int, product: int) -> str:
    s = ""    
    eps = 0.00001
    name = mmt.ABC_names[fgd]

    assert mmt.data_type in { np.complex64, np.complex128 }, "oops!"

    for row, col in mmt.indices_fgd(fgd):
        v = mmt.data(fgd, row, col, product)

        if abs(v) > eps:
            lit = literal(name, row, col)
            if v.real < 0:
                sv = "-" + str(-v).replace("-0j", "")
            else:
                sv = str(v).replace("+0j", "")

            if "j" not in sv:
                sv = sv.replace("(", "").replace(")", "")

            if s == "":
                s = f"{sv}*{lit}"
            elif sv.startswith("-"):
                s += f" - {sv[1:]}*{lit}"
            else:
                s += f" + {sv}*{lit}"

    if s == "":
        return "(verbatim: " + get_complex_term_verbatim(fgd, product) + ")"

    return f"({s})"


def get_complex_term_mod2(fgd, product: int) -> str:
    s = ""    
    eps = 0.00001
    name = mmt.ABC_names[fgd]

    assert mmt.data_type in { np.complex64, np.complex128 }, "oops!"

    for row, col in mmt.indices_fgd(fgd):
        v = mmt.data(fgd, row, col, product)

        if abs(v) > eps:
            lit = literal(name, row, col)
            v = 1
            if s == "":
                s = f"{lit}"
            else:
                s += f" + {lit}"

    if s == "":
        return "(verbatim: " + get_complex_term_verbatim(fgd, product) + ")"

    return f"({s})"


def s_product(s: str) -> str:
    if s.startswith("1*"):
        return s[2:]
    if s.startswith("1.0*"):
        return s[4:]
    if s.startswith("-1*"):
        return f"- {s[3:]}"
    if s.startswith("-1.0*"):
        return f"- {s[5:]}"
    return s


def get_float_term(fgd, product: int) -> str:
    s = ""    
    name = mmt.ABC_names[fgd]

    for row, col in mmt.indices_fgd(fgd):
        v = mmt.data(fgd, row, col, product)

        if v != 0:
            lit = literal(name, row, col)

            if s == "":
                s = s_product(f"{v}*{lit}")                        
            elif v < 0:
                s = f"{s} - {s_product(f"{abs(v)}*{lit}")}"
            else:
                s = f"{s} + {s_product(f"{v}*{lit}")}"

    return f"({s})"


def get_term(fgd, product: int) -> str:
    s = ""    

    if mmt.data_type in { np.complex64, np.complex128 }:
        return get_complex_term(fgd, product)

    if mmt.data_type == np.float32:
        return get_float_term(fgd, product)

    name = mmt.ABC_names[fgd]
    for row, col in mmt.indices_fgd(fgd):
        v = mmt.data(fgd, row, col, product)

        if v != 0:
            v = int(v)
            lit = literal(name, row, col)
            if s == "":
                if v > 0:
                    if v == 1:
                        s = lit
                    else:
                        s = f"{v}*{lit}"
                else:
                    if v == -1:
                        s = f"- {lit}"
                    else:
                        s = f"- {abs(v)}*{lit}]"
            elif v > 0: 
                if v == 1:
                    s += f" + {lit}"
                else:
                    s += f" + {v}*{lit}"
            elif v < 0:
                if v == -1:
                    s += f" - {lit}"
                else:
                    s += f" - {abs(v)}*{lit}"
            else:
                assert False, f"Strange value! {v}"

    return f"({s})"


def get_term_mod2(fgd: int, product: int) -> str:

    if mmt.data_type in { np.complex64, np.complex128 }:
        return get_complex_term_mod2(fgd, product)

    s = ""    
    name = mmt.ABC_names[fgd]
    for row, col in mmt.indices_fgd(fgd):
        v = mmt.data(fgd, row, col, product)

        if v != 0:
            v = 1
            lit = literal(name, row, col)
            if s == "":
                s = lit
            else: 
                s += f" + {lit}"

    return f"({s})"


def literal(name: str, row: int, col: int) -> str:
    if name != "c":
        return f"{name}{row+1}{col+1}"
    else:
        return f"{name}{col+1}{row+1}"


def o(s: str = ""):
    print(s)


def pretty_3_num(i):
    if i < 0:
        return f"-{pretty_3_num(-i)}"
    if i < 1000:
        return str(i)
    return f"{pretty_3_num(i // 1000)},{str(i % 1000).zfill(3)}"


def transpose_array(a: np.array, rows: int, cols: int, products: int) -> None:
    for row in range(rows):
        for col in range(cols):
            if row != col:
                ic = row*cols + col
                ict = col*rows + row
                
                for k in range(products):
                    tmp = a[ic, k]
                    a[ic, k] = a[ict, k]
                    a[ict, k] = tmp


def validate_algorithm():
    """ check if Brent Equations are fulfilled in Z """
    err_cnt = 0
    ok_cnt = 0
    eqn = 0
    for a_row, a_col in mmt.AIndices:
        for b_row, b_col in mmt.BIndices:
            for c_row, c_col in mmt.CIndices:
                sum = 0
                for k in mmt.Products:
                    p = mmt.a(a_row, a_col, k) * mmt.b(b_row, b_col, k) * mmt.c(c_row, c_col, k)
                    sum += p
                odd = 1 if (a_row == c_row) and (a_col == b_row) and (b_col == c_col) else 0
                if odd != sum:
                    err_cnt += 1
                    o(f"Equation {eqn}: sum {sum} != {odd}")
                else:
                    ok_cnt += 1
                eqn += 1

    return err_cnt, ok_cnt


def validate_algorithm_mod2():
    """ check if Brent Equations are fulfilled """
    err_cnt = 0
    ok_cnt = 0
    eqn = 0
    eps = 0.001

    is_complex = (mmt.data_type in { np.complex64, np.complex128 })

    if is_complex:
        common_factor = 1
        is_float = False
    else:
        is_float = (mmt.data_type == np.float32)

    for a_row, a_col in mmt.AIndices:
        for b_row, b_col in mmt.BIndices:
            for c_row, c_col in mmt.CIndices:
                sum = 0
                s = f"{a_row+1}{a_col+1}{b_row+1}{b_col+1}{c_col+1}{c_row+1}: "
                for k in mmt.Products:
                    if is_complex:
                        f = common_factor * mmt.a(a_row, a_col, k).real
                        g = common_factor * mmt.b(b_row, b_col, k).real
                        d = common_factor * mmt.c(c_row, c_col, k).real
                    elif is_float:
                        f = int(mmt.a(a_row, a_col, k)) % 2
                        g = int(mmt.b(b_row, b_col, k)) % 2
                        d = int(mmt.c(c_row, c_col, k)) % 2
                    else:
                        f = int(mmt.a(a_row, a_col, k)) % 2
                        g = int(mmt.b(b_row, b_col, k)) % 2
                        d = int(mmt.c(c_row, c_col, k)) % 2
                    p = f * g * d
                    if abs(p) > eps:
                        sum += p
                        s += f" + {p}"
                odd = 1 if (a_row == c_row) and (a_col == b_row) and (b_col == c_col) else 0
                sum = int(sum)
                if odd != (sum % 2):
                    err_cnt += 1
                    o(f"Equation {eqn}: sum {sum} % 2 != {odd}")
                    o(s)
                else:
                    ok_cnt += 1
                eqn += 1

    return err_cnt, ok_cnt


def w(s: str = ""):
    """ write a line to the tensor output file """
    global wrf_line_count
    wrf.write(f"{s}\n")
    wrf_line_count += 1
    print(s)


def write_tensor_file(err_cnt: int, ok_cnt: int) -> None:    
    global wrf   #  output file for w() and comment()

    file_name = f"s{mmt.problem}.tensor.txt"

    data_type = mmt.data_type

    with open(file_name, "wt", encoding="ascii") as f:
        wrf = f
        now = datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
        comment(f"Matrix multiplication scheme in tensor form: '{file_name}'")
        comment()
        comment(f"Created: {now}")
        comment()
        comment("[c] is processed in transposed form")
        comment()
        if data_type in { np.complex64, np.complex128 }:
            comment("Coefficients are complex numbers")
        c = mmt.get_coefficients()
        s = ", ".join([str(x) for x in c])
        comment(f"Coefficients: {{{s}}}")
        comment()

        if err_cnt != 0:
            comment()
            comment("Error(s) detected:")
            comment(f"Unfulfilled Brent Equations: {pretty_3_num(err_cnt)}")
        else:
            comment("Algorithm is valid!")
        comment(f"Fulfilled Brent Equations: {pretty_3_num(ok_cnt)}")
        comment()
        w()

        for product in mmt.Products:
            ta = get_term(mmt.F, product)
            tb = get_term(mmt.G, product)
            tc = get_term(mmt.D, product)
            o(f"{product+1}:")
            w(f"{ta}*{tb}*{tc}")

        w()
        comment(f"end of file '{file_name}', lines: {wrf_line_count + 2}")
        w()


def write_tensor_file_mod2(err_cnt: int, ok_cnt: int) -> None:    
    global wrf   #  output file for w() and comment()

    file_name = f"s{mmt.problem}.tensor.mod2.txt"
    data_type = mmt.data_type
    with open(file_name, "wt", encoding="ascii") as f:
        wrf = f
        now = datetime.datetime.now().strftime("%Y-%m-%d %H:%M")
        comment(f"Matrix multiplication scheme in tensor form: '{file_name}'")
        comment()
        comment(f"Created: {now}")
        comment("Modus: modulo 2")
        comment()
        comment("[c] is processed in transposed form")
        if data_type in { np.complex64, np.complex128 }:
            comment("Coefficients are complex numbers")
        if err_cnt != 0:
            comment()
            comment("Error(s) detected:")
            comment(f"Unfulfilled Brent Equations: {pretty_3_num(err_cnt)}")
        else:
            comment("Algorithm is valid!")
        comment(f"Fulfilled Brent Equations: {pretty_3_num(ok_cnt)}")
        comment()
        w()

        for product in mmt.Products:
            ta = get_term_mod2(mmt.F, product)
            tb = get_term_mod2(mmt.G, product)
            tc = get_term_mod2(mmt.D, product)
            o(f"{product+1}:")
            w(f"{ta}*{tb}*{tc}")

        w()
        comment(f"end of file '{file_name}', lines: {wrf_line_count + 2}")
        w()


def main():
    """ the output file """
    global wrf
    """ the MatMultTensor """
    global mmt

    mmt = MatMultTensor("555")

    """ check if Brent Equations are fulfilled """
    err_cnt, ok_cnt = validate_algorithm()

    write_tensor_file(err_cnt, ok_cnt)

    if (mmt.data_type == np.int32):
        err_cnt, ok_cnt = validate_algorithm_mod2()

        write_tensor_file_mod2(err_cnt, ok_cnt)
    else:
        o("No mod 2 tensor file written due to data type != int")

    o()
    o("Ciao!")
    o()
    exit(0)


if __name__ == "__main__":
    main()
