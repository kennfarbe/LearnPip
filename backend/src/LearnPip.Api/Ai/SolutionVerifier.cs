using System.Globalization;
using System.Text.RegularExpressions;

namespace LearnPip.Api.Ai;

public sealed record SolutionCheck(string Status, string Reason, string? Expected);

/// <summary>Checks only bounded, unitless decimal arithmetic. Unsupported subjects stay unverified.</summary>
public static class SolutionVerifier
{
    public static bool SafeHint(string? hint, string answer) =>
        string.IsNullOrWhiteSpace(hint) || string.IsNullOrWhiteSpace(answer) ||
        hint.Length <= 500 && answer.Length <= 4000 &&
        !Regex.IsMatch(hint, @"(?<![\p{L}\p{N}])" + Regex.Escape(answer.Trim()) +
            @"(?![\p{L}\p{N}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));

    public static SolutionCheck Check(string? formula, string? computed, string? reference,
        string? chosenAnswer, IReadOnlyList<string>? steps = null, string? questionText = null)
    {
        var hasFormula = TryTask(formula, out var expected);
        var hasQuestion = TryTask(questionText, out var questionValue);
        if (hasFormula && hasQuestion && expected != questionValue)
            return new("conflict", "Erkannte Frage und Formel widersprechen sich.", null);
        if (!hasFormula && !hasQuestion)
            return new("unverified", "Keine eindeutig berechenbare Dezimalaufgabe erkannt; " +
                "fachlich selbst prüfen.", null);
        if (!hasFormula) expected = questionValue;
        var value = expected.ToString(CultureInfo.InvariantCulture);
        foreach (var (label, candidate) in new[]
        {
            ("Lösung", computed), ("Musterlösung", reference), ("ausgewählte Antwort", chosenAnswer)
        })
        {
            if (string.IsNullOrWhiteSpace(candidate)) continue;
            if (!TryNumber(candidate, out var number))
                return new("unverified", $"{label} ist nicht eindeutig numerisch; fachlich selbst prüfen.", value);
            if (number != expected)
                return new("conflict", $"{label} widerspricht der unabhängig berechneten Aufgabe ({value}).", value);
        }
        if (string.IsNullOrWhiteSpace(computed) || string.IsNullOrWhiteSpace(chosenAnswer))
            return new("unverified", "Lösung oder bestätigte Antwort fehlt.", value);
        foreach (var step in steps ?? [])
        {
            var parts = step.Split('=');
            if (parts.Length != 2 || parts[0].Length > 128 || parts[1].Length > 128) continue;
            if (TryExpression(parts[0], out var left) && TryExpression(parts[1], out var right) &&
                left != right)
                return new("conflict", "Ein numerischer Lösungsschritt enthält einen Widerspruch.", value);
        }
        return new("verified", "Dezimalrechnung, angegebene Lösung und gewählte Antwort stimmen " +
            "überein. Aufgabenverständnis und Bildübertragung selbst kontrollieren.", value);
    }

    private static bool TryTask(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var formula = text.Trim().Replace('×', '*').Replace('÷', '/').Replace('−', '-');
        if (formula.EndsWith("=?", StringComparison.Ordinal)) formula = formula[..^2];
        else if (formula.EndsWith('=') || formula.EndsWith('?')) formula = formula[..^1];
        return TryExpression(formula, out value);
    }

    private static bool TryNumber(string? text, out decimal value) =>
        decimal.TryParse(text?.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value);

    private static bool TryExpression(string? text, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(text) || text.Length > 128) return false;
        try
        {
            var parser = new ArithmeticParser(text);
            return parser.Parse(out result);
        }
        catch (Exception error) when (error is OverflowException or DivideByZeroException)
        {
            return false;
        }
    }

    private sealed class ArithmeticParser(string input)
    {
        private int position;

        public bool Parse(out decimal value)
        {
            var valid = Expression(out value);
            Space();
            return valid && position == input.Length;
        }

        private bool Expression(out decimal value)
        {
            if (!Term(out value)) return false;
            while (true)
            {
                Space();
                if (position == input.Length || input[position] is not ('+' or '-')) return true;
                var op = input[position++];
                if (!Term(out var right)) return false;
                value = op == '+' ? checked(value + right) : checked(value - right);
            }
        }

        private bool Term(out decimal value)
        {
            if (!Factor(out value)) return false;
            while (true)
            {
                Space();
                if (position == input.Length || input[position] is not ('*' or '/')) return true;
                var op = input[position++];
                if (!Factor(out var right)) return false;
                value = op == '*' ? checked(value * right) : checked(value / right);
            }
        }

        private bool Factor(out decimal value)
        {
            value = 0;
            Space();
            if (position == input.Length) return false;
            if (input[position] is '+' or '-')
            {
                var negative = input[position++] == '-';
                if (!Factor(out value)) return false;
                if (negative) value = -value;
                return true;
            }
            if (input[position] == '(')
            {
                position++;
                if (!Expression(out value)) return false;
                Space();
                if (position == input.Length || input[position++] != ')') return false;
                return true;
            }
            var start = position;
            while (position < input.Length &&
                (char.IsAsciiDigit(input[position]) || input[position] == '.')) position++;
            return start != position && decimal.TryParse(input[start..position],
                NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
        }

        private void Space()
        {
            while (position < input.Length && char.IsWhiteSpace(input[position])) position++;
        }
    }
}
