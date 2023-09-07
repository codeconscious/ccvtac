namespace CCVTAC.Console;

public static class ExtensionMethods
{
    public static bool ContainsCaseInsensitive(this IEnumerable<string> collection, string toCheck)
    {
        return collection?.Any(item => item.ContainsCaseInsensitive(toCheck)) == true;
    }

    public static bool ContainsCaseInsensitive(this string source, string toCheck)
    {
        return source?.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
