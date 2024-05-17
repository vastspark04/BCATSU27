using System.Collections.Generic;

namespace BigGustave
{

    public static class Adler32Checksum
    {
        private const int AdlerModulus = 65521;

        public static int Calculate(IEnumerable<byte> data)
        {
            int num = 1;
            int num2 = 0;
            foreach (byte datum in data)
            {
                num = (num + datum) % 65521;
                num2 = (num + num2) % 65521;
            }
            return num2 * 65536 + num;
        }
    }
}