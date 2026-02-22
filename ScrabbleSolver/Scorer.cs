using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrabbleSolver
{
    public class Scorer
    {
        Enums.ScoreModifier[,] scoringBoard = new Enums.ScoreModifier[15, 15]
        {
            { Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleWord, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleWord, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter },
            { Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleWord, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleWord, Enums.ScoreModifier.None },
            { Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None },
            { Enums.ScoreModifier.TripleWord, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleWord, Enums.ScoreModifier.None,Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleWord },
            { Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None },
            { Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None },
            { Enums.ScoreModifier.None,Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None },
            { Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleWord, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleWord, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter },
            { Enums.ScoreModifier.None,Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None },
            { Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None },
            { Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None },
            { Enums.ScoreModifier.TripleWord, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleWord, Enums.ScoreModifier.None,Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleWord },
            { Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None },
            { Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleWord, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleWord, Enums.ScoreModifier.None },
            { Enums.ScoreModifier.TripleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleWord, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.DoubleLetter, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleWord, Enums.ScoreModifier.None, Enums.ScoreModifier.None, Enums.ScoreModifier.TripleLetter }
        };


        Dictionary<char, int> _letterValues = new Dictionary<char, int>();

        public Scorer()
        {
            _letterValues.Add('A', 1);
            _letterValues.Add('B', 4);
            _letterValues.Add('C', 3);
            _letterValues.Add('D', 2);
            _letterValues.Add('E', 1);
            _letterValues.Add('F', 4);
            _letterValues.Add('G', 4);
            _letterValues.Add('H', 3);
            _letterValues.Add('I', 1);
            _letterValues.Add('J', 10);
            _letterValues.Add('K', 6);
            _letterValues.Add('L', 2);
            _letterValues.Add('M', 3);
            _letterValues.Add('N', 1);
            _letterValues.Add('O', 1);
            _letterValues.Add('P', 3);
            _letterValues.Add('Q', 10);
            _letterValues.Add('R', 1);
            _letterValues.Add('S', 1);
            _letterValues.Add('T', 1);
            _letterValues.Add('U', 2);
            _letterValues.Add('V', 6);
            _letterValues.Add('W', 5);
            _letterValues.Add('X', 8);
            _letterValues.Add('Y', 4);
            _letterValues.Add('Z', 10);
        }


        public int GetScoreForMove(Move move)
        {
            var boardState = move.GetBoardState();
            var newWords = move.FindNewWords();
            var changedPositions = move.GetChangedPositions();

            foreach(var newWord in newWords)
            {
                int wordMultiplier = 1;
                List<int> letterMultiplier = new List<int>();
                foreach(var wordPosition in newWord.Positions)
                {
                    var changedPositionsWithinWord = changedPositions.Where(x => x.Item2 == wordPosition.Item2 && x.Item3 == wordPosition.Item3);

                    foreach(var changedPositionWithinWord in changedPositionsWithinWord)
                    {
                        var scoringModifier = scoringBoard[changedPositionWithinWord.Item2, changedPositionWithinWord.Item3];
                        switch (scoringModifier)
                        {
                            case Enums.ScoreModifier.TripleWord:
                                wordMultiplier = wordMultiplier * 3;
                                letterMultiplier.Add(1);
                                break;
                            case Enums.ScoreModifier.DoubleWord:
                                wordMultiplier = wordMultiplier * 2;
                                letterMultiplier.Add(1);
                                break;
                            case Enums.ScoreModifier.TripleLetter:
                                letterMultiplier.Add(2);
                                break;
                            case Enums.ScoreModifier.DoubleLetter:
                                letterMultiplier.Add(2);
                                break;
                            default:
                                letterMultiplier.Add(1);
                                break;
                        }
                    }
                }



            }

            return 1;
        }
    }
}
