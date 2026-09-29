namespace LearnPip.Api;

public sealed record ApiResponse<T>(T Data);
public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public sealed record QuestionSummary(Guid Id, int? Version, string? Prompt);
public sealed record QuestionDetails(Guid Id, QuestionVersionDetails? LatestVersion);
public sealed record QuestionVersionDetails(Guid Id, int Version, string Prompt);
public sealed record MediaDetails(Guid Id, string MediaType, long ByteLength, Guid? QuestionVersionId, string AltText);
