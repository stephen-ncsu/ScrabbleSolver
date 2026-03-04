using Scrabble;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrabbleSolver
{
    public class Word
    {
        public string Text
        {
            get;
            private set;
        }

        public void Lock()
        {
            Text = WordBuilder.ToString();
        }

        public List<Position> Positions { get; set; } = new List<Position>();

        public StringBuilder WordBuilder { get; set; } = new StringBuilder();

        public int GetPointValue(Dictionary<int, Enums.ScoreModifier> scoreModifiers, Dictionary<char, int> letterValues)
        {
            int wordMultiplier = 1;
            int totalPoints = 0;
            for (int i = 0; i < Text.Length; i++)
            {
                char letter = Text[i];
                int letterPointValue = letterValues[letter];
                Enums.ScoreModifier modifier = scoreModifiers[i];

                if (modifier == Enums.ScoreModifier.DoubleWord)
                {
                    wordMultiplier = wordMultiplier * 2;
                }

                if (modifier == Enums.ScoreModifier.TripleWord)
                {
                    wordMultiplier = wordMultiplier * 3;
                }

                if (modifier == Enums.ScoreModifier.DoubleLetter)
                {
                    letterPointValue = letterPointValue * 2;
                }

                if (modifier == Enums.ScoreModifier.TripleLetter)
                {
                    letterPointValue = letterPointValue * 3;
                }

                totalPoints += letterPointValue;
            }


            totalPoints = totalPoints * wordMultiplier;

            return totalPoints;

        }
    }

    public class WordComparer : IEqualityComparer<Word>
    {
        public bool Equals(Word x, Word y)
        {
            // Check for nulls and compare properties
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;

            // Quick length check
            if (x.Positions.Count != y.Positions.Count) return false;

            // Single-pass comparison of row and column positions
            for (int i = 0; i < x.Positions.Count; i++)
            {
                if (x.Positions[i].Col != y.Positions[i].Col || 
                    x.Positions[i].Row != y.Positions[i].Row)
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(Word obj)
        {
            if (obj == null) return 0;

            var hash = new HashCode();

            // Loop through the same data you use in Equals
            foreach (var pos in obj.Positions)
            {
                hash.Add(pos.Col);
                hash.Add(pos.Row);
            }

            return hash.ToHashCode();
        }
    }
}
