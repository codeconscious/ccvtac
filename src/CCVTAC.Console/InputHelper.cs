using System.Text.RegularExpressions;

namespace CCVTAC.Console;

public static class InputHelper
{
    /// <summary>
    /// A regular expression that detects where commands and URLs begin.
    /// </summary>
    private static readonly Regex _regex = new("""(?:https:|\\)""");

    private record IndexPair(int Start, int End);

    /// <summary>
    /// Takes a user input string and splits it into a collection of inputs based
    /// upon substrings detected by the class's regular expression pattern.
    /// </summary>
    public static ImmutableArray<string> SplitInputs(string input)
    {
        var matches = _regex
            .Matches(input)
            .OfType<Match>()
            .ToImmutableArray();

        if (matches.Length == 0)
        {
            return [];
        }

        if (matches.Length == 1)
        {
            return [input];
        }

        var startIndices = matches
            .Select(m => m.Index)
            .ToImmutableArray();

        var indexPairs = startIndices
            .Select((startIndex, iterIndex) =>
                {
                    int endIndex = iterIndex == startIndices.Length - 1
                        ? input.Length
                        : startIndices[iterIndex + 1];

                    return new IndexPair(startIndex, endIndex);
                });

        var splitInputs = indexPairs
            .Select(p => input[p.Start..p.End].Trim())
            .Distinct();

        return [.. splitInputs];
    }
}
