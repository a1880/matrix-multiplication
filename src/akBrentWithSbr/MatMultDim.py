from Array2D import Array2D

"""  Matrix Multiplication Problem Dimension """


class MatMultDim:
    def __init__(self, aRows, aCols, bCols, noOfProducts):
        self._aRows = aRows
        self._aCols = aCols
        self._bCols = bCols
        self._noOfProducts = noOfProducts
        self._Products = range(noOfProducts)
        print(f"Matrix multiplication {self.toString}")

    @classmethod
    def from_problem(cls, problem):
        dim = None
        a = problem.split("_")
        if len(a) != 2:
            raise ValueError(f"Problem has {len(a)} rather than 2 parts: {problem}")

        noOfProducts = int(a[1])
        a = a[0].split("x")
        if len(a) != 3:
            raise ValueError(
                f"Problem has {len(a)} rather than 3 dimensions: {problem}"
            )

        aRows, aCols, bCols = int(a[0]), int(a[1]), int(a[2])

        dim = cls(aRows, aCols, bCols, noOfProducts)

        return dim

    def __str__(self):
        return self.toString

    @property
    def toString(self):
        return f"{self.aRows}x{self.aCols}x{self.bCols}_{self.noOfProducts:0{2}}"

    @staticmethod
    def indices(rows, cols):
        return [(row, col) for row in range(rows) for col in range(cols)]

    @property
    def aRows(self):
        return self._aRows

    @property
    def AIndices(self):
        return MatMultDim.indices(self._aRows, self._aCols)

    @property
    def BIndices(self):
        return MatMultDim.indices(self.bRows, self._bCols)

    @property
    def CIndices(self):
        return MatMultDim.indices(self.cRows, self.cCols)

    @property
    def aCols(self):
        return self._aCols

    @property
    def aElements(self):
        return self.aRows * self.aCols

    @property
    def bRows(self):
        return self.aCols

    @property
    def bCols(self):
        return self._bCols

    @property
    def bElements(self):
        return self.bRows * self.bCols

    @property
    def cRows(self):
        return self.aRows

    @property
    def cCols(self):
        return self.bCols

    @property
    def cElements(self):
        return self.cRows * self.cCols

    @property
    def noOfProducts(self):
        return self._noOfProducts

    @property
    def Products(self):
        return self._Products

    @property
    def validOperands(self):
        """construct 2D lookup array to filter valid multiplication operands"""
        arr = [[int(ca == rb) for rb, cb in self.BIndices] for ra, ca in self.AIndices]

        return Array2D.from_array(arr)
