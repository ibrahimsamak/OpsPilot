using System.Text;

namespace OpsPilot.Orchestrator.Knowledge;
public sealed record TextChunk(int Index, string Heading, string Content);
public static class MarkdownChunker
{
    public static IReadOnlyList<TextChunk> Split(string markdown, int maxChars = 1200, int overlapChars = 150)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxChars, 200);
        ArgumentOutOfRangeException.ThrowIfNegative(overlapChars);
        if(overlapChars >= maxChars / 2)
            throw new ArgumentOutOfRangeException(nameof(overlapChars), "Overlap must be less than half of maxChars.");
    
        var chunks = new List<TextChunk>();

        foreach (var (heading, body) in ReadSections(markdown))
            foreach(var piece in SplitBody(body, maxChars, overlapChars))
                chunks.Add(new TextChunk(chunks.Count, heading, piece));

        return chunks;
    }

    public static List<(string Heading, string Body)> ReadSections(string mmarkdown)
    {
        var sections = new List<(string, string)>();
        var title = "";
        var heading = "";
        var body = new StringBuilder();
        var inCodeFence = false;

        void Flush()
        {
            var text = body.ToString().Trim();
            if(text.Length > 0) sections.Add((heading, text));
            body.Clear();

        }
        foreach(var line in mmarkdown.Replace("\r\n", "\n").Split("\n"))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                inCodeFence = !inCodeFence;


            var level = inCodeFence ? 0 : HeadingLevel(line);
            if (level == 0)
            {
                body.AppendLine(line);
                continue;
            }

            Flush();
            var text = line[level..].Trim();
            if (level == 1)
            {
                title = text;
                heading = text;
            }
            else
            {
                heading = title.Length > 0 ? $"{title} > {text}" : text;
            }
        }
        Flush();
        return sections;
    }
    private static int HeadingLevel(string line)
    {
        var level = 0;
        while (level < line.Length && line[level] == '#') level++;
        return level is >= 1 and <= 6 && line.Length > level && line[level] == ' ' ? level : 0;
    }
    private static IEnumerable<string> SplitBody(string body, int maxChars, int overlapChars)
    {
        if (body.Length <= maxChars)
        {
            yield return body;
            yield break;
        }

        // Longest paragraph that still fits after an overlap prefix and separator.
        var limit = maxChars - overlapChars - 2;
        var paragraphs = body
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(p => p.Length <= limit ? new[] { p } : HardSplit(p, limit));

        var current = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            if (current.Length > 0 && current.Length + paragraph.Length + 2 > maxChars)
            {
                var done = current.ToString().TrimEnd();
                yield return done;
                current.Clear();
                if (overlapChars > 0) current.Append(Tail(done, overlapChars)).Append("\n\n");
            }
            current.Append(paragraph).Append("\n\n");
        }

        if (current.Length > 0) yield return current.ToString().TrimEnd();
    }

    private static string[] HardSplit(string text, int size)
    {
        var parts = new List<string>();
        for (var i = 0; i < text.Length; i += size)
            parts.Add(text.Substring(i, Math.Min(size, text.Length - i)));
        return [.. parts];
    }

    private static string Tail(string text, int chars)
    {
        if (text.Length <= chars) return text;
        var tail = text[^chars..];
        var firstSpace = tail.IndexOf(' ');
        return firstSpace > 0 ? tail[(firstSpace + 1)..] : tail;
    }
}