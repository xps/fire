using Microsoft.Extensions.FileSystemGlobbing;

namespace Fire.Core;

/// <summary>
/// Matches relative file paths against a user-supplied glob pattern.
/// A pattern without a directory separator (e.g. "*.cs") matches at any depth.
/// </summary>
public class GlobFilter
{
    private readonly Matcher matcher;

    public GlobFilter(string pattern)
    {
        pattern = pattern.Replace('\\', '/').TrimStart('/');
        if (pattern.StartsWith("./"))
            pattern = pattern.Substring(2);
        if (!pattern.Contains('/'))
            pattern = "**/" + pattern;

        matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(pattern);
    }

    public bool IsMatch(string relativePath) => matcher.Match(relativePath).HasMatches;
}
