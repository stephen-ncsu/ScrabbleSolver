using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrabbleSolver
{
    public static class ArrayExtensions
    {
        public static char[,] Duplicate(this char[,] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            int rows = array.GetLength(0);
            int cols = array.GetLength(1);

            char[,] clonedArray = new char[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    clonedArray[i, j] = array[i, j];
                }
            }

            return clonedArray;
        }
    }
}
