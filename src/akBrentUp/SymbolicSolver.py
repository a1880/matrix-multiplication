"""
Module SymbolicSolver.py

This module is meant to supply an interface to an external Gröbner solver engine
like Sage or Giac. These engines are not (yet) available under Windows.
Therefore, the approach is to write the euqations and variables to an external file,
let it process/solve externally and re-import the solution

Axel Kemper  20-May-2025

"""
import re
from util import check, close_output, fatal, is_integer, o, open_output, timestamp

class Expression():
    """
    Symbolic Expression
    Includes operator overloading with +, - and *
    The resulting Expression is simply stored as text

    The caller can write and store Expressions with integers and Symbol() variables.
    """

    def __init__(self, text: str = ""):
        self.text = text
        self.isnum = text.isdigit()

    def __add__(self, o):
        if isinstance(o, Expression):
            if self.isnum and o.isnum:
                return Expression(f"{self.as_int() + o.as_int()}")
            else:
                return Expression(f"{self.text} + {o.text}") 
        if isinstance(o, int):
            if self.isnum:
                return Expression(f"{self.as_int() + o}")
            else:
                return Expression(f"{self.text} + {o}")
        fatal("add operator not supported")

    def __mul__(self, o):
        if isinstance(o, Expression):
            if self.isnum and o.isnum:
                return Expression(f"{self.as_int()*o.as_int()}") 
            else:
                return Expression(f"{self.text}*{o.text}") 
        if isinstance(o, int):
            if self.isnum:
                return Expression(f"{self.as_int()*o}") 
        fatal("mul operator not supported")

    def __radd__(self, o):
        if isinstance(o, Expression):
            if self.isnum and o.isnum:
                return Expression(f"{o.as_int() + self.as_int()}") 
            else:
                return Expression(f"{o.text} + {self.text}") 
        if isinstance(o, int):
            if self.isnum and o.isnum:
                return Expression(f"{o + self.as_int()}")
            else:
                return Expression(f"{o} + {self.text}")
        fatal("radd operator not supported")

    def __rmul__(self, o):
        if isinstance(o, Expression):
            if self.isnum and o.isnum:
                return Expression(f"{o * self.as_int()}") 
            else:
                return Expression(f"{o.text}*{self.text}") 
        if isinstance(o, int):
            if self.isnum and o.isnum:
                return Expression(f"{o * self.as_int()}")
            else:
                return Expression(f"{o}*{self.text}")
        fatal("rmul operator not supported")

    def __str__(self) -> str:
        return self.text

    def __sub__(self, o):
        if isinstance(o, Expression):
            if self.isnum and o.isnum:
                return Expression(f"{self.as_int() - o.as_int()}") 
            else:
                return Expression(f"{self.text} - {o.text}") 
        if isinstance(o, int):
            if self.isnum:
                return Expression(f"{self.as_int() - o}")
            else:
                return Expression(f"{self.text} - {o}")
        fatal("sub operator not supported")

    def __rsub__(self, o):
        if isinstance(o, Expression):
            if self.isnum and o.isnum:
                return Expression(f"{o.as_int() - self.as_int()}") 
            else:
                return Expression(f"{o.text} - {self.text}") 
        if isinstance(o, int):
            if self.isnum:
                return Expression(f"{o - self.as_int()}")
            else:
                return Expression(f"{o} - {self.text}")
        fatal("rsub operator not supported")

    def as_int(self) -> int:
        check(self.isnum, "Cannot convert expr to int")
        return int(self.text)

    def is_polynomial(self) -> bool:
        if isinstance(self, Symbol):
            return True
        terms = split_poly_in_terms(self.text)
        return all([is_term(term) for term in terms])

    def simplify(self) -> 'Expression':
        """
        Try to simplify and unify Expression
        For the time being only for polynomial expressions
        """
        check(self.is_polynomial(), \
              "simplify does not support arbitrary Expressions yet, only polynomials")
        expr = unify_expression(self.text)
        return self if expr == self.text else Expression(expr)


class Poly():
    """
    Symbolic polynomial class
    Polynomial is modelled as Expression with a list of variables

    A list of Polynomials his handed over to the groebner() solver
    """

    def __init__(self, expr: Expression, vars: list = [], domain: str = 'ZZ'):
        check(expr != None, "None expr")
        check(vars, "Empty vars")
        self.expr = expr
        self.vars = vars
        self.domain = domain

    def __str__(self) -> str:
        return str(self.expr)

    def as_expr(self):
        return self.expr

    @staticmethod
    def from_expr(expr: Expression, vars: list, domain: str ='ZZ'):
        return Poly(expr, vars, domain)


class Symbol(Expression):
    """
    Symbolic Symbol class
    A Symbol is a symbolic variable with a name and some properties.
    Symbol is the elementary/primary Expression
    """

    def __init__(self, name: str, integer: bool, negative: bool = False, nonzero: bool = False):
        self.name = name
        self.integer = integer
        self.negative = negative
        self.nonzero = nonzero
        super().__init__(name)

    def __str__(self):
        return self.name

    def __repr__(self):
        return self.name

    def is_symbol(self) -> bool:
        return True

""" =============================<  module functions  >============================= """

def collapse_constant_factors(factors):
    """
    Loop through the list of factors and extract all integer factors.
    Put the resulting product in front of the list
    If the product is 1, ignore it
    If the product is -1, swap sign of the first factor
    """
    product = 1

    i = len(factors) - 1
    while i >= 0:
        factor = factors[i]
        if is_integer(factor):
            product *= int(factor)
            factors.pop(i)
        i -= 1

    if product != 1:
        if (product == -1) and factors:
            if factors[0].startswith('-'):
                factors[0] = factors[0][1:]
            else:
                factors[0] = f"-{factors[0]}".replace("-+", "-")
        else:
            factors.insert(0, str(product))


def collapse_constant_terms(terms):
    """
    Loop through the list of terms and extract all integers
    If the resulting sum is 0, ignore it
    Otherwise, add it as last term
    """
    sum = 0

    i = len(terms) - 1
    while i >= 0:
        term = terms[i]
        if is_integer(term):
            sum += int(term)
            terms.pop(i)
        i -= 1
    if sum != 0:
        terms.append(str(sum))


def groebner(polynomials: list[Poly], vars: list[Symbol], method: str ='f5b', order: str = 'grlex'):
    """
    Compute the Gröbner bases for a list of polynomials
    For the time being, the polynomials and variables are written to an external text file.
    This file can then be processed by some external system.
    """
    file_name = "SymbolicSolver.txt"
    open_output(file_name)
    print(f"Creating SymbolicSolver script file {file_name}")

    o("#")
    o(f"# SymbolicSolver script {file_name} created {timestamp()}")
    o("#")
    o("#")
    o(f"# Method: '{method}' ignored")
    o(f"# Order:  '{order}' ignored'")

    o()

    o(f"# Polynomials: {len(polynomials)}")
    for poly in polynomials:
        o(f"p: {poly.as_expr()}")

    #  variables are assumed to be sorted
    o()
    o(f"# Variables: {len(vars)}")
    for var in vars:
        o(f"v: {var.name}")
    o()

    close_output()

   
def is_factor(factor: str) -> bool:
    """
    Check if string represents a valid factor.
    Valid factors include integers and variables,
    optionally with a + or - sign
    """
    return is_integer(factor) or \
           is_identifier(factor) or \
           ((factor.startswith('+') or factor.startswith('-')) and is_factor(factor[1:]))

           
def is_identifier(id:str) -> bool:
    """
    Check if string represents a variable identifier.
    Identifiers have to start with an alphabetical character.
    The remaining identifier is composed of alphanumeric characaters and optional underscore 
    characters
    """
    return id and id[0].isalpha() and id[1:].replace("_", "").isalnum()


def is_term(term: str) -> bool:
    """
    Check if term represents a factor or a product of factors
    """
    factors = term.split('*')
    return all([is_factor(factor) for factor in factors])


def simplify(expr: Expression):
    """
    Return a simplified expression.
    Simplification included collapsed integer constants
    """
    return expr.simplify()


def sort_factors(factor: str):
    if factor.startswith('-'):
        #  leading '-' is irrelevant for sorting
        return sort_factors(factor[1:])
    if is_integer(factor):
        #  pure int constants come last
        inf = 1000
        return (inf, inf, inf, inf)
    return sort_vars(factor)


def sort_polynomial_terms(polynomials: list[str], variables: list[str]) -> None:
    variables.sort(key = sort_var_symbols)

    for i in range(len(polynomials)):
        polynomials[i] = unify_polynomial(polynomials[i])


def sort_term(term: str):
    factors = term.split('*')
    n = len(factors)
    t = [sort_factors("1")] * 4
    for i in range(n):
        t[i] = sort_factors(factors[i])
    return tuple(t)


def sort_var_symbols(var_symbol):
    return sort_vars(var_symbol.name)


def sort_vars(vname: str):
    c = vname[0]
    if c != 'h':
        pos = "fdg".find(c)
        check(pos >= 0, "Inconsistent var name")
        name = pos
        row = int(vname[1])
        col = int(vname[2])
        product = int(vname[4:])
        check(vname[3] == '_', "Inconsistent var name")
    else:
        name = 3
        row = int(vname[1:3])
        col = int(vname[3:5])
        product = int(vname[6:])
        check(vname[5] == '_', "Inconsistent h var name")
    #  sort keys in order of priority (decreasing)
    return (product, name, row, col)


def split_poly_in_terms(poly_str: str) -> list[str]:
    """
    Return an array of terms
    Terms are separated by + or -
    """
    # Remove spaces for easier splitting
    poly_str = poly_str.replace(' ', '')
    # Split on '+' and '-'
    # Add a '+' before any minus sign not at the start for correct splitting
    poly_str = re.sub(r'(?<!^)-', '+-', poly_str)
    terms = poly_str.split('+')
    # Remove empty strings (can happen if the string starts with '+')
    terms = [term for term in terms if term]
    return terms


def unify_term(term: str) -> str:
    sign = ""
    if term.startswith('-'):
        sign = '-'
        term = term[1:]

    factors = term.split('*')
    n = len(factors)
    if n == 1:
        return sign + term
    sorted(factors, key = sort_factors)
    collapse_constant_factors(factors)
    return sign + '*'.join(factors)


def unify_expression(expr: str) -> str:
    """
    Split expression in terms
    Unify every term to be in sorted order
    The sort the terms within the expression.
    Use variables as sorting criterion
    """
    terms = split_poly_in_terms(expr)
    for i in range(len(terms)):
        terms[i] = unify_term(terms[i])
    terms.sort(key = sort_term)
    collapse_constant_terms(terms)
    return ('+'.join(terms)).replace("+-", "-")


def unify_polynomial(poly: Poly) -> Poly:
    """
    Split polynomial poly in terms
    Unify every term to be in sorted order
    The sort the terms within the polynomial
    Use variables as sorting criterion
    """
    poly.expr.text = unify_expression(poly.expr.text)
    return poly
        

