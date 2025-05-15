from z3 import *

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
    op_dicx[Z3_OP_AND] = "And"
    op_dicx[Z3_OP_OR] = "Or"
    
def export_op1(ass, dic, akbc):
    op = op_dic1[ass.kind()]
    e = export_assertion(ass.arg(0), dic, akbc)
    return f"{op}{e}"                       

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
    e = [export_assertion(ass.arg(i), dic, akbc) for i in range(ass.num_args())]
    se = ", ".join(e)
    return f"{op}({se})"                

def export_assertion(ass, dic, akbc):
    global varidx
    
    if not op_dic0:
        define_op_dictionaries()
        
    create_temp = True
    rhs = ""
    
    kind = ass.kind()
    
    if (ass.num_args() == 0) and (kind in op_dic0):
        create_temp = False
        rhs = op_dic0[kind]
        if rhs is None:
            rhs = str(ass)        
    elif (ass.num_args() == 1) and (kind in op_dic1):
        rhs = export_op1(ass, dic, akbc)                                   
    elif (ass.num_args() == 2) and (kind in op_dic2):
        rhs = export_op2(ass, dic, akbc)                               
    elif (ass.num_args() == 3) and (kind in op_dic3):
        rhs = export_op3(ass, dic, akbc)                               
    elif (ass.num_args() > 2) and (kind in op_dicx):
        rhs = export_opx(ass, dic, akbc)                               
    else:
        RuntimeError(f"Unknown kind {ass.kind()}")
            
    if create_temp:
        if rhs in dic:
            lhs = dic[rhs]
        else:
            varidx += 1
            lhs = f"k!{varidx}"
            dic[rhs] = lhs
            akbc.write(f"{lhs} := {rhs};\n")
            print(f"{lhs} := {rhs};")
    else:
        lhs = rhs
            
    return lhs


class var_name_comparator:
    def __init__(self, obj, *args):
       self.obj = obj
    def __lt__(self, other):
        n1 = other.obj
        n2 = self.obj
        if not n1.startswith("k!"):
            return n1 > n2
        elif not n2.startswith("k!"):
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
    asserts = set()
    with open(file_name, "w", encoding='ascii') as akbc:
        for ass in solver.assertions():
            e = export_assertion(ass, dic, akbc)
            asserts.add(e)
            
        s = "Assert "
        sep = ""
        for e in sorted(asserts, key = var_name_comparator):
            if len(s + sep + e) > 72:
                akbc.write(f"{s},\n")
                s = "       "
            s += sep + e
            sep = ", "
        akbc.write(f"{s};\n")                       
        akbc.write("\n")
        print(f"{s};")                       
        print("")
    
    print(f"akbc circuit file '{file_name}' written")
    
    
a, b, c, d, e, f, g, h = Bools('a b c d e f g h')

solver = Solver()

solver.add(Xor(Xor(And(a, And(b, c)), And(Implies(a, b), And(b, c))), And(And(e, f, g), And(b, c), h)))
solver.add(If(a, b, Distinct(c, d)))
solver.add(And(a, b, c, d, e))

test1 = False
if test1:
    goal = Goal()
    goal.add(solver.assertions())
    t = Then('simplify', 'bit-blast', 'tseitin-cnf')

    cnf = t(goal)[0]

    print(cnf)
    
test2 = True
if test2:
    export_akbc(solver, "akbc.txt")
    
