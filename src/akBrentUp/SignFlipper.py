"""  SignFlipper.py  -  module to beautify the +/- signs in a solution  """        
from MatMultDim import MatMultDim
from util import o

product_index_digits = 2

MatNames = ["a", "b", "c"]


def beautify_literals(solution, mmDim: MatMultDim):
    """
    For every product, count the +/- signs in all three terms.
    Depending on the counts, flip the signs in F+G, F+D, or G+D
    """
    global product_index_digits
        
    reducedMinusSigns = 0
    product_index_digits = len(str(mmDim.noOfProducts))
    
    for product in mmDim.Products:
        aPlus, aMinus = get_term_plus_minus(product, mmDim.MatF, mmDim, solution)
        bPlus, bMinus = get_term_plus_minus(product, mmDim.MatG, mmDim, solution)
        cPlus, cMinus = get_term_plus_minus(product, mmDim.MatD, mmDim, solution)

        #  zero or two parts can be flipped without altering the product
        #  we compare the outcomes of the four possible cases and chose the
        #  one with the least number of minus signs
        flip = [True, True, False]
        least = aPlus + bPlus + cMinus
        minusSigns = aPlus + bMinus + cPlus
        if minusSigns < least:
            least = minusSigns
            flip = [True, False, True]
        minusSigns = aMinus + bPlus + cPlus
        if minusSigns < least:
            least = minusSigns
            flip = [False, True, True]
        minusSigns = aMinus + bMinus + cMinus
        if minusSigns < least:
            flip = [False, False, False]
        reducedMinusSigns += minusSigns - least

        flip_literals(solution, mmDim, product, flip)

    if reducedMinusSigns > 0:
        o(f"# Minus signs reduced by literal sign flipping: {reducedMinusSigns}")
        o("#")
    else:
        o("# No minus signs reduction possible.")
        o("#")


def get_term_plus_minus(product, fgd, mmDim: MatMultDim, solution):
    """
    Count number of plus/minus elements in term
    The first element is counted twice to avoid leading '-' signs
    param: product: Product index (0-based)
    param: fgd: index for coefficient matrix for term
    return: number of positive/negative elements
    """
    plus = 0
    minus = 0
    first = True
    
    for row, col in mmDim.indices_fgd(fgd):
        lit = literal(MatNames[fgd], row, col, product)
        val = solution.get(lit, 0)

        if val > 0:
            plus += 2 if first else 1
            first = False
        elif val < 0:
            minus += 2 if first else 1
            first = False

    return plus, minus

        
def flip_literals(solution, mmDim: MatMultDim, product, flip):
    """
    Go through all three literal arrays
    and flip the literal signs for product, if flip[] is true
    param: product: The product
    param: flip: Array of switches to control/steer flipping
    """
    for fgd in range(3):
        if flip[fgd]:
            for row, col in mmDim.indices_fgd(fgd):
                lit = literal(MatNames[fgd], row, col, product)
                val = solution.get(lit, 0)

                if val != 0:
                    solution[lit] = -val


def literal(name, row, col, product):
    return f"{name}{row+1}{col+1}_{str(product+1).zfill(product_index_digits)}"

