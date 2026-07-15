namespace Fire.Core;

public static class SmartCase
{
    private enum WordCase { Lower, Upper, Pascal, Camel }

    private static readonly (string Separator, WordCase Case)[] Styles =
    {
        ("", WordCase.Pascal),   // BionicBeaver
        ("", WordCase.Camel),    // bionicBeaver
        ("", WordCase.Lower),    // bionicbeaver
        ("", WordCase.Upper),    // BIONICBEAVER
        ("-", WordCase.Lower),   // bionic-beaver
        ("-", WordCase.Upper),   // BIONIC-BEAVER
        ("-", WordCase.Pascal),  // Bionic-Beaver
        ("_", WordCase.Lower),   // bionic_beaver
        ("_", WordCase.Upper),   // BIONIC_BEAVER
        ("_", WordCase.Pascal),  // Bionic_Beaver
    };

    /// <summary>
    /// Builds the map of case variants of <paramref name="find"/> to the matching
    /// case variants of <paramref name="replace"/>. Returns null when either string
    /// is not identifier-like (caller should fall back to plain replacement).
    /// The verbatim find string always maps to the verbatim replace string.
    /// </summary>
    public static IReadOnlyDictionary<string, string>? BuildVariantMap(string find, string replace)
    {
        var findTokens = Tokenizer.Tokenize(find);
        var replaceTokens = Tokenizer.Tokenize(replace);
        if (findTokens == null || replaceTokens == null)
            return null;

        var map = new Dictionary<string, string>(StringComparer.Ordinal) { [find] = replace };
        foreach (var (separator, wordCase) in Styles)
        {
            var key = Render(findTokens, separator, wordCase);
            if (!map.ContainsKey(key))
                map[key] = Render(replaceTokens, separator, wordCase);
        }

        return map;
    }

    private static string Render(IReadOnlyList<string> tokens, string separator, WordCase wordCase)
    {
        return string.Join(separator, tokens.Select((token, i) => wordCase switch
        {
            WordCase.Upper => token.ToUpperInvariant(),
            WordCase.Pascal => Capitalize(token),
            WordCase.Camel when i > 0 => Capitalize(token),
            _ => token,
        }));
    }

    private static string Capitalize(string token) =>
        char.ToUpperInvariant(token[0]) + token.Substring(1);
}
