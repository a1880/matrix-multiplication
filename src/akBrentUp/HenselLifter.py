"""
HenselLifter.py  -  module to implement Hensel lifting for solving
                    Brent equations
"""

from BiniScheme import BiniScheme
from MatMultDim import MatMultDim
import numpy as np
from util import fatal, o, print_array


class HenselLifter:
    def __init__(self, mmDim: MatMultDim, bini: BiniScheme):
        self.mmDim = mmDim
        self.bini = bini
        self.lifted = None
        self.varIdx = 0
        self.variables: dict[str, int] = {}
        self.var_values: dict[int, int] = {}
        self.product_index_digits = len(str(mmDim.noOfProducts))
        
    def D(self, row, col, product):
        return self.bini.c(row, col, product)

    def Dmod(self, row, col, product, solution):
        d = self.bini.c(row, col, product)
        v = self.variable_value("d", row, col, product, solution)
        return d * (1 - 2*v)
    
    def F(self, row, col, product):
        return self.bini.a(row, col, product)

    def Fmod(self, row, col, product, solution):
        f = self.bini.a(row, col, product)
        v = self.variable_value("f", row, col, product, solution)
        return f * (1 - 2*v)

    def G(self, row, col, product):
        return self.bini.b(row, col, product)
    
    def Gmod(self, row, col, product, solution):
        g = self.bini.b(row, col, product)
        v = self.variable_value("g", row, col, product, solution)
        return g * (1 - 2*v)

    def lift(self):
        """ local variable to reduce typing """
        dim = self.mmDim
        o("Starting Hensel lifting")
        o(dim.toString)
        
        m = []
        
        """ construct matrix of system of linear equations """
        for a_row, a_col in dim.AIndices:
            for b_row, b_col in dim.BIndices:
                for c_row, c_col in dim.CIndices:
                    line = set()
                    for product in dim.Products:
                        f = self.F(a_row, a_col, product)
                        g = self.G(b_row, b_col, product)
                        d = self.D(c_row, c_col, product)
                        if g*d != 0:
                            v = self.variable("f", a_row, a_col, product, f)
                            line.add(v)
                        if f*d != 0:
                            v = self.variable("g", b_row, b_col, product, g)
                            line.add(v)
                        if f*g != 0:
                            v = self.variable("d", c_row, c_col, product, d)
                            line.add(v)
                    if len(line) > 0:
                        m.append(line)
        o(f"varIdx {self.varIdx}")
        o(f"len(m) {len(m)}")
        
        arr = np.full((len(m), self.varIdx), 0)
        row = 0
        for line in m:
            for col in range(self.varIdx):
                if col in line:
                    arr[row, col] = 1
            row += 1

        solutions = self.solve_homogeneous_mod2(arr)
            
        sol = self.get_best_solution(solutions)
        print_array(sol)

        self.lifted = self.bini.copy()
        self.lifted.mod2_mode = False

        for product in dim.Products:
            for a_row, a_col in dim.AIndices:
                self.lifted.alpha[a_row][a_col][product] = \
                    self.Fmod(a_row, a_col, product, sol)
            for b_row, b_col in dim.BIndices:
                self.lifted.beta[b_row][b_col][product] = \
                    self.Gmod(b_row, b_col, product, sol)
            for c_row, c_col in dim.CIndices:
                self.lifted.gamma[c_row][c_col][product] = \
                    self.Dmod(c_row, c_col, product, sol)

        self.lifted.validate()
                
        o("Hensel lifting complete")
        return 0

    def get_best_solution(self, solutions):
        best = None
        best_cnt = len(solutions[0]) + 1
        best_sno = None
        sno = 0
        #  try all single solutions
        for sol in solutions:
            sno += 1
            if self.is_valid_Brent_solution(sol, sno):
                cnt = np.count_nonzero(sol)
                if cnt < best_cnt:
                    best = sol
                    best_cnt = cnt
                    best_sno = sno
                    
        if best is None:
            #  try to combine two solutions
            i_max = len(solutions)
            for i in range(i_max):
                sol_i = solutions[i]
                for j in range(i + 1, i_max):
                    sol = sol_i ^ solutions[j]
                    sno += 1
                    if self.is_valid_Brent_solution(sol, sno):
                        cnt = np.count_nonzero(sol)
                        if cnt < best_cnt:
                            best = sol
                            best_cnt = cnt
                            best_sno = sno

        if best is None:
            #  try to combine four solutions
            i_max = len(solutions)
            for i in range(i_max):
                sol_i = solutions[i]
                print(f"{sno}")
                for j in range(i + 1, i_max):
                    sol_i_j = sol_i ^ solutions[j]
                    for k in range(j + 1, i_max):
                        sol_i_j_k = sol_i_j ^ solutions[k]
                        for L in range(k + 1, i_max):
                            sol = sol_i_j_k ^ solutions[L]
                            sno += 1
                            if self.is_valid_Brent_solution(sol, sno):
                                cnt = np.count_nonzero(sol)
                                if cnt < best_cnt:
                                    best = sol
                                    best_cnt = cnt
                                    best_sno = sno

        if not (best is None):
            o(f"Solution {sno} is the best with {best_cnt} non-zero elements")
        else:
            o("No valid solutions found!")
            fatal("oops!")
        return best

    def is_valid_Brent_solution(self, solution, sno):
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
                    odd = (a_row == c_row) and (a_col == b_row) and (b_col == c_col)
                    if odd:
                        if sum != 1:
                            cnt_err += 1
                            if cnt_err == 0:
                                o(f"Bummer! Solution {sno:-4} sum is: {sum:2}  expected: {1}")
                    else:
                        if sum != 0:
                            cnt_err += 1
                            if cnt_err == 0:
                                o(f"Bummer! Solution {sno:-4} sum is: {sum:2}  expected: {0}")
        if cnt_err == 0:
            o("Correct solution for Brent equations!")
        return cnt_err == 0
                                                            
    def is_valid_solution(self, A, sol):
        rows, cols = A.shape
        
        for row in range(rows):
            sum = 0
            for col in range(cols):
                sum += A[row, col] * sol[col]
            if sum % 2 != 0:
                # print_array([A[row, i] for i in range(cols)])
                # print(f"Invalid row {row}  sum {sum}")
                return False
        return True
        
    def solve_homogeneous_mod2(self, A):
        """Solves A·x ≡ 0 mod 2 for non-trivial solutions.
        Args:
            A: 2D numpy array (m x n) representing coefficients mod 2
        Returns:
            List of solution basis vectors (as numpy arrays)
            Empty list if only trivial solution exists
        """
        m, n = A.shape
        # Convert to row-echelon form mod 2
        rank = 0
        for col in range(n):
            # Find pivot row
            pivot = -1
            for row in range(rank, m):
                if A[row, col]:
                    pivot = row
                    break
            if pivot == -1:
                continue
            # Swap rows if needed
            if pivot != rank:
                A[[rank, pivot]] = A[[pivot, rank]]
            # Eliminate other rows
            for row in range(m):
                if row != rank and A[row, col]:
                    A[row] = (A[row] + A[rank]) % 2
            rank += 1
            if rank == m:
                break
        # Find free variables and construct basis
        pivot_cols = [np.where(A[r] == 1)[0][0] for r in range(rank)]
        free_cols = sorted(set(range(n)) - set(pivot_cols))
        basis = []
        for free in free_cols:
            vec = np.zeros(n, dtype=int)
            vec[free] = 1
            for r in range(rank):
                pivot_col = pivot_cols[r]
                vec[pivot_col] = (vec[pivot_col] + A[r, free]) % 2
            basis.append(vec)
        return basis              

    def literal(self, name, row, col, product):
        return f"{name}{row+1}{col+1}_{str(product+1).zfill(self.product_index_digits)}"
    
    def variable(self, name, row, col, product, val):
        lit = self.literal(name, row, col, product)
        if lit not in self.variables:
            ll = self.varIdx
            self.variables[lit] = ll
            self.var_values[ll] = val
            self.varIdx += 1
        else:
            """ re-use existing literal """
            ll = self.variables[lit]
        return ll
    
    def variable_id(self, name, row, col, product):
        lit = self.literal(name, row, col, product)
        if lit in self.variables:
            ll = self.variables[lit]
            return ll
        else:
            fatal("oops!")

    def variable_value(self, name, row, col, product, solution):
        id = self.variable_id(name, row, col, product)
        return solution[id]
    
            