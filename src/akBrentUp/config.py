import os
import argparse
import time
from util import get_seconds_since_midnight, set_debug_level, str2bool

class Config:
    def __init__(self):
        """
        Initialize configuration.
        """
        parser = argparse.ArgumentParser(
            description="find matrix multiplication algorithm",
            epilog="contact: Axel@KemperZone.de",
            usage="python -OO %(prog)s [options] bini_scheme_file",
        )
        parser.add_argument(
            "-L",
            "--lift",
            nargs='?',  # optional
            default="direct",
            help="lifting method to be used: direct, hensel, groebner [default: direct]",
        )
        parser.add_argument(
            "-s",
            "--seed",
            type=int,
            default=0,
            help="seed for random numbers [-1 = clock, default: 0]",
        )
        parser.add_argument(
            "-t", "--threads", type=int, default=os.cpu_count(), help=f"number of parallel threads [default: {os.cpu_count()}]"
        )
        parser.add_argument(
            "-T",
            "--timeout",
            type=int,
            default=0,
            help="timeout in seconds [default: 0 = no timeout]",
        )
        parser.add_argument("-v", "--verbose", action="count", default=2)
        parser.add_argument(
            "-S",
            "--solver",
            nargs='?',  # optional
            default="z3",
            help="solver to be used: cvc5, minizinc, yices, z3 or sat_xxx [default: z3]",
        )
        parser.add_argument("--akbc", type=str2bool, nargs='?',
                            const=True, default=True,
                            help="Translate assertions via akBool2cnf")
        bini_default = "s2x1x2_04.bini.mod2.txt"
        # bini_default = "s5x5x5_93.bini.mod2.txt"
        # bini_default = "s2x2x2_07.bini.mod2.txt"
        # bini_default = "s6x6x6_153.bini.mod2.txt"
        # bini_default = "s2x3x2_11.bini.mod2.txt"
        # bini_default = "s4x4x4_47.Fawzi.bini.mod2.txt"
        parser.add_argument(
            "bini",
            nargs='?',  # optional
            default=bini_default,
            help=f"matrix multiplication problem in Bini form [default: {bini_default}]",
        )
        self.args = parser.parse_args()
        set_debug_level(self.debugLevel)

    @property
    def akbc(self) -> bool:
        return self.args.akbc

    @property
    def debugLevel(self) -> int:
        return self.args.verbose

    @property
    def bini_input_file(self) -> str:
        return self.args.bini

    @property
    def lift(self) -> str:
        return self.args.lift

    @property
    def seed(self) -> int:
        s = self.args.seed
        if s == -1:
            s = get_seconds_since_midnight()
            """  write back to get the same answer again """
            self.args.seed = s
        return s

    @property
    def solver(self) -> str:
        return self.args.solver

    @property
    def timeout(self) -> int:
        return self.args.timeout

    @property
    def threads(self) -> int:
        return self.args.threads


