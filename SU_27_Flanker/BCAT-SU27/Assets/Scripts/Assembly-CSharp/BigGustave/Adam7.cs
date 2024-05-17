using System.Collections.Generic;

namespace BigGustave
{
    internal static class Adam7
    {
        private static readonly IReadOnlyDictionary<int, int[]> PassToScanlineGridIndex = new Dictionary<int, int[]>
    {
        {
            1,
            new int[1]
        },
        {
            2,
            new int[1]
        },
        {
            3,
            new int[1] { 4 }
        },
        {
            4,
            new int[2] { 0, 4 }
        },
        {
            5,
            new int[2] { 2, 6 }
        },
        {
            6,
            new int[4] { 0, 2, 4, 6 }
        },
        {
            7,
            new int[4] { 1, 3, 5, 7 }
        }
    };

        private static readonly IReadOnlyDictionary<int, int[]> PassToScanlineColumnIndex = new Dictionary<int, int[]>
    {
        {
            1,
            new int[1]
        },
        {
            2,
            new int[1] { 4 }
        },
        {
            3,
            new int[2] { 0, 4 }
        },
        {
            4,
            new int[2] { 2, 6 }
        },
        {
            5,
            new int[4] { 0, 2, 4, 6 }
        },
        {
            6,
            new int[4] { 1, 3, 5, 7 }
        },
        {
            7,
            new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 }
        }
    };

        public static int GetNumberOfScanlinesInPass(ImageHeader header, int pass)
        {
            int[] array = PassToScanlineGridIndex[pass + 1];
            int num = header.Height % 8;
            if (num == 0)
            {
                return array.Length * (header.Height / 8);
            }
            int num2 = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] < num)
                {
                    num2++;
                }
            }
            return array.Length * (header.Height / 8) + num2;
        }

        public static int GetPixelsPerScanlineInPass(ImageHeader header, int pass)
        {
            int[] array = PassToScanlineColumnIndex[pass + 1];
            int num = header.Width % 8;
            if (num == 0)
            {
                return array.Length * (header.Width / 8);
            }
            int num2 = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] < num)
                {
                    num2++;
                }
            }
            return array.Length * (header.Width / 8) + num2;
        }

        public static (int x, int y) GetPixelIndexForScanlineInPass(ImageHeader header, int pass, int scanlineIndex, int indexInScanline)
        {
            int[] array = PassToScanlineColumnIndex[pass + 1];
            int[] array2 = PassToScanlineGridIndex[pass + 1];
            int num = scanlineIndex % array2.Length;
            int num2 = indexInScanline % array.Length;
            int num3 = 8 * (scanlineIndex / array2.Length);
            return (8 * (indexInScanline / array.Length) + array[num2], num3 + array2[num]);
        }
    }
}