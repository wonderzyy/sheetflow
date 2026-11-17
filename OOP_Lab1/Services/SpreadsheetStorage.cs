
using System.Text;
using System.Text.Json;
using OOP_Lab1.Models;

namespace OOP_Lab1.Services;

public class SpreadsheetDocument
{
    public int Rows { get; set; }
    public int Columns { get; set; }
    public Dictionary<string, string> Cells { get; set; } = new();
}

public static class SpreadsheetStorage
{
    public static MemoryStream CreateStream(Spreadsheet spreadsheet)
    {
        var document = new SpreadsheetDocument
        {
            Rows = spreadsheet.RowCount,
            Columns = spreadsheet.ColumnCount
        };

        for (int row = 0; row < spreadsheet.RowCount; row++)
        {
            for (int col = 0; col < spreadsheet.ColumnCount; col++)
            {
                var cell = spreadsheet.GetCell(row, col);

                if (!string.IsNullOrEmpty(cell.Expression))
                {
                    document.Cells[cell.Address] = cell.Expression;
                }
            }
        }

        string json = JsonSerializer.Serialize(
            document,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        return new MemoryStream(Encoding.UTF8.GetBytes(json));
    }

    public static async Task<Spreadsheet> LoadAsync(Stream stream)
    {
        SpreadsheetDocument document =
            await JsonSerializer.DeserializeAsync<SpreadsheetDocument>(stream)
            ?? throw new FormatException("Некоректний файл таблиці");

        if (document.Rows < 1 ||
            document.Columns < 1 ||
            document.Cells is null)
        {
            throw new FormatException("Некоректні дані таблиці");
        }

        var spreadsheet = new Spreadsheet(
            document.Rows,
            document.Columns);

        foreach (var item in document.Cells)
        {
            spreadsheet.GetCell(item.Key).Expression =
                item.Value ?? string.Empty;
        }

        return spreadsheet;
    }
}
