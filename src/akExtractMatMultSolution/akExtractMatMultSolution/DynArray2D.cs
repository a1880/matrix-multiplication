using static akExtractMatMultSolution.Util;

namespace akExtractMatMultSolution
{
    class DynArray2D<T>(T defaultValue)
    {
        private DynArray1D<T>[] arr = [];
        private readonly T defaultValue = defaultValue;
        public int Length => arr.Length;
        public bool Readonly = false;

        public T this[int x, int y]
        {
            get => x < arr.Length ? this.arr[x][y] : this.defaultValue;

            set
            {
                CheckDimension(x);
                arr[x][y] = value;
                Assert(!Readonly);
                Assert(this[x, y].Equals(value));
            }
        }

        private void CheckDimension(int x)
        {
            Assert(x >= 0);
            Assert(!Readonly);

            if (x >= Length)
            {
                //  it might be more efficient to allocate more here
                var tmp = new DynArray1D<T>[x + 1];

                for (int i = 0; i <= x; i++)
                {
                    tmp[i] = (i < Length) ? arr[i] : new DynArray1D<T>(defaultValue);
                }

                arr = tmp;
            }
        }

        public int GetLength(int n)
        {
            int len;

            if (n == 0)
            {
                len = Length;
            }
            else
            {
                len = 0;
                for (int x = 0; x < Length; x++)
                {
                    int lx = arr[x].Length;

                    len = (lx > len) ? lx : len;
                }
            }

            return len;
        }
    }
}
