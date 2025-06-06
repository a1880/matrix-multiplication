"""

MatMultTensor.py  -  wrapper around AlphaEvolve tensor decompositions

Axel Kemper  23-May-20205

"""
import importlib
from math import floor
from typing import Any
import numpy as np
from numpy._typing import NDArray

class MatMultTensor:
    """
    MatMultTensor provides the numpy arrays published by AlphaEvolve
    They reside in external Python files which are dynamically imported.
    """

    def __init__(self, signature: str):
        """ signature show, which decomposition file to import """
        self.check(len(signature) == 3, "Inconistent signature length")
        self.check(signature.isdigit(), "Inconistent signature type")
        self.file_name = f"decomposition_{signature}"
        decomposition = importlib.import_module(self.file_name)
        self.check(self.file_name == decomposition.__name__, "Inconsistent decomposition name")

        """ once, we have the docomposition module, we have the tuple of 3 np.array objects """
        self._data: tuple[NDArray[Any], NDArray[Any], NDArray[Any]] = getattr(decomposition, self.file_name)
        self.check(len(self._data) == 3, "Unexpected tuple len != 3")
        self._a: NDArray[Any] = self._data[0]
        self._b: NDArray[Any] = self._data[1]
        self._c: NDArray[Any] = self._data[2]

        """ the matrix dimensions are encoded in the problem signature """
        self.aRows = int(signature[0:1])
        self.aCols = int(signature[1:2])
        self.bRows = self.aCols
        self.bCols = int(signature[2:3])
        self.cRows = self.aRows
        self.cCols = self.bCols

        """ second dimension of np.array objects is the number of projects """
        self.noOfProducts = len(self._a[0])

        """ fgd lookup of matrix dimensions """
        self._Rows = [self.aRows, self.aCols, self.aRows]
        self._Cols = [self.aCols, self.bCols, self.bCols]

        """ for compact loops we have pairs of indices as tuples """
        self._indices_fgd = [self.indices(self.Rows(fgd), self.Cols(fgd)) for fgd in self.FGD]

        """ we only evaluate the data type at the beginning, and based on a[]
            This need to be changed, if tensor data should become mutable  """
        self._data_type = self.get_data_type()

        """ save our souls """
        self.validate()

    def a(self, row: int, col: int, product: int):
        return self.data(self.F, row, col, product);

    def b(self, row: int, col: int, product: int):
        return self.data(self.G, row, col, product);

    def c(self, row: int, col: int, product: int):
        return self.data(self.D, row, col, product);

    def data(self, fgd: int, row:int, col:int, product: int):
        rows = self.Rows(fgd)
        cols = self.Cols(fgd)
        self.check(row < rows, "Inconsistent row")
        self.check(col < cols, "Inconsistent col")
        self.check(product < self.noOfProducts, "Inconsistent product")
        if fgd != self.D:
            idx = row * cols  + col
        else:
            idx = col * rows  + row
        return self._data[fgd][idx, product]

    @property
    def data_type(self):
        return self._data_type

    @property
    def AIndices(self) -> list[tuple[int, int]]:
        return self._indices_fgd[self.F]

    @property
    def BIndices(self) -> list[tuple[int, int]]:
        return self._indices_fgd[self.G]

    def check(self, condition: bool, msg: str):
        if not condition:
            self.o("Condition violated:")
            raise RuntimeError(msg)

    @property
    def CIndices(self) -> list[tuple[int, int]]:
        return self._indices_fgd[self.D]

    def Cols(self, fgd: int) -> int:
        return self._Cols[fgd]

    @property
    def D(self) -> int:
        return 2

    @property
    def F(self) -> int:
        return 0
    
    @property
    def FGD(self) -> list[int]:
        return [self.F, self.G, self.D]

    @property
    def ABC_names(self) -> list[str]:
        return ["a", "b", "c"]

    @property
    def FGD_names(self) -> list[str]:
        return ["f", "g", "d"]

    @property
    def G(self) -> int:
        return 1

    def get_coefficients(self) -> list[Any]:
        c: set[Any] = set()
        for fgd in self.FGD:
            for row, col in self.indices_fgd(fgd):
                for k in self.Products:
                    v = self.data(fgd, row, col, k)
                    if v != 0:
                        c.add(v)

        if self.data_type in {np.complex64, np.complex128}:
            a = list(c)
            a = np.sort_complex(a)
        else:
            a = sorted(c)

        return a # type: ignore

    def get_data_type(self):
        if self._a.dtype == np.complex64:
            return np.complex64
        if self._a.dtype == np.complex128:
            return np.complex128
        if self._a.dtype == np.float32:
            for row, col in self.AIndices:
                for k in self.Products:
                    v = self.a(row, col, k)
                    if floor(v) != v:
                        return np.float32
            return np.int32
        raise RuntimeError("data type unexpected")

    @staticmethod
    def indices(rows: int, cols: int) -> list[tuple[int, int]]:
        return [(row, col) for row in range(rows) for col in range(cols)]

    def indices_fgd(self, fgd: int) -> list[tuple[int, int]]:
        return self._indices_fgd[fgd]

    def o(self, s: str):
        print(s)

    @property
    def problem(self) -> str:
        return f"{self.aRows}x{self.aCols}x{self.bCols}_{self.noOfProducts}"

    @property
    def Products(self) -> range:
        return range(self.noOfProducts)

    def Rows(self, fgd: int) -> int:
        return self._Rows[fgd]
    
    def validate(self):
        self.check(len(self._a) == self.aRows * self.aCols, "Inconsistent len(_a)")
        self.check(len(self._b[0]) == self.noOfProducts, "Inconsisten len(_b[0])")
        self.check(len(self._c[0]) == self.noOfProducts, "Inconsisten len(_c[0])")


    