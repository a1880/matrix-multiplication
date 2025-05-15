
# akExtractMatMultSolution  -  Recover or Convert Matrix Multiplication Solution

Call with "--help" to get the usage instructions.

```
akExtractMatMultSolution  -  Recover or Convert Matrix Multiplication Solution
------------------------------------------------------------------------------
Compiled: 14.05.2025

DEBUG mode

usage:

akExtractMatMultSolution akboole_solution_file [yacas_script_output_file [brent_equations_file]]
Translate akBoole solution file to Yacas algorithm file.

or

akExtractMatMultSolution --sat akboole_cnf_file sat_solution_file [yacas_script_output_file [brent_equations_file]]
Translate akBool CNF input and SAT solver solution file to Yacas algorithm file

or

akExtractMatMultSolution --gringo gringo_solution_file [yacas_script_output_file [brent_equations_file]]
Translate Gringo solution file to Yacas algorithm file

or

akExtractMatMultSolution --bini bini_solution_file [yacas_script_output_file [brent_equations_file]]
Translate solution from Bini matrix form in Yacas algorithm file

or

akExtractMatMultSolution --binigen yacas_solution_file [bini_output_file [brent_equations_file]]
Translate Yacas algorithm file to Bini matrix format

or

akExtractMatMultSolution --tensor tensor_solution_file [yacas_script_file [brent_equations_file]]
Translate tensor solution format to Yacas algorithm file.
In tensor solution form, each product is a product of a, b and c sums.

or

akExtractMatMultSolution --tensorgen yacas_solution_file [tensor_output_file [brent_equations_file]]
Translate Yacas algorithm file to tensor solution form.

or

akExtractMatMultSolution --combine akboole_mod2_solution akboole_rev_solution [yacas_script_output_file [brent_equations_file]]
Combine akBoole solutions for the mod2 case and the revised case into a universal Yacas algorithm file.
The revised solution provides '+'/'-' signs for the mod 2 solution. 1 = '+', 0 = '-'

or

akExtractMatMultSolution --yacas yacas_solution_file [unified_yacas_script_output_file [brent_equations_file]]
Unify a Yacas algorithm to adjust syntax and layout.

or

akExtractMatMultSolution --graph yacas_solution_file [graphviz_detail_file [graphviz_overview_file [product_report_file]]]
Translate Yacas algorithm file into a GraphViz graph

or

akExtractMatMultSolution --reduce yacas_solution_file ixjxn [yacas_script_output_file]
Derive a reduced solution for the ixjxn problem.
i = number of A rows, j = number of A cols, n = number of C columns
By default, one row and one column are eliminated from product matrix [C].

or

akExtractMatMultSolution --literals yacas_solution_file [akboole_solution_file [brent_equations_file]]
Translate Yacas algorithm file to akBoole solution format (= list of literals and their values).

or

akExtractMatMultSolution --contrib yacas_solution_file [product_contributions_file [brent_equations_file]]
Show which coefficient contributes to which product

or

akExtractMatMultSolution --cse yacas_solution_file [simplified_yacas_script_output_file [brent_equations_file]]
Strive to eliminate common subexpressions from sums in algorithm.

Note: use empty string "" or "none" to suppress output of brent_eqations_file
```

