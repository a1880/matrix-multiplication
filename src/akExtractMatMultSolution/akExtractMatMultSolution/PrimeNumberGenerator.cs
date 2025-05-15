using System;
using System.Collections;
using System.Collections.Generic;

namespace akExtractMatMultSolution
{
    public class PrimeNumberGenerator : IEnumerable<int>
    {
        private int start;
        private int numbersLeft;
        public IEnumerator<int> Primes(int start, int len)
        {
            this.start = start + ((start + 1) % 2);
            
            numbersLeft = len;

            return GetEnumerator();
        }

        // Return true if the value is prime.
        private bool IsOddPrime(long value)
        {
            long sqrt = (long)Math.Sqrt(value);
            for (long i = 3; i <= sqrt; i += 2)
            {
                if (value % i == 0)
                {
                    return false;
                }
            }

            // If we get here, value is prime.
            return true;
        }

        public IEnumerator<int> GetEnumerator()
        {
            // Generate odd primes.
            for (int i = start; ; i += 2)
            {
                if (numbersLeft < 1)
                {
                    yield break;
                }
                if (IsOddPrime(i))
                {
                    numbersLeft--;
                    yield return i;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
