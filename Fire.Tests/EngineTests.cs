using Fire.Core;
using static Fire.Tests.TestHelpers;

namespace Fire.Tests;

[TestClass]
public class EngineTests
{
    // ---- End-to-end renaming and adapt-case ----

    [TestMethod]
    public void Base_Case_Renames_Files_Directories_And_Contents()
    {
        var path = CreateTestProject(nameof(Base_Case_Renames_Files_Directories_And_Contents),
            "BionicBeaver.sln",
            "BionicBeaver/Something/BionicBeaver/.keep",
            "BionicBeaver/Build.cs",
            "BionicBeaver/BionicBeaver.csproj",
            "BionicBeaver/Constants.cs",
            "BionicBeaver.Cli/BionicBeaver.Cli.csproj",
            "BionicBeaver.Cli/Program.cs");

        var exitCode = Run(path, new Options { FindString = "BionicBeaver", ReplaceString = "DiscoDingo", AdaptCase = true });

        Assert.AreEqual(0, exitCode);
        AssertFiles(path,
        [
            "DiscoDingo.sln",
            "DiscoDingo/Something/DiscoDingo/.keep",
            "DiscoDingo/Build.cs",
            "DiscoDingo/DiscoDingo.csproj",
            "DiscoDingo/Constants.cs",
            "DiscoDingo.Cli/DiscoDingo.Cli.csproj",
            "DiscoDingo.Cli/Program.cs",
        ]);
    }

    [TestMethod]
    public void Kebab_Case_Names_Are_Matched_And_Adapted()
    {
        var path = CreateTestProject(nameof(Kebab_Case_Names_Are_Matched_And_Adapted),
            "BionicBeaver/Files/bionic-beaver.pdf");

        Run(path, new Options { FindString = "BionicBeaver", ReplaceString = "DiscoDingo", AdaptCase = true });

        AssertFiles(path, ["DiscoDingo/Files/disco-dingo.pdf"]);
    }

    [TestMethod]
    public void Whole_Word_Only_Renames_Exact_Words()
    {
        var path = CreateTestProject(nameof(Whole_Word_Only_Renames_Exact_Words),
            "SpeechService.Host/SpeechService.cs",     // whole-word match in both directory and file names
            "Services/GreatSpeechService.cs");         // partial match: must not be renamed

        Run(path, new Options { FindString = "SpeechService", ReplaceString = "TextToSpeechService", WholeWord = true });

        AssertFiles(path,
        [
            "TextToSpeechService.Host/TextToSpeechService.cs",
            "Services/GreatSpeechService.cs",
        ]);
    }

    // ---- Safety: rename collisions must never lose data ----

    [TestMethod]
    public void Directory_Collision_Is_Skipped_Without_Data_Loss()
    {
        var path = CreateTestProject(nameof(Directory_Collision_Is_Skipped_Without_Data_Loss));
        CreateFile(path, "Old/a.txt", "keep-a");
        CreateFile(path, "New/b.txt", "keep-b");

        Run(path, new Options { FindString = "Old", ReplaceString = "New" }, out var output);

        Assert.AreEqual("keep-a", ReadFile(path, "Old/a.txt"));
        Assert.AreEqual("keep-b", ReadFile(path, "New/b.txt"));
        StringAssert.Contains(output, "target already exists");
    }

    [TestMethod]
    public void File_Collision_Is_Skipped_Without_Data_Loss()
    {
        var path = CreateTestProject(nameof(File_Collision_Is_Skipped_Without_Data_Loss));
        CreateFile(path, "OldName.txt", "original");
        CreateFile(path, "NewName.txt", "other");

        Run(path, new Options { FindString = "OldName", ReplaceString = "NewName" }, out var output);

        Assert.AreEqual("original", ReadFile(path, "OldName.txt"));
        Assert.AreEqual("other", ReadFile(path, "NewName.txt"));
        StringAssert.Contains(output, "target already exists");
    }

    [TestMethod]
    public void Case_Only_Rename_Works_On_Windows()
    {
        var path = CreateTestProject(nameof(Case_Only_Rename_Works_On_Windows),
            "fire/readme.txt");

        Run(path, new Options { FindString = "fire", ReplaceString = "FIRE" });

        AssertFiles(path, ["FIRE/readme.txt"]);
        var actualDirName = Path.GetFileName(Directory.GetDirectories(path).Single());
        Assert.AreEqual("FIRE", actualDirName);
    }

    // ---- Whole-word applies to every replacement inside a file ----

    [TestMethod]
    public void Whole_Word_Applies_To_Content_Not_Just_Search()
    {
        var path = CreateTestProject(nameof(Whole_Word_Applies_To_Content_Not_Just_Search));
        // The file is processed because it contains a whole-word occurrence, but
        // the partial occurrences inside it must still be left alone.
        CreateFile(path, "a.txt", "SpeechService GreatSpeechService SpeechServiceX");

        Run(path, new Options { FindString = "SpeechService", ReplaceString = "TTS", WholeWord = true });

        Assert.AreEqual("TTS GreatSpeechService SpeechServiceX", ReadFile(path, "a.txt"));
    }

    // ---- .gitignore handling ----

    [TestMethod]
    public void GitIgnored_Files_Are_Not_Touched_By_Default()
    {
        var path = CreateTestProject(nameof(GitIgnored_Files_Are_Not_Touched_By_Default));
        CreateFile(path, ".gitignore", "bin/\n");
        CreateFile(path, "bin/Old.txt", "Old");
        CreateFile(path, "src/Old.txt", "Old");

        Run(path, new Options { FindString = "Old", ReplaceString = "New" });

        Assert.AreEqual("Old", ReadFile(path, "bin/Old.txt"));
        Assert.AreEqual("New", ReadFile(path, "src/New.txt"));
        Assert.IsFalse(File.Exists(Path.Combine(path, "src", "Old.txt")));
    }

    [TestMethod]
    public void NoIgnore_Flag_Processes_Ignored_Files()
    {
        var path = CreateTestProject(nameof(NoIgnore_Flag_Processes_Ignored_Files));
        CreateFile(path, ".gitignore", "bin/\n");
        CreateFile(path, "bin/Old.txt", "Old");

        Run(path, new Options { FindString = "Old", ReplaceString = "New", NoIgnore = true });

        Assert.AreEqual("New", ReadFile(path, "bin/New.txt"));
    }

    [TestMethod]
    public void Nested_GitIgnore_Files_Are_Honoured()
    {
        var path = CreateTestProject(nameof(Nested_GitIgnore_Files_Are_Honoured));
        CreateFile(path, "sub/.gitignore", "*.log\n");
        CreateFile(path, "sub/app-Old.log", "Old");
        CreateFile(path, "sub/app-Old.txt", "Old");
        CreateFile(path, "root-Old.log", "Old"); // only ignored under sub/

        Run(path, new Options { FindString = "Old", ReplaceString = "New" });

        Assert.AreEqual("Old", ReadFile(path, "sub/app-Old.log"));
        Assert.AreEqual("New", ReadFile(path, "sub/app-New.txt"));
        Assert.AreEqual("New", ReadFile(path, "root-New.log"));
    }

    [TestMethod]
    public void Git_Directory_Is_Never_Touched()
    {
        var path = CreateTestProject(nameof(Git_Directory_Is_Never_Touched));
        CreateFile(path, ".git/config", "Old config");
        CreateFile(path, "Old.txt", "Old");

        Run(path, new Options { FindString = "Old", ReplaceString = "New", NoIgnore = true });

        Assert.AreEqual("Old config", ReadFile(path, ".git/config"));
        Assert.AreEqual("New", ReadFile(path, "New.txt"));
    }

    // ---- Binary and encoding safety ----

    [TestMethod]
    public void Binary_Files_Are_Skipped()
    {
        var path = CreateTestProject(nameof(Binary_Files_Are_Skipped));
        byte[] binary = [0x4F, 0x6C, 0x64, 0x00, 0x01, 0xFF]; // contains "Old" and NULs
        File.WriteAllBytes(Path.Combine(path, "data.bin"), binary);

        Run(path, new Options { FindString = "Old", ReplaceString = "New" });

        CollectionAssert.AreEqual(binary, ReadBytes(path, "data.bin"));
    }

    [TestMethod]
    public void Line_Endings_And_Encoding_Are_Preserved()
    {
        var path = CreateTestProject(nameof(Line_Endings_And_Encoding_Are_Preserved));
        File.WriteAllBytes(Path.Combine(path, "mixed.txt"), "Old\r\nOld\nOld"u8.ToArray());

        Run(path, new Options { FindString = "Old", ReplaceString = "New" });

        CollectionAssert.AreEqual("New\r\nNew\nNew"u8.ToArray(), ReadBytes(path, "mixed.txt"));
    }

    // ---- Options behavior ----

    [TestMethod]
    public void Regex_Mode_Supports_Groups_In_Contents_And_Names()
    {
        var path = CreateTestProject(nameof(Regex_Mode_Supports_Groups_In_Contents_And_Names));
        CreateFile(path, "gray.txt", "gray grey");

        Run(path, new Options { FindString = "gr([ae])y", ReplaceString = "r$1d", Regex = true });

        Assert.AreEqual("rad red", ReadFile(path, "rad.txt"));
    }

    [TestMethod]
    public void Preview_Changes_Nothing_On_Disk()
    {
        var path = CreateTestProject(nameof(Preview_Changes_Nothing_On_Disk),
            "BionicBeaver/BionicBeaver.csproj");
        var before = ListFiles(path);

        Run(path, new Options { FindString = "BionicBeaver", ReplaceString = "DiscoDingo", Preview = true }, out var output);

        CollectionAssert.AreEqual(before, ListFiles(path));
        Assert.AreEqual("BionicBeaver/BionicBeaver.csproj", ReadFile(path, "BionicBeaver/BionicBeaver.csproj"));
        StringAssert.Contains(output, "[preview] Would replace 2 occurrence(s) in BionicBeaver/BionicBeaver.csproj");
        StringAssert.Contains(output, "[preview] Would rename BionicBeaver/BionicBeaver.csproj to BionicBeaver/DiscoDingo.csproj");
        StringAssert.Contains(output, "[preview] Would rename BionicBeaver to DiscoDingo");
    }

    [TestMethod]
    public void File_Pattern_Restricts_Scope_And_Disables_Directory_Renames()
    {
        var path = CreateTestProject(nameof(File_Pattern_Restricts_Scope_And_Disables_Directory_Renames),
            "OldDir/Old.cs",
            "OldDir/Old.txt");

        Run(path, new Options { FindString = "Old", ReplaceString = "New", FilePattern = "*.cs" });

        AssertFiles(path,
        [
            "OldDir/New.cs",   // renamed and content replaced ("OldDir/Old.cs" -> "NewDir/New.cs"... see below)
            "OldDir/Old.txt",  // untouched: does not match the pattern
        ], checkContents: false);

        // Content replacement is not segment-aware, so the directory part of the
        // conventional content changes even though the directory is not renamed.
        Assert.AreEqual("NewDir/New.cs", ReadFile(path, "OldDir/New.cs"));
        Assert.AreEqual("OldDir/Old.txt", ReadFile(path, "OldDir/Old.txt"));
    }

    [TestMethod]
    public void No_Filenames_Flag_Only_Changes_Contents()
    {
        var path = CreateTestProject(nameof(No_Filenames_Flag_Only_Changes_Contents),
            "Old/Old.txt");

        Run(path, new Options { FindString = "Old", ReplaceString = "New", NoFilenames = true });

        AssertFiles(path, ["Old/Old.txt"], checkContents: false);
        Assert.AreEqual("New/New.txt", ReadFile(path, "Old/Old.txt"));
    }

    [TestMethod]
    public void Identical_Find_And_Replace_Is_A_NoOp()
    {
        var path = CreateTestProject(nameof(Identical_Find_And_Replace_Is_A_NoOp), "Same.txt");

        var exitCode = Run(path, new Options { FindString = "Same", ReplaceString = "Same" }, out var output);

        Assert.AreEqual(0, exitCode);
        StringAssert.Contains(output, "Nothing to do");
        AssertFiles(path, ["Same.txt"]);
    }

    [TestMethod]
    public void Identical_Find_And_Replace_With_IgnoreCase_Normalizes_Case()
    {
        var path = CreateTestProject(nameof(Identical_Find_And_Replace_With_IgnoreCase_Normalizes_Case));
        CreateFile(path, "a.txt", "FOO foo Foo");

        Run(path, new Options { FindString = "Foo", ReplaceString = "Foo", IgnoreCase = true });

        Assert.AreEqual("Foo Foo Foo", ReadFile(path, "a.txt"));
    }

    [TestMethod]
    public void Regex_And_AdaptCase_Are_Rejected()
    {
        var path = CreateTestProject(nameof(Regex_And_AdaptCase_Are_Rejected), "a.txt");

        var exitCode = Run(path, new Options { FindString = "a", ReplaceString = "b", Regex = true, AdaptCase = true }, out var output);

        Assert.AreEqual(1, exitCode);
        StringAssert.Contains(output, "incompatible");
    }

    [TestMethod]
    public void Invalid_Regex_Fails_Cleanly()
    {
        var path = CreateTestProject(nameof(Invalid_Regex_Fails_Cleanly), "a.txt");

        var exitCode = Run(path, new Options { FindString = "(", ReplaceString = "b", Regex = true }, out var output);

        Assert.AreEqual(1, exitCode);
        StringAssert.Contains(output, "invalid regular expression");
    }

    [TestMethod]
    public void Empty_Find_String_Is_Rejected()
    {
        var path = CreateTestProject(nameof(Empty_Find_String_Is_Rejected), "a.txt");

        var exitCode = Run(path, new Options { FindString = "", ReplaceString = "b" });

        Assert.AreEqual(1, exitCode);
    }

    [TestMethod]
    public void Special_Regex_Characters_In_Literal_Find_String()
    {
        var path = CreateTestProject(nameof(Special_Regex_Characters_In_Literal_Find_String));
        CreateFile(path, "view.cshtml", "<small>(@Model.Count())</small>");

        Run(path, new Options
        {
            FindString = "<small>(@Model.Count())</small>",
            ReplaceString = "<small>(@Model.Count().ToString(\"N0\"))</small>",
        });

        Assert.AreEqual("<small>(@Model.Count().ToString(\"N0\"))</small>", ReadFile(path, "view.cshtml"));
    }

    [TestMethod]
    public void Deeply_Nested_Matching_Directories_All_Rename()
    {
        var path = CreateTestProject(nameof(Deeply_Nested_Matching_Directories_All_Rename),
            "Old/Old/Old/Old.txt");

        Run(path, new Options { FindString = "Old", ReplaceString = "New" });

        AssertFiles(path, ["New/New/New/New.txt"]);
    }
}
