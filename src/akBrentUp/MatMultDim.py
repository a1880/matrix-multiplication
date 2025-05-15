"""  Matrix Multiplication Problem Dimension """


class MatMultDim:
    def __init__(self, aRows: int, aCols: int, bCols: int, noOfProducts: int):
        self._aRows = aRows
        self._aCols = aCols
        self._bCols = bCols
        self._noOfProducts = noOfProducts
        self._Products = range(noOfProducts)
        self._Rows = [aRows, aCols, aRows]
        self._Cols = [aCols, bCols, bCols]
        print(f"Matrix multiplication {self.toString}")

    @classmethod
    def from_problem(cls, problem: str):
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

        mmDim: MatMultDim = cls(aRows, aCols, bCols, noOfProducts)

        return mmDim

    def __str__(self):
        return self.toString

    @property
    def toString(self) -> str:
        return f"{self.aRows}x{self.aCols}x{self.bCols}_{self.noOfProducts:0{2}}"

    @staticmethod
    def indices(rows, cols) -> list:
        return [(row, col) for row in range(rows) for col in range(cols)]

    def indices_fgd(self, fgd: int) -> list:
        return MatMultDim.indices(self._Rows[fgd], self._Cols[fgd])
    
    @property
    def aRows(self) -> int:
        return self._aRows

    @property
    def AIndices(self) -> list:
        return MatMultDim.indices(self._aRows, self._aCols)

    @property
    def BIndices(self) -> list:
        return MatMultDim.indices(self.bRows, self._bCols)

    @property
    def CIndices(self) -> list:
        return MatMultDim.indices(self.cRows, self.cCols)

    @property
    def aCols(self) -> int:
        return self._aCols

    @property
    def aElements(self) -> int:
        return self.aRows * self.aCols

    @property
    def bRows(self) -> int:
        """ bRows is defined as aCols """
        return self.aCols

    @property
    def bCols(self) -> int:
        return self._bCols

    @property
    def bElements(self) -> int:
        return self.bRows * self.bCols

    def Cols(self, fgd) -> list:
        return self._Cols[fgd]

    @property
    def cRows(self) -> int:
        """ cRows is defined as aRows """
        return self.aRows

    @property
    def cCols(self) -> int:
        """ cCols is defined as bCols """
        return self.bCols

    @property
    def cElements(self) -> int:
        return self.cRows * self.cCols

    @property
    def MatF(self) -> int:
        return 0
    
    @property
    def MatG(self) -> int:
        return 1

    @property
    def MatD(self) -> int:
        return 2
    
    @property
    def FGD(self) -> list[int]:
        return [self.MatF, self.MatG, self.MatD]

    @property
    def FGD_names(self) -> list[str]:
        return ["f", "g", "d"]

    @property
    def noOfProducts(self) -> int:
        return self._noOfProducts

    @property
    def Products(self) -> list[int]:
        return self._Products

    def Rows(self, fgd) -> list[int]:
        return self._Rows[fgd]
    
