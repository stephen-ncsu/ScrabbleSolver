using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        HashSet<char>[,] _eliminatedCharPositions = new HashSet<char>[15, 15];
        HashSet<char>[,] _validatedCharPositions = new HashSet<char>[15, 15];

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

            var length = GetLongestPossibleWordWithCharacters(_remainingLetters);

            var playablePositions = GetPlayablePositions(length);
            var stopwatch = new Stopwatch();

            for(int column = 0; column < playablePositions.GetLength(0); column++)
            {
                for(int row = 0; row < playablePositions.GetLength(1); row++)
                {
                    if (playablePositions[column, row] != Enums.Direction.None)
                    {
                        if(_baseMove.GetBoardState()[column, row] != ' ')
                        {
                            continue;
                        }

                        stopwatch.Restart();
                        //DetectImpossibleLetters(_remainingLetters, column, row);

                        if(playablePositions[column, row] == Enums.Direction.All)
                        {
                            Serilog.Log.Logger.Information($"Generating moves for position ({column}, {row}) in both directions.");
                            var downMoves = GenerateMoves(column, row, Enums.Direction.Down);
                            var rightMoves = GenerateMoves(column, row, Enums.Direction.Right);
                            if(downMoves.Any())
                            {
                                _possibleMoves.AddRange(downMoves);
                            }
                            if(rightMoves.Any())
                            {
                                _possibleMoves.AddRange(rightMoves);
                            }
                        }
                        else
                        {
                            Serilog.Log.Logger.Information($"Generating moves for position ({column}, {row}) in the {playablePositions[column, row]} direction");

                            var newMoves = GenerateMoves(column, row, playablePositions[column, row]);

                            if (newMoves.Any())
                            {
                                _possibleMoves.AddRange(newMoves);
                            }
                        }



                        stopwatch.Stop();
                        Serilog.Log.Logger.Information($"Generated moves for position ({column}, {row}) in {stopwatch.ElapsedMilliseconds} ms. Total moves: {_possibleMoves.Count}");
                    }
                }
            }

            return _possibleMoves;
        }

        private int GetLongestPossibleWordWithCharacters(List<char> remainingLetters)
        {
            if (remainingLetters == null || remainingLetters.Count == 0)
                return 0;

            // Build regex pattern with positive lookaheads for each letter
            // Example: for letters [L,E,T] -> "^(?=.*L)(?=.*E)(?=.*T).*$"
            StringBuilder patternBuilder = new StringBuilder("^");
            foreach (var letter in remainingLetters)
            {
                patternBuilder.Append($"(?=.*{char.ToUpper(letter)})");
            }
            patternBuilder.Append(".*$");

            Regex regex = new Regex(patternBuilder.ToString(), RegexOptions.IgnoreCase);

            int longestLength = 0;

            // Read dictionary line by line and find longest matching word
            foreach (string word in File.ReadLines("myDictionary.txt"))
            {
                if (regex.IsMatch(word))
                {
                    longestLength = Math.Max(longestLength, word.Length);
                }
            }

            Serilog.Log.Logger.Information($"Longest possible word with characters {string.Join("", remainingLetters)}: {longestLength} letters");

            return longestLength;
        }

        private void DetectImpossibleLetters(List<char> letters, int row, int col)
        {
            _impossibleLetters = new List<char>();
            foreach (var letter in letters)
            {
                var testMove = new Move(_baseMove.GetBoardState().Duplicate());
                testMove.AddNewLetter(letter, col, row);
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

            return words.Where(word => !_dictionary.Contains(word.Text)).ToList();

        }

        private Enums.Direction[,] GetPlayablePositions(int maxLength)
        {
            if(maxLength > 7)
            {
                maxLength = 7;
            }

            if(maxLength == 0)
            {
                maxLength = 6;
            }

            Enums.Direction[,] positions = new Enums.Direction[15, 15];
            var boardState = _baseMove.GetBoardState();
            for (int row = 0; row < 15; row++)
            {
                for (int col = 0; col < 15; col++)
                {
                    for(int i = 0; i < maxLength; i++)
                    {
                        if(row + i < 15 && boardState[col, row + i] != ' ')
                        {
                            if (positions[col, row] == Enums.Direction.None)
                            {
                                positions[col, row] = Enums.Direction.Down;
                            }
                            else
                            {
                                positions[col, row] = Enums.Direction.All;
                            }
                        }

                        if (row + i + 1 < 15 && boardState[col, row + i + 1] != ' ')
                        {
                            if (positions[col, row] == Enums.Direction.None)
                            {
                                positions[col, row] = Enums.Direction.Down;
                            }
                            else
                            {
                                positions[col, row] = Enums.Direction.All;
                            }
                        }

                        if (row + i < 15 && col + 1 < 15 && boardState[col + 1, row + i] != ' ')
                        {
                            if (positions[col, row] == Enums.Direction.None)
                            {
                                positions[col, row] = Enums.Direction.Down;
                            }
                            else
                            {
                                positions[col, row] = Enums.Direction.All;
                            }
                        }

                        if (row + i < 15 && col - 1 >= 0 && boardState[col - 1, row + i] != ' ')
                        {
                            if (positions[col, row] == Enums.Direction.None)
                            {
                                positions[col, row] = Enums.Direction.Down;
                            }
                            else
                            {
                                positions[col, row] = Enums.Direction.All;
                            }
                        }



                        if (col + i < 15 && boardState[col + i, row] != ' ')
                        {
                            if (positions[col, row] == Enums.Direction.None)
                            {
                                positions[col, row] = Enums.Direction.Right;
                            }
                            else
                            {
                                positions[col, row] = Enums.Direction.All;
                            }
                        }

                        if (col + i < 15 && row + 1 < 15 && boardState[col + i, row + 1] != ' ')
                        {
                            if (positions[col, row] == Enums.Direction.None)
                            {
                                positions[col, row] = Enums.Direction.Right;
                            }
                            else
                            {
                                positions[col, row] = Enums.Direction.All;
                            }
                        }

                        if (col + i < 15 && row - 1 >= 0 && boardState[col + i, row - 1] != ' ')
                        {
                            if (positions[col, row] == Enums.Direction.None)
                            {
                                positions[col, row] = Enums.Direction.Right;
                            }
                            else
                            {
                                positions[col, row] = Enums.Direction.All;
                            }
                        }

                        if (col + i + 1 < 15 && boardState[col + i + 1, row] != ' ')
                        {
                            if (positions[col, row] == Enums.Direction.None)
                            {
                                positions[col, row] = Enums.Direction.Right;
                            }
                            else
                            {
                                positions[col, row] = Enums.Direction.All;
                            }
                        }
                    }
                }
            }


            return positions;
        }

        public Enums.Direction[,] GetPlayablePositionsDebug(char[,] board, string rack)
        {
            _baseMove = new Move(board);
            _remainingLetters = rack.ToCharArray().ToList();
            var maxLength = GetLongestPossibleWordWithCharacters(_remainingLetters);
            return GetPlayablePositions(maxLength);
        }


        private int IsPositionPlayable(char [,] board, int row, int col, bool needsToBeAdjacent)
        {
            if(row < 0 || row >= 15 || col < 0 || col >= 15)
            {
                return 0;
            }

            if (board[col, row] == ' ')
            {
                // Check if adjacent to a tile
                if (needsToBeAdjacent && ((row > 0 && board[col, row - 1] != ' ') ||
                    (row < 14 && board[col, row + 1] != ' ') ||
                    (col > 0 && board[col - 1, row] != ' ') ||
                    (col < 14 && board[col + 1, row] != ' ')))
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

        private int IsValidBoardPosition(char[,] board, int row, int col)
        {
            if (row < 0 || row >= 15 || col < 0 || col >= 15)
            {
                return 0;
            }

            return 1;
        }


        private HashSet<Move> GenerateMoves(int startingColumn, int startingRow, Enums.Direction direction)
        {
            HashSet<Move> moves = new HashSet<Move>();

            Move newMove = null;

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
                if (_validatedCharPositions[currentColumn, currentRow] == null)
                {
                    _validatedCharPositions[currentColumn, currentRow] = new HashSet<char>();
                }

                if (_eliminatedCharPositions[currentColumn, currentRow] == null)
                {
                    _eliminatedCharPositions[currentColumn, currentRow] = new HashSet<char>();
                }

                if (_eliminatedCharPositions[currentColumn, currentRow] != null && _eliminatedCharPositions[currentColumn, currentRow].Contains(letter))
                {
                    continue;
                }

                testMove.AddNewLetter(letter, currentColumn, currentRow);

                if (ValidateMovePosition(testMove))
                {
                    var currentWords = testMove.FindNewWords();
                    var previouslyValidated = _validatedCharPositions[currentColumn, currentRow].Contains(letter);
                    if (previouslyValidated || ValidateWordSubstring(currentWords))
                    {
                        if (previouslyValidated == false)
                        {
                            _validatedCharPositions[currentColumn, currentRow].Add(letter);
                        }

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
                    }
                    else
                    {
                        if (startingRow == currentRow && startingColumn == currentColumn)
                        {
                            _eliminatedCharPositions[currentColumn, currentRow].Add(letter);
                        }
                    }
                }

                switch (direction)
                {
                    case Enums.Direction.Down:
                        currentRow++;
                        bool availablePosition = false;
                        while (IsValidBoardPosition(testMove.GetBoardState(), currentRow, currentColumn) == 1)
                        {
                            var isPositionPlayable = IsPositionPlayable(testMove.GetBoardState(), currentRow, currentColumn, false);
                            if (isPositionPlayable == 1)
                            {
                                availablePosition = true;
                                break;
                            }
                            else
                            {
                                currentRow++;
                            }
                        }

                        if (availablePosition)
                        {
                            continue;
                        }

                        break;
                    case Enums.Direction.Right:
                        currentColumn++;
                        bool availableHorizontalPosition = false;
                        while (IsValidBoardPosition(testMove.GetBoardState(), currentRow, currentColumn) == 1)
                        {
                            var isPositionPlayable = IsPositionPlayable(testMove.GetBoardState(), currentRow, currentColumn, false);
                            if (isPositionPlayable == 1)
                            {
                                availableHorizontalPosition = true;
                                break;
                            }
                            else
                            {
                                currentColumn++;
                            }
                        }

                        if (availableHorizontalPosition)
                        {
                            continue;
                        }

                        break;
                }

                break;
            }

            return null;
        }

        private bool ValidateMovePosition(Move move)
        {
            bool validMove = false;
            var board = move.GetInitialBoardState();
            foreach (var changedPositions in move.GetChangedPositions())
            {
                // Check if adjacent to a tile
                if ((changedPositions.Col > 0 && board[changedPositions.Col - 1, changedPositions.Row] != ' ') ||
                    (changedPositions.Col < 14 && board[changedPositions.Col + 1, changedPositions.Row] != ' ') ||
                    (changedPositions.Row > 0 && board[changedPositions.Col, changedPositions.Row - 1] != ' ') ||
                    (changedPositions.Row < 14 && board[changedPositions.Col, changedPositions.Row + 1] != ' '))
                {
                    validMove = true;
                    break;
                }

            }

            return validMove;
        }

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
