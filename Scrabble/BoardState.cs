using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScrabbleSolver
{
    public class BoardStateData
    {
        public string[] BoardRows { get; set; } = new string[15];
        public string Rack { get; set; } = string.Empty;
        public string? OriginalImagePath { get; set; }
        public DateTime SavedDate { get; set; }
        public string Version { get; set; } = "1.0";

        public BoardStateData()
        {
            for (int i = 0; i < 15; i++)
            {
                BoardRows[i] = new string(' ', 15);
            }
        }

        public static BoardStateData FromBoardAndRack(char[,] board, char[] rack, string? imagePath = null)
        {
            var state = new BoardStateData
            {
                Rack = new string(rack).Trim(),
                OriginalImagePath = imagePath,
                SavedDate = DateTime.Now
            };

            for (int row = 0; row < 15; row++)
            {
                char[] rowChars = new char[15];
                for (int col = 0; col < 15; col++)
                {
                    rowChars[col] = board[row, col];
                }
                state.BoardRows[row] = new string(rowChars);
            }

            return state;
        }

        public char[,] GetBoard()
        {
            char[,] board = new char[15, 15];
            
            for (int row = 0; row < 15; row++)
            {
                string rowData = BoardRows[row];
                for (int col = 0; col < 15; col++)
                {
                    board[row, col] = col < rowData.Length ? rowData[col] : ' ';
                }
            }

            return board;
        }

        public char[] GetRack()
        {
            char[] rack = new char[7];
            for (int i = 0; i < 7; i++)
            {
                rack[i] = i < Rack.Length ? Rack[i] : ' ';
            }
            return rack;
        }

        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Serialize(this, options);
        }

        public static BoardStateData? FromJson(string json)
        {
            return JsonSerializer.Deserialize<BoardStateData>(json);
        }

        public void SaveToFile(string filePath)
        {
            File.WriteAllText(filePath, ToJson());
        }

        public static BoardStateData? LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            string json = File.ReadAllText(filePath);
            return FromJson(json);
        }
    }
}
