# Matrix Multiplication

This repository is devoted to fast matrix multiplication.
"fast" means that the number of elementary products is smaller than in the classic
schoolbook method.

## Solution Schemes

The solutions provided include:

$N \times M \times P$ \_ $r$ stands for multiplication of an $N \times M$
matrix times an $M \times P$ matrix, using $r$ elementary products.

| Solution  | Remarks                                    |
| --------- | ------------------------------------------ |
| 2x2x2_07  | small example á la [Strassen][1]           |
| 2x3x2_11  | small example                              |
| 4x4x4_47  | the [Fawzi][2] solution for $\mathbb{Z_2}$ |
| 4x5x6_90  | solution of [Kauers+Wood][3] re-lifted     |
| 5x5x5_93  | solution of [Moosbauer+Poole][4] re-lifted |
| 4x6x6_106 | solution of [Kauers+Wood][3] re-lifted     |
| 5x5x6_110 | solution of [Kauers+Wood][3] re-lifted     |
| 5x5x7_127 | solution of [Kauers+Wood][3] re-lifted     |
| 5x6x6_130 | solution of [Kauers+Wood][3] re-lifted     |
| 5x6x7_150 | solution of [Kauers+Wood][3] re-lifted     |
| 6x6x6_153 | solution of [Moosbauer+Poole][4] re-lifted |

The following solutions of [DeepMind/AlphaEvolve][5] were extracted, converted and analyzed.

| Solution | Coefficients in solution                                      |
| -------- | ------------------------------------------------------------- |
| 3x3x3_23 | $\lbrace -1, +1\rbrace$                                       |
| 2x4x5_32 | float coefficients $\lbrace -2, -1, -0.5, 0.5, +1, +2\rbrace$ |
| 2x4x7_45 | $\lbrace -1, +1\rbrace$                                       |
| 2x5x6_47 | $\lbrace -1, +1\rbrace$                                       |
| 4x4x4_48 | complex coefficients $\mathbb{Z}_{C0.5}$                      |
| 2x4x8_51 | $\lbrace -1, +1\rbrace$                                       |
| 3x4x6_54 | float coefficients $\lbrace -1, -0.5, +0.5, +1, +2\rbrace$    |
| 4x4x5_61 | $\lbrace -1, +1\rbrace$                                       |
| 3x4x7_63 | complex coefficients $\mathbb{Z}_{C0.5}$                      |
| 3x5x6_68 | $\lbrace -2, -1, +1, +2\rbrace$                               |
| 3x4x8_74 | $\lbrace -1, +1\rbrace$                                       |
| 3x5x7_80 | $\lbrace -2, -1, +1, +2\rbrace$                               |
| 4x5x6_90 | $\lbrace -2, -1, +1, +2\rbrace$ <br/>relifted to { -1, +1 }   |
| 5x5x5_93 | $\lbrace -1, +1\rbrace$                                       |

## Papers

Three non-published papers are provided:

| Paper                                                                                                                                                                   | Remarks                                                                                                          |
| ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| [Verfahren von Makarov zur Multiplikation von 5x5 Matrizen](papers/Kemper%20-%202012%20-%20Verfahren%20von%20Makarov%20zur%20Multiplikation%20von%205x5%20Matrizen.pdf) | 2012: Description of Makarov's solution for <5x5x5_100> (paper in German)                                        |
| [Fast Matrix Multiplication Attempts](papers/Kemper%20-%202017%20-%20Fast%20Matrix%20Multiplication%20Attempts.pdf)                                                     | 2017: 13 slides                                                                                                  |
| [From F2 to Z Solutions for Brent Equations](papers/Kemper%20-%202025%20-%20From%20F2%20to%20Z%20Solutions%20for%20Brent%20Equations.pdf)                               | 2025: Description of the lifting method to derive a general multiplication algorithm from a modulo $2$ algorithm |

Feedback and questions/suggestions are welcome!</br>
Contact: axel.kemper at Google's mail site

## Tools

The following tools are provided "as-is". </br>
Don't expect production quality.

| Tool                                                     | Remarks                                                                           |
| -------------------------------------------------------- | --------------------------------------------------------------------------------- |
| [akBrentUp](src/akBrentUp)                               | Python tool to lift a matrix multiplication algorithm                             |
| [akBrentWithSbr](src/akBrentWithSbr)                     | Python tool to solve Brent Equations modulo 2 with symmetry breaking              |
| [akDumpNumpyTensor](src/akDumpNumpyTensor)               | Python tool to write AlphaEvolve solution to our tensor format                    |
| [akExtractMatMultSolution](src/akExtractMatMultSolution) | C# tool to convert between various formats of matrix multiplication algorithms    |
| [akTensorMod2](src/akTensorMod2)                         | Python tool to down-convert general multiplication algorithm to its modulo 2 form |
| [akYacasChecker](src/akYacasChecker)                     | C# tool to check the correctness of matrix multiplication algorithms in Yacas form |

[1]: https://gdz.sub.uni-goettingen.de/id/PPN362160546_0013?tify=%7B%22view%22:%22info%22,%22pages%22:%5B358%5D%7D
[2]: https://www.nature.com/articles/s41586-022-05172-4
[3]: https://arxiv.org/abs/2505.05896
[4]: https://arxiv.org/abs/2502.04514
[5]: https://storage.googleapis.com/deepmind-media/DeepMind.com/Blog/alphaevolve-a-gemini-powered-coding-agent-for-designing-advanced-algorithms/AlphaEvolve.pdf
