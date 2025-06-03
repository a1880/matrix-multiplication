namespace akExtractMatMultSolution
{
    using static Util;

    public class DynArray3D<T>(T defaultValue)
    {
        private DynArray2D<T>[] arr = [];
        private readonly T defaultValue = defaultValue;
        public int Length => arr.Length;
        public bool Readonly = false;

        public T this[int x, int y, int z]
        {
            get => x < arr.Length ? arr[x][y, z] : defaultValue;

            set
            {
                CheckDimension(x);
                arr[x][y, z] = value;
                Assert(!Readonly);
                Assert(this[x, y, z].Equals(value));
            }
        }

        public T Default => defaultValue;

        private void CheckDimension(int x)
        {
            Assert(x >= 0);
            Assert(!Readonly);

            if (x >= Length)
            {
                //  it might be more efficient to allocate more here
                var tmp = new DynArray2D<T>[x + 1];

                for (int i = 0; i <= x; i++)
                {
                    tmp[i] = (i < Length) ? arr[i] : new DynArray2D<T>(defaultValue);
                }

                arr = tmp;
            }
        }

        public int GetLength(int n)
        {
            int len;

            Assert((n >= 0) && (n <= 2));

            if (n == 0)
            {
                len = Length;
            }
            else
            {
                len = 0;
                for (int x = 0; x < Length; x++)
                {
                    int lx = arr[x].GetLength(n - 1);

                    len = (lx > len) ? lx : len;
                }
            }

            return len;
        }
    }
}
