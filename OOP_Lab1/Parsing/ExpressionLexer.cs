
namespace OOP_Lab1.Parsing;

public enum TokenType
{
    Number,
    CellReference,
    Power,
    Equal,
    Less,
    Greater,
    Not,
    And,
    Or,
    Max,
    Min,
    LeftParenthesis,
    RightParenthesis,
    Comma,
    End
}

public record Token(TokenType Type, string Text, int Position);

public class ExpressionLexer
{
    private readonly string input;
    private int position;

    public ExpressionLexer(string input)
    {
        this.input = input ?? string.Empty;
    }

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (position < input.Length)
        {
            if (char.IsWhiteSpace(input[position]))
            {
                position++;
                continue;
            }

            int start = position;
            char current = input[position];

            if (IsDigit(current))
            {
                while (position < input.Length &&
                       IsDigit(input[position]))
                {
                    position++;
                }

                tokens.Add(new Token(
                    TokenType.Number,
                    input[start..position],
                    start));

                continue;
            }

            if (IsLetter(current))
            {
                while (position < input.Length &&
                       IsLetter(input[position]))
                {
                    position++;
                }

                string word = input[start..position];

                if (position < input.Length &&
                    IsDigit(input[position]))
                {
                    while (position < input.Length &&
                           IsDigit(input[position]))
                    {
                        position++;
                    }

                    tokens.Add(new Token(
                        TokenType.CellReference,
                        input[start..position].ToUpperInvariant(),
                        start));

                    continue;
                }

                TokenType type = word.ToLowerInvariant() switch
                {
                    "not" => TokenType.Not,
                    "and" => TokenType.And,
                    "or" => TokenType.Or,
                    "mmax" => TokenType.Max,
                    "mmin" => TokenType.Min,
                    _ => throw new FormatException(
                        $"Невідомий ідентифікатор '{word}' у позиції {start + 1}")
                };

                tokens.Add(new Token(type, word, start));
                continue;
            }

            TokenType tokenType = current switch
            {
                '^' => TokenType.Power,
                '=' => TokenType.Equal,
                '<' => TokenType.Less,
                '>' => TokenType.Greater,
                '(' => TokenType.LeftParenthesis,
                ')' => TokenType.RightParenthesis,
                ',' => TokenType.Comma,
                _ => throw new FormatException(
                    $"Невідомий символ '{current}' у позиції {start + 1}")
            };

            tokens.Add(new Token(
                tokenType,
                current.ToString(),
                start));

            position++;
        }

        tokens.Add(new Token(
            TokenType.End,
            string.Empty,
            position));

        return tokens;
    }

    private static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    private static bool IsLetter(char value)
    {
        return value is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z';
    }
}
