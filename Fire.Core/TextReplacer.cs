using System.Text.RegularExpressions;

namespace Fire.Core;

/// <summary>
/// Builds the single regex + evaluator used for every replacement (file contents
/// and file/directory names), so all options behave identically everywhere.
/// </summary>
public class TextReplacer
{
    private readonly Regex regex;
    private readonly Func<Match, string> evaluate;

    private TextReplacer(Regex regex, Func<Match, string> evaluate)
    {
        this.regex = regex;
        this.evaluate = evaluate;
    }

    /// <exception cref="ArgumentException">The user-supplied regular expression is invalid.</exception>
    public static TextReplacer Create(Options options, Action<string>? warn = null)
    {
        string pattern;
        Func<Match, string> evaluate;
        var regexOptions = RegexOptions.CultureInvariant;

        if (options.AdaptCase)
        {
            var map = SmartCase.BuildVariantMap(options.FindString, options.ReplaceString);
            if (map != null)
            {
                // Longest variant first so e.g. "BIONIC-BEAVER" wins over a shorter overlap.
                pattern = string.Join("|", map.Keys
                    .OrderByDescending(k => k.Length)
                    .ThenBy(k => k, StringComparer.Ordinal)
                    .Select(Regex.Escape));
                evaluate = match => map[match.Value];
            }
            else
            {
                warn?.Invoke("Note: --adapt-case expects identifier-like strings; falling back to case-insensitive literal replacement.");
                pattern = Regex.Escape(options.FindString);
                regexOptions |= RegexOptions.IgnoreCase;
                evaluate = _ => options.ReplaceString;
            }
        }
        else
        {
            pattern = options.Regex ? options.FindString : Regex.Escape(options.FindString);
            if (options.IgnoreCase)
                regexOptions |= RegexOptions.IgnoreCase;
            evaluate = options.Regex
                ? match => match.Result(options.ReplaceString)
                : _ => options.ReplaceString;
        }

        if (options.WholeWord)
            pattern = $@"(?<!\w)(?:{pattern})(?!\w)";

        return new TextReplacer(new Regex(pattern, regexOptions), evaluate);
    }

    public string Replace(string input, out int count)
    {
        var replacements = 0;
        var result = regex.Replace(input, match =>
        {
            replacements++;
            return evaluate(match);
        });
        count = replacements;
        return result;
    }
}
