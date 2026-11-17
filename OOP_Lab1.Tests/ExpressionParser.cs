
namespace OOP_Lab1.Parsing;

public abstract record ExpressionNode;

public record NumberNode(string Value) : ExpressionNode;

public record CellReferenceNode(string Address) : ExpressionNode;

public record BinaryNode(
    TokenType Operator,
    ExpressionNode Left,
    ExpressionNode Right
) : ExpressionNode;

public record UnaryNode(
    TokenType Operator,
    ExpressionNode Operand
) : ExpressionNode;

public record FunctionNode(
    TokenType Function,
    List<ExpressionNode> Arguments
) : ExpressionNode;

public class ExpressionParser
{
    private readonly List<Token> tokens;
    private int position;

    public ExpressionParser(string expression)
    {
        tokens = new ExpressionLexer(expression).Tokenize();
    }

    public ExpressionNode Parse()
    {
        ExpressionNode result = ParseOr();

        Expect(TokenType.End);

        return result;
    }

    private ExpressionNode ParseOr()
    {
        ExpressionNode left = ParseAnd();

        while (Match(TokenType.Or))
        {
            left = new BinaryNode(
                TokenType.Or,
                left,
                ParseAnd()
            );
        }

        return left;
    }

    private ExpressionNode ParseAnd()
    {
        ExpressionNode left = ParseNot();

        while (Match(TokenType.And))
        {
            left = new BinaryNode(
                TokenType.And,
                left,
                ParseNot()
            );
        }

        return left;
    }

    private ExpressionNode ParseNot()
    {
        if (Match(TokenType.Not))
        {
            return new UnaryNode(
                TokenType.Not,
                ParseNot()
            );
        }

        return ParseComparison();
    }

    private ExpressionNode ParseComparison()
    {
        ExpressionNode left = ParsePower();

        if (Current.Type is
            TokenType.Equal or
            TokenType.Less or
            TokenType.Greater)
        {
            TokenType operation = Current.Type;
            position++;

            return new BinaryNode(
                operation,
                left,
                ParsePower()
            );
        }

        return left;
    }

    private ExpressionNode ParsePower()
    {
        ExpressionNode left = ParsePrimary();

        if (Match(TokenType.Power))
        {
            return new BinaryNode(
                TokenType.Power,
                left,
                ParsePower()
            );
        }

        return left;
    }

    private ExpressionNode ParsePrimary()
    {
        Token token = Current;

        if (Match(TokenType.Number))
        {
            return new NumberNode(token.Text);
        }

        if (Match(TokenType.CellReference))
        {
            return new CellReferenceNode(token.Text);
        }

        if (token.Type is TokenType.Max or TokenType.Min)
        {
            position++;
            return ParseFunction(token.Type);
        }

        if (Match(TokenType.LeftParenthesis))
        {
            ExpressionNode expression = ParseOr();
            Expect(TokenType.RightParenthesis);

            return expression;
        }

        throw new FormatException(
            $"Неочікуваний елемент '{token.Text}' у позиції {token.Position + 1}"
        );
    }

    private ExpressionNode ParseFunction(TokenType function)
    {
        Expect(TokenType.LeftParenthesis);

        var arguments = new List<ExpressionNode>();

        arguments.Add(ParseOr());

        while (Match(TokenType.Comma))
        {
            arguments.Add(ParseOr());
        }

        Expect(TokenType.RightParenthesis);

        return new FunctionNode(function, arguments);
    }

    private bool Match(TokenType type)
    {
        if (Current.Type != type)
            return false;

        position++;
        return true;
    }

    private void Expect(TokenType type)
    {
        if (!Match(type))
        {
            throw new FormatException(
                $"Синтаксична помилка у позиції {Current.Position + 1}. Очікується: {type}"
            );
        }
    }

    private Token Current => tokens[
        Math.Min(position, tokens.Count - 1)
    ];
}
