import datetime
import io
import os
import sys

""" global flag: matrix [C] is assumed to be transposed """

c_is_transposed: bool = True

""" global set to collect coefficients in input scheme """

coefficients = {0}


"""  consistency checking a la assert() """
class CheckException(Exception):

    def __init__(self, value):
        self.value = value

    def __str__(self):
        return str(self.value)


def check(condition, message):
    """
    Throw CheckException if condition is not fulfilled
    :param condition: The condition
    :param message: The message
    """
    if not condition:
        raise CheckException(message)


def datestamp():
    now = datetime.datetime.now()
    mmm = [
        "Jan",
        "Feb",
        "Mar",
        "Apr",
        "May",
        "Jun",
        "Jul",
        "Aug",
        "Sep",
        "Oct",
        "Nov",
        "Dec",
    ][now.month - 1]
    return f"{now.day:02}-{mmm}-{now.year} "


def timestamp():
    now = datetime.datetime.now()
    return f"{datestamp()} {now.hour:02}:{now.minute:02}:{now.second:02}"


def exists(file_name):
    """Returns true iff fileName belongs to an existing file"""
    return os.path.exists(file_name) and os.path.isfile(file_name)


def fatal(msg):
    print()
    print(f"Fatal error: {msg}")
    finish(1)


def finish(status):
    print("", flush=True)
    print("", flush=True)
    exit(status)


def get_problem_parameters(lines: list[str]):
    reg_a = RowColRegistry("a")
    reg_b = RowColRegistry("b")
    reg_c = RowColRegistry("c")
    product = 0

    for line in lines:
        if line.startswith("#"):
            continue
        if len(line) < 3:
            continue
        product += 1
        reg_a.register_literals(line)
        reg_b.register_literals(line)
        reg_c.register_literals(line)

    reg_a.validate_completeness()
    reg_b.validate_completeness()
    reg_c.validate_completeness()

    a_rows = reg_a.rows
    a_cols = reg_a.cols
    b_cols = reg_b.cols

    b_rows = reg_b.rows
    c_rows = reg_c.rows
    c_cols = reg_c.cols

    if c_is_transposed:
        check(a_rows == c_cols, f"a_rows {a_rows} != c_colss {c_cols}")
        check(a_cols == b_rows, f"a_cols {a_cols} != b_rows {b_rows}")
        check(b_cols == c_rows, f"b_cols {b_cols} != c_rows {c_rows}")
    else:
        check(a_rows == c_rows, f"a_rows {a_rows} != c_rows {c_rows}")
        check(a_cols == b_rows, f"a_cols {a_cols} != b_rows {b_rows}")
        check(b_cols == c_cols, f"b_cols {b_cols} != c_cols {c_cols}")

    return a_rows, a_cols, b_cols, product


def get_term(name: str, line: str) -> str:
    term = ""
    sep = ""
    for pos in range(len(line)):
        if line[pos] == name:
            if pos > 0:
                if line[pos-1] == '(':
                    coefficients.add(1)
                elif line[pos-1] == '+':
                    coefficients.add(1)
                elif line[pos-1] == '-':
                    coefficients.add(-1)
                elif line[pos-1] == '*':
                    d = int(line[pos-2])
                    if pos > 2:
                        if line[pos-3] == '-':
                            d = -d

                    coefficients.add(d)

                    if d % 2 == 0:
                        """ disregard even-valued coefficients """
                        continue
            lit = line[pos:pos+3]
            term += f"{sep}{lit}"
            sep = " + "
    return f"({term})"


def o(s:str = "") -> None:
    print(s)


def read_input_file(input_file_name: str) -> list[str]:
    check(exists(input_file_name), f"Input file '{input_file_name}' not found")
    lines = []
    o(f"Reading input file '{input_file_name}'")
    with open(input_file_name) as input_file:
        for line in input_file:
            line = line.replace("\n", "").strip()
            lines.append(line)
    o(f"Reading completed. Lines: {len(lines)}")
    return lines


class RowColRegistry:

    def __init__(self, name):
        self.name = name
        self.dic: dict[str, int] = {}
        self.rows: int = 0
        self.cols: int = 0

    def register_literal(self, literal: str):
        check(literal.startswith(self.name), f"Literal '{literal}' unsuitable for registry '{self.name}'")
        check(len(literal) == 3, f"Literal '{literal}' has unexpected format with len != 3")
        row = int(literal[1:2])
        col = int(literal[2:3])
        self.rows = max(row, self.rows)
        self.dic[literal] = self.dic.get(literal, 0) + 1
        self.cols = max(col, self.cols)

    def register_literals(self, line: str) -> None:
        for pos in range(len(line)):
            if line[pos] == self.name:
                self.register_literal(line[pos:pos+3])


    def validate_completeness(self):
        err_cnt = 0
        for row in range(self.rows):
            for col in range(self.cols):
                literal = f"{self.name}{row+1}{col+1}"
                if literal not in self.dic.keys():
                    err_cnt += 1
        if err_cnt == 0:
            o(f"RowColRegistry: {self.name} is complete! All {self.rows}x{self.cols} literals registered")
        else:
            o(f"RowColRegistry: {self.name} is incomplete! Missing: {err_cnt} literals")


def write_tensor_mod2_file(input_file_name: str, output_file_name: str, lines: list[str]) -> None:
    o(f"Writing MatMult tensor file '{output_file_name}'")
    output_file = io.open(output_file_name, "wt", encoding="ascii")

    i_name = os.path.basename(input_file_name)
    f_name = os.path.basename(output_file_name)
    output_file.write(f"# MatMult Tensor file modulo 2: {f_name}\n")
    output_file.write(f"# Source file: {i_name}\n")
    output_file.write(f"# Created:     {timestamp()}\n")
    output_file.write("#\n")
    output_file.write("# Contact:      axel.kemper@gmail.com\n")
    output_file.write("#\n")

    for line in lines:
        if line.startswith("#") or (len(line) < 3):
            output_file.write(f"{line}\n")
        else:
            a = get_term("a", line)
            b = get_term("b", line)
            c = get_term("c", line)
            output_file.write(f"{a} * {b} * {c}\n")

    s = ""
    sep = ""
    for c in sorted(coefficients):
        s = f"{s}{sep}{c}"
        sep = ", "

    output_file.write("#\n")
    output_file.write(f"# Coefficients in input file: {{{s}}}\n")
    output_file.write("#\n")

    o(f"MatMult tensor file '{output_file_name}' written. Lines: {len(lines) + 6}")


def main():
    """
    Use main() to avoid global variables
    """
    global product_index_digits
    
    o("[]--------------------------------------------------------------[]")
    o("|                                                                |")
    o("|  akTensorMod2 -  Convert a Matrix Multiplication tensor file   |")
    o("|                  to a tensor file with Z2 coefficients         |")
    o("|                                                                |")
    o("|  Axel Kemper   14-May-2025                                     |")
    o("|                                                                |")
    o("[]--------------------------------------------------------------[]")
    o()

    check(len(sys.argv) == 2, "No filename on commandline")
    input_file_name = sys.argv[1]

    lines = read_input_file(input_file_name)
    a_rows, a_cols, b_cols, products = get_problem_parameters(lines)
    o(f"MatMult problem signature {a_rows}x{a_cols}x{b_cols}_{products}")

    path = os.path.dirname(input_file_name)
    if path == "":
        output_file_name = f"s{a_rows}x{a_cols}x{b_cols}_{products}.tensor.mod2.txt"
    else:
        output_file_name = f"{path}\\s{a_rows}x{a_cols}x{b_cols}_{products}.tensor.mod2.txt"

    write_tensor_mod2_file(input_file_name, output_file_name, lines)

    finish(0)


"""
MAIN program  starts here
"""
if __name__ == "__main__":
    main()
