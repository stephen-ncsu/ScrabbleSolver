using System;
using System.Collections.Generic;
using System.Text;

namespace Scrabble
{
    public class Position
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public char Letter { get; set; }
        public Position(int col, int row, char letter)
        {
            Row = row;
            Col = col;
            Letter = letter;
        }

        public override bool Equals(object obj)
        {
            if (obj is Position other)
            {
                return this.Row == other.Row && this.Col == other.Col;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Col);
        }
    }
}
