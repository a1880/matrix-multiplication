"""
GroebnerLifter.py  -  module to implement lifting via Gröbner bases for solving
                      Brent equations
"""

from BiniScheme import BiniScheme
from MatMultDim import MatMultDim
import numpy as np
from SymbolicSolver import sort_polynomial_terms
from util import fatal, o

use_sympy = False
use_symbolic_solver = True

if use_sympy:
    # import sympy as sp
    # from sympy.polys.orderings import grlex
    pass
elif use_symbolic_solver:
    import SymbolicSolver as sp
    grlex = 'grlex'
else:
    fatal("inconsistent solver setup")

class GroebnerLifter:
    def __init__(self, mmDim: MatMultDim, bini: BiniScheme):
        self.mmDim: MatMultDim = mmDim
        self.bini: BiniScheme = bini
        self.lifted: BiniScheme = None
        self.dyads: dict[str, sp.core.mul.Mul] = {}
        self.dyad_expressions: dict[str, sp.core.mulMul] = {}
        self.variables: dict[str, sp.Symbol] = {}
        self.var_values: dict[str, int] = {}
        self.var_id: dict[str, int] = {}
        self.product_index_digits: int = len(str(mmDim.noOfProducts))
        self.use_zero_one: bool = True  # False: use -1 and +1  
        bini.validate()
        
    def dyad(self, a_row: int, a_col: int, 
                   b_row: int, b_col: int, 
                   product: int, cnt: int):  #  -> sp.core.mul.Mul:
        """
        A "dyad" is a product of two variables.
        """
        sa = self.idx(a_row, a_col)
        sb = self.idx(b_row, b_col)
        sk = str(product+1).zfill(self.product_index_digits)
        lit = f"h{sa}{sb}_{sk}"   #  h comes after d,f,g
        if lit in self.dyads:
            fg = self.dyads[lit]
        else:
            val = self.F(a_row, a_col, product)
            f = self.variable("f", a_row, a_col, product, val)
            val = self.G(b_row, b_col, product)
            g = self.variable("g", b_row, b_col, product, val)
            if self.use_zero_one:
                # fg = sp.expand((1-2*f) * (1-2*g))
                fg = 1 - 2*g - 2*f - 4*f*g
            else:
                fg = f * g
            if cnt > 1:
                self.dyad_expressions[lit] = sp.simplify(fg)
                fg = self.Symbol(lit)
                self.dyads[lit] = fg
                self.variables[lit] = fg
        return fg
        
    def D(self, row: int, col: int, product: int) -> int:
        return self.bini.c(row, col, product)

    def Dmod(self, row: int, col: int, product: int, solution):
        d = self.bini.c(row, col, product)
        if d == 0:
            return 0
        elif self.literal("d", row, col, product) not in self.variables:
            return d
        else:
            v = self.variable_value("d", row, col, product, solution)
            if self.use_zero_one:
                return d * (1 - 2*v)
            else:
                return d * v
    
    def F(self, row, col, product):
        return self.bini.a(row, col, product)

    def Fmod(self, row, col, product, solution):
        f = self.bini.a(row, col, product)
        if f == 0:
            return 0
        elif self.literal("f", row, col, product) not in self.variables:
            return f
        else:
            v = self.variable_value("f", row, col, product, solution)
            if self.use_zero_one:
                return f * (1 - 2*v)
            else:
                return f * v

    def G(self, row, col, product):
        return self.bini.b(row, col, product)
    
    def Gmod(self, row, col, product, solution):
        g = self.bini.b(row, col, product)
        if g == 0:
            return 0
        elif self.literal("g", row, col, product) not in self.variables:
            return g
        else:
            v = self.variable_value("g", row, col, product, solution)
            if self.use_zero_one:
                return g * (1 - 2*v)
            else:
                return g * v

    def idx(self, row, col):
        return f"{row+1}{col+1}"

    def is_non_zero_triple(self, a_row, a_col, b_row, b_col, c_row, c_col, product):
        """  use 'and'  to benefit from expression short circuiting """
        b = self.bini
        return \
               (b.a(a_row, a_col, product) != 0) and \
               (b.b(b_row, b_col, product) != 0) and \
               (b.c(c_row, c_col, product) != 0)

    def lift(self):
        """ local variable to reduce typing """
        dim = self.mmDim
        o("Starting Groebner lifting")
        o(dim.toString)
        
        polynomials: list[sp.Poly] = []
        non_poly_cnt = 0
        """ set of d coefficients which don't require to be fixed """
        no_fix_vars: set = set()
        
        """ find dyads which are used multiple times 
            fill the list of variables to establish
            an ordering of monomials """
        dyad_counts: dict[str, int] = {}
        for a_row, a_col in dim.AIndices:
            for b_row, b_col in dim.BIndices:
                for c_row, c_col in dim.CIndices:
                    no_of_triples = 0
                    d_no_fix = None
                    f_no_fix = None
                    g_no_fix = None
                    for product in self.bini.r_products:
                        if self.is_non_zero_triple(a_row, a_col, b_row, b_col, c_row, c_col, product):
                            """ check how often this dyad is used  """
                            d_args = (a_row, a_col, b_row, b_col, product)
                            d_cnt = dyad_counts.get(d_args, 0) + 1
                            dyad_counts[d_args] = d_cnt
                            """ create variables to get them in the list self.variables """
                            val = self.F(a_row, a_col, product)
                            f_no_fix = self.variable("f", a_row, a_col, product, val)
                            val = self.G(b_row, b_col, product)
                            g_no_fix = self.variable("g", b_row, b_col, product, val)                            
                            d_val = self.D(c_row, c_col, product)
                            d_no_fix = self.variable("d", c_row, c_col, product, d_val)
                            if d_cnt > 1:
                                """  only create a dyad if used more than once  """
                                _ = self.dyad(a_row, a_col, b_row, b_col, product, d_cnt)
                            no_of_triples += 1
                    if no_of_triples == 1:
                        """ coefficients are part of single triple
                            no need to constrain domains.
                            They must be +/- 1"""
                        no_fix_vars.add(d_no_fix)
                        no_fix_vars.add(f_no_fix)
                        no_fix_vars.add(g_no_fix)
                        
        keys = sorted(self.variables.keys())
        vars = [self.variables[v] for v in keys]

        id = 0
        for v in keys:
            self.var_id[v] = id
            id += 1
            
        o(f"Variables {len(keys)}")
        for v in keys:
            o(f"{self.var_id[v]}: {v}")
                    
        """ construct set of polynomials """
        for a_row, a_col in dim.AIndices:
            for b_row, b_col in dim.BIndices:
                for c_row, c_col in dim.CIndices:
                    sum_of_triples = 0
                    first = True
                    for product in self.bini.r_products:
                        if self.is_non_zero_triple(a_row, a_col, b_row, b_col, c_row, c_col, product):
                            """
                            for mod2 1*1*1 triple, we have to find the '+'/'-' signs
                            
                            A "triple" is a product of three variables.
                            """
                            d = (a_row, a_col, b_row, b_col, product)                            
                            fg = self.dyad(a_row, a_col, b_row, b_col, product, dyad_counts[d])
                            d_val = self.D(c_row, c_col, product)
                            d = self.variable("d", c_row, c_col, product, d_val)
                            if self.use_zero_one:
                                fgd = fg - 2*fg*d
                            else:
                                fgd = fg*d;
                            if first:
                                sum_of_triples = fgd
                                first = False
                            else:
                                sum_of_triples += fgd
                    if not first:
                        """ Triple(s) found: we have something to append """
                        # o(f"triples {sum_of_triples} {str(type(sum_of_triples))}")
                        if (a_row == c_row) and (a_col == b_row) and (b_col == c_col):
                            """ -1 first to allow constant folding """
                            expr = sum_of_triples - 1
                        else:
                            expr = sum_of_triples
                        expr = sp.simplify(expr)
                        poly: sp.Poly = self.Poly(expr, vars)
                        polynomials.append(poly)
                    else:
                        """ No non-zero triples in this equation """
                        non_poly_cnt += 1
        
        o(f"len(polynomials) {len(polynomials)}")
        o(f"non polynomials {non_poly_cnt}")
        
        fix_vars: set = set(vars) - no_fix_vars - set(self.dyads.values())
        
        o(f"no fix vars {len(no_fix_vars)}")
        o(no_fix_vars)
        o(f"fix vars {len(fix_vars)}")
        o(fix_vars)

        if not fix_vars:
            o("No variables to be fixed to their domain")
        else:
            s = f"constraints for {len(fix_vars)} of " + \
                f"{len(self.variables) - len(self.dyads)} variables in"
            if self.use_zero_one:
                o(f"Appending x² - x {s} {{0, 1}}")
            
                """ all variables {0, 1} have to satify x² - x == 0 """
                polynomials += [self.Poly(v*v - v, vars) for v in fix_vars]
            else:
                o(f"Appending x² - 1 {s} {{-1, +1}}")
            
                """ all variables {-1, +1} have to satify x² - 1 == 0 """
                polynomials += [self.Poly(v*v - 1, vars) for v in fix_vars]
        
        if not self.dyads:
            o(f"No dyads defined")
        else:
            o(f"Appending dyad expressions: {len(self.dyads)}")
            polynomials += [self.Poly(self.dyad_expressions[d] - self.dyads[d], vars) for d in self.dyads]
            
        o(f"Polynomials: {len(polynomials)}")
        pno = 0
        for poly in polynomials:
            p: sp.Poly = poly
            o(f"{pno}: {p.as_expr()}")
            pno += 1
            
        """
        Ensure that variables and polynomials are sorted/ordered
        """
        sort_polynomial_terms(polynomials, vars)

        o("Computing Groebner bases")
        
        # G = sp.groebner(polynomials, vars, order=grlex)
        G = sp.groebner(polynomials, vars, method='f5b', order=grlex)
        
        if not use_sympy:
            fatal("NIY")

        o(f"Gröbner basis computed len(G)={len(G)}")
        
        show_groebner = True
        if show_groebner:
            print(G)
            
            print(f"args {len(G.args)}")
            for arg in G.args:
                print(arg)
            print("------")
            
            print(f"exprs {len(G.exprs)}")
            for exp in G.exprs:
                print(exp)
            print("------")
            
            print(f"polys {len(G.polys)}")
            for poly in G.polys:
                print(poly)
            print("------")
            
            print(f"gens {len(G.gens)}")
            for gen in G.gens:
                print(gen)
                print(str(type(gen)))
                v: sp.Symbol = gen
                val = gen.evalf()
                print(f"val {val} {str(type(val))}")
                
            print("------")

            print(f"len(G) {len(G)}")
            
            print("for i in G")
            for i in range(len(G)):
                print(G[i])
            print("------")

            print("for g in G")
            for g in G:
                print(g)
            print("------")
            
        # finish(0)
        
        # Solve the system using the Gröbner basis
        # solutions = sp.solve_poly_system(G, vars)
        solutions = sp.solve(G, dict=True)        
        if (solutions is None) or (len(solutions) == 0):
            fatal("No solution found!")

        do_print_sol = True
        if do_print_sol:
            # Print the solutions        
            o(f"solutions: {len(solutions)}")
            sno = 0
            for sol in solutions:
                sno += 1
                o(f"{sno}: {sol}")
                if self.is_valid_Brent_solution(sol, sno):
                    o("ok!")
                else:
                    o("invalid!")

        solution = self.get_best_solution(solutions)
        
        self.copy_lifted_solution(solution)
                
        o("Groebner lifting complete")
        return 0

    def Poly(self, expr, vars) -> sp.Poly:
        return sp.Poly.from_expr(expr, vars, domain='ZZ')
    
    def Symbol(self, name) -> sp.Symbol:
        if self.use_zero_one:
            return sp.Symbol(name, integer=True, negative=False)
        else:
            return sp.Symbol(name, integer=True, nonzero=True)
    
    def copy_lifted_solution(self, solution) -> None:
        o(self.variables)
        self.lifted = self.bini.copy()
        self.lifted.mod2_mode = False

        dim = self.mmDim
        ll = self.lifted
        for product in dim.Products:
            arr = ll.alpha
            for row, col in dim.AIndices:
                arr[row][col][product] = self.Fmod(row, col, product, solution)
            arr = ll.beta
            for row, col in dim.BIndices:
                arr[row][col][product] = self.Gmod(row, col, product, solution)
            arr = ll.gamma
            for row, col in dim.CIndices:
                arr[row][col][product] = self.Dmod(row, col, product, solution)
            
        o("alpha")
        o(self.bini.alpha)
        o(self.lifted.alpha)
        o("beta")
        o(self.bini.beta)
        o(self.lifted.beta)
        o("gamma")
        o(self.bini.gamma)
        o(self.lifted.gamma)
        
        self.lifted.validate()
        
    def get_best_solution(self, solutions):
        best = None
        best_cnt = len(solutions[0]) + 1
        best_sno = None
        sno = 0
        
        for sol in solutions:
            sno += 1
            if self.is_valid_Brent_solution(sol, sno):
                if self.use_zero_one:
                    cnt = np.count_nonzero(sol)
                else:
                    cnt = len([1 for i in sol if i < 0])
                if cnt < best_cnt:
                    best = sol
                    best_cnt = cnt
                    best_sno = sno
                if best_cnt < 1:
                    """ we can't get any better """
                    break
                    
        if not (best is None):
            if self.use_zero_one:
                o(f"Solution {best_sno} is the best with {best_cnt} non-zero elements")
            else:
                o(f"Solution {best_sno} is the best with {best_cnt} negative elements")
            o(best)
        else:
            o("No valid solutions found!")
            fatal("oops!")
        return best

    def is_valid_Brent_solution(self, solution, sno) -> bool:
        dim = self.mmDim
        cnt_err = 0
        for a_row, a_col in dim.AIndices:
            for b_row, b_col in dim.BIndices:
                for c_row, c_col in dim.CIndices:
                    sum = 0
                    for product in dim.Products:
                        f = self.Fmod(a_row, a_col, product, solution)
                        g = self.Gmod(b_row, b_col, product, solution)
                        d = self.Dmod(c_row, c_col, product, solution)
                        sum += f*g*d
                    expected = 1 if (a_row == c_row) and (a_col == b_row) and (b_col == c_col) else 0
                    if sum != expected:
                        cnt_err += 1
                        if cnt_err == 1:
                            o(f"Bummer! Solution {sno:-4} sum is: {sum:2}  expected: {expected}")
        return cnt_err == 0
                                                                    
    def literal(self, name: str, row: int, col: int, product: int) -> str:
        return f"{name}{row+1}{col+1}_{str(product+1).zfill(self.product_index_digits)}"
    
    def variable(self, name: str, row: int, col: int, product: int, 
                 val: int) -> sp.Symbol:
        lit = self.literal(name, row, col, product)
        if lit not in self.variables:
            ll = self.Symbol(lit)
            self.var_values[lit] = val
            self.variables[lit] = ll
        else:
            """ re-use existing literal """
            ll = self.variables[lit]
        return ll
    

    def variable_value(self, name: str, row: int, col: int, product: int, solution):
        lit = self.literal(name, row, col, product)
        id = self.var_id[lit]
        return solution[id]
    
            
