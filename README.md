
# Matrix Multiplication

This repository is devoted to fast matrix multiplication.
"fast" means that the number of elementary products is smaller than in the classic
schoolbook method.

## Solution Schemes

The solutions provided include:

$N \times M \times P$ \_ $r$ stands for multiplication of an $N \times M$
matrix times an $M \times P$ matrix, using $r$ elementary products.

| Solution | Remarks |
| -------- | ------- |
| 2x2x2_07 | small example á la [Strassen][1] |
| 4x4x4_47 | the [Fawzi][2] solution for Z2 |
| 4x6x6_106 | solution of [Kauers+Wood][3] re-lifted |
| 5x5x6_110 | solution of [Kauers+Wood][3] re-lifted |
| 5x6x6_130 | solution of [Kauers+Wood][3] re-lifted |
| 6x6x6_153 | solution of [Moosbauer+Poole][4] re-lifted |
| 2x3x2_11 | small example |
| 4x5x6_90 | solution of [Kauers+Wood][3] re-lifted |
| 5x5x5_93 | solution of [Moosbauer+Poole][4] re-lifted|
| 5x5x7_127 | solution of [Kauers+Wood][3] re-lifted |
| 5x6x7_150 | solution of [Kauers+Wood][3] re-lifted |

## Papers

Three non-published papers are provided:

| Paper | Remarks |
| ----- | ------- |
| [Verfahren von Makarov zur Multiplikation von 5x5 Matrizen](papers/Kemper%20-%202012%20-%20Verfahren%20von%20Makarov%20zur%20Multiplikation%20von%205x5%20Matrizen.pdf) | 2012: Description of Makarov's solution for <5x5x5_100> (paper in German) |
| [Fast Matrix Multiplication Attempts](papers/Kemper%20-%202017%20-%20Fast%20Matrix%20Multiplication%20Attempts.pdf) | 2017: 13 slides |
| [From F2 to Z Solutions for Brent Equations](papers/Kemper%20-%202025%20-%20From%20F2%20to%20Z%20Solutions%20for%20Brent%20Equations.pdf) | 2025: Description of the lifting method to derive a general multiplication algorithm from a modulo $2$ algorithm |

Feedback and questions/suggestions are welcome!

## Tools

The following tools are provided "as-is". <br>
Don't expect production quality.

| Tool | Remarks |
| ---- | ------- |
| [akBrentUp](src/akBrentUp) | Python tool to lift a matrix multiplication algorithm |
| [akBrentWithSbr](src/akBrentWithSbr) | Python tool to solve Brent Equations<br>modulo 2 with symmetry breaking |
| [akExtractMatMultSolution](src/akExtractMatMultSolution) | c# tool to convert between<br>various formats of matrix multiplication algorithms |

[1]: https://gdz.sub.uni-goettingen.de/id/PPN362160546_0013?tify=%7B%22view%22:%22info%22,%22pages%22:%5B358%5D%7D
[2]: https://www.nature.com/articles/s41586-022-05172-4
[3]: https://arxiv.org/abs/2505.05896
[4]: https://arxiv.org/abs/2502.04514
