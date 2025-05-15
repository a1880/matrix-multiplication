import subprocess

from z3 import *

from util import (check, find_file_in_path, o)

varidx = 0

op_dic0 = {}
op_dic1 = {}
op_dic2 = {}
op_dic3 = {}
op_dicx = {}


def define_op_dictionaries():
    op_dic0[Z3_OP_TRUE] = "T"
    op_dic0[Z3_OP_FALSE] = "F"
    op_dic0[Z3_OP_UNINTERPRETED] = None
    op_dic1[Z3_OP_NOT] = "!"
    op_dic2[Z3_OP_EQ] = "=="
    op_dic2[Z3_OP_DISTINCT] = "!="
    op_dic2[Z3_OP_AND] = "&&"
    op_dic2[Z3_OP_OR] = "||"
    op_dic2[Z3_OP_XOR] = "!="
    op_dic2[Z3_OP_IMPLIES] = "->"
    op_dic3[Z3_OP_ITE] = "?", ":"
    op_dicx[Z3_OP_AND] = "AND"
    op_dicx[Z3_OP_OR] = "OR"


def export_op1(ass, dic, akbc):
    op = op_dic1[ass.kind()]
    _e = export_assertion(ass.arg(0), dic, akbc)
    return f"{op}{_e}"


def export_op2(ass, dic, akbc):
    op = op_dic2[ass.kind()]
    e0 = export_assertion(ass.arg(0), dic, akbc)
    e1 = export_assertion(ass.arg(1), dic, akbc)
    return f"{e0} {op} {e1}"


def export_op3(ass, dic, akbc):
    op1, op2 = op_dic3[ass.kind()]
    e0 = export_assertion(ass.arg(0), dic, akbc)
    e1 = export_assertion(ass.arg(1), dic, akbc)
    e2 = export_assertion(ass.arg(2), dic, akbc)
    return f"{e0} {op1} {e1} {op2} {e2}"


def export_opx(ass, dic, akbc):
    op = op_dicx[ass.kind()]
    _e = [export_assertion(ass.arg(i), dic, akbc) for i in range(ass.num_args())]
    se = ", ".join(_e)
    return f"{op}({se})"


def export_assertion(ass, dic, akbc):
    """
    Translate assertion to AKBC text statements
    :param ass: The assertion
    :param dic: dictionary of known assertions
    :param akbc: The AKBC text file
    :return: left-hand-side of the translated assertion
    """
    global varidx

    if not op_dic0:
        define_op_dictionaries()

    create_temp = True
    rhs = ""

    kind = ass.kind()

    if (ass.num_args() == 0) and (kind in op_dic0):
        #  an assertion without operands: T, F or variable
        create_temp = False
        rhs = op_dic0[kind]
        if rhs is None:
            # take variable name as string
            rhs = str(ass)
    elif (ass.num_args() == 1) and (kind in op_dic1):
        #  assertion with single operand
        rhs = export_op1(ass, dic, akbc)
    elif (ass.num_args() == 2) and (kind in op_dic2):
        #  assertion with two operands
        rhs = export_op2(ass, dic, akbc)
    elif (ass.num_args() == 3) and (kind in op_dic3):
        #  assertion with three operands
        rhs = export_op3(ass, dic, akbc)
    elif (ass.num_args() > 2) and (kind in op_dicx):
        #  AND() or OR() assertion with mor than 2 operands
        rhs = export_opx(ass, dic, akbc)
    else:
        RuntimeError(f"Unknown kind of assertion: {ass.kind()}")

    if create_temp:
        if rhs in dic:
            lhs = dic[rhs]
        else:
            varidx += 1
            lhs = f"_k{varidx}"
            dic[rhs] = lhs
            akbc.write(f"{lhs} := {rhs};\n")
            # print(f"{lhs} := {rhs};")
    else:
        lhs = rhs

    return lhs


class var_name_comparator:
    def __init__(self, obj, *args):
        self.obj = obj

    def __lt__(self, other):
        n1 = other.obj
        n2 = self.obj
        if not n1.startswith("_k"):
            return n1 > n2
        elif not n2.startswith("_k"):
            return n1 > n2
        else:
            i1 = int(n1[2:])
            i2 = int(n2[2:])
            return i1 > i2


def export_akbc(solver, file_name):
    #  cache dictionary for common sub-expression elimination
    dic = {}
    print()
    print(f"Writing akbc circuit file '{file_name}'")
    #  store solver assertions in a set
    #  for sorted ASSIGN
    asserts = set()
    with open(file_name, "w", encoding='ascii') as akbc:
        akbc.write("AKBC1.0\n")
        for ass in solver.assertions():
            _e = export_assertion(ass, dic, akbc)
            check(not (_e in asserts), "Duplicate assertion")
            asserts.add(_e)

        s = "ASSIGN "
        sep = ""
        for e in sorted(asserts, key=var_name_comparator):
            if len(s + sep + e) > 72:
                akbc.write(f"{s},\n")
                sep = ""
                s = "       "
            s += sep + e
            sep = ", "
        akbc.write(f"{s};\n")
        akbc.write("\n")
        # print(f"{s};")                       
        # print("")

    print(f"akbc circuit file '{file_name}' written")


def run_akbc(akbc_file_name, cnf_file_name, akbc_log_file_name, debug):
    akbc = "akBool2cnf"
    akbc_exe = f"{akbc}.exe"
    exe = find_file_in_path(akbc_exe, "$PATH")
    check(exe is not None, f"{akbc_exe} not found!")
    command = \
        [exe,
         "--debug=2",
         f"--log={akbc_log_file_name}",
         "--no-wait",
         "--no-simplify",
         "--xcnf",
         f"{akbc_file_name}",
         f"{cnf_file_name}"]

    o("")
    o(f"Running {akbc}: {exe}")
    if debug:
        print(command)

    #  make sure that akBoole output comes after the
    #  output stored so far
    print("", flush=True)
    print("", flush=True)

    result = subprocess.run(command)

    o()
    if result.returncode == 0:
        o(f"{akbc} executed successfully.")
    else:
        o(f"{akbc} failed with return code {result.returncode}.")

    return result.returncode == 0
