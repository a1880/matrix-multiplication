class Array2D:
    def __init__(self, rows, cols):
        """
        Initialize a 2D array with the given dimensions (rows, cols).
        """
        self.rows = rows
        self.cols = cols
        self.data = [[None for _ in range(cols)] for _ in range(rows)]

    @classmethod
    def from_array(cls, data):
        """
        Construct Array2D object from existing 2D array
        """
        # Validate that the input is a 2D array (list of lists)
        if not isinstance(data, list) or not all(isinstance(row, list) for row in data):
            raise ValueError("Array2D: Input must be a 2D array (list of lists)")

        # Ensure all rows have the same length (rectangular array)
        num_cols = len(data[0])
        if not all(len(row) == num_cols for row in data):
            raise ValueError("All rows must have the same length")

        # Create a new TwoDArray instance
        rows = len(data)
        cols = num_cols
        instance = cls(rows, cols)

        # Copy the data into the instance
        for i in range(rows):
            for j in range(cols):
                instance[i, j] = data[i][j]

        return instance

    def __getitem__(self, indices):
        """
        Get the value at the specified indices (row, col).
        """
        row, col = self.verify(indices)
        return self.data[row][col]

    def __setitem__(self, indices, value):
        """
        Set the value at the specified indices (row, col).
        """
        row, col = self.verify(indices)
        self.data[row][col] = value

    def __str__(self):
        """
        Return a string representation of the 3D array.
        """
        return "\n".join(
            [
                " ".join([str(self.data[row][col]) for col in range(self.cols)])
                for row in range(self.rows)
            ]
        )

    def verify(self, indices):
        row, col = indices
        if not (0 <= row < self.rows and 0 <= col < self.cols):
            raise IndexError(
                f"Array2D: Index [{row}, {col}] out of range "
                + f"[0..{self.rows-1}, 0..{self.cols-1}]"
            )
        return row, col


"""
# Example usage:
if __name__ == "__main__":
    # Create a 2D array with dimensions 3x4
    arr = Array2D(3, 4)

    # Set some values
    arr[1, 2] = 5
    arr[2, 3] = 10

    # Get some values
    print(arr[1, 2])  # Output: 5
    print(arr[2, 3])  # Output: 10

    # Print the entire array
    print(arr)

    # Attempt to access an out-of-range index
    try:
        print(arr[3, 4])  # This will raise an IndexError
    except IndexError as e:
        print(e)  # Output: Index out of range
"""
