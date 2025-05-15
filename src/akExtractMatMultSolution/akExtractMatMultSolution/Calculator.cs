using System;
using System.Collections.Generic;
using static akExtractMatMultSolution.Util;

namespace akExtractMatMultSolution
{
    /// <summary>
    /// Class to calculate and store numerical values.
    /// 
    /// Calculation is the evaluation of Subexpressions.
    /// Values are stored and later retrieved under their name.
    /// Accuracy of 'double' type values is 15 or more digits.
    /// </summary>
    class Calculator
    {
        readonly Dictionary<string, double> dicVar2Value= [];

        public Calculator() { }

        /// <summary>
        /// Calculate Subexpression value and store it under 'name'
        /// </summary>
        public void Calculate(string name, Subexpression se)
        {
            double v = 0;

            foreach(var op in se.PosOperands)
            {
                v += Retrieve(op);
            }
            foreach (var op in se.NegOperands)
            {
                v -= Retrieve(op);
            }

            Store(name, v);
        }

        /// <summary>
        /// Calculate value as positive or inverted variable 
        /// </summary>
        public void Calculate(string name, string varName)
        {
            var s = varName.Replace(" ", "").Replace("+", "");

            if (s.StartsWith("(") && s.EndsWith(")"))
            {
                s = s.Substring(1, s.Length - 2);
            }

            if (s.StartsWith("-"))
            {
                Store(name, -Retrieve(s.Substring(1)));
            }
            else
            {
                Store(name, Retrieve(s));
            }
        }

        /// <summary>
        /// Extract value for variable 'name'
        /// </summary>
        public double Retrieve(string name) 
        {
            Check(dicVar2Value.ContainsKey(name), $"Variable '{name}' not found");
            return dicVar2Value[name];
        }

        /// <summary>
        /// Register value for variable 'name'.
        /// Note that values cannot be changed.
        /// </summary>
        public void Store(string name, double value)
        {
            Check(!dicVar2Value.ContainsKey(name), $"Variable '{name}' already calculated");
            Check(Subexpression.IsValidIdentifier(name), $"Invalid identifier '{name}'");
            dicVar2Value[name] = value;
        }

        /// <summary>
        /// Check if all values stored in 'dic' do have the predicted values
        /// </summary>
        public bool Validate(Dictionary<string, double> dic, string commentPrefix)
        {
            bool ret = true;
            int errors = 0;
            const double epsilon = 1e-8;

            foreach(var op in dic.Keys) 
            {
                Check(Subexpression.IsValidIdentifier(op), $"Invalid identifier '{op}'");
                double opVal = dic[op];
                double val = Retrieve(op);

                if (Math.Abs(opVal - val) > epsilon)
                {
                    o($"{commentPrefix}{op} = {opVal} != {val}");
                    errors++;
                    ret = false;
                }
            }

            if (errors > 0)
            {
                o($"{commentPrefix}Errors found: {errors}");
                o($"{commentPrefix}Algorithm is **not** valid.");
            }

            return ret;
        }
    }
}
