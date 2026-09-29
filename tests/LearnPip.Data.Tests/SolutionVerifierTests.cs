using LearnPip.Api.Ai;
using LearnPip.Api.Questions;

namespace LearnPip.Data.Tests;

public sealed class SolutionVerifierTests
{
    [Theory]
    [InlineData("2 + 3 * 4 = ?", "14", "14", "14", "verified")]
    [InlineData("(2 + 3) * 4", "20", null, "20", "verified")]
    [InlineData("2 + 3 * 4", "20", null, "20", "conflict")]
    [InlineData("8 / 0", "0", null, "0", "unverified")]
    [InlineData("10 m / 2", "5", null, "5", "unverified")]
    [InlineData("2 + 2", "4", "5", "4", "conflict")]
    [InlineData("Biologie: Mitochondrien", "Zellatmung", null, "Zellatmung", "unverified")]
    public void Arithmetic_is_checked_but_unsupported_subjects_are_not_claimed_verified(
        string formula, string solution, string? reference, string answer, string status)
    {
        Assert.Equal(status, SolutionVerifier.Check(formula, solution, reference, answer).Status);
    }

    [Fact]
    public void Contradictory_steps_and_spoiler_hints_are_withheld()
    {
        Assert.Equal("conflict", SolutionVerifier.Check("2+2", "4", null, "4",
            questionText: "2+3=?").Status);
        Assert.Equal("conflict", SolutionVerifier.Check("2+2", "4", null, "4",
            ["2+2=5"]).Status);
        Assert.False(SolutionVerifier.SafeHint("Das Ergebnis ist 4.", "4"));
        Assert.True(SolutionVerifier.SafeHint("Zähle die beiden Summanden.", "4"));
        var explanation = new[]
        {
            new ContentBlockOutput("text", "[Hinweis] Denke an 4.\n[Nächster Schritt] " +
                "Zähle zuerst die Summanden.\nDie Lösung ist 4.", null, null)
        };
        var answers = new[]
        {
            new AnswerOutput(Guid.NewGuid(), true,
                [new ContentBlockOutput("text", "4", null, null)])
        };
        var guidance = LearningSessionEndpoints.Guidance(explanation, answers);
        Assert.Null(guidance.Hint);
        Assert.Equal("Zähle zuerst die Summanden.", guidance.NextStep);
    }
}
