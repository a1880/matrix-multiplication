# akYacasChecker -  Validate correctness of Matrix Multiplication algorithms in Yacas form

 Matrix multiplication algorithms in Yacas format contain one line per product.
 Each of the lines is a product of two sums.

 Example:

```
 p01 := (a12 + a21)*(b11 - b22);
```

Further down, for every element of matrix \[c\], a sum of products is written. The products may have factors and/or signs.

```
c11 := P2 + P5;
c12 := P2 - P3 + P4 + P6;
c21 := P1 + P3 + P5 + P7;
c22 := P1 + P6;
```

```
Simplify(c11 - (a11*b11 + a12*b21));
Simplify(c12 - (a11*b12 + a12*b22));
Simplify(c21 - (a21*b11 + a22*b21));
Simplify(c22 - (a21*b12 + a22*b22));
```

The `Simplify()` lines compare the resulting values against the values obtained by using the schoolbook method. The checker actually determines, if all `Simplify()` expressions in fact result in zero values. Non-zero values indicate errors in the algorithm.

The check involves parsing all expressions and translating them to abstract syntax trees (AST). The parsed expression trees - with their potentially complex factors - are then expanded to get sums / differences of coefficients. For all coefficients, the resulting sum must be zero. Otherwise, the algorithm is not correct.