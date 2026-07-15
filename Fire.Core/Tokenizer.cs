namespace Fire.Core;

public static class Tokenizer
{
    /// <summary>
    /// Splits an identifier-like string into lowercase word tokens.
    /// "BionicBeaver" -> [bionic, beaver], "XMLHttpRequest" -> [xml, http, request],
    /// "utf8String" -> [utf8, string], "BIONIC_BEAVER" -> [bionic, beaver].
    /// Returns null when the string is not identifier-like (contains characters
    /// other than letters, digits, '-' and '_').
    /// </summary>
    public static IReadOnlyList<string>? Tokenize(string s)
    {
        if (string.IsNullOrEmpty(s))
            return null;

        foreach (var c in s)
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                return null;

        var tokens = new List<string>();
        var start = 0;
        for (var i = 0; i <= s.Length; i++)
        {
            var atEnd = i == s.Length;
            var isSeparator = !atEnd && (s[i] == '-' || s[i] == '_');
            if (atEnd || isSeparator || IsCaseBoundary(s, i))
            {
                if (i > start)
                    tokens.Add(s.Substring(start, i - start).ToLowerInvariant());
                start = isSeparator ? i + 1 : i;
            }
        }

        return tokens.Count > 0 ? tokens : null;
    }

    private static bool IsCaseBoundary(string s, int i)
    {
        if (i == 0 || i >= s.Length)
            return false;

        char prev = s[i - 1], c = s[i];
        if (prev == '-' || prev == '_')
            return false;

        // aB / 8B boundaries
        if (char.IsUpper(c) && (char.IsLower(prev) || char.IsDigit(prev)))
            return true;

        // Acronym end: "XMLHttp" splits before "Http"
        if (char.IsUpper(prev) && char.IsUpper(c) && i + 1 < s.Length && char.IsLower(s[i + 1]))
            return true;

        return false;
    }
}
