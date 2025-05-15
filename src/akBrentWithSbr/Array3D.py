class Array3D:
    def __init__(self, depth, rows, cols):
        """
        Initialize a 3D array with the given dimensions (depth, rows, cols).
        """
        self.depth = depth
        self.rows = rows
        self.cols = cols
        self.data = [
            [[None for _ in range(cols)] for _ in range(rows)] for _ in range(depth)
        ]

    @classmethod
    def from_array(cls, array_3d):
        """
        Initializes an Array3D object from an existing 3D array.

        Args:
            array_3d: The 3D array to initialize from.  Assumed to be a list of lists of lists.

        Returns:
            An Array3D object.

        Raises:
            TypeError: If the input is not a list of lists of lists.
            ValueError: If the dimensions of the input array are inconsistent or empty.
        """

        if (
            not isinstance(array_3d, list)
            or not all(isinstance(row, list) for row in array_3d)
            or not all(isinstance(item, list) for row in array_3d for item in row)
        ):
            raise TypeError(
                "Array3D: Input must be a 3D list (list of lists of lists)."
            )

        depth = len(array_3d)
        if depth == 0:
            raise ValueError("Array3D: Input array cannot be empty.")
        rows = len(array_3d[0])
        if rows == 0 or not all(len(row) == rows for row in array_3d):
            raise ValueError("Array3D: Inconsistent row-dimension size.")
        cols = len(array_3d[0][0])
        if cols == 0 or not all(len(item) == cols for row in array_3d for item in row):
            raise ValueError("Array3D: Inconsistent col-dimension size.")

        new_array = cls(
            depth, rows, cols
        )  # Use the normal constructor for initialization

        for d in range(depth):
            for row in range(rows):
                for col in range(cols):
                    new_array.data[d][row][col] = array_3d[d][row][col]

        return new_array

    def __getitem__(self, indices):
        """
        Get the value at the specified indices (depth, row, col).
        """
        depth, row, col = self.verify(indices)
        if not (
            0 <= depth < self.depth and 0 <= row < self.rows and 0 <= col < self.cols
        ):
            raise IndexError("Index out of range")
        return self.data[depth][row][col]

    def __setitem__(self, indices, value):
        """
        Set the value at the specified indices (depth, row, col).
        """
        depth, row, col = self.verify(indices)
        if not (
            0 <= depth < self.depth and 0 <= row < self.rows and 0 <= col < self.cols
        ):
            raise IndexError("Index out of range")
        self.data[depth][row][col] = value

    def __str__(self):
        """
        Return a string representation of the 3D array.
        """
        return "\n".join(
            [
                "\n".join(
                    [
                        " ".join(
                            [
                                str(self.data[depth][row][col])
                                for col in range(self.cols)
                            ]
                        )
                        for row in range(self.rows)
                    ]
                )
                for depth in range(self.depth)
            ]
        )

    def verify(self, indices):
        depth, row, col = indices
        if not (
            0 <= depth < self.depth and 0 <= row < self.rows and 0 <= col < self.cols
        ):
            raise IndexError(
                f"Array3D: Index [{depth}, {row}, {col}] out of range "
                + f"[0..{self.depth-1}, 0..{self.rows-1}, 0..{self.cols-1}]"
            )
        return depth, row, col


"""
# Example usage:
if __name__ == "__main__":
    # Create a 3D array with dimensions 2x3x4
    arr = Array3D(2, 3, 4)

    # Set some values
    arr[0, 1, 2] = 5
    arr[1, 2, 3] = 10

    # Get some values
    print(arr[0, 1, 2])  # Output: 5
    print(arr[1, 2, 3])  # Output: 10

    # Print the entire array
    print(arr)

    # Attempt to access an out-of-range index
    try:
        print(arr[2, 3, 4])  # This will raise an IndexError
    except IndexError as e:
        print(e)  # Output: Index out of range
"""
