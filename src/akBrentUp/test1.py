# from giacpy import giac, gbasis
from giacpy import *
from Parser import parse_giac_expression
from typing import Tuple

debugLevel = 4


def read_polynomial(line):
    poly = line.replace("p: ", "").rstrip()
    return poly


def read_variable(line):
    name = line.replace("v: ", "").rstrip()
    return name


def read_symbolic_input(file_name: str) -> Tuple[list, list]:
    polynomials = []
    variables = []
    with open(file_name, "r+t", encoding="ascii") as input_file:
        for line in input_file:
            if len(line) < 3:
                continue
            if line.startswith("#"):
                continue
            if line.startswith("p:"):
                polynomials.append(read_polynomial(line))
            elif line.startswith("v:"):
                variables.append(read_variable(line))
            else:
                print(f"Unexpected line: {line}")
                
    print(f"Polynomials: {len(polynomials)}")
    print(f"Variables:   {len(variables)}")

    return polynomials, variables


def parse_expressions(expressions, name2var):
    polynomials = []
    
    for expr in expressions:
        try:
            print(f"expr:               '{expr}'")           
            result = parse_giac_expression(expr, name2var)
            print(f"Evaluation result:  '{result}'")
            polynomials.append(result)
            print()
        except ValueError as e:
            print(f"Value Error: {e}")
        except SyntaxError as e:
            print(f"Syntax Error: {e}")
            
    return polynomials
    

def main():
    input_file_name = "SymbolicSolver.txt"
    print(f"Reading {input_file_name}")
    expressions, variables = read_symbolic_input(input_file_name)
    
    """ prepare a dictionary of named giac variables """
    # name2var = {var_name: giac(var_name) for var_name in variables}
    name2var = dict([(var_name, giac(var_name)) for var_name in variables])
        
    # print(variables)
    
    print(f"Parsing expressions: {len(expressions)}")
    polynomials = parse_expressions(expressions, name2var)

    # print(polynomials)
    # print(name2var.values())
    
    # help("giacpy")
    
    print("Compute Groebner bases")
    # Compute the Gröbner basis
    groebner_basis = gbasis(polynomials, list(name2var.values()))

    # Print the Gröbner basis
    print(f"Gröbner Basis: len {len(groebner_basis)}")
    i = 0
    for g in groebner_basis:
        i += 1
        print(f"{i}: {g}")
        if i > 100:
            break
            
    
            
if __name__ == "__main__":
    main()