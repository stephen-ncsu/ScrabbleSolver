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
        private List<List<bool>> _allPermutationWildcards; // Track which positions are wildcards
        private List<char> _impossibleLetters = new List<char>();
        private HashSet<string> _dictionarySubstringIndex;
        private List<Word> _baseWords;
        private List<char> _remainingLetters;
        private int _wildcardCount = 0;
        private const char WILDCARD_CHAR = '*';

        public Solver(char[,] board, string rack)
        {
            _baseMove = new Move(board);

            // Count and separate wildcards
            _wildcardCount = rack.Count(c => c == WILDCARD_CHAR || c == '?' || c == '_');
            _remainingLetters = rack.Where(c => c != WILDCARD_CHAR && c != '?' && c != '_').ToList();

            _dictionary = System.IO.File.ReadAllLines("myDictionary.txt").ToHashSet(StringComparer.OrdinalIgnoreCase);
            _invalidSubstrings = System.IO.File.ReadAllLines("InvalidSubstrings.txt").ToHashSet(StringComparer.OrdinalIgnoreCase);
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

            // Generate permutations with wildcard expansion
            if (_wildcardCount > 0)
            {
                Serilog.Log.Logger.Information($"Generating permutations with {_wildcardCount} wildcard(s)...");
                GeneratePermutationsWithWildcards(_remainingLetters, _wildcardCount, usableCharacters);
                Serilog.Log.Logger.Information($"Generated {_allPermutations.Count} permutations with wildcard expansions.");
            }
            else
            {
                _allPermutations = GenerateUniquePermutations(_remainingLetters);
                _allPermutationWildcards = new List<List<bool>>();
                foreach (var perm in _allPermutations)
                {
                    _allPermutationWildcards.Add(Enumerable.Repeat(false, perm.Count).ToList());
                }
            }

            //var someting = BuildInvalid2LetterSubstrings();
            //var result = String.Join(Environment.NewLine, someting);

            //var someting2 = BuildInvalid3LetterSubstrings();
            //var result2 = String.Join(Environment.NewLine, someting2);
            RemoveImpossiblePermuatations();

            //List<object> possibleMoves = new List<object>();
            var playablePositions = GetPlayablePositions();
            var stopwatch = new Stopwatch();

            for(int i = 0; i < playablePositions.GetLength(0); i++)
            {
                for(int j = 0; j < playablePositions.GetLength(1); j++)
                {
                    if (playablePositions[i, j] == 1)
                    {
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

        private HashSet<string> BuildInvalid2LetterSubstrings()
        {
            var invalid2Letters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Serilog.Log.Logger.Information("Building list of invalid 4-letter substrings (excluding those covered by 3-letter invalids)...");

            int totalChecked = 0;
            int skippedDueToInvalid3Letter = 0;

            // Generate all possible 4-letter combinations (26^4 = 456,976 total)
            for (char c1 = 'A'; c1 <= 'Z'; c1++)
            {
                for (char c2 = 'A'; c2 <= 'Z'; c2++)
                {
                    string substring = $"{c1}{c2}";
                    totalChecked++;

                    // Check if this 4-letter substring is NOT in the dictionary
                    if (!_dictionarySubstringIndex.Contains(substring))
                    {
                        invalid2Letters.Add(substring);
                    }
                }
            }

            Serilog.Log.Logger.Information($"Found {invalid2Letters.Count} unique invalid 2-letter substrings (out of {totalChecked} total combinations).");
            
            return invalid2Letters;
        }

        private HashSet<string> BuildInvalid3LetterSubstrings()
        {
            var invalid5Letters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Serilog.Log.Logger.Information("Building list of invalid 5-letter substrings (excluding those covered by 4-letter invalids)...");

            int totalChecked = 0;
            int skippedDueToInvalid4Letter = 0;

            // Generate all possible 5-letter combinations (26^5 = 11,881,376 total)
            for (char c1 = 'A'; c1 <= 'Z'; c1++)
            {
                for (char c2 = 'A'; c2 <= 'Z'; c2++)
                {
                    for (char c3 = 'A'; c3 <= 'Z'; c3++)
                    {
                        string substring = $"{c1}{c2}{c3}";
                        totalChecked++;

                        // Check if this 5-letter substring is NOT in the dictionary
                        if (!_dictionarySubstringIndex.Contains(substring))
                        {
                            // Extract all 4-character substrings from this 5-character string
                            string sub1 = substring.Substring(0, 2); // c1c2c3c4
                            string sub2 = substring.Substring(1, 2); // c2c3c4c5

                            // Only add this 5-letter substring if BOTH 4-letter substrings are valid
                            // (i.e., NOT in the invalid substrings list)
                            if (!_invalidSubstrings.Contains(sub1) && !_invalidSubstrings.Contains(sub2))
                            {
                                invalid5Letters.Add(substring);
                            }
                            else
                            {
                                skippedDueToInvalid4Letter++;
                            }
                        }
                    }
                }
            }

            Serilog.Log.Logger.Information($"Found {invalid5Letters.Count} unique invalid 5-letter substrings (out of {totalChecked} total combinations).");
            Serilog.Log.Logger.Information($"Skipped {skippedDueToInvalid4Letter} 5-letter substrings already covered by invalid 4-letter substrings.");

            return invalid5Letters;
        }

        private void RemoveImpossiblePermuatations()
        {
            Serilog.Log.Information($"Current permutations count before filtering: {_allPermutations.Count}");
            Serilog.Log.Information("Removing impossible permutations based on substring validation...");
            foreach (var invalidSubstring in _invalidSubstrings)
            {
                List<List<char>> permutationsToRemove = new List<List<char>>();
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
                        if (subStringStartIndex.Value == 0)
                        {
                            permutationsToRemove.Add(permuatation);
                        }
                        else
                        {
                            for (int i = subStringStartIndex.Value + invalidSubstring.Length - 1; i < subStringStartIndex + invalidSubstring.Length; i++)
                            {
                                permuatation.RemoveAt(i);
                            }
                        }
                    }
                }

                foreach(var perm in permutationsToRemove)
                {
                    _allPermutations.Remove(perm);
                }
                
                if(permutationsToRemove.Count > 0)
                {
                    Serilog.Log.Information($"Current Permutations Count : {_allPermutations.Count}");
                }
            }

            var longInvalidSubstrings = new HashSet<string>();
            foreach (var permuatation in _allPermutations)
            {
                var subStringIndex = BuildSubstringIndex(new string(permuatation.ToArray()));

                foreach(var substring in subStringIndex)
                {
                    if(_dictionarySubstringIndex.Contains(substring) == false)
                    {
                        if (longInvalidSubstrings.Contains(substring) == false)
                        {
                            longInvalidSubstrings.Add(substring);
                        }
                    }
                }
            }

            Serilog.Log.Information($"Found {longInvalidSubstrings.Count} impossible long substrings.");

            foreach (var invalidSubstring in longInvalidSubstrings)
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

            Serilog.Log.Information($"Current permutations count after filtering: {_allPermutations.Count}");

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

            // Add all letters from board words
            foreach(var word in words)
            {
                foreach(var c in word.Text)
                {
                    result.Add(char.ToUpper(c));
                }
            }

            // Add all rack letters
            foreach(var letter in letters)
            {
                result.Add(char.ToUpper(letter));
            }

            // If we have wildcards, include ALL letters A-Z since wildcard can be anything
            if (_wildcardCount > 0)
            {
                for (char c = 'A'; c <= 'Z'; c++)
                {
                    result.Add(c);
                }
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
                    positions[row, col] = IsPositionPlayable(_baseMove.GetBoardState(), row, col, true);
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
                for (int permIndex = 0; permIndex < _allPermutations.Count; permIndex++)
                {
                    var permutation = _allPermutations[permIndex];
                    var wildcardFlags = _allPermutationWildcards[permIndex];

                    int skipCounter = 0;
                    do
                    {
                        newMove = GenerateMove(permutation, wildcardFlags, startingRow, startingColumn, direction, skipCounter);

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

        private Move GenerateMove(List<char> letters, List<bool> isWildcard, int startingRow, int startingColumn, Enums.Direction direction, int skipCounter)
        {
            var testMove = new Move(_baseMove.GetBoardState().Duplicate());
            int currentRow = startingRow;
            int currentColumn = startingColumn;

            //generate moves for letter set at position
            for (int i = 0; i < letters.Count; i++)
            {
                char letter = letters[i];
                bool wild = isWildcard[i];

                //if(_impossibleLetters.Contains(letter))
                //{
                //    return null;
                //}

                testMove.AddNewLetter(letter, currentRow, currentColumn, wild);

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

            return null;
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

        private void GeneratePermutationsWithWildcards(List<char> regularLetters, int wildcardCount, HashSet<char> usableCharacters)
        {
            _allPermutations = new List<List<char>>();
            _allPermutationWildcards = new List<List<bool>>();
            var seen = new HashSet<string>();

            // Generate all wildcard combinations
            GenerateWildcardCombinations(
                regularLetters, 
                wildcardCount, 
                usableCharacters.ToList(), 
                new List<char>(), 
                new List<bool>(),
                0, 
                seen);
        }

        private void GenerateWildcardCombinations(
            List<char> regularLetters, 
            int remainingWildcards,
            List<char> usableCharacters, 
            List<char> currentWildcards,
            List<bool> currentWildcardFlags,
            int startIndex, 
            HashSet<string> seen)
        {
            if (remainingWildcards == 0)
            {
                // Combine regular letters + wildcard substitutions
                var combined = new List<char>(regularLetters);
                combined.AddRange(currentWildcards);

                var wildcardFlags = new List<bool>(Enumerable.Repeat(false, regularLetters.Count));
                wildcardFlags.AddRange(currentWildcardFlags);

                // Generate all permutations of this combination
                var permutations = GenerateUniquePermutations(combined);

                foreach (var perm in permutations)
                {
                    string permKey = string.Join("", perm);
                    if (!seen.Contains(permKey))
                    {
                        seen.Add(permKey);

                        // Map wildcard flags to the permuted positions
                        var permWildcardFlags = new List<bool>();
                        for (int i = 0; i < perm.Count; i++)
                        {
                            // Find this character's original position in combined
                            int originalIndex = -1;
                            var usedIndices = new HashSet<int>();

                            for (int j = 0; j < combined.Count; j++)
                            {
                                if (combined[j] == perm[i] && !usedIndices.Contains(j))
                                {
                                    originalIndex = j;
                                    usedIndices.Add(j);
                                    break;
                                }
                            }

                            permWildcardFlags.Add(originalIndex >= 0 ? wildcardFlags[originalIndex] : false);
                        }

                        _allPermutations.Add(perm);
                        _allPermutationWildcards.Add(permWildcardFlags);
                    }
                }
                return;
            }

            // Try each usable character as a wildcard
            for (int i = 0; i < usableCharacters.Count; i++)
            {
                currentWildcards.Add(usableCharacters[i]);
                currentWildcardFlags.Add(true); // Mark as wildcard

                GenerateWildcardCombinations(
                    regularLetters, 
                    remainingWildcards - 1, 
                    usableCharacters,
                    currentWildcards, 
                    currentWildcardFlags,
                    i, 
                    seen);

                currentWildcards.RemoveAt(currentWildcards.Count - 1);
                currentWildcardFlags.RemoveAt(currentWildcardFlags.Count - 1);
            }
        }
    }

}
