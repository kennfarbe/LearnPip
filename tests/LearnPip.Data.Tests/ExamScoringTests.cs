using LearnPip.Api.Exams;

namespace LearnPip.Data.Tests;

public sealed class ExamScoringTests
{
    [Fact]
    public void Every_required_part_must_pass_and_timing_is_per_part()
    {
        var operations = new SnapshotPart(new ProfilePart("B", "Betrieb", "B", 2, 45, 2, "B"),
            false, [new CatalogQuestion("B1", "B", "?", ["A", "B"], 0),
                new CatalogQuestion("B2", "B", "?", ["A", "B"], 1)]);
        var rules = new SnapshotPart(new ProfilePart("V", "Vorschriften", "V", 1, 45, 1, "V"),
            false, [new CatalogQuestion("V1", "V", "?", ["A", "B"], 0)]);
        var credited = new SnapshotPart(new ProfilePart("T-N", "Technik", "T-N", 1, 45, 1,
            "T-N"), true, []);
        var b = ExamScoring.Score(operations, new Dictionary<string, int>
        {
            ["B1"] = 0,
            ["B2"] = 1
        }, false);
        var v = ExamScoring.Score(rules, new Dictionary<string, int> { ["V1"] = 1 }, false);
        var t = ExamScoring.Score(credited, new Dictionary<string, int>(), false);
        Assert.True(b.Passed);
        Assert.False(v.Passed);
        Assert.True(t.Passed);
        Assert.False(ExamScoring.Passed([operations, rules, credited], [b, v, t]));
        Assert.False(ExamScoring.Score(operations, new Dictionary<string, int>
        {
            ["B1"] = 0,
            ["B2"] = 1
        }, true).Passed);
    }
}
