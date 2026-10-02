using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using OpsPilot.Orchestrator.Chat;

namespace OpsPilot.Orchestrator.Knowledge;

public sealed record GroundingResult(IReadOnlyList<Citation> Citations, IReadOnlyList<string> UnknownIds);

public sealed partial class CitationCollector
{
    private readonly ConcurrentDictionary<string, KnowledgeHit> _retrieved = new();

    public void Register(IEnumerable<KnowledgeHit> hits)
    {
        foreach (var hit in hits) _retrieved[hit.Id] = hit;
    }

    public GroundingResult Resolve(string answer)
    {
        var ids = CitationId().Matches(answer).Select(m => m.Groups[1].Value).Distinct().ToList();

        var citations = ids
            .Where(_retrieved.ContainsKey)
            .Select(id => _retrieved[id])
            .Select(h => new Citation(h.Id, h.Source, h.Heading, Snippet(h.Content)))
            .ToList();

        var unknown = ids.Where(id => !_retrieved.ContainsKey(id)).ToList();
        return new GroundingResult(citations, unknown);
    }

    private static string Snippet(string text) => text.Length <= 280 ? text : text[..277] + "...";

    // Matches ids like payment-failure-codes.md#4 anywhere in the text (with or without brackets).
    [GeneratedRegex(@"([A-Za-z0-9._\-]+\.md#\d+)")]
    private static partial Regex CitationId();
}
