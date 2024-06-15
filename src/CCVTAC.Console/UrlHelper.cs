using System.Text.RegularExpressions;

namespace CCVTAC.Console;

public static class UrlHelper
{
    private static readonly Regex _httpsRegex = new("(?:https:|history)");

    private record IndexPair(int Start, int End);

    public static ImmutableList<string> SplitCombinedUrls(ICollection<string> urls)
    {
        List<string> safeUrls = new(urls.Count);

        foreach (var url in urls)
        {
            var matches = _httpsRegex.Matches(url).OfType<Match>();

            if (matches.Count() == 1)
            {
                safeUrls.Add(url);
                continue;
            }

            var startIndices = matches
                .Select(m => m.Index)
                .Reverse()
                .ToImmutableList();

            var indexPairs =
                startIndices
                    .Select((si, itemIndex) => {
                            int endIndex = itemIndex == 0 // If the last URL entered by user.
                                ? url.Length
                                : startIndices[itemIndex-1];
                            return new IndexPair(si, endIndex);
                        })
                    .ToImmutableList();

            safeUrls.AddRange(
                indexPairs
                    .Select(p => url[p.Start..p.End])
                    .Reverse());
        }

        return [.. safeUrls.Distinct()];
    }
}
