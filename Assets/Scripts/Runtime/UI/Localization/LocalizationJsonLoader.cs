using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class LocalizationJsonLoader
{
    private static readonly Regex PairRegex = new(
        "\"(?<key>(?:\\\\.|[^\"])*)\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"",
        RegexOptions.Compiled);

    public static Dictionary<string, string> ParseFlatJsonObject(string json)
    {
        Dictionary<string, string> values = new();
        if (string.IsNullOrWhiteSpace(json))
        {
            return values;
        }

        foreach (Match match in PairRegex.Matches(json))
        {
            string key = Unescape(match.Groups["key"].Value);
            string value = Unescape(match.Groups["value"].Value);
            if (!string.IsNullOrEmpty(key))
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static string Unescape(string value)
    {
        return value
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\");
    }
}
