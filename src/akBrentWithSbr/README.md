
# akBrentWidthSbr - Tool to solve Brent Equations modulo 2

```
usage: python -OO akBrentWithSbr.py [options] [problem]

find matrix multiplication algorithm mod 2

positional arguments:
  problem               matrix multiplication problem [default: 2x2x2_7]

options:
  -h, --help            show this help message and exit
  -f, --fix FIX         number of odd coefficient triples to fix to 1*1*1
                        [default:0]
  -s, --seed SEED       seed for random numbers [-1 = clock]
  -t, --threads THREADS
                        number of parallel threads
  -T, --timeout TIMEOUT
                        timeout in seconds [0 = no timeout]
  -v, --verbose
  -S, --solver [SOLVER]
                        solver to be used: yices, cvc5, z3 or sat_xxx
                        [default: z3]

contact: axel.kemper at Google's mail site
```
