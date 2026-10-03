// <copyright file="SolutionVerifier.cs" company="LearnPip contributors">
// Copyright (c) LearnPip contributors. Licensed under AGPL-3.0-only.
// </copyright>

using System.Globalization;
using System.Text.RegularExpressions;

namespace LearnPip.Api.Ai;

/// <summary>Checks only bounded, unitless decimal arithmetic. Unsupported subjects stay unverified.</summary>
public static class SolutionVerifier
{
    /// <summary>
    /// Prüft, ob ein Hinweis die Antwort nicht vorwegnimmt.
    /// </summary>
    /// <param name="hint">Der zu prüfende Lernhinweis.</param>
    /// <param name="answer">Die Antwort, die der Hinweis nicht vorwegnehmen darf.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static bool SafeHint(string? hint, string answer) =>
        string.IsNullOrWhiteSpace(hint) || string.IsNullOrWhiteSpace(answer) ||
        (hint.Length <= 500 && answer.Length <= 4000 &&
        !Regex.IsMatch(
        hint,
        @"(?<![\p{L}\p{N}])" + Regex.Escape(answer.Trim()) + @"(?![\p{L}\p{N}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100)));

    /// <summary>
    /// Vergleicht berechnete und referenzierte Lösungsangaben.
    /// </summary>
    /// <param name="formula">Die zu prüfende Formel, sofern vorhanden.</param>
    /// <param name="computed">Die berechnete Lösung, sofern vorhanden.</param>
    /// <param name="reference">Die Referenzlösung, sofern vorhanden.</param>
    /// <param name="chosenAnswer">Die vom Benutzer bestätigte Antwort.</param>
    /// <param name="steps">Die beschriebenen Lösungsschritte.</param>
    /// <param name="questionText">Die erkannte Fragenformulierung.</param>
    /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
    public static SolutionCheck Check(
        string? formula,
        string? computed,
        string? reference,
        string? chosenAnswer,
        IReadOnlyList<string>? steps = null,
        string? questionText = null)
    {
        var hasFormula = TryTask(formula, out var expected);
        var hasQuestion = TryTask(questionText, out var questionValue);
        if (hasFormula && hasQuestion && expected != questionValue)
        {
            return new("conflict", "Erkannte Frage und Formel widersprechen sich.", null);
        }

        if (!hasFormula && !hasQuestion)
        {
            return new("unverified", "Keine eindeutig berechenbare Dezimalaufgabe erkannt; " + "fachlich selbst prüfen.", null);
        }

        if (!hasFormula)
        {
            expected = questionValue;
        }

        var value = expected.ToString(CultureInfo.InvariantCulture);
        foreach (var (label, candidate) in new[]
        {
            ("Lösung", computed), ("Musterlösung", reference), ("ausgewählte Antwort", chosenAnswer),
        })
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (!TryNumber(candidate, out var number))
            {
                return new("unverified", $"{label} ist nicht eindeutig numerisch; fachlich selbst prüfen.", value);
            }

            if (number != expected)
            {
                return new("conflict", $"{label} widerspricht der unabhängig berechneten Aufgabe ({value}).", value);
            }
        }

        if (string.IsNullOrWhiteSpace(computed) || string.IsNullOrWhiteSpace(chosenAnswer))
        {
            return new("unverified", "Lösung oder bestätigte Antwort fehlt.", value);
        }

        foreach (var step in steps ?? [])
        {
            var parts = step.Split('=');
            if (parts.Length != 2 || parts[0].Length > 128 || parts[1].Length > 128)
            {
                continue;
            }

            if (TryExpression(parts[0], out var left) && TryExpression(parts[1], out var right) &&
                left != right)
            {
                return new("conflict", "Ein numerischer Lösungsschritt enthält einen Widerspruch.", value);
            }
        }

        return new("verified", "Dezimalrechnung, angegebene Lösung und gewählte Antwort stimmen " + "überein. Aufgabenverständnis und Bildübertragung selbst kontrollieren.", value);
    }

    private static bool TryTask(
        string? text,
        out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var formula = text.Trim().Replace('×', '*').Replace('÷', '/').Replace('−', '-');
        if (formula.EndsWith('?'))
        {
            formula = formula[..^1].TrimEnd();
        }

        if (formula.EndsWith('='))
        {
            formula = formula[..^1].TrimEnd();
        }

        return TryExpression(formula, out value);
    }

    private static bool TryNumber(string? text, out decimal value) =>
        decimal.TryParse(
        text?.Trim(),
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
        CultureInfo.InvariantCulture,
        out value);

    private static bool TryExpression(
        string? text,
        out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(text) || text.Length > 128)
        {
            return false;
        }

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

        /// <summary>
        /// Liest ein strukturiertes KI-Ergebnis und prüft den erkannten Fragenentwurf.
        /// </summary>
        /// <param name="value">Der neue Wert der Einstellung.</param>
        /// <returns>Das Ergebnis der beschriebenen Operation.</returns>
        public bool Parse(out decimal value)
        {
            var valid = this.Expression(out value);
            this.Space();
            return valid && this.position == input.Length;
        }

        private bool Expression(out decimal value)
        {
            if (!this.Term(out value))
            {
                return false;
            }

            while (true)
            {
                this.Space();
                if (this.position == input.Length || input[this.position] is not ('+' or '-'))
                {
                    return true;
                }

                var op = input[this.position++];
                if (!this.Term(out var right))
                {
                    return false;
                }

                value = op == '+' ? checked(value + right) : checked(value - right);
            }
        }

        private bool Term(out decimal value)
        {
            if (!this.Factor(out value))
            {
                return false;
            }

            while (true)
            {
                this.Space();
                if (this.position == input.Length || input[this.position] is not ('*' or '/'))
                {
                    return true;
                }

                var op = input[this.position++];
                if (!this.Factor(out var right))
                {
                    return false;
                }

                value = op == '*' ? checked(value * right) : checked(value / right);
            }
        }

        private bool Factor(out decimal value)
        {
            value = 0;
            this.Space();
            if (this.position == input.Length)
            {
                return false;
            }

            if (input[this.position] is '+' or '-')
            {
                var negative = input[this.position++] == '-';
                if (!this.Factor(out value))
                {
                    return false;
                }

                if (negative)
                {
                    value = -value;
                }

                return true;
            }

            if (input[this.position] == '(')
            {
                this.position++;
                if (!this.Expression(out value))
                {
                    return false;
                }

                this.Space();
                if (this.position == input.Length || input[this.position++] != ')')
                {
                    return false;
                }

                return true;
            }

            var start = this.position;
            while (this.position < input.Length &&
                (char.IsAsciiDigit(input[this.position]) || input[this.position] == '.'))
            {
                this.position++;
            }

            return start != this.position && decimal.TryParse(
                input[start..this.position],
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out value);
        }

        private void Space()
        {
            while (this.position < input.Length && char.IsWhiteSpace(input[this.position]))
            {
                this.position++;
            }
        }
    }
}
