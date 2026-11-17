
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OOP_Lab1.Models;
using OOP_Lab1.Parsing;
using OOP_Lab1.Services;

namespace OOP_Lab1.Tests;

[TestClass]
public class SpreadsheetTests
{
    [TestMethod]
    public void LexerRecognizesTokens()
    {
        var tokens = new ExpressionLexer("A1 > 5").Tokenize();

        Assert.AreEqual(TokenType.CellReference, tokens[0].Type);
        Assert.AreEqual(TokenType.Greater, tokens[1].Type);
        Assert.AreEqual(TokenType.Number, tokens[2].Type);
        Assert.AreEqual(TokenType.End, tokens[3].Type);
    }

    [TestMethod]
    public void ParserRejectsInvalidSyntax()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            new ExpressionParser("2^^3").Parse();
        });
    }

    [TestMethod]
    public void EvaluatorCalculatesExpression()
    {
        var parser = new ExpressionParser("2^3 > 5");

        var evaluator = new ExpressionEvaluator(
            address => throw new InvalidOperationException());

        var result = evaluator.Evaluate(parser.Parse());

        Assert.IsTrue(result.AsBoolean());
    }

    [TestMethod]
    public void SpreadsheetResolvesReferences()
    {
        var spreadsheet = new Spreadsheet();

        spreadsheet.SetExpression(0, 0, "2^3");
        spreadsheet.SetExpression(0, 1, "A1 > 5");

        var result = spreadsheet.EvaluateCell("B1");

        Assert.IsTrue(result.AsBoolean());
    }

    [TestMethod]
    public void SpreadsheetDetectsCycle()
    {
        var spreadsheet = new Spreadsheet();

        spreadsheet.SetExpression(0, 0, "B1 > 5");
        spreadsheet.SetExpression(0, 1, "A1 > 5");

        Assert.ThrowsExactly<FormatException>(() =>
        {
            spreadsheet.EvaluateCell("A1");
        });
    }
}
    