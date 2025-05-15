using static akExtractMatMultSolution.Util;

namespace akExtractMatMultSolution
{
    class DynArray1D<T>(T defaultValue)
    {
        private T[] arr = [];
        private readonly T defaultValue = defaultValue;

        public int Length => arr.Length;
        public bool Readonly = false;

        public T this[int x]
        {
            get => x < Length ? arr[x] : defaultValue;

            set
            {
                CheckDimension(x);
                arr[x] = value;
                Assert(!Readonly);
                Assert(this[x].Equals(value));
            }
        }

        private void CheckDimension(int x)
        {
            Assert(x >= 0);
            Assert(!Readonly);

            if (x >= Length)
            {
                //  it might be more efficient to allocate more here
                var tmp = new T[x + 1];

                for (int i = 0; i <= x; i++)
                {
                    tmp[i] = (i < Length) ? arr[i] : defaultValue;
                }

                arr = tmp;
            }
        }
    }
}
