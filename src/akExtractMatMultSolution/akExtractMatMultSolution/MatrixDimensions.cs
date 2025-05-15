using System;
using System.Linq;
using System.Net;
using static akExtractMatMultSolution.MatrixDimensions;
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

        public enum Mat { F = 0, G = 1, D = 2, unknown = 999 };

        public static readonly string[] literalName = ["F", "G", "D"];
        public static readonly string[] matrixName = ["A", "B", "C"];
        public static readonly string[] coefficientName = ["a", "b", "c"];

        public static int[] Rows;
        public static int[] Cols;

        public const int undefined = -999;
        public static DynArray3D<int> litArrayF;
        public static DynArray3D<int> litArrayG;
        public static DynArray3D<int> litArrayD;

        public enum AlgorithmMode { FullBrent, Mod2Brent };

        public static AlgorithmMode algorithmMode;

        public static bool transposedMode = false;

        //  initiated by CreateLiteralArrays()
        public static DynArray3D<int>[] litArrays;

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

            for (int row = 1; row <= Rows[(int)mat]; row++)
                for (int col = 1; col <= Cols[(int)mat]; col++)
                {
                    int lit = litArrays[(int)mat][row, col, k];

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

        /// <summary>
        /// Go through all three LiteralArrays
        /// and flip the literal signs for product k, if flip[] is true
        /// </summary>
        /// <param name="k">The product</param>
        /// <param name="flip">Array of switchs to force flipping</param>
        private static void FlipLiterals(int k, bool[] flip)
        {
            for (int ri = 0; ri <= (int)Mat.D; ri++)
            {
                if (flip[ri])
                {
                    for (int row = 1; row <= Rows[ri]; row++)
                        for (int col = 1; col <= Cols[ri]; col++)
                        {
                            int lit = litArrays[ri][row, col, k];

                            litArrays[ri][row, col, k] = -lit;
                        }
                }
            }
        }

        public static void BeautifyLiterals()
        {
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
            litArrayF = new DynArray3D<int>(defaultValue);
            litArrayG = new DynArray3D<int>(defaultValue);
            litArrayD = new DynArray3D<int>(defaultValue);

            litArrays = [litArrayF, litArrayG, litArrayD];
        }

        private static void DetermineMode()
        {
            int min = int.MaxValue;
            bool bInLowerHalf = false;

            //  enumerate all literals to get the minimum value
            //  negative values cannot happen for mod 2 mode
            for (int ri = 0; ri < Rows.Length; ri++)
                for (int row = 1; row <= Rows[ri]; row++)
                    for (int col = 1; col <= Cols[ri]; col++)
                        foreach (int k in Products)
                        {
                            try
                            {
                                int lit = litArrays[ri][row, col, k];

                                if (lit < min)
                                {
                                    min = lit;
                                }

                                bInLowerHalf = bInLowerHalf
                                            || ((ri == Rows.Length - 1) && (Math.Abs(lit) <= 1) && (row > col));
                            }
                            catch (Exception)
                            {
                                //  ignore
                            }
                        }

            transposedMode = !bInLowerHalf;
            algorithmMode = (min < 0) ? AlgorithmMode.FullBrent : AlgorithmMode.Mod2Brent;
        }

        public static void GetProblemDimensions()
        {
            aRows = litArrayF.GetLength(0) - 1;
            aCols = litArrayF.GetLength(1) - 1;
            noOfProducts = litArrayF.GetLength(2) - 1;
            productDigits = noOfProducts.ToString().Length;
            Check(noOfProducts > 0, "No products. Probably empty solution file?");

            Products = Range(1, noOfProducts);

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

            DetermineMode();
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
        public static void ReduceLiteralArrays(int[] selectedARows, int[] selectedACols, int[] selectedBCols, int[] selectedProducts)
        {
            DynArray3D<int> F = new(0);
            DynArray3D<int> G = new(0);
            DynArray3D<int> D = new(0);

            //  effective "real" row/col values
            int raEff = 0;
            int rbEff = 0;

            for (int ra = 1; ra <= aRows; ra++)
            {
                if (selectedARows.Contains(ra))
                {
                    raEff++;
                    int caEff = 0;
                    for (int ca = 1; ca <= aCols; ca++)
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
                    for (int cb = 1; cb <= bCols; cb++)
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

            for (int rb = 1; rb <= bRows; rb++)
            {
                if (selectedACols.Contains(rb))
                {
                    rbEff++;

                    int cbEff = 0;
                    for (int cb = 1; cb <= bCols; cb++)
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
