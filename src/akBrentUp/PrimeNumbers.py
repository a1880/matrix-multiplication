class PrimeNumbers:
    def __init__(self, start_value = 2):
        self.current = start_value

    def __iter__(self):
        return self

    def __next__(self):
        while True:
            if self.is_prime(self.current):
                prime = self.current
                self.current += 1
                return prime
            self.current += 1

    def is_prime(self, n):
        if n <= 1:
            return False
        if n <= 3:
            return True
        if n % 2 == 0 or n % 3 == 0:
            return False
        i = 5
        while i * i <= n:
            if n % i == 0 or n % (i + 2) == 0:
                return False
            i += 6
        return True

# Example usage:
# prime_iterator = PrimeNumbers()
# for _ in range(10):
#    print(next(prime_iterator))
