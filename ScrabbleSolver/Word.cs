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

        public List<Tuple<char, int, int>> Positions { get; set; } = new List<Tuple<char, int, int>>();

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
}
