
 # akTensorMod2 -  Convert a Matrix Multiplication tensor file to a tensor file with Z2 coefficients
 
 Matrix multiplication algorithms in tensor format contain one line per product.
 Each of the lines is a product of three sums.
 
 Example:
 
```
 (a12 + a21)*(b11 - b22)*(c12 - c21)
```

Note that matrix $\[c\]$ is referenced in transposed order (columns and rows interchanged).

```
 usage of akTensorMod2:
 
 python akTensorMod2.py <tensor.file> <tensor.mod2.file>
 ```
 
 The transformation between a general tensor file and a tensor.mod2 file is done by
 replacing all coefficients $c$ by their $c \,\text{mod}\, 2$ counterparts. Summands with even-valued coefficients
 are removed. Summands with odd-valued coefficients are kept with coefficient $1$.
 
 