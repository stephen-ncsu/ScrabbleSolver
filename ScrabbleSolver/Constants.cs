using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrabbleSolver
{
    public class Constants
    {
        public static int CC_STAT_LEFT = 0;
        public static int CC_STAT_TOP = 1;
        public static int CC_STAT_WIDTH = 2;
        public static int CC_STAT_HEIGHT = 3;
        public static int CC_STAT_AREA = 4;
        public static Scalar[] Colors = new Scalar[5]
        {
            Scalar.Red,
            Scalar.Green,
            Scalar.Blue,
            Scalar.Orange,
            Scalar.Purple
        };
    }
}
