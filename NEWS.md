# Change Log

## 01-Jun-2025 Type checking

Python type checking in akDumpNumpyTensor to increase code quality

## 29-May-2025 eXmm (former akExtractMatMultSolution)

eXmm was extended to process complex coefficients.
A recursive descent parser was implemented to translate expressions
into abstract syntax trees (AST).

## 24-May-2025 AlphaEvolve solutions

[AlphaEvolve solutions](https://github.com/google-deepmind/alphaevolve_results) were converted and analyzed with akDumpNumpyTensor

akDumpNumpyTensor was extended. <br/>A MatMultTensor class was introduced to cleanup the code.

Solutions validated against Brent Equations.

Some of the solutions have complex, float or extended coefficient sets, <br/>not just $\lbrace -1, +1 \rbrace$

<4x5x6_90> solution of [AlphaEvolve](https://github.com/google-deepmind/alphaevolve_results) was re-lifted from $\lbrace -2, -1, +1, +2 \rbrace$ to $\lbrace -1, +1 \rbrace$

[Paper preprint](https://github.com/a1880/matrix-multiplication/blob/master/papers/Kemper%20-%202025%20-%20From%20F2%20to%20Z%20Solutions%20for%20Brent%20Equations.pdf) was updated accordingly.

Fixed conversion yacas/tensor in akExtractMatMultSolution

## 17-May-2025 Solution statistics

Added statistical outputs to akExtractMatMultSolutions

Added statistics to schemes for <5x5x5_93>

## 16-May-2025 Additional scheme <4x4x4_48>

Added [AlphaEvolve](https://github.com/google-deepmind/alphaevolve_results) discovery of <4x4x4_48> to the paper

Improved akDumpNumpyTensor to validate algorithms and handle complex coefficients

## 15-May-2025 Initial Commit

Repository created

Added [AlphaEvolve](https://github.com/google-deepmind/alphaevolve_results) discovery of <5x5x5_93> to the paper
