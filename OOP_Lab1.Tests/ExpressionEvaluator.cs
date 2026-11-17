
using System.Numerics;
using OOP_Lab1.Parsing;

namespace OOP_Lab1.Services;

public readonly record struct ExpressionValue(
    BigInteger? Number,
    bool? Boolean)
{
    public static ExpressionValue FromNumber(BigInteger value)
    {
        return new ExpressionValue(value, null);
    }

    public static ExpressionValue FromBoolean(bool value)
    {
        return new ExpressionValue(null, value);
    }

    public BigInteger AsNumber()
    {
        return Number ?? throw new FormatException(
            "Очікується числове значення");
    }

    public bool AsBoolean()
    {
        return Boolean ?? throw new FormatException(
            "Очікується логічне значення");
    }

    public override string ToString()
    {
        if (Boolean.HasValue)
            return Boolean.Value ? "ІСТИНА" : "ХИБА";

        return Number?.ToString() ?? "";
    }
}

public class ExpressionEvaluator
{
    private readonly Func<string, ExpressionValue> resolveCell;

    public ExpressionEvaluator(Func<string, ExpressionValue> resolveCell)
    {
        this.resolveCell = resolveCell;
    }

    public ExpressionValue Evaluate(ExpressionNode node)
    {
        return node switch
        {
            NumberNode number => ExpressionValue.FromNumber(
                BigInteger.Parse(number.Value)),

            CellReferenceNode reference =>
                resolveCell(reference.Address),

            UnaryNode unary => EvaluateUnary(unary),

            BinaryNode binary => EvaluateBinary(binary),

            FunctionNode function => EvaluateFunction(function),

            _ => throw new FormatException(
                "Невідомий тип виразу")
        };
    }

    private ExpressionValue EvaluateUnary(UnaryNode node)
    {
        if (node.Operator != TokenType.Not)
            throw new FormatException("Невідома унарна операція");

        bool value = Evaluate(node.Operand).AsBoolean();

        return ExpressionValue.FromBoolean(!value);
    }

    private ExpressionValue EvaluateBinary(BinaryNode node)
    {
        ExpressionValue left = Evaluate(node.Left);
        ExpressionValue right = Evaluate(node.Right);

        return node.Operator switch
        {
            TokenType.Power => ExpressionValue.FromNumber(
                CalculatePower(left.AsNumber(), right.AsNumber())),

            TokenType.Equal => ExpressionValue.FromBoolean(
                left.AsNumber() == right.AsNumber()),

            TokenType.Less => ExpressionValue.FromBoolean(
                left.AsNumber() < right.AsNumber()),

            TokenType.Greater => ExpressionValue.FromBoolean(
                left.AsNumber() > right.AsNumber()),

            TokenType.And => ExpressionValue.FromBoolean(
                left.AsBoolean() && right.AsBoolean()),

            TokenType.Or => ExpressionValue.FromBoolean(
                left.AsBoolean() || right.AsBoolean()),

            _ => throw new FormatException(
                "Невідома бінарна операція")
        };
    }

    private ExpressionValue EvaluateFunction(FunctionNode node)
    {
        if (node.Arguments.Count == 0)
            throw new FormatException("Функція потребує аргументів");

        List<BigInteger> values = node.Arguments
            .Select(argument => Evaluate(argument).AsNumber())
            .ToList();

        return node.Function switch
        {
            TokenType.Max => ExpressionValue.FromNumber(
                values.Max()),

            TokenType.Min => ExpressionValue.FromNumber(
                values.Min()),

            _ => throw new FormatException(
                "Невідома функція")
        };
    }

    private static BigInteger CalculatePower(
        BigInteger baseValue,
        BigInteger exponent)
    {
        if (exponent < 0)
            throw new FormatException(
                "Від'ємний показник степеня не підтримується");

        if (exponent > 10000)
            throw new FormatException(
                "Показник степеня перевищує допустиме значення");

        return BigInteger.Pow(baseValue, (int)exponent);
    }
}
