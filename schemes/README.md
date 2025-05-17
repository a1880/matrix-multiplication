# Solution Schemes

The solutions provided include:

$N \times M \times P$ \_ $r$ stands for multiplication of an $N \times M$
matrix times an $M \times P$ matrix, using $r$ elementary products.

| Solution               | Remarks                                                                    |
| ---------------------- | -------------------------------------------------------------------------- |
| [2x2x2_07](2x2x2_07)   | small example á la [Strassen][1]                                           |
| [4x4x4_47](4x4x4_47)   | the [Fawzi][2] solution for $\mathbb{Z}_2$                                 |
| [4x4x4_48](4x4x4_48)   | the [DeepMind/AlphaEvolve][5] for complex coefficients $\mathbb{Z_{C0.5}}$ |
| [4x6x6_106](4x6x6_106) | solution of [Kauers+Wood][3] re-lifted                                     |
| [5x5x6_110](5x5x6_110) | solution of [Kauers+Wood][3] re-lifted                                     |
| [5x6x6_130](5x6x6_130) | solution of [Kauers+Wood][3] re-lifted                                     |
| [6x6x6_153](6x6x6_153) | solution of [Moosbauer+Poole][4] lifted                                    |
| [2x3x2_11](2x3x2_11)   | small example                                                              |
| [4x5x6_90](4x5x6_90)   | solution of [Kauers+Wood][3] re-lifted                                     |
| [5x5x5_93](5x5x5_93)   | solution of [Moosbauer+Poole][4] lifted                                    |
| [5x5x5_93](5x5x5_93b)  | solution of [DeepMind/AlphaEvolve][5]                                      |
| [5x5x7_127](5x5x7_127) | solution of [Kauers+Wood][3] re-lifted                                     |
| [5x6x7_150](5x6x7_150) | solution of [Kauers+Wood][3] re-lifted                                     |

lifted = modulo $2$ solution was lifted to $\mathbb{Z}$ with coefficients in $\lbrace -1, 0, +1 \rbrace$

re-lifted = general solution was lowered to F<sub>2</sub> and then lifted to $\mathbb{Z}$

[1]: https://gdz.sub.uni-goettingen.de/id/PPN362160546_0013?tify=%7B%22view%22:%22info%22,%22pages%22:%5B358%5D%7D
[2]: https://www.nature.com/articles/s41586-022-05172-4
[3]: https://arxiv.org/abs/2505.05896
[4]: https://arxiv.org/abs/2502.04514
[5]: https://storage.googleapis.com/deepmind-media/DeepMind.com/Blog/alphaevolve-a-gemini-powered-coding-agent-for-designing-advanced-algorithms/AlphaEvolve.pdf
