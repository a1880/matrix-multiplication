using System;

namespace akExtractMatMultSolution
{
    using static Util;

    /// <summary>
    /// Class to hold and model a Coefficient.
    /// The value can be int, float or complex.
    /// </summary>
    public readonly struct Coefficient
    {
        // accuracy for equality checks
        const double EPS = 1e-12;
        public double Real { get; }
        public double Imaginary { get; }

        public Coefficient(double real, double imaginary = 0)
        {
            Real = real;
            Imaginary = imaginary;
        }

        public Coefficient(Coefficient other)
        {
            Real = other.Real;
            Imaginary = other.Imaginary;    
        }

        public Coefficient(string s)
        {
            Coefficient c = ParseComplex(s);

            Real = c.Real;
            Imaginary = c.Imaginary;
        }

        static private Coefficient ParseComplex(string complexNumberString)
        {
            // Remove any whitespace from the string
            complexNumberString = complexNumberString
                .Replace(" ", "").Replace("-", "+-").Replace("i", "j").ToLower();

            // Split the string into real and imaginary parts
            string[] parts = complexNumberString.Split(['+'], StringSplitOptions.RemoveEmptyEntries);

            double realPart = 0;
            double imaginaryPart = 0;

            if (parts.Length == 1)
            {
                // Handle the case where the complex number is purely real or purely imaginary
                if (complexNumberString.Contains("j"))
                {
                    imaginaryPart = atof(parts[0].Replace("j", ""));
                }
                else
                {
                    realPart = atof(parts[0]);
                }
            }
            else if (parts.Length == 2)
            {
                Check(!parts[0].Contains("j"), "imaginary part not expected first");
                Check(parts[1].Contains("j"), "imaginary part expected second");

                // Handle the case where the complex number has both real and imaginary parts
                realPart = atof(parts[0]);
                imaginaryPart = atof(parts[1].Replace("j", ""));
            }
            else
            {
                Fatal($"Complex '{complexNumberString}' has neither one nor two parts");
            }

            return new Coefficient(realPart, imaginaryPart);
        }

        private static bool IsEqual(double a, double b) => Math.Abs(a - b) < EPS;
        private static bool IsZero(double a) => Math.Abs(a) < EPS;

        public static Coefficient operator -(Coefficient a) => new(-a.Real, -a.Imaginary);
        public static Coefficient operator +(Coefficient a) => a;
        public static Coefficient operator *(int i, Coefficient c)
        {
            return new Coefficient(i * c.Real, i * c.Imaginary);
        }
        public static Coefficient operator *(Coefficient c, Coefficient d)
        {
            double re = c.Real * d.Real - c.Imaginary * d.Imaginary;
            double im = c.Real * d.Imaginary + c.Imaginary * d.Real;

            return new Coefficient(re, im);
        }
        public static Coefficient operator +(Coefficient c, Coefficient d)
        {
            double re = c.Real + d.Real;
            double im = c.Imaginary + d.Imaginary;

            return new Coefficient(re, im);
        }
        public static Coefficient operator -(Coefficient c, Coefficient d)
        {
            double re = c.Real - d.Real;
            double im = c.Imaginary - d.Imaginary;

            return new Coefficient(re, im);
        }
        public static Coefficient operator -(Coefficient c, int i)
        {
            return new Coefficient(c.Real - i, c.Imaginary);
        }
        public static bool operator ==(Coefficient c, int i)
        {
            return IsEqual(c.Real, i) && IsZero(c.Imaginary);
        }
        public static bool operator !=(Coefficient c, int i)
        {
            return (c.Real != i) || (c.Imaginary != 0);
        }

        public static bool operator ==(Coefficient c, Coefficient d)
        {
            return IsEqual(c.Real, d.Real) && IsEqual(c.Imaginary, d.Imaginary);
        }
        public static bool operator !=(Coefficient c, Coefficient d)
        {
            return !IsEqual(c.Real, d.Real) || !IsEqual(c.Imaginary, d.Imaginary);
        }

        public static bool operator >(Coefficient c, int i)
        {
            return (c.Real > i) && (c.Imaginary == 0);
        }
        public static bool operator <(Coefficient c, int i)
        {
            return (c.Real < i) && (c.Imaginary == 0);
        }

        public static bool operator >(Coefficient c, Coefficient d)
        {
            return (float)c > (float)d;
        }
        public static bool operator <(Coefficient c, Coefficient d)
        {
            return (float)c < (float)d;
        }

        public bool IsComplex => !IsZero(Imaginary);
        public bool IsNegativeImaginary => IsZero(Real) && (Imaginary < -EPS);
        public bool IsFloat => IsZero(Imaginary);
        public bool IsInt => IsFloat && IsEqual(Math.Floor(Real), Real);

        //  Type conversions
        public static implicit operator Coefficient(int value) => new(value, 0);
        public static implicit operator Coefficient(float value) => new(value, 0);
        public static implicit operator Coefficient(double value) => new(value, 0);

        //  Convert back
        public static explicit operator int(Coefficient nv) =>
            nv.IsInt ? (int)nv.Real : throw new InvalidCastException();
        public static explicit operator float(Coefficient nv) =>
            nv.IsFloat ? (float)nv.Real : throw new InvalidCastException();
        public static explicit operator double(Coefficient nv) =>
            nv.IsFloat ? nv.Real : throw new InvalidCastException();

        public double Abs() => IsComplex ? Math.Sqrt(Real*Real + Imaginary*Imaginary) : Math.Abs(Real);

        public override readonly string ToString()
        {
            if (IsZero(Imaginary))
            {
                return $"{Real}";
            }
            else if (IsZero(Real))
            {
                return $"{Imaginary}j";
            }
            else if (Imaginary > 0)
            {
                return $"({Real} + {Imaginary}j)";
            }
            else 
            {
                return $"({Real} - {Math.Abs(Imaginary)}j)";
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Coefficient coefficient &&
                   IsEqual(Real, coefficient.Real) &&
                   IsEqual(Imaginary, coefficient.Imaginary);
        }

        public override int GetHashCode()
        {
            int hashCode = -837395861;
            hashCode = hashCode * -1521134295 + Real.GetHashCode();
            hashCode = hashCode * -1521134295 + Imaginary.GetHashCode();
            return hashCode;
        }
    }
}
