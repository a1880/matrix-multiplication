import argparse
from util import (set_debug_level, str2bool)


class Config:
    def __init__(self):
        """
        Initialize configuration.
        """
        parser = argparse.ArgumentParser(
            description="find matrix multiplication algorithm mod 2",
            epilog="contact: Axel@KemperZone.de",
            usage="python -OO %(prog)s [options] [problem]",
        )
        parser.add_argument(
            "-f",
            "--fix",
            type=int,
            default=0,
            help="number of odd coefficient triples to fix to 1*1*1 [default:0]",
        )
        parser.add_argument(
            "-s",
            "--seed",
            type=int,
            default=0,
            help="seed for random numbers [-1 = clock]",
        )
        parser.add_argument(
            "-t", "--threads", type=int, default=1, help="number of parallel threads"
        )
        parser.add_argument(
            "-T",
            "--timeout",
            type=int,
            default=0,
            help="timeout in seconds [0 = no timeout]",
        )
        parser.add_argument("-v", "--verbose", action="count", default=0)
        parser.add_argument(
            "-S",
            "--solver",
            nargs='?',  # optional
            default="sat_clasp",
            help="solver to be used: yices, cvc5, z3 or sat_xxx [default: sat_clasp]",
        )
        parser.add_argument("--akbc", type=str2bool, nargs='?',
                            const=True, default=True,
                            help="Translate assertions via akBool2cnf")
        parser.add_argument(
            "problem",
            nargs='?',  # optional
            default="1x2x1_2",
            help="matrix multiplication problem [default: 1x2x1_2]",
        )
        self.args = parser.parse_args()
        set_debug_level(self.debugLevel)

    @property
    def akbc(self):
        return self.args.akbc

    @property
    def debugLevel(self):
        return self.args.verbose

    @property
    def fix(self):
        return self.args.fix

    @property
    def problem(self):
        return self.args.problem

    @property
    def seed(self):
        return self.args.seed

    @property
    def solver(self):
        return self.args.solver

    @property
    def timeout(self):
        return self.args.timeout

    @property
    def threads(self):
        return self.args.threads
