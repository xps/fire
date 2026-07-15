using System.Text;
using Fire.Core;

namespace Fire.Tests;

[TestClass]
public static class TestHelpers
{
    public static readonly string TestRoot = Path.Combine(Path.GetTempPath(), "FireTests");

    /// <summary>
    /// Creates a temp directory containing the given files. By convention each
    /// file's content is its own relative path, so after a run a renamed file is
    /// expected to contain its new relative path (contents and names are replaced
    /// by the same engine).
    /// </summary>
    public static string CreateTestProject(string name, params string[] files)
    {
        var directory = Path.Combine(TestRoot, name + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(directory);
        foreach (var file in files)
            CreateFile(directory, file, NormalizePath(file));
        return directory;
    }

    public static void CreateFile(string root, string relativePath, string content)
    {
        var full = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    public static int Run(string path, Options options, out string output)
    {
        using var writer = new StringWriter();
        var exitCode = new FireEngine(path, options, writer).Run();
        output = writer.ToString();
        return exitCode;
    }

    public static int Run(string path, Options options) => Run(path, options, out _);

    /// <summary>
    /// Asserts the directory contains exactly the expected files (ignoring .git) and,
    /// when checkContents is set, that each file's content equals its relative path
    /// (see CreateTestProject).
    /// </summary>
    public static void AssertFiles(string path, string[] expectedFiles, bool checkContents = true)
    {
        var expected = expectedFiles.Select(NormalizePath).ToHashSet(StringComparer.Ordinal);
        var actual = ListFiles(path).ToHashSet(StringComparer.Ordinal);

        var message = new StringBuilder();

        var missing = expected.Except(actual).OrderBy(f => f, StringComparer.Ordinal).ToList();
        if (missing.Count > 0)
            message.AppendLine("Missing files:\n" + string.Join("\n", missing) + "\n");

        var unexpected = actual.Except(expected).OrderBy(f => f, StringComparer.Ordinal).ToList();
        if (unexpected.Count > 0)
            message.AppendLine("Unexpected files:\n" + string.Join("\n", unexpected) + "\n");

        if (checkContents)
        {
            foreach (var file in expected.Intersect(actual))
            {
                var content = File.ReadAllText(Path.Combine(path, file));
                if (content != file)
                    message.AppendLine($"Content mismatch in {file}:\n  expected: {file}\n  actual:   {content}\n");
            }
        }

        if (message.Length > 0)
            Assert.Fail(message.ToString().Trim());
    }

    public static List<string> ListFiles(string path)
    {
        return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
            .Select(f => NormalizePath(Path.GetRelativePath(path, f)))
            .Where(f => !f.StartsWith(".git/"))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
    }

    public static string ReadFile(string root, string relativePath) =>
        File.ReadAllText(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    public static byte[] ReadBytes(string root, string relativePath) =>
        File.ReadAllBytes(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    public static string NormalizePath(string path) => path.Replace('\\', '/');

    [AssemblyCleanup]
    public static void CleanUp()
    {
        try
        {
            if (Directory.Exists(TestRoot))
                Directory.Delete(TestRoot, true);
        }
        catch
        {
            // Best effort: leftovers in the temp folder are harmless.
        }
    }
}
