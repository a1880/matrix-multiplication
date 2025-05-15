
# akBrentUp - Lift mod 2 matrix multiplication algorithms to Z

```
usage: python -OO akBrentUp.py [options] bini_scheme_file

find matrix multiplication algorithm

positional arguments:
  bini                  matrix multiplication problem in Bini form [default:
                        s2x1x2_04.bini.mod2.txt]

options:
  -h, --help            show this help message and exit
  -L, --lift [LIFT]     lifting method to be used: direct, hensel, groebner
                        [default: direct]
  -s, --seed SEED       seed for random numbers [-1 = clock, default: 0]
  -t, --threads THREADS
                        number of parallel threads [default: 28]
  -T, --timeout TIMEOUT
                        timeout in seconds [default: 0 = no timeout]
  -v, --verbose
  -S, --solver [SOLVER]
                        solver to be used: cvc5, minizinc, yices, z3 or
                        sat_xxx [default: z3]
  --akbc [AKBC]         Translate assertions via akBool2cnf

contact: axel.kemper at Google's mail site
```