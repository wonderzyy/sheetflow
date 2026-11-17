
using OOP_Lab1.Parsing;
using OOP_Lab1.Services;

namespace OOP_Lab1.Models;

public class Spreadsheet
{
    private readonly Dictionary<string, Cell> cells = new();

    public int RowCount { get; private set; }
    public int ColumnCount { get; private set; }

    public Spreadsheet(int rows = 8, int columns = 6)
    {
        if (rows < 1 || columns < 1)
            throw new ArgumentOutOfRangeException();

        RowCount = rows;
        ColumnCount = columns;
    }

    public Cell GetCell(int row, int column)
    {
        if (row < 0 || row >= RowCount ||
            column < 0 || column >= ColumnCount)
        {
            throw new ArgumentOutOfRangeException();
        }

        string address = $"{GetColumnName(column)}{row + 1}";

        if (!cells.TryGetValue(address, out Cell? cell))
        {
            cell = new Cell(address);
            cells[address] = cell;
        }

        return cell;
    }

    public Cell GetCell(string address)
    {
        address = address.Trim().ToUpperInvariant();

        int index = 0;
        long column = 0;

        while (index < address.Length &&
               address[index] >= 'A' &&
               address[index] <= 'Z')
        {
            column = column * 26 + address[index] - 'A' + 1;

            if (column > ColumnCount)
                throw new FormatException(
                    $"Клітинка {address} поза межами таблиці");

            index++;
        }

        string rowText = address[index..];

        if (index == 0 ||
            rowText.Length == 0 ||
            rowText[0] == '0' ||
            rowText.Any(c => c < '0' || c > '9') ||
            !int.TryParse(rowText, out int row) ||
            row < 1 ||
            row > RowCount)
        {
            throw new FormatException(
                $"Некоректна адреса клітинки: {address}");
        }

        return GetCell(row - 1, (int)column - 1);
    }

    public void SetExpression(int row, int column, string expression)
    {
        Cell cell = GetCell(row, column);
        cell.Expression = expression ?? string.Empty;
        cell.Error = null;
    }

    public void ValidateSyntax(string address)
    {
        Cell cell = GetCell(address);

        if (string.IsNullOrWhiteSpace(cell.Expression))
            return;

        var parser = new ExpressionParser(cell.Expression);
        parser.Parse();
    }
    public ExpressionValue EvaluateCell(string address)
    {
        var visiting = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        ExpressionValue result = EvaluateCellInternal(
            address,
            visiting);

        if (!result.Boolean.HasValue)
        {
            throw new FormatException(
                $"Вираз у клітинці {address} повинен мати логічний результат");
        }

        return result;
    }

    private ExpressionValue EvaluateCellInternal(
        string address,
        HashSet<string> visiting)
    {
        Cell cell = GetCell(address);
        string normalizedAddress = cell.Address;

        if (!visiting.Add(normalizedAddress))
        {
            throw new FormatException(
                $"Циклічне посилання на клітинку {normalizedAddress}");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(cell.Expression))
            {
                throw new FormatException(
                    $"Клітинка {normalizedAddress} порожня");
            }

            var parser = new ExpressionParser(cell.Expression);
            ExpressionNode expression = parser.Parse();

            var evaluator = new ExpressionEvaluator(
                reference => EvaluateCellInternal(reference, visiting));

            return evaluator.Evaluate(expression);
        }
        finally
        {
            visiting.Remove(normalizedAddress);
        }
    }

    public void AddRow()
    {
        RowCount++;
    }

    public void AddColumn()
    {
        ColumnCount++;
    }

    public void DeleteRow()
    {
        if (RowCount <= 1)
            return;

        for (int col = 0; col < ColumnCount; col++)
        {
            cells.Remove($"{GetColumnName(col)}{RowCount}");
        }

        RowCount--;
    }

    public void DeleteColumn()
    {
        if (ColumnCount <= 1)
            return;

        string columnName = GetColumnName(ColumnCount - 1);

        for (int row = 1; row <= RowCount; row++)
        {
            cells.Remove($"{columnName}{row}");
        }

        ColumnCount--;
    }

    public static string GetColumnName(int index)
    {
        string name = "";
        index++;

        while (index > 0)
        {
            index--;
            name = (char)('A' + index % 26) + name;
            index /= 26;
        }

        return name;
    }
}
