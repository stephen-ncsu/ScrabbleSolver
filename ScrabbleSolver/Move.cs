using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrabbleSolver
{
    public class Move
    {
        char[,] _boardState;
        List<Tuple<int, int, char>> _changedPositions = new List<Tuple<int, int, char>>();

        public Move(char[,] boardState)
        {
            _boardState = boardState;
            _changedPositions = new List<Tuple<int, int, char>>();
        }

        public char[,] GetBoardState()
        {
            return _boardState;
        }

        public List<Tuple<int, int, char>> GetChangedPositions()
        {
            return _changedPositions;
        }

        //public void SetChangedPositions(char[,] otherBoardState)
        //{
        //    int rows = _boardState.GetLength(0);
        //    int cols = _boardState.GetLength(1);
        //    char[,] changedPositions = new char[rows, cols];
        //    for (int i = 0; i < rows; i++)
        //    {
        //        for (int j = 0; j < cols; j++)
        //        {
        //            if (_boardState[i, j] != otherBoardState[i, j])
        //            {
        //                _changedPositions[i, j] = _boardState[i, j];
        //            }
        //        }
        //    }
        //}

        public void AddNewLetter(char letter, int row, int col)
        {
            _boardState[row, col] = letter;

            _changedPositions.Add(new Tuple<int, int, char>(row, col, letter));
        }

        public bool AreMovesEqual(Move comparedMove)
        {
            if(comparedMove == null)
            {
                return false;
            }

            var comparedMoveChangedPositions = comparedMove.GetChangedPositions();
            var myChangedPositions = this.GetChangedPositions();
            if(CompareTuples(comparedMoveChangedPositions, myChangedPositions))
            {
                return true;
            }

            return false;
        }

        public bool CompareTuples(List<Tuple<int, int, char>> list1, List<Tuple<int, int, char>> list2)
        {
            if (list1.Count != list2.Count)
                return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i].Item1 != list2[i].Item1 || list1[i].Item2 != list2[i].Item2 || list1[i].Item3 != list2[i].Item3)
                    return false;
            }

            for (int i = 0; i < list2.Count; i++)
            {
                if (list1[i].Item1 != list2[i].Item1 || list1[i].Item2 != list2[i].Item2 || list1[i].Item3 != list2[i].Item3)
                    return false;
            }


            return true;
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
