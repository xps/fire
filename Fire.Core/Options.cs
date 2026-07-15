using System.Text;
using CommandLine;

namespace Fire.Core;

// Usage: fire [options] <find_string> <replace_string> [file_pattern]
public class Options
{
    [Value(0, MetaName = "find_string", HelpText = "Text to find", Required = true)]
    public string FindString { get; set; } = "";

    [Value(1, MetaName = "replace_string", HelpText = "Replacement text", Required = true)]
    public string ReplaceString { get; set; } = "";

    [Value(2, MetaName = "files", HelpText = "Glob pattern restricting which files are processed (e.g. *.cs or src/**/*.cshtml)", Required = false)]
    public string? FilePattern { get; set; }

    [Option('v', "verbose", HelpText = "Output more information")]
    public bool Verbose { get; set; }

    [Option('n', "no-filenames", HelpText = "Don't replace in file/directory names")]
    public bool NoFilenames { get; set; }

    [Option('i', "ignore-case", HelpText = "Case insensitive text comparison")]
    public bool IgnoreCase { get; set; }

    [Option('a', "adapt-case", HelpText = "Match case variants of find_string (PascalCase, camelCase, kebab-case, snake_case, UPPERCASE...) and adapt replace_string to each match")]
    public bool AdaptCase { get; set; }

    [Option('w', "whole-word", HelpText = "Match whole word only")]
    public bool WholeWord { get; set; }

    [Option('x', "regex", HelpText = "Match find_string as a .NET regular expression (replace_string may use $1, ${name}...)")]
    public bool Regex { get; set; }

    [Option('p', "preview", HelpText = "Preview changes only, don't touch any files")]
    public bool Preview { get; set; }

    [Option('u', "no-ignore", HelpText = "Also process files excluded by .gitignore (.git folders are always skipped)")]
    public bool NoIgnore { get; set; }

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.AppendLine("Options:");
        builder.AppendLine($"  FindString = {FindString}");
        builder.AppendLine($"  ReplaceString = {ReplaceString}");
        builder.AppendLine($"  FilePattern = {FilePattern}");
        builder.AppendLine($"  NoFilenames = {NoFilenames}");
        builder.AppendLine($"  IgnoreCase = {IgnoreCase}");
        builder.AppendLine($"  AdaptCase = {AdaptCase}");
        builder.AppendLine($"  WholeWord = {WholeWord}");
        builder.AppendLine($"  Regex = {Regex}");
        builder.AppendLine($"  Preview = {Preview}");
        builder.AppendLine($"  NoIgnore = {NoIgnore}");

        return builder.ToString();
    }
}
