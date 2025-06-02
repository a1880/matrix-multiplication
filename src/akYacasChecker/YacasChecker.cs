using akExtractMatMultSolution;
using System;
using System.Collections.Generic;
using System.Text;
using static akUtil.Util;

namespace akYacasChecker
{

    /// <summary>
    /// Class represents a product of two coefficients
    /// with a numeric factor
    /// </summary>
    internal class CoefficientProduct: AstNode
    {
        private readonly string name;
        private readonly Coefficient factor;

        public string Name => name;
        public Coefficient Factor => factor;
        public CoefficientProduct(string name, Coefficient factor)
        {
            this.name = name;
            this.factor = factor;
        }
        public CoefficientProduct(string name)
        {
            this.name = name;
            factor = 1;
        }

        public override void Print(int indent, bool compact = false)
        {
            if (!compact)
            {
                Console.WriteLine($"{new string(' ', indent)}CoefficientProduct: {Factor} {Name}");
            }
            else
            {
                Console.Write($"{Factor}*{Name}");
            }
        }
    }

    /// <summary>
    /// Class represents a variable
    /// with a numeric factor
    /// </summary>
    internal class VariableProduct : AstNode
    {
        private readonly string name;
        private readonly Coefficient factor;

        public string Name => name;
        public Coefficient Factor => factor;
        public VariableProduct(string name, Coefficient factor)
        {
            this.name = name;
            this.factor = factor;
        }
        public VariableProduct(string name)
        {
            this.name = name;
            factor = 1;
        }

        public override void Print(int indent, bool compact = false)
        {
            if (!compact)
            {
                Console.WriteLine($"{new string(' ', indent)}VariableProduct: {Factor} {Name}");
            }
            else
            {
                Console.Write($"{Factor}*{Name}");
            }
        }
    }


    internal class YacasChecker
    {
        private readonly string fileName;
        private readonly Dictionary<string, string> elements = [];
        private readonly Dictionary<string, string> products = [];
        private readonly Parser parser = new();

        public YacasChecker(string fileName)
        {
            Check(Exists(fileName), $"File '{fileName}' not found!");
            this.fileName = fileName;
        }

        private bool IsAddOrSub(AstNode nd)
        {
            return (nd is BinaryOperationNode binOp) &&
                   ((binOp.Operator == TokenType.add) ||
                   (binOp.Operator == TokenType.subtract));
        }

        private void CheckForZeroSumsTraverse(AstNode nd, int sign, Dictionary<string, Coefficient> dic)
        {
            if (nd is BinaryOperationNode binOp)
            {
                if (binOp.Operator == TokenType.add)
                {
                    CheckForZeroSumsTraverse(binOp.Left, sign, dic);
                    CheckForZeroSumsTraverse(binOp.Right, sign, dic);
                }
                else if (binOp.Operator == TokenType.subtract)
                {
                    CheckForZeroSumsTraverse(binOp.Left, sign, dic);
                    CheckForZeroSumsTraverse(binOp.Right, -sign, dic);
                }
                else if (binOp.Operator == TokenType.times)
                {
                    CheckForZeroSumsTraverse(binOp.Left, sign, dic);
                    CheckForZeroSumsTraverse(binOp.Right, sign, dic);
                }
                else
                {
                    Fatal("Unexpected bin op type");
                }
            }
            else if (nd is UnaryOperationNode unaryOp)
            {
                CheckForZeroSumsTraverse(unaryOp.Right, -sign, dic);
            }
            else if (nd is CoefficientProduct prod)
            {
                if (dic.ContainsKey(prod.Name))
                {
                    dic[prod.Name] += new Coefficient(sign * prod.Factor);
                }
                else
                {
                    dic[prod.Name] = sign * prod.Factor;
                }
            }
            else
            {
                Fatal("Unexpected op type");
            }

        }
        /// <summary>
        /// Traverse AST and count the Coefficient products with their factors
        /// </summary>
        private bool CheckForZeroSums(AstNode nd)
        {
            Dictionary<string, Coefficient> dic = [];

            //  recursion starts here
            CheckForZeroSumsTraverse(nd, sign: 1, dic);

            int errCount = 0;

            foreach (string prod in dic.Keys)
            {
                if (dic[prod] != 0.0)
                {
                    o($"{prod} = {dic[prod]} != 0");
                    errCount++;
                }
            }

            return errCount == 0;
        }

        /// <summary>
        /// Recursively traverse AST and push '*' to the leav nodes.
        /// Replace products of variables by CoefficientProduct nodes
        /// Resulting AST should just contain add/sub of Coefficient products
        /// </summary>
        private AstNode Expand(AstNode node)
        {
            if (node == null) return null;
            if (node is VariableNode) { return node; }
            if (node is ComplexNode) { return node; }
            if (node is VariableProduct) { return node; }
            if (node is CoefficientProduct) { return node; }
            if (node.Expanded) { return node; }

            if (node is BinaryOperationNode binOp)
            {
                return ExpandBinary(binOp);
            }
            else if (node is UnaryOperationNode unaryOp)
            {
                return ExpandUnary(unaryOp);
            }
            else
            {
                Fatal("Unexpected AST type");
            }

            Fatal("Oops!");
            return null;
        }


        private AstNode ExpandBinary(BinaryOperationNode binOp)
        {
            if (IsAddOrSub(binOp))
            {
                AstNode left = Expand(binOp.Left);
                AstNode right = Expand(binOp.Right);
                binOp.Expanded = true;
                return new BinaryOperationNode(left, binOp.Operator, right);
            }
            else if (binOp.Operator == TokenType.times)
            {
                return ExpandTimes(binOp);
            }
            else
            {
                Fatal("Unexpected binOp operator");
            }

            Fatal("Oops!");
            return null;
        }

        private string TypeString(AstNode nd)
        {
            string s = nd.GetType().Name switch
            {
                "BinaryOperationNode" => "binary",
                "ComplexNode" => "cplx",
                "CoefficientProduct" => "cprd",
                "UnaryOperationNode" => "unary",
                "VariableNode" => "var",
                "VariableProduct" => "vprd",
                _ => "?"
            };

            return s;
        }
        private AstNode ExpandTimes(BinaryOperationNode binOp)
        {
            binOp = new(Expand(binOp.Left), binOp.Operator, Expand(binOp.Right));

            string sel = TypeString(binOp.Left) + "-" + TypeString(binOp.Right);
            AstNode retNd = null;

            switch(sel)
            {
                case "binary-binary":
                    {
                        //  (a op1 b) * (c op2 d)  ==> (a*c op2 a*d) op1 (b*c op2 b*d)
                        BinaryOperationNode left = binOp.Left as BinaryOperationNode;
                        BinaryOperationNode right = binOp.Right as BinaryOperationNode;
                        AstNode ac = Times(left.Left, right.Left);
                        AstNode ad = Times(left.Left, right.Right);
                        AstNode bc = Times(left.Right, right.Left);
                        AstNode bd = Times(left.Right, right.Right);
                        AstNode acad = new BinaryOperationNode(ac, right.Operator, ad);
                        AstNode bcbd = new BinaryOperationNode(bc, right.Operator, bd);
                        BinaryOperationNode nd = new(acad, left.Operator, bcbd); 

                        binOp.Expanded = true;
                        retNd = ExpandBinary(nd);
                        break;
                    }
                case "binary-cplx":
                    {
                        // (a op b) * c  => (a*c) op (b*c)
                        BinaryOperationNode left = binOp.Left as BinaryOperationNode;
                        ComplexNode c = binOp.Right as ComplexNode;
                        BinaryOperationNode nd = new(Times(left.Left, c),
                                                     left.Operator,
                                                     Times(left.Right, c));
                        binOp.Expanded = true;
                        retNd = ExpandBinary(nd);
                        break;
                    }
                case "binary-cprd":
                    {
                        // (a op b) * c  => (a*c) op (b*c)
                        BinaryOperationNode left = binOp.Left as BinaryOperationNode;
                        CoefficientProduct c = binOp.Right as CoefficientProduct;
                        BinaryOperationNode nd = new(Times(left.Left, c),
                                                     left.Operator,
                                                     Times(left.Right, c));
                        binOp.Expanded = true;
                        retNd = ExpandBinary(nd);
                        break;
                    }
                case "binary-unary":
                    {
                        //  (x op1 y) * op2 z   ==> op2 ((x op1 y) * z)
                        BinaryOperationNode left = binOp.Left as BinaryOperationNode;
                        UnaryOperationNode right = binOp.Right as UnaryOperationNode;
                        UnaryOperationNode nd = new(right.Operator, Times(left, right.Right));

                        binOp.Expanded = true;
                        retNd = ExpandUnary(nd);
                        break;
                    }
                case "binary-var":
                    {
                        //  (x op y) * v  ==>  (x*v op y*v)
                        BinaryOperationNode left = binOp.Left as BinaryOperationNode;
                        VariableNode v = binOp.Right as VariableNode;
                        BinaryOperationNode nd = new(Times(left.Left, v), 
                                                     left.Operator, 
                                                     Times(left.Right, v));
                        retNd = ExpandBinary(nd);
                        break;
                    }
                case "binary-vprd":
                    {
                        //  (x op y) * v  ==>  (x*v op y*v)
                        BinaryOperationNode left = binOp.Left as BinaryOperationNode;
                        VariableProduct v = binOp.Right as VariableProduct;
                        BinaryOperationNode nd = new(Times(left.Left, v),
                                                     left.Operator,
                                                     Times(left.Right, v));
                        retNd = ExpandBinary(nd);
                        break;
                    }
                case "cplx-binary":
                    {
                        //  c * (x op y)  ==>  (c*x op c*y)
                        ComplexNode c = binOp.Left as ComplexNode;
                        BinaryOperationNode right = binOp.Right as BinaryOperationNode;
                        BinaryOperationNode nd = new(Times(c, right.Left),
                                                     right.Operator,
                                                     Times(c, right.Right));
                        retNd = ExpandBinary(nd);
                        break;
                    }
                case "cplx-cplx":
                    {
                        //  c * d  ==>  cd
                        ComplexNode c = binOp.Left as ComplexNode;
                        ComplexNode d = binOp.Right as ComplexNode;

                        retNd = new ComplexNode(c.Value * d.Value);
                        break;
                    }
                case "cplx-cprd":
                    {
                        //  c * d  ==>  cd
                        ComplexNode c = binOp.Left as ComplexNode;
                        CoefficientProduct d = binOp.Right as CoefficientProduct;

                        retNd = new CoefficientProduct(d.Name, c.Value * d.Factor);
                        break;
                    }
                case "cplx-unary":
                    {
                        //  c * op z   ==> (op c) * z
                        ComplexNode c = binOp.Left as ComplexNode;
                        UnaryOperationNode z = binOp.Right as UnaryOperationNode;
                        ComplexNode cn = new(-c.Value);
                        BinaryOperationNode nd = Times(cn, z);

                        retNd = ExpandBinary(nd);
                        break;
                    }
                case "cplx-var":
                    {
                        //  c * v  ==>  cv
                        ComplexNode c = binOp.Left as ComplexNode;
                        VariableNode v = binOp.Right as VariableNode;

                        retNd = new VariableProduct(v.Name, c.Value);
                        break;
                    }
                case "cplx-vprd":
                    {
                        //  c * v  ==>  cv
                        ComplexNode c = binOp.Left as ComplexNode;
                        VariableProduct v = binOp.Right as VariableProduct;

                        retNd = new VariableProduct(v.Name, v.Factor * c.Value);
                        break;
                    }

                case "cprd-binary":
                    {
                        //  c * (x op y)  ==>  (c*x op c*y)
                        CoefficientProduct c = binOp.Left as CoefficientProduct;
                        BinaryOperationNode right = binOp.Right as BinaryOperationNode;
                        BinaryOperationNode nd = new(Times(c, right.Left),
                                                     right.Operator,
                                                     Times(c, right.Right));
                        retNd = ExpandBinary(nd);
                        break;
                    }
                case "cprd-cplx":
                    {
                        //  c * d  ==>  cd
                        CoefficientProduct c = binOp.Left as CoefficientProduct;
                        ComplexNode d = binOp.Right as ComplexNode;

                        retNd = new CoefficientProduct(c.Name, c.Factor * d.Value);
                        break;
                    }
                case "cprd-cprd":
                    {
                        //  c * d  ==>  cd
                        CoefficientProduct c = binOp.Left as CoefficientProduct;
                        CoefficientProduct d = binOp.Right as CoefficientProduct;

                        retNd = new CoefficientProduct(c.Name, c.Factor * d.Factor);
                        break;
                    }
                case "cprd-unary":
                    {
                        //  c * op z   ==> (op c) * z
                        CoefficientProduct c = binOp.Left as CoefficientProduct;
                        UnaryOperationNode z = binOp.Right as UnaryOperationNode;
                        CoefficientProduct cn = new(c.Name, -c.Factor);
                        BinaryOperationNode nd = Times(cn, z);

                        retNd = ExpandBinary(nd);
                        break;
                    }
                case "cprd-var":
                    {
                        //  c * v  ==>  cv
                        CoefficientProduct c = binOp.Left as CoefficientProduct;
                        VariableNode v = binOp.Right as VariableNode;

                        retNd = new VariableProduct(v.Name, c.Factor);
                        break;
                    }
                case "cprd-vprd":
                    {
                        //  c * v  ==>  cv
                        CoefficientProduct c = binOp.Left as CoefficientProduct;
                        VariableProduct v = binOp.Right as VariableProduct;

                        retNd = new VariableProduct(v.Name, v.Factor * c.Factor);
                        break;
                    }

                case "unary-binary":
                case "unary-cplx":
                case "unary-cprd":
                case "unary-unary":
                case "unary-var":
                case "unary-vprd":
                    {
                        //  (op x) * y   ==> op (x * y)
                        UnaryOperationNode left = binOp.Left as UnaryOperationNode;
                        UnaryOperationNode nd = new(left.Operator, Times(left.Right, binOp.Right));

                        retNd = ExpandUnary(nd);
                        break;
                    }

                case "var-binary":
                    {
                        //  v * (x op y)  ==> (v*x) op (v*y)
                        VariableNode v = binOp.Left as VariableNode;
                        BinaryOperationNode right = binOp.Right as BinaryOperationNode;
                        BinaryOperationNode nd = new(Times(v, right.Left), 
                                                     right.Operator, 
                                                     Times(v, right.Right));
                        retNd = ExpandBinary(nd);
                        break;
                    }
                case "var-cplx":
                    {
                        //  v * c  ==> vc
                        VariableNode v = binOp.Left as VariableNode;
                        ComplexNode c = binOp.Right as ComplexNode;
                        VariableProduct nd = new(v.Name, c.Value);

                        retNd = nd;
                        break;
                    }
                case "var-cprd":
                    {
                        //  v * c  ==> vc
                        VariableNode v = binOp.Left as VariableNode;
                        CoefficientProduct c = binOp.Right as CoefficientProduct;
                        VariableProduct nd = new(v.Name, c.Factor);

                        retNd = nd;
                        break;
                    }
                case "var-unary":
                    {
                        //  v * op z   ==> (op v) * z
                        VariableNode v = binOp.Left as VariableNode;
                        UnaryOperationNode z = binOp.Right as UnaryOperationNode;
                        VariableProduct vn = new(v.Name, -1);
                        BinaryOperationNode nd = Times(vn, z.Right);

                        retNd = ExpandBinary(nd);
                        break;
                    }
                case "var-var":
                    {
                        //  v * v   ==> vv
                        VariableNode vx = binOp.Left as VariableNode;
                        VariableNode vy = binOp.Right as VariableNode;
                        CoefficientProduct nd = new($"{vx.Name}{vy.Name}", 1);

                        retNd = nd;
                        break;
                    }
                case "var-vprd":
                    {
                        //  v * v   ==> vv
                        VariableNode vx = binOp.Left as VariableNode;
                        VariableProduct vy = binOp.Right as VariableProduct;
                        CoefficientProduct nd = new($"{vx.Name}{vy.Name}", vy.Factor);

                        retNd = nd;
                        break;
                    }

                case "vprd-binary":
                    {
                        //  v * (x op y)   ==> (v*x) op (v*y)
                        VariableProduct v = binOp.Left as VariableProduct;
                        BinaryOperationNode right = binOp.Right as BinaryOperationNode;
                        BinaryOperationNode nd = new(Times(v, right.Left), 
                                                     right.Operator, 
                                                     Times(v, right.Right));
                        retNd = ExpandBinary(nd);
                        break;
                    }

                case "vprd-cplx":
                    {
                        //  v * c  ==> vc
                        VariableProduct v = binOp.Left as VariableProduct;
                        ComplexNode c = binOp.Right as ComplexNode;
                        VariableProduct nd = new(v.Name, v.Factor * c.Value);

                        retNd = nd;
                        break;
                    }
                case "vprd-cprd":
                    {
                        Fatal("vprd-cprd can't happen, we have only two coefficients in one product");
                        break;
                    }
                case "vprd-unary":
                    {
                        //  v * op x  ==> op (v*x)
                        VariableProduct v = binOp.Left as VariableProduct;
                        UnaryOperationNode x = binOp.Right as UnaryOperationNode;
                        UnaryOperationNode nd = new(x.Operator, Times(v, x.Right));

                        retNd = ExpandUnary(nd);
                        break;
                    }
                case "vprd-var":
                    {
                        //  v * w   ==> vw
                        VariableProduct v = binOp.Left as VariableProduct;
                        VariableNode w = binOp.Right as VariableNode;
                        CoefficientProduct nd = new($"{v.Name}{w.Name}", v.Factor);

                        retNd = nd;
                        break;
                    }
                case "vprd-vprd":
                    {
                        //  v * w   ==> vw
                        VariableProduct v = binOp.Left as VariableProduct;
                        VariableProduct w = binOp.Right as VariableProduct;
                        CoefficientProduct nd = new($"{v.Name}{w.Name}", v.Factor * w.Factor);

                        retNd = nd;
                        break;
                    }

                default:
                    Fatal($"Selector {sel}?");
                    break;
            }

            binOp.Expanded = true;

            return retNd;
        }

        private AstNode ExpandUnary(UnaryOperationNode unaryOp)
        {
            UnaryOperationNode nd = new(unaryOp.Operator, Expand(unaryOp.Right));

            if ((nd.Operator == TokenType.subtract) &&
                (nd.Right is CoefficientProduct prod))
            {
                return new CoefficientProduct(prod.Name, -prod.Factor);
            }

            return nd;
        }

        /// <summary>
        /// Read a Yacas text line and add the contained expression to Dictionary dic
        /// </summary>
        private void ProcessEntry(string line, int lineCount, Dictionary<string, string> dic)
        {
            int eq = line.IndexOf(":=");
            Check(eq > 0, $"Line {lineCount}: no ':=' found");
            string p = line.Substring(0, eq - 1).Trim();
            if (dic.ContainsKey(p))
            {
                Fatal($"Line {lineCount}: duplicate entry '{p}'");
            }
            dic[p] = line.Substring(eq + 2).Replace(";", "").Trim();
        }

        /// <summary>
        /// Read Yacas Simplify() line and validate if the result is zero
        /// </summary>
        private bool ProcessSimplify(string line, int lineCount)
        {
            //  leave ';' as endmarker for parser
            string s = line.Replace("Simplify(", "").Replace(");", ";").Trim();
            string c = s.Substring(0, 3);

            Check(elements.ContainsKey(c), "Line {lineCount}: Unknown element '{c}'");

            s = s.Replace(c, elements[c]);

            foreach(string p in products.Keys)
            {
                s = s.Replace(p, $"({products[p]})");
            }

            AstNode ast = parser.Parse(s, lineCount);

            
            // o("vor Expand -----------------------------------------");
            // ast.Print(compact: false);
            // ast.Print(compact: true);
            ast = Expand(ast);
            // o();
            // o("nach Expand ----------------------------------------");
            // ast.Print(compact: false);
            // ast.Print(compact: true);
            

            if (!CheckForZeroSums(ast))
            {
                Console.WriteLine($"{c} is ***not*** OK!");
                // ast.Print();
                return false;
            }
            else
            {
                Console.WriteLine($"{c} is OK!");
                return true;
            }
        }

        private BinaryOperationNode Times(AstNode left, AstNode right)
        {
            return new BinaryOperationNode(left, TokenType.times, right);
        }

        /// <summary>
        /// Open the Yacas text file
        /// Read all expressions
        /// Validate the Simplify() statements against 0 as result
        /// </summary>
        public void Validate()
        {
            o($"Validating Yacas script file '{fileName}'");
            using var rd = OpenReader(fileName, Encoding.ASCII);
            int lineCount = 0;
            int errCount = 0;
            int okCount = 0;

            while (!rd.EndOfStream)
            {
                string line = rd.ReadLine().Trim();
                lineCount++;

                //  ignore empty lines and comments
                if (line.Length == 0) { continue; }
                if (line.StartsWith("#")) { continue; }

                switch (line[0])
                {
                    case 'c':
                        ProcessEntry(line, lineCount, elements); break;
                    case 'P':
                    case 'p':
                        ProcessEntry(line, lineCount, products); break;
                    case 'S':
                        if (ProcessSimplify(line, lineCount))
                        {
                            okCount++;
                        }
                        else
                        {
                            errCount++;
                        }
                        break;
                    default:
                        o($"Line {lineCount} does not start with 'c', 'p' or 'S' but with {line[0]}");
                        o("This does not look like a valid Yacas script line.");
                        Fatal($"Line {lineCount}: unexpected '{line}'");
                        break;
                }
            }

            o();
            if (errCount > 0)
            {
                o($"Errors found: {errCount}");
            }
            else if (okCount > 0)
            {
                o($"No errors found in '{fileName}'!");
                o($"[c] elements validated: {okCount}");
            }
            else
            {
                o("Nothing checked. Nothing validated!");
            }            
        }
    }
}
