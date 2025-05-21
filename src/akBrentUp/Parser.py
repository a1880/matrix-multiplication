"""

Parser.py

expression   ::= term (('+' | '-') term)*
term         ::= factor ('*' factor)*
factor       ::= ('+' | '-')* primary
primary      ::= INTEGER | VARIABLE | '(' expression ')'
INTEGER      ::= DIGIT+
VARIABLE     ::= LETTER (LETTER | DIGIT)*
DIGIT        ::= '0'..'9'
LETTER       ::= 'a'..'z' | 'A'..'Z' | '_'

"""
from giacpy import giac
import re

""" switch to get compact AST prints """
ast_long_repr = False

def o(s = ""):    
    # print(s)
    del s
    return
    
    
class Token:
    def __init__(self, type_, value=None):
        self.type = type_
        self.value = value
        o(self.__repr__())

    def __repr__(self):
        return f"Token({self.type}, '{self.value}')"

def tokenize(expr):
    token_spec = [
        ('NUMBER',   r'\d+'),           # Integer (unsigned)
        ('ID',       r'[a-zA-Z_]\w*'),  # Variable names
        ('OP',       r'[+\-*]'),        # Operators
        ('LPAREN',   r'\('),            # (
        ('RPAREN',   r'\)'),            # )
        ('SKIP',     r'[ \t]+'),        # Skip spaces/tabs
    ]
    tok_regex = '|'.join(f'(?P<{name}>{regex})' for name, regex in token_spec)
    for mo in re.finditer(tok_regex, expr):
        kind = mo.lastgroup
        value = mo.group()
        if kind == 'NUMBER':
            yield Token('NUMBER', int(value))
        elif kind == 'ID':
            yield Token('ID', value)
        elif kind == 'OP':
            yield Token('OP', value)
        elif kind == 'LPAREN':
            yield Token('LPAREN', value)
        elif kind == 'RPAREN':
            yield Token('RPAREN', value)
        elif kind == 'SKIP':
            continue
    yield Token('EOF')

# AST Node types
class Number:
    def __init__(self, value):
        self.value = value
    def eval(self, env):
        del env
        return self.value
    def __repr__(self):
        return f"Number({self.value})" if ast_long_repr else f"{self.value}"

class Variable:
    def __init__(self, name):
        self.name = name
    def eval(self, env):
        return env[self.name]
    def __repr__(self):
        return f"Variable({self.name})" if ast_long_repr else f"{self.name}"

class BinOp:
    def __init__(self, left, op, right):
        self.left = left
        self.op = op
        self.right = right
    def eval(self, env):
        l = self.left.eval(env)
        r = self.right.eval(env)
        if self.op == '+':
            return l + r
        elif self.op == '-':
            return l - r
        elif self.op == '*':
            return l * r
        raise ValueError(f"Unsupported op '{self.op}' in eval()")
    def __repr__(self):
        return f"BinOp({self.left}, {self.op}, {self.right})" if ast_long_repr else f"({self.left} {self.op} {self.right})"

class Parser:
    def __init__(self, tokens):
        self.tokens = iter(tokens)
        self.current = next(self.tokens)

    def eat(self, type_, value=None):
        if self.current.type == type_ and (value is None or self.current.value == value):
            self.current = next(self.tokens)
        else:
            raise SyntaxError(f"Expected {type_} {value}, got {self.current}")

    def parse(self):
        node = self.expr()
        if self.current.type != 'EOF':
            raise SyntaxError(f"Unexpected token at end. Expected 'EOF', got {self.current.type} '{self.current.value}'")
        return node

    def expr(self):
        o("expr parse:")
        node = self.term()
        o(f"expr parse: term = {node}")
        while self.current.type == 'OP' and self.current.value in ('+', '-'):
            o(f"expr parse: op {self.current}")
            op = self.current.value
            self.eat('OP')
            node = BinOp(node, op, self.term())
        o(f"expr parse: return = {node}")
        return node

    def term(self):
        o("term parse: ")
        node = self.factor()
        while self.current.type == 'OP' and self.current.value == '*':
            op = self.current.value
            self.eat('OP')
            node = BinOp(node, op, self.factor())
        o(f"term parse return: {node}")
        return node

    def factor(self):
        token = self.current
        o(f"factor parse: '{token}'")
        if token.type == 'NUMBER':
            self.eat('NUMBER')
            o(f"factor parse return Number {token.value}")
            return Number(token.value)
        elif token.type == 'ID':
            self.eat('ID')
            o(f"factor parse return Variable {token.value}")
            return Variable(token.value)
        elif token.type == 'OP' and token.value in ('+', '-'):
            op = token.value
            self.eat('OP')
            factor = self.factor()
            if op == '-':
                if isinstance(factor, Number):
                    factor = Number(- factor.value)
                else:
                    factor = BinOp(Number(0), '-', factor)
            o(f"factor parse return {factor}")
            return factor
        elif token.type == 'LPAREN':
            self.eat('LPAREN')
            node = self.expr()
            self.eat('RPAREN')
            return node
        else:
            raise SyntaxError(f"Unexpected token in factor: {token}")


def parse_giac_expression(expr, variables):
    """
    Tokenize expression.
    Create an Abstract Syntax Tree (AST).
    Evaluate the AST by inserting Giac variables
    Return the evaluated Giac expression as symbolic expression
    """
    tokens = list(tokenize(expr))
    parser = Parser(tokens)
    ast = parser.parse()
    return ast.eval(variables)


# Example usage:
def parse_expression(expr):
    tokens = list(tokenize(expr))
    parser = Parser(tokens)
    return parser.parse()

def main():
    expr = "-4*x*y*x + -x + 2 * (y - 3)"
    expr = "-41*f11_4*g15_4*d15_4-42*f11_4*g15_4-2*f11_4-2*f11_4-2*g15_4-2*g15_4-2"
    expr = "f11_4-42"
    ast = parse_expression(expr)
    print("AST:", ast)
    print(f"expr: {expr}")
    env = {'x': 4, 'y': 10}
    env = {'f11_4': giac('f11_4'),
           'g15_4': giac('g15_4'),
           'd15_4': giac('d15_4')}
    print("Evaluated:", ast.eval(env))
    
    
# Example:
if __name__ == "__main__":
    main()
    