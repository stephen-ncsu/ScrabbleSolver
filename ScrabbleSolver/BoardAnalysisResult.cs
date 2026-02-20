using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrabbleSolver
{
    public class BoardAnalysisResult
    {
        public char[,] BoardState { get; set; }
        public int ErrorCount { get; set; }
    }
}
