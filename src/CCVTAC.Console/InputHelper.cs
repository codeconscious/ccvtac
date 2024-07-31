using System.Text.RegularExpressions;

namespace CCVTAC.Console;

public static class InputHelper
{
    /// <summary>
    /// A regular expression that detects where commands and URLs begin.
    /// </summary>
    private static readonly Regex _regex = new("""(?:https:|\\)""");

    private record IndexPair(int Start, int End);

    public static ImmutableList<string> SplitInputs(string input)
    {
        var splitInputs = new List<string>();

        var matches = _regex.Matches(input).OfType<Match>().ToImmutableArray();

        if (matches.Length == 0)
        {
            return [..splitInputs];
        }

        if (matches.Length == 1)
        {
            splitInputs.Add(input);
            return [.. splitInputs];
        }

        var startIndices = matches
            .Select(m => m.Index)
            .Reverse()
            .ToImmutableList();

        var indexPairs =
            startIndices
                .Select((si, itemIndex) => {
                        int endIndex = itemIndex == 0 // If the last URL entered by user.
                            ? input.Length
                            : startIndices[itemIndex-1];
                        return new IndexPair(si, endIndex);
                    })
                .ToImmutableList();

        splitInputs.AddRange(
            indexPairs
                .Select(p => input[p.Start..p.End])
                .Reverse());


        return [.. splitInputs.Select(i => i.Trim()).Distinct()];
    }
}
