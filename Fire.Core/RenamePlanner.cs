namespace Fire.Core;

public record RenameOperation(string FromRelative, string ToRelative, bool IsDirectory);

/// <summary>
/// Plans and executes file/directory renames. The replacement is applied to each
/// name segment individually. Files are renamed first, then directories deepest
/// first, so no rename ever invalidates the path of a later one. A rename whose
/// target already exists is skipped with a warning (never deleted or merged),
/// except case-only renames which are always allowed.
/// </summary>
public static class RenamePlanner
{
    public static List<RenameOperation> Plan(
        WalkResult tree,
        TextReplacer replacer,
        Func<string, bool> fileFilter,
        bool renameDirectories,
        Action<string> warn)
    {
        var operations = new List<RenameOperation>();

        foreach (var file in tree.Files.Where(fileFilter))
            AddOperation(operations, file, isDirectory: false, replacer, warn);

        if (renameDirectories)
        {
            var deepestFirst = tree.Directories
                .OrderByDescending(d => d.Count(c => c == '/'))
                .ThenByDescending(d => d, StringComparer.Ordinal);
            foreach (var dir in deepestFirst)
                AddOperation(operations, dir, isDirectory: true, replacer, warn);
        }

        return operations;
    }

    private static void AddOperation(List<RenameOperation> operations, string relPath, bool isDirectory, TextReplacer replacer, Action<string> warn)
    {
        var slash = relPath.LastIndexOf('/');
        var parent = slash < 0 ? "" : relPath.Substring(0, slash);
        var name = slash < 0 ? relPath : relPath.Substring(slash + 1);

        var newName = replacer.Replace(name, out var count);
        if (count == 0 || newName == name)
            return;

        if (newName.Length == 0 || newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            warn($"Warning: skipped renaming {relPath}: \"{newName}\" is not a valid file name.");
            return;
        }

        operations.Add(new RenameOperation(relPath, parent.Length == 0 ? newName : parent + "/" + newName, isDirectory));
    }

    public static int Execute(List<RenameOperation> operations, string root, bool preview, Action<string> log)
    {
        var done = 0;
        foreach (var op in operations)
        {
            var fromAbs = ToAbsolute(root, op.FromRelative);
            var toAbs = ToAbsolute(root, op.ToRelative);

            // A case-only rename is valid even though the target "exists" (it is the source itself).
            var caseOnly = string.Equals(op.FromRelative, op.ToRelative, StringComparison.OrdinalIgnoreCase);
            if (!caseOnly && (File.Exists(toAbs) || Directory.Exists(toAbs)))
            {
                log($"Warning: skipped renaming {op.FromRelative} to {op.ToRelative}: target already exists.");
                continue;
            }

            if (preview)
            {
                log($"[preview] Would rename {op.FromRelative} to {op.ToRelative}");
                done++;
                continue;
            }

            try
            {
                if (op.IsDirectory)
                    Directory.Move(fromAbs, toAbs);
                else
                    File.Move(fromAbs, toAbs);
                log($"Renamed {op.FromRelative} to {op.ToRelative}");
                done++;
            }
            catch (IOException ex)
            {
                log($"Warning: could not rename {op.FromRelative}: {ex.Message}");
            }
        }
        return done;
    }

    private static string ToAbsolute(string root, string relative) =>
        Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
}
