using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static akExtractMatMultSolution.Util;

namespace akExtractMatMultSolution
{
    public class MatrixDimensions
    {
        public static int aRows = 0;
        public static int aCols = 0;
        public static int bRows = 0;
        public static int bCols = 0;
        public static int cRows = 0;
        public static int cCols = 0;
        public static int noOfProducts = 0;
        public static int productDigits = 0;

        public static int[] Products = null;

        public static int[] Fgd_all = null;

        public enum Mat { F = 0, G = 1, D = 2, unknown = 999 };
        public readonly static int F = (int)Mat.F;

        public readonly static int G = (int)Mat.G;

        public readonly static int D = (int)Mat.D;

        public static readonly string[] literalName = ["F", "G", "D"];
        public static readonly string[] matrixNames = ["A", "B", "C"];
        public static readonly string[] coefficientName = ["a", "b", "c"];

        private static bool RowsColsAreValid = false;
        private static bool HasComplexCoefficients = false;
        public static int[] Rows;
        public static int[] Cols;

        public static IEnumerable<int>[] RowsRange;
        public static IEnumerable<int>[] ColsRange;

        public const int undefined = int.MinValue;
        public static DynArray3D<Coefficient> litArrayF;
        public static DynArray3D<Coefficient> litArrayG;
        public static DynArray3D<Coefficient> litArrayD;

        public enum AlgorithmMode { FullBrent, Mod2Brent };

        public static AlgorithmMode algorithmMode;

        public static bool transposedMode = false;

        //  initiated by CreateLiteralArrays()
        public static DynArray3D<Coefficient>[] litArrays;

        /// <summary>
        /// Count number of plus/minus elements in term
        /// The first element is counted twice to avoid leading '-' signs
        /// </summary>
        /// <param name="k">Product index</param>
        /// <param name="mat">Coefficient matrix for term</param>
        /// <returns>number of positive/negative elements</returns>
        private static (int, int) GetTermPlusMinus(int k, Mat mat)
        {
            int plus = 0;
            int minus = 0;
            bool first = true;

            foreach ((int row, int col) in Indices(mat))
            {
                Coefficient lit = litArrays[(int)mat][row, col, k];

                if (lit > 0)
                {
                    plus += first ? 2 : 1;
                    first = false;
                }
                else if (lit < 0)
                {
                    minus += first ? 2 : 1;
                    first = false;
                }
            }

            return (plus, minus);
        }


        public static (int ops, int add_ops, int sub_ops)
            GetTermStatistics(int fgd, int product)
        {
            bool transpose = (fgd == D) && transposedMode;
            return GetTermStatistics(fgd, product, litArrays[fgd], transpose);
        }

        public static (int ops, int add_ops, int sub_ops)
            GetTermStatistics(int fgd, int product, DynArray3D<Coefficient> litArray, bool transpose)
        {
            int ops = 0;
            int add_ops = 0;
            int sub_ops = 0;

            bool first = true;
            foreach ((int row, int col) in Indices(fgd))
            {
                Coefficient val = transpose ? litArray[col, row, product] : litArray[row, col, product];

                if (val != 0)
                {
                    ops++;
                    if (!first)
                    {
                        if (val < 0)
                        {
                            sub_ops++;
                        }
                        else
                        {
                            add_ops++;
                        }
                    }
                    else
                    {
                        first = false;
                    }
                }
            }

            return (ops, add_ops, sub_ops);
        }


        /// <summary>
        /// Go through all three LiteralArrays
        /// and flip the literal signs for product k, if flip[] is true
        /// </summary>
        /// <param name="k">The product</param>
        /// <param name="flip">Array of switchs to force flipping</param>
        private static void FlipLiterals(int k, bool[] flip)
        {
            foreach (int fgd in Fgd_all)
            {
                if (flip[fgd])
                {
                    foreach ((int row, int col) in Indices(fgd))
                    {
                        Coefficient lit = litArrays[fgd][row, col, k];

                        litArrays[fgd][row, col, k] = -lit;
                    }
                }
            }
        }

        public static void BeautifyLiterals()
        {
            if (HasComplexCoefficients)
            {
                o("# No attempt to reduce minus signs:");
                o("# Complex coefficients don't lend themselves to sign flipping.");
                o("#");
                return;
            }
            SetArraysToReadonly(_readonly: false);

            int reducedMinusSigns = 0;

            foreach (int k in Products)
            {
                int aPlus;
                int aMinus;
                int bPlus;
                int bMinus;
                int cPlus;
                int cMinus;

                (aPlus, aMinus) = GetTermPlusMinus(k, Mat.F);
                (bPlus, bMinus) = GetTermPlusMinus(k, Mat.G);
                (cPlus, cMinus) = GetTermPlusMinus(k, Mat.D);

                bool[] flip = [true, true, false];
                int least = aPlus + bPlus + cMinus;
                int minusSigns = aPlus + bMinus + cPlus;
                if (minusSigns < least)
                {
                    least = minusSigns;
                    flip = [true, false, true];
                }
                minusSigns = aMinus + bPlus + cPlus;
                if (minusSigns < least)
                {
                    least = minusSigns;
                    flip = [false, true, true];
                }
                minusSigns = aMinus + bMinus + cMinus;
                if (minusSigns < least)
                {
                    flip = [false, false, false];
                }
                reducedMinusSigns += minusSigns - least;

                FlipLiterals(k, flip);
            }

            if (reducedMinusSigns > 0)
            {
                o($"# Minus signs reduced by literal sign flipping: {reducedMinusSigns}");
                o("#");
            }
            else
            {
                o($"# No minus signs reduction possible.");
                o("#");
            }

            SetArraysToReadonly(_readonly: true);
        }

        public static void CreateLiteralArrays(int defaultValue)
        {
            litArrayF = new DynArray3D<Coefficient>(defaultValue);
            litArrayG = new DynArray3D<Coefficient>(defaultValue);
            litArrayD = new DynArray3D<Coefficient>(defaultValue);

            litArrays = [litArrayF, litArrayG, litArrayD];
        }

        private static void DetermineMode()
        {
            Coefficient min = int.MaxValue;
            bool bInLowerHalf = false;
            bool bComplexValues = false;

            //  enumerate all literals to get the minimum value
            //  negative values cannot happen for mod 2 mode
            foreach (int fgd in Fgd_all)
                foreach ((int row, int col) in Indices(fgd))
                    foreach (int k in Products)
                    {
                        try
                        {
                            Coefficient lit = litArrays[fgd][row, col, k];

                            if ( lit.IsComplex)
                            {
                                bComplexValues = true;
                                break;
                            }
                            if (lit < min)
                            {
                                min = lit;
                            }

                            if (fgd == (int)Mat.D)
                            {
                                bInLowerHalf = bInLowerHalf
                                               || ((lit != litArrays[fgd].Default) && (row > col));
                            }
                        }
                        catch (Exception ex)
                        {
                            //  ignore
                            Debug.WriteLine(ex.Message);
                        }
                    }

            transposedMode = !bInLowerHalf && (Rows[(int)Mat.D] == Cols[(int)Mat.D]) && !bComplexValues;
            algorithmMode = (bComplexValues || (min < 0)) ? AlgorithmMode.FullBrent : AlgorithmMode.Mod2Brent;
            HasComplexCoefficients = bComplexValues;
        }

        public static void GetProblemDimensions()
        {
            aRows = litArrayF.GetLength(0) - 1;
            aCols = litArrayF.GetLength(1) - 1;
            noOfProducts = litArrayF.GetLength(2) - 1;
            productDigits = noOfProducts.ToString().Length;
            Check(noOfProducts > 0, "No products. Probably empty solution file?");

            Products = Range(1, noOfProducts);
            Fgd_all = Range(0, matrixNames.Length);

            bRows = litArrayG.GetLength(0) - 1;
            bCols = litArrayG.GetLength(1) - 1;
            Check(litArrayG.GetLength(2) - 1 == noOfProducts,
                  "Invalid product dim for G array. "
                  + $"{litArrayG.GetLength(2) - 1} != {noOfProducts} G slices");

            cRows = litArrayD.GetLength(0) - 1;
            cCols = litArrayD.GetLength(1) - 1;
            Check(litArrayD.GetLength(2) - 1 == noOfProducts,
                  "Invalid product dim for D array. "
                  + $"{litArrayD.GetLength(2) - 1} != {noOfProducts} D slices");

            Check(aRows == cRows, $"aRows != cRows, {aRows} != {cRows}");
            Check(aCols == bRows, $"aCols != bRows, {aCols} != {bRows}");
            Check(bCols == cCols, $"bCols != cCols, {bCols} != {cCols}");

            Rows = [aRows, bRows, cRows];
            Cols = [aCols, bCols, cCols];

            RowsRange = new IEnumerable<int>[Fgd_all.Length];
            ColsRange = new IEnumerable<int>[Fgd_all.Length];

            foreach (int fgd in Fgd_all)
            {
                RowsRange[fgd] = Range(1, Rows[fgd]);
                ColsRange[fgd] = Range(1, Cols[fgd]);
            }
            RowsColsAreValid = true;

            DetermineMode();
        }

        public static IEnumerable<(int row, int col)> AIndices => Indices(F);
        public static IEnumerable<(int row, int col)> BIndices => Indices(G);
        public static IEnumerable<(int row, int col)> CIndices => Indices(D);

        public static IEnumerable<int> ARows => RowsRange[F];
        public static IEnumerable<int> ACols => ColsRange[F];
        public static IEnumerable<int> BRows => RowsRange[G];
        public static IEnumerable<int> BCols => ColsRange[G];
        public static IEnumerable<int> CRows => RowsRange[D];
        public static IEnumerable<int> CCols => ColsRange[D];

        public static IEnumerable<(int row, int col)> Indices(Mat mat) => Indices((int)mat);

        public static IEnumerable<(int row, int col)> Indices(int fgd)
        {
            Check(RowsColsAreValid, "Problem dimensions not determined yet!");
            return
                RowsRange[fgd].SelectMany(i => ColsRange[fgd].Select(j => (i, j)));
        }

        public static string ProductName(int product)
        {
            Check(productDigits > 0, "Inconsistent productDigits");

            return $"P{product.ToString().PadLeft(productDigits, '0')}";
        }

        /// <summary>
        /// Replace literalArrayF, literalArrayG, and literalArrayD by stripped versions.
        /// Omit rows, columns and products unless selected.
        /// </summary>
        /// <param name="selectedARows"></param>
        /// <param name="selectedACols"></param>
        /// <param name="selectedBCols"></param>
        /// <param name="selectedProducts"></param>
        public static void ReduceLiteralArrays(
            int[] selectedARows, 
            int[] selectedACols, 
            int[] selectedBCols, 
            int[] selectedProducts)
        {
            DynArray3D<Coefficient> F = new(0);
            DynArray3D<Coefficient> G = new(0);
            DynArray3D<Coefficient> D = new(0);

            //  effective "real" row/col values
            int raEff = 0;
            int rbEff = 0;

            foreach (int ra in ARows)
            {
                if (selectedARows.Contains(ra))
                {
                    raEff++;
                    int caEff = 0;
                    foreach (int ca in ACols)
                    {
                        if (selectedACols.Contains(ca))
                        {
                            caEff++;

                            int kEff = 0;

                            foreach (int k in Products)
                            {
                                if (selectedProducts.Contains(k))
                                {
                                    kEff++;

                                    F[raEff, caEff, kEff] = litArrayF[ra, ca, k];
                                }
                            }
                        }
                    }

                    int cbEff = 0;
                    foreach (int cb in BCols)
                    {
                        if (selectedBCols.Contains(cb))
                        {
                            cbEff++;

                            int kEff = 0;

                            foreach (int k in Products)
                            {
                                if (selectedProducts.Contains(k))
                                {
                                    kEff++;

                                    D[raEff, cbEff, kEff] = litArrayD[ra, cb, k];
                                }
                            }
                        }
                    }
                }
            }

            foreach (int rb in BRows)
            {
                if (selectedACols.Contains(rb))
                {
                    rbEff++;

                    int cbEff = 0;
                    foreach (int cb in BCols)
                    {
                        if (selectedBCols.Contains(cb))
                        {
                            cbEff++;

                            int kEff = 0;

                            foreach (int k in Products)
                            {
                                if (selectedProducts.Contains(k))
                                {
                                    kEff++;

                                    G[rbEff, cbEff, kEff] = litArrayG[rb, cb, k];
                                }
                            }
                        }
                    }
                }
            }

            //  update literal arrays and problem dimensions
            //  to reflect the reduction
            litArrayF = F;
            litArrayG = G;
            litArrayD = D;
            litArrays = [F, G, D];

            GetProblemDimensions();
            SetArraysToReadonly(_readonly: true);
        }

        public static void SetArraysToReadonly(bool _readonly = true)
        {
            //  set to readonly to detected unsolicited write attempts
            litArrayF.Readonly = _readonly;
            litArrayG.Readonly = _readonly;
            litArrayD.Readonly = _readonly;
        }
    }
}
