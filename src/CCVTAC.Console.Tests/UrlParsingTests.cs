using System.Collections.Immutable;
using CCVTAC.Console;

namespace CCVTAC.Tests;

public sealed class UrlParsingTests
{
    [Fact]
    public void MultipleValidUrlsEntered_CorrectlyParsed()
    {
        ImmutableList<string> combined = ["https://youtu.be/5OpuZHsPBhQhttps://youtu.be/NT22EGxTuNw"];
        ImmutableList<string> expected = ["https://youtu.be/5OpuZHsPBhQ", "https://youtu.be/NT22EGxTuNw"];
        var actual = UrlHelper.SplitCombinedUrls(combined);
        Assert.Equal(expected.Count, actual.Count);
        Assert.Equal(expected[0], actual[0]);
        Assert.Equal(expected[1], actual[1]);
    }
}
