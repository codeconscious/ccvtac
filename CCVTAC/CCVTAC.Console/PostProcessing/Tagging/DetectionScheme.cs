namespace CCVTAC.Console.PostProcessing.Tagging;

/// <summary>
/// A scheme used to apply regex to search specific text to search for matches,
/// then specify a regex match group
/// </summary>
/// <param name="RegexPattern">A regex pattern that will be used to instantiate a new `Regex`.</param>
/// <param name="MatchGroup">
///     The regex match group whose value should be used.
///     Use `(` and `)` in the pattern to make groups.
///     Zero represents the entire match.
/// </param>
/// <param name="TagName">The text to which the regex pattern should be applied.</param>
/// <param name="Note">The source of the match, used only for user output.</param>
public record struct DetectionScheme(
    string    RegexPattern, // TODO: Might be worth instantiating the `Regex` instance in the ctor.
    int       MatchGroup,
    SourceTag TagName,
    string?   Note = null
);
