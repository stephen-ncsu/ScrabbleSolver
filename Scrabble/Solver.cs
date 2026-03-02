using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IronOcr.OcrResult;

namespace ScrabbleSolver
{
    public class Solver
    {
        Move _baseMove = null;
        List<Move> _possibleMoves = new List<Move>();

        HashSet<string> _dictionary = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> _validSubstrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> _invalidSubstrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<List<char>> _allPermutations;
        private List<char> _impossibleLetters = new List<char>();
        private HashSet<string> _dictionarySubstringIndex;
        private List<Word> _baseWords;
        private List<char> _remainingLetters;

        public Solver(char[,] board, string rack)
        {
            _baseMove = new Move(board);
            _remainingLetters = rack.ToCharArray().ToList();
            _dictionary = System.IO.File.ReadAllLines("myDictionary.txt").ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public List<Move> Solve()
        {
            var baseWords = _baseMove.FindWords();
            _baseWords = baseWords;
            HashSet<char> usableCharacters = FindChars(baseWords, _remainingLetters);

            _dictionary = _dictionary.Where(word => word.All(c => usableCharacters.Contains(c))).ToHashSet(StringComparer.OrdinalIgnoreCase);

            Serilog.Log.Logger.Information("Found words: " + String.Join(',', baseWords.Select(x=>x.Text).ToArray()));
            var errorWords = ValidateWords(baseWords);
            Serilog.Log.Logger.Information("Error words: " + String.Join(',', errorWords.Select(x=>x.Text).ToArray()));

            Serilog.Log.Logger.Information("Building substring index from dictionary...");
            _dictionarySubstringIndex = BuildSubstringIndex(_dictionary);
            Serilog.Log.Logger.Information($"Substring index built successfully with {_dictionarySubstringIndex.Count} entries.");

            _allPermutations = GenerateUniquePermutations(_remainingLetters);

            //RemoveImpossiblePermuatations();

            //List<object> possibleMoves = new List<object>();
            var playablePositions = GetPlayablePositions();
            var stopwatch = new Stopwatch();

            for(int i = 0; i < playablePositions.GetLength(0); i++)
            {
                for(int j = 0; j < playablePositions.GetLength(1); j++)
                {
                    if (playablePositions[i, j] == 1)
                    {
                        if(_baseMove.GetBoardState()[i, j] != ' ')
                        {
                            continue;
                        }

                        stopwatch.Restart();
                        DetectImpossibleLetters(_remainingLetters, i, j);

                        var newMoves = GenerateMoves(i, j);
                        
                        if(newMoves.Any())
                        {
                            _possibleMoves.AddRange(newMoves);
                        }
                        stopwatch.Stop();
                        Serilog.Log.Logger.Information($"Generated moves for position ({i}, {j}) in {stopwatch.ElapsedMilliseconds} ms. Total moves: {_possibleMoves.Count}");
                    }
                }
            }

            return _possibleMoves;
        }

        private void RemoveImpossiblePermuatations()
        {
            List<string> invalidSubstrings = new List<string>();
            foreach(var permuatation in _allPermutations)
            {
                var subStringIndex = BuildSubstringIndex(new string(permuatation.ToArray()));

                foreach(var substring in subStringIndex)
                {
                    if(_dictionarySubstringIndex.Contains(substring) == false)
                    {
                        if (invalidSubstrings.Contains(substring) == false)
                        {
                            invalidSubstrings.Add(substring);
                        }
                    }
                }
            }

            foreach(var invalidSubstring in invalidSubstrings)
            {
                foreach (var permuatation in _allPermutations)
                {
                    int index = 0;
                    int? subStringStartIndex = null;

                    while (index < permuatation.Count)
                    {
                        if (permuatation[index] == invalidSubstring[0])
                        {
                            bool match = true;
                            subStringStartIndex = index;
                            for (int j = 1; j < invalidSubstring.Length; j++)
                            {
                                if (index + j >= permuatation.Count || permuatation[index + j] != invalidSubstring[j])
                                {
                                    match = false;
                                    subStringStartIndex = null;
                                    break;
                                }
                            }

                            if (match)
                            {
                                break;
                            }
                        }
                        index++;
                    }

                    if (subStringStartIndex != null)
                    {
                        for (int i = subStringStartIndex.Value + invalidSubstring.Length - 1; i < subStringStartIndex + invalidSubstring.Length; i++)
                        {
                            permuatation.RemoveAt(i);
                        }
                    }
                }
            }

            _allPermutations = _allPermutations.Where(permutation => permutation.Count > 0).ToList();

        }

        private void DetectImpossibleLetters(List<char> letters, int row, int col)
        {
            _impossibleLetters = new List<char>();
            foreach (var letter in letters)
            {
                var testMove = new Move(_baseMove.GetBoardState().Duplicate());
                testMove.AddNewLetter(letter, row, col);
                var words = testMove.FindWords();
                if (ValidateWordSubstring(words) == false)
                {
                    _impossibleLetters.Add(letter);
                }
            }
        }

        private HashSet<char> FindChars(List<Word> words, List<char> letters)
        {
            var result = new HashSet<char>();
            foreach(var word in words)
            {
                foreach(var c in word.Text)
                {
                    result.Add(c);
                }
            }

            foreach(var letter in letters)
            {
                result.Add(letter);
            }

            return result;
        }

        private HashSet<string> BuildSubstringIndex(HashSet<string> dictionary)
        {
            var substrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var word in dictionary)
            {
                for (int i = 0; i < word.Length; i++)
                {
                    for (int len = 1; len <= word.Length - i; len++)
                    {
                        substrings.Add(word.Substring(i, len));
                    }
                }
            }

            return substrings;
        }

        private HashSet<string> BuildSubstringIndex(string word)
        {
            var substrings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < word.Length; i++)
            {
                for (int len = 1; len <= word.Length - i; len++)
                {
                    substrings.Add(word.Substring(i, len));
                }
            }


            return substrings;
        }



        private List<Word> ValidateWords(List<Word> words)
        {
            if (words.Count > 100)
            {
                return words.AsParallel()
                            .Where(word => !_dictionary.Contains(word.Text))
                            .ToList();
            }
            else
            {
                return words.Where(word => !_dictionary.Contains(word.Text)).ToList();
            }
        }

        private int[,] GetPlayablePositions()
        {
            int[,] positions = new int[15, 15];
            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    positions[row, col] = 1; // IsPositionPlayable(_baseMove.GetBoardState(), row, col, true);
                }
            }


            return positions;
        }


        private int IsPositionPlayable(char [,] board, int row, int col, bool needsToBeAdjacent)
        {
            if(row < 0 || row >= 15 || col < 0 || col >= 15)
            {
                return 0;
            }

            if (board[row, col] == ' ')
            {
                // Check if adjacent to a tile
                if (needsToBeAdjacent && ((row > 0 && board[row - 1, col] != ' ') ||
                    (row < 14 && board[row + 1, col] != ' ') ||
                    (col > 0 && board[row, col - 1] != ' ') ||
                    (col < 14 && board[row, col + 1] != ' ')))
                {
                    return 1;
                }
                else if(needsToBeAdjacent == false)
                {
                    return 1;
                }
            }

            return 0;
        }


        private HashSet<Move> GenerateMoves(int startingRow, int startingColumn)
        {
            HashSet<Move> moves = new HashSet<Move>();

            Move newMove = null;

            List<Enums.Direction> availableDirections = new List<Enums.Direction>();

            if(IsPositionPlayable(_baseMove.GetBoardState(), startingRow + 1, startingColumn, false) == 1)
            {
                availableDirections.Add(Enums.Direction.Down);
            }

            if(IsPositionPlayable(_baseMove.GetBoardState(), startingRow - 1, startingColumn, false) == 1)
            {
                availableDirections.Add(Enums.Direction.Up);
            }

            if (IsPositionPlayable(_baseMove.GetBoardState(), startingRow, startingColumn + 1, false) == 1)
            {
                availableDirections.Add(Enums.Direction.Right);
            }

            if (IsPositionPlayable(_baseMove.GetBoardState(), startingRow, startingColumn - 1, false) == 1)
            {
                availableDirections.Add(Enums.Direction.Left);
            }

            foreach (var direction in availableDirections)
            {
                foreach (var permutation in _allPermutations)
                {
                    int skipCounter = 0;
                    do
                    {
                        newMove = GenerateMove(permutation, startingRow, startingColumn, direction, skipCounter);

                        if (newMove != null)
                        {
                            moves.Add(newMove);
                            skipCounter++;
                        }
                    } while (newMove != null);
                }
            }

            return moves;
        }

        private Move GenerateMove(List<char> letters, int startingRow, int startingColumn, Enums.Direction direction, int skipCounter)
        {
            var testMove = new Move(_baseMove.GetBoardState().Duplicate());
            int currentRow = startingRow;
            int currentColumn = startingColumn;

            //generate moves for letter set at position
            foreach (var letter in letters)
            {
                //if(_impossibleLetters.Contains(letter))
                //{
                //    return null;
                //}

                testMove.AddNewLetter(letter, currentRow, currentColumn);

                if (ValidateBoardState(testMove.GetBoardState()))
                {

                    var currentWords = testMove.FindNewWords();
                    if (ValidateWordSubstring(currentWords))
                    {
                        if (ValidateWords(currentWords).Any() == false)
                        {
                            if (skipCounter == 0)
                            {
                                //first valid move, return it
                                return testMove;
                            }
                            else
                            {
                                skipCounter--;
                            }
                        }

                        switch (direction)
                        {
                            case Enums.Direction.Up:
                                if (IsPositionPlayable(testMove.GetBoardState(), currentRow - 1, currentColumn, true) == 1)
                                {
                                    currentRow--;
                                    continue;
                                }
                                break;
                            case Enums.Direction.Down:
                                if (IsPositionPlayable(testMove.GetBoardState(), currentRow + 1, currentColumn, true) == 1)
                                {
                                    currentRow++;
                                    continue;
                                }
                                break;
                            case Enums.Direction.Left:
                                if (IsPositionPlayable(testMove.GetBoardState(), currentRow, currentColumn - 1, true) == 1)
                                {
                                    currentColumn--;
                                    continue;
                                }
                                break;
                            case Enums.Direction.Right:
                                if (IsPositionPlayable(testMove.GetBoardState(), currentRow, currentColumn + 1, true) == 1)
                                {
                                    currentColumn++;
                                    continue;
                                }
                                break;
                        }

                        break;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return null;
        }

        private bool ValidateBoardState(char[,] chars)
        {
            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    if (chars[row, col] != ' ')
                    {
                        // Check if adjacent to a tile
                        if ((row > 0 && chars[row - 1, col] != ' ') ||
                            (row < 14 && chars[row + 1, col] != ' ') ||
                            (col > 0 && chars[row, col - 1] != ' ') ||
                            (col < 14 && chars[row, col + 1] != ' '))
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        //private bool ValidateWordSubstring(List<string> words)
        //{
        //    foreach (var word in words)
        //    {
        //        bool foundSubstring = false;
        //        foreach(var dictionaryItem in _dictionary)
        //        {
        //            if (dictionaryItem.Length > word.Length && dictionaryItem.Contains(word))
        //            {
        //                foundSubstring = true;
        //                break;
        //            }
        //        }

        //        if(foundSubstring == false)
        //        {
        //            return false;
        //        }
        //    }

        //    return true;
        //}

        private bool ValidateWordSubstring(List<Word> words)
        {
            var newWords = words.Except(_baseWords);

            // Optimization: Use HashSet.Contains() instead of LINQ Any() for O(1) lookups
            var substringValidated = newWords.All(word => _validSubstrings.Contains(word.Text));

            if(substringValidated == false)
            {
                // Optimization: Use HashSet.Contains() directly instead of nested LINQ Any()
                if(newWords.Any(word => _invalidSubstrings.Contains(word.Text)))
                {
                    return false;
                }
            }

            if(substringValidated == false)
            {
                // Optimization: Filter using HashSet.Contains() which is O(1) vs O(n) for Any()
                var unvalidatedSubstrings = newWords.Where(word => !_validSubstrings.Contains(word.Text));

                foreach(var unvalidatedSubstring in unvalidatedSubstrings)
                {
                    // Optimization: Direct Contains() check instead of LINQ Any()
                    if (!_validSubstrings.Contains(unvalidatedSubstring.Text))
                    {
                        if (_dictionarySubstringIndex.Contains(unvalidatedSubstring.Text))
                        {
                            if (!_validSubstrings.Contains(unvalidatedSubstring.Text))
                            {
                                _validSubstrings.Add(unvalidatedSubstring.Text);
                            }
                        }
                        else
                        {
                            if (!_invalidSubstrings.Contains(unvalidatedSubstring.Text))
                            {
                                _invalidSubstrings.Add(unvalidatedSubstring.Text);
                            }

                            return false;
                        }
                    }
                }

                substringValidated = true;

                //var fullDictValidated = newWords.AsParallel().FirstOrDefault(word => _dictionary.Any(dictItem => dictItem.Length > word.Text.Length && dictItem.Contains(word.Text)));
            }

            return substringValidated;

//            return newWords.AsParallel().All(word =>
//    _dictionary.Any(dictItem => dictItem.Length > word.Text.Length && dictItem.Contains(word.Text))
//);
        }



        private List<List<char>> GenerateUniquePermutations(List<char> characters)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));

            if (characters.Count == 0)
                return new List<List<char>> { new List<char>() };

            if (characters.Count == 1)
                return new List<List<char>> { new List<char>(characters) };

            // Sort to group duplicates together
            var sortedChars = characters.OrderBy(c => c).ToList();
            var result = new HashSet<string>(); // Use string representation to detect duplicates
            var permutations = new List<List<char>>();

            GenerateUniquePermutationsHelper(sortedChars, new List<char>(), new bool[sortedChars.Count], result, permutations);

            return permutations;
        }

        private void GenerateUniquePermutationsHelper(List<char> characters, List<char> current, bool[] used, HashSet<string> seen, List<List<char>> result)
        {
            if (current.Count == characters.Count)
            {
                string permKey = string.Join("", current);
                if (!seen.Contains(permKey))
                {
                    seen.Add(permKey);
                    result.Add(new List<char>(current));
                }
                return;
            }

            for (int i = 0; i < characters.Count; i++)
            {
                if (used[i]) continue;

                // Skip duplicates: if current character is same as previous and previous is not used
                if (i > 0 && characters[i] == characters[i - 1] && !used[i - 1])
                    continue;

                used[i] = true;
                current.Add(characters[i]);

                GenerateUniquePermutationsHelper(characters, current, used, seen, result);

                current.RemoveAt(current.Count - 1);
                used[i] = false;
            }
        }
    }

}
