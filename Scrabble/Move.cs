using Scrabble;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrabbleSolver
{
    public class Move
    {
        char[,] _initialBoardState;
        char[,] _boardState;
        List<Position> _changedPositions = new List<Position>();

        public Move(char[,] boardState)
        {
            _boardState = boardState.Duplicate();
            _initialBoardState = boardState.Duplicate();
            _changedPositions = new List<Position>();
        }

        public int Score { get; set; }

        public char[,] GetBoardState()
        {
            return _boardState;
        }

        public char[,] GetInitialBoardState()
        {
            return _initialBoardState;
        }

        public List<Position> GetChangedPositions()
        {
            return _changedPositions;
        }

        public void AddNewLetter(char letter, int col, int row)
        {
            _dirtyCache = true;
            _boardState[col, row] = letter;

            _changedPositions.Add(new Position(col, row, letter));
        }

        public bool AreMovesEqual(Move comparedMove)
        {
            if(comparedMove == null)
            {
                return false;
            }

            var comparedMoveChangedPositions = comparedMove.GetChangedPositions().OrderBy(x => x.Row).ThenBy(x => x.Col);
            var myChangedPositions = this.GetChangedPositions().OrderBy(x=>x.Row).ThenBy(x=>x.Col);

            if(comparedMoveChangedPositions.Count() != myChangedPositions.Count())
            {
                return false;
            }

            for (int i = 0; i < comparedMoveChangedPositions.Count(); i++)
            {
                if (comparedMoveChangedPositions.ElementAt(i).Equals(myChangedPositions.ElementAt(i)) == false)
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return AreMovesEqual((Move)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (var position in _changedPositions)
                {
                    hash = hash * 31 + position.Col.GetHashCode();
                    hash = hash * 31 + position.Row.GetHashCode();
                    hash = hash * 31 + position.Letter.GetHashCode();
                }
                return hash;
            }
        }

        private List<Word> _wordCache = null;
        bool _dirtyCache = true;
        public List<Word> FindWords()
        {
            if(_wordCache == null || _dirtyCache)
            {
                _wordCache = FindWords(GetBoardState());
                _dirtyCache = false;
            }
            
            return _wordCache;
        }

        public List<Word> FindWords(char[,] board)
        {
            const int boardSize = 15;
            List<Word> words = new List<Word>();

            for (int currentRow = 0; currentRow < boardSize; currentRow++)
            {
                for (int currentColumn = 0; currentColumn < boardSize; currentColumn++)
                {
                    char tile = board[currentColumn, currentRow];
                    if (tile != ' ')
                    {
                        // Check horizontally
                        if (currentColumn == 0 || board[currentColumn - 1, currentRow] == ' ')
                        {
                            int columnPointer = currentColumn;
                            int wordLength = 0;

                            while (columnPointer < boardSize && board[columnPointer, currentRow] != ' ')
                            {
                                columnPointer++;
                                wordLength++;
                            }

                            if (wordLength > 1)
                            {
                                Word word = new Word();
                                word.Positions.Capacity = wordLength;

                                for (int col = currentColumn; col < currentColumn + wordLength; col++)
                                {
                                    char letter = board[col, currentRow];
                                    word.WordBuilder.Append(letter);
                                    word.Positions.Add(new Position(col, currentRow, letter));
                                }

                                word.Lock();
                                words.Add(word);
                            }
                        }

                        // Check vertically
                        if (currentRow == 0 || board[currentColumn, currentRow - 1] == ' ')
                        {
                            int rowPointer = currentRow;
                            int wordLength = 0;

                            while (rowPointer < boardSize && board[currentColumn, rowPointer] != ' ')
                            {
                                rowPointer++;
                                wordLength++;
                            }

                            if (wordLength > 1)
                            {
                                Word word = new Word();
                                word.Positions.Capacity = wordLength;

                                for (int row = currentRow; row < currentRow + wordLength; row++)
                                {
                                    char letter = board[currentColumn, row];
                                    word.WordBuilder.Append(letter);
                                    word.Positions.Add(new Position(currentColumn, row, letter));
                                }

                                word.Lock();
                                words.Add(word);
                            }
                        }
                    }
                }
            }

            return words;
        }

        public List<Word> FindNewWords()
        {
            var oldWords = FindWords(_initialBoardState);
            var newWords = FindWords();


            return newWords.Except(oldWords, new WordComparer()).ToList();
        }

        public bool AreCharArraysEqual(char[,] array1, char[,] array2)
        {
            // 1. Handle null references
            if (ReferenceEquals(array1, array2)) return true;
            if (array1 == null || array2 == null) return false;

            // 2. Check dimensions
            if (array1.Rank != array2.Rank) return false; // Always 2 for char[,] but good practice

            int rows = array1.GetLength(0);
            int cols = array1.GetLength(1);

            if (rows != array2.GetLength(0) || cols != array2.GetLength(1))
            {
                return false;
            }

            // 3. Compare individual elements
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (array1[i, j] != array2[i, j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
