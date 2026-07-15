namespace Fire.Core;

/// <summary>
/// Orchestrates a find/replace run: discovers files, replaces in contents, then
/// renames files and directories. Never changes the current directory, never
/// touches the git index, and in preview mode never touches the disk at all.
/// </summary>
public class FireEngine
{
    private readonly string workingDirectory;
    private readonly Options options;
    private readonly TextWriter output;

    public FireEngine(string workingDirectory, Options options, TextWriter? output = null)
    {
        this.workingDirectory = Path.GetFullPath(workingDirectory);
        this.options = options;
        this.output = output ?? Console.Out;
    }

    public int Run()
    {
        var version = typeof(FireEngine).Assembly.GetName().Version;
        Log($"Fire v{version?.ToString(2)}");
        Log($"Directory: {workingDirectory}", verboseOnly: true);
        Log(options.ToString(), verboseOnly: true);

        if (options.Regex && options.AdaptCase)
        {
            Log("Error: --regex and --adapt-case are incompatible options.");
            return 1;
        }

        if (string.IsNullOrEmpty(options.FindString))
        {
            Log("Error: find_string must not be empty.");
            return 1;
        }

        // Identical strings can still be a meaningful run with -i (case normalization)
        // or -x (a pattern is not a literal), but never with a plain literal match.
        if (options.FindString == options.ReplaceString && !options.Regex && !options.IgnoreCase)
        {
            Log("find_string and replace_string are identical. Nothing to do.");
            return 0;
        }

        TextReplacer replacer;
        try
        {
            replacer = TextReplacer.Create(options, Log);
        }
        catch (ArgumentException ex)
        {
            Log($"Error: invalid regular expression: {ex.Message}");
            return 1;
        }

        var tree = new FileSystemWalker().Walk(workingDirectory, respectGitIgnore: !options.NoIgnore);

        Func<string, bool> fileFilter = _ => true;
        if (!string.IsNullOrEmpty(options.FilePattern))
            fileFilter = new GlobFilter(options.FilePattern).IsMatch;

        var (changedFiles, replacements) = ReplaceInContents(tree, replacer, fileFilter);

        var renames = 0;
        if (!options.NoFilenames)
        {
            // A file pattern targets files, so directory names are left alone when one is given.
            var renameDirectories = string.IsNullOrEmpty(options.FilePattern);
            var plan = RenamePlanner.Plan(tree, replacer, fileFilter, renameDirectories, Log);
            renames = RenamePlanner.Execute(plan, workingDirectory, options.Preview, Log);
        }

        var prefix = options.Preview ? "[preview] " : "";
        Log($"{prefix}Done: {replacements} replacement(s) in {changedFiles} file(s), {renames} rename(s).");
        return 0;
    }

    private (int ChangedFiles, int Replacements) ReplaceInContents(WalkResult tree, TextReplacer replacer, Func<string, bool> fileFilter)
    {
        var changedFiles = 0;
        var totalReplacements = 0;

        foreach (var file in tree.Files.Where(fileFilter))
        {
            var absolute = Path.Combine(workingDirectory, file.Replace('/', Path.DirectorySeparatorChar));

            TextFile text;
            try
            {
                text = TextFile.Read(absolute);
            }
            catch (IOException ex)
            {
                Log($"Warning: could not read {file}: {ex.Message}");
                continue;
            }

            if (text.IsBinary)
            {
                Log($"Skipped binary file {file}", verboseOnly: true);
                continue;
            }

            var replaced = replacer.Replace(text.Content, out var count);
            if (count == 0)
                continue;

            changedFiles++;
            totalReplacements += count;

            if (options.Preview)
            {
                Log($"[preview] Would replace {count} occurrence(s) in {file}");
            }
            else
            {
                text.Write(absolute, replaced);
                Log($"Replaced {count} occurrence(s) in {file}");
            }
        }

        return (changedFiles, totalReplacements);
    }

    private void Log(string line) => Log(line, verboseOnly: false);

    private void Log(string line, bool verboseOnly)
    {
        if (!verboseOnly || options.Verbose)
            output.WriteLine(line);
    }
}
