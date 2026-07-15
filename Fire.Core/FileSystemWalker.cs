using MAB.DotIgnore;

namespace Fire.Core;

public sealed class WalkResult
{
    public List<string> Files { get; } = [];        // relative paths, '/'-separated
    public List<string> Directories { get; } = [];  // relative paths, '/'-separated
}

/// <summary>
/// Walks the tree under a root directory. ".git" directories are always skipped.
/// When <c>respectGitIgnore</c> is set, .gitignore files (including nested ones)
/// are honoured. Nested files are combined with union semantics: a path excluded
/// at any level stays excluded (a nested "!pattern" cannot re-include something a
/// parent .gitignore excluded).
/// </summary>
public class FileSystemWalker
{
    public WalkResult Walk(string root, bool respectGitIgnore)
    {
        var result = new WalkResult();
        Recurse(root, "", respectGitIgnore, [], result);
        return result;
    }

    private static void Recurse(string absDir, string relDir, bool respect, List<(IgnoreList Rules, string BaseRel)> stack, WalkResult result)
    {
        var pushed = false;
        if (respect)
        {
            var gitIgnore = Path.Combine(absDir, ".gitignore");
            if (File.Exists(gitIgnore))
            {
                stack.Add((new IgnoreList(File.ReadAllLines(gitIgnore)), relDir));
                pushed = true;
            }
        }

        foreach (var dir in Directory.EnumerateDirectories(absDir))
        {
            var name = Path.GetFileName(dir);
            if (name == ".git")
                continue;
            var rel = Join(relDir, name);
            if (respect && IsIgnored(stack, rel, isDirectory: true))
                continue;
            result.Directories.Add(rel);
            Recurse(dir, rel, respect, stack, result);
        }

        foreach (var file in Directory.EnumerateFiles(absDir))
        {
            var rel = Join(relDir, Path.GetFileName(file));
            if (respect && IsIgnored(stack, rel, isDirectory: false))
                continue;
            result.Files.Add(rel);
        }

        if (pushed)
            stack.RemoveAt(stack.Count - 1);
    }

    private static bool IsIgnored(List<(IgnoreList Rules, string BaseRel)> stack, string rel, bool isDirectory)
    {
        foreach (var (rules, baseRel) in stack)
        {
            // Each .gitignore applies to paths relative to its own directory.
            var sub = baseRel.Length == 0 ? rel : rel.Substring(baseRel.Length + 1);
            if (rules.IsIgnored(sub, isDirectory))
                return true;
        }
        return false;
    }

    private static string Join(string a, string b) => a.Length == 0 ? b : a + "/" + b;
}
