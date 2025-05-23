# Solution Schemes

The solutions provided include:

$N \times M \times P$ \_ $r$ stands for multiplication of an $N \times M$
matrix times an $M \times P$ matrix, using $r$ elementary products.

| Solution               | Remarks                                                                    |
| ---------------------- | -------------------------------------------------------------------------- |
| [2x2x2_07](2x2x2_07)   | small example á la [Strassen][1]                                           |
| [2x3x2_11](2x3x2_11)   | small example                                                              |
| [3x3x3_23](3x3x3_23)   | [DeepMind/AlphaEvolve][5] coefficients $\lbrace -1, +1\rbrace$             |
| [2x4x5_32](2x4x5_32)   | [DeepMind/AlphaEvolve][5] coefficients $\lbrace -2, -1, -0.5, 0.5, 1, 2\rbrace$|
| [2x4x7_45](2x4x7_45)   | [DeepMind/AlphaEvolve][5] coefficients $\lbrace -1, +1\rbrace$             |
| [2x5x6_47](2x5x6_47)   | [DeepMind/AlphaEvolve][5] coefficients $\lbrace -1, +1\rbrace$             |
| [4x4x4_47](4x4x4_47)   | [Fawzi][2] solution for $\mathbb{Z}_2$                                     |
| [4x4x4_48](4x4x4_48)   | [DeepMind/AlphaEvolve][5] for complex coefficients $\mathbb{Z}_{C0.5}$     |
| [2x4x8_51](2x4x8_51)   | [DeepMind/AlphaEvolve][5] coefficients $\lbrace -1, +1\rbrace$             |
| [3x4x6_54](3x4x6_54)   | [DeepMind/AlphaEvolve][5] coefficients $\lbrace -1, -0.5, 0.5, 1, 2\rbrace$|
| [4x4x5_61](4x4x5_61)   | [DeepMind/AlphaEvolve][5] coefficients $\lbrace -1, +1\rbrace$             |
| [3x4x7_63](3x4x7_63)   | [DeepMind/AlphaEvolve][5] for complex coefficients $\mathbb{Z}_{C0.5}$     |
| [3x5x6_68](3x5x6_68)   | [DeepMind/AlphaEvolve][5] coefficients $\lbrace -2, -1, 1, 2\rbrace$       |
| [3x4x8_74](3x4x8_74)   | [DeepMind/AlphaEvolve][5] coefficients $\lbrace -1, +1\rbrace$             |
| [3x5x7_80](3x5x7_80)   | [DeepMind/AlphaEvolve][5] coefficients $\lbrace -2, -1, 1, 2\rbrace$       |
| [4x5x6_90](4x5x6_90)   | solution of [Kauers+Wood][3] re-lifted                                     |
| [4x5x6_90](4x5x6_90b)  | [DeepMind/AlphaEvolve][5] coefficients $\lbrace -2, -1, 1, 2\rbrace        |
| [5x5x5_93](5x5x5_93)   | solution of [Moosbauer+Poole][4] lifted                                    |
| [5x5x5_93](5x5x5_93b)  | solution of [DeepMind/AlphaEvolve][5]                                      |
| [4x6x6_106](4x6x6_106) | solution of [Kauers+Wood][3] re-lifted                                     |
| [5x5x6_110](5x5x6_110) | solution of [Kauers+Wood][3] re-lifted                                     |
| [5x5x7_127](5x5x7_127) | solution of [Kauers+Wood][3] re-lifted                                     |
| [5x6x6_130](5x6x6_130) | solution of [Kauers+Wood][3] re-lifted                                     |
| [5x6x7_150](5x6x7_150) | solution of [Kauers+Wood][3] re-lifted                                     |
| [6x6x6_153](6x6x6_153) | solution of [Moosbauer+Poole][4] lifted                                    |

lifted = modulo $2$ solution was lifted to $\mathbb{Z}$ with coefficients in $\lbrace -1, 0, +1 \rbrace$

re-lifted = general solution was lowered to F<sub>2</sub> and then lifted to $\mathbb{Z}$

[1]: https://gdz.sub.uni-goettingen.de/id/PPN362160546_0013?tify=%7B%22view%22:%22info%22,%22pages%22:%5B358%5D%7D
[2]: https://www.nature.com/articles/s41586-022-05172-4
[3]: https://arxiv.org/abs/2505.05896
[4]: https://arxiv.org/abs/2502.04514
[5]: https://storage.googleapis.com/deepmind-media/DeepMind.com/Blog/alphaevolve-a-gemini-powered-coding-agent-for-designing-advanced-algorithms/AlphaEvolve.pdf
