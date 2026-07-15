using Fire.Core;

namespace Fire.Tests;

[TestClass]
public class TokenizerTests
{
    [TestMethod]
    [DataRow("BionicBeaver", "bionic beaver")]
    [DataRow("bionicBeaver", "bionic beaver")]
    [DataRow("bionic-beaver", "bionic beaver")]
    [DataRow("BIONIC_BEAVER", "bionic beaver")]
    [DataRow("bionicbeaver", "bionicbeaver")]
    [DataRow("XMLHttpRequest", "xml http request")]
    [DataRow("utf8String", "utf8 string")]
    [DataRow("beaver", "beaver")]
    [DataRow("Some-Mixed_styleHere", "some mixed style here")]
    public void Tokenize_Splits_Identifiers(string input, string expected)
    {
        var tokens = Tokenizer.Tokenize(input);
        Assert.IsNotNull(tokens);
        Assert.AreEqual(expected, string.Join(" ", tokens));
    }

    [TestMethod]
    [DataRow("<small>")]
    [DataRow("a b")]
    [DataRow("a.b")]
    [DataRow("")]
    [DataRow("--")]
    public void Tokenize_Rejects_Non_Identifiers(string input)
    {
        Assert.IsNull(Tokenizer.Tokenize(input));
    }
}

[TestClass]
public class SmartCaseTests
{
    [TestMethod]
    public void BuildVariantMap_Covers_Common_Styles()
    {
        var map = SmartCase.BuildVariantMap("BionicBeaver", "DiscoDingo");
        Assert.IsNotNull(map);

        Assert.AreEqual("DiscoDingo", map["BionicBeaver"]);
        Assert.AreEqual("discoDingo", map["bionicBeaver"]);
        Assert.AreEqual("discodingo", map["bionicbeaver"]);
        Assert.AreEqual("DISCODINGO", map["BIONICBEAVER"]);
        Assert.AreEqual("disco-dingo", map["bionic-beaver"]);
        Assert.AreEqual("DISCO-DINGO", map["BIONIC-BEAVER"]);
        Assert.AreEqual("Disco-Dingo", map["Bionic-Beaver"]);
        Assert.AreEqual("disco_dingo", map["bionic_beaver"]);
        Assert.AreEqual("DISCO_DINGO", map["BIONIC_BEAVER"]);
        Assert.AreEqual("Disco_Dingo", map["Bionic_Beaver"]);
    }

    [TestMethod]
    public void BuildVariantMap_Verbatim_Strings_Take_Precedence()
    {
        // "WEIRDcase" is not one of the generated styles but must still map verbatim.
        var map = SmartCase.BuildVariantMap("WEIRDcase", "OtherTHING");
        Assert.IsNotNull(map);
        Assert.AreEqual("OtherTHING", map["WEIRDcase"]);
    }

    [TestMethod]
    public void BuildVariantMap_Returns_Null_For_Non_Identifiers()
    {
        Assert.IsNull(SmartCase.BuildVariantMap("<small>", "big"));
        Assert.IsNull(SmartCase.BuildVariantMap("small", "<big>"));
    }

    [TestMethod]
    public void BuildVariantMap_Single_Word()
    {
        var map = SmartCase.BuildVariantMap("speech", "voice");
        Assert.IsNotNull(map);
        Assert.AreEqual("voice", map["speech"]);
        Assert.AreEqual("Voice", map["Speech"]);
        Assert.AreEqual("VOICE", map["SPEECH"]);
    }
}

[TestClass]
public class TextReplacerTests
{
    private static string Replace(Options options, string input, out int count)
    {
        var replacer = TextReplacer.Create(options);
        return replacer.Replace(input, out count);
    }

    [TestMethod]
    public void Literal_Escapes_Regex_Special_Characters()
    {
        var options = new Options { FindString = "a+b(c)", ReplaceString = "x" };
        var result = Replace(options, "a+b(c)d aab", out var count);
        Assert.AreEqual("xd aab", result);
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void Literal_Replacement_Does_Not_Expand_Dollar_Groups()
    {
        var options = new Options { FindString = "x", ReplaceString = "$1" };
        var result = Replace(options, "x", out _);
        Assert.AreEqual("$1", result);
    }

    [TestMethod]
    public void Regex_Supports_Group_Substitution()
    {
        var options = new Options { FindString = "gr([ae])y", ReplaceString = "$1!", Regex = true };
        var result = Replace(options, "gray grey", out var count);
        Assert.AreEqual("a! e!", result);
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void WholeWord_Skips_Partial_Matches()
    {
        var options = new Options { FindString = "SpeechService", ReplaceString = "TTS", WholeWord = true };
        var result = Replace(options, "SpeechService GreatSpeechService SpeechServiceX (SpeechService) foo_SpeechService", out var count);
        Assert.AreEqual("TTS GreatSpeechService SpeechServiceX (TTS) foo_SpeechService", result);
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void WholeWord_Works_At_String_Boundaries()
    {
        var options = new Options { FindString = "cat", ReplaceString = "dog", WholeWord = true };
        var result = Replace(options, "cat", out var count);
        Assert.AreEqual("dog", result);
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void IgnoreCase_Replaces_All_Casings_With_Literal()
    {
        var options = new Options { FindString = "oldtext", ReplaceString = "newtext", IgnoreCase = true };
        var result = Replace(options, "OLDTEXT oldtext OldText", out var count);
        Assert.AreEqual("newtext newtext newtext", result);
        Assert.AreEqual(3, count);
    }

    [TestMethod]
    public void AdaptCase_Replaces_Each_Variant_In_Style()
    {
        var options = new Options { FindString = "BionicBeaver", ReplaceString = "DiscoDingo", AdaptCase = true };
        var input = "bionic_beaver BIONIC_BEAVER bionicBeaver BIONICBEAVER BionicBeaver bionic-beaver";
        var result = Replace(options, input, out var count);
        Assert.AreEqual("disco_dingo DISCO_DINGO discoDingo DISCODINGO DiscoDingo disco-dingo", result);
        Assert.AreEqual(6, count);
    }

    [TestMethod]
    public void AdaptCase_With_WholeWord_Skips_Partial_Matches()
    {
        var options = new Options { FindString = "BionicBeaver", ReplaceString = "DiscoDingo", AdaptCase = true, WholeWord = true };
        var result = Replace(options, "bionic-beaver superbionic-beaver", out var count);
        // '-' is not a word character, so "superbionic-beaver" contains a
        // "bionic-beaver" preceded by a word character and must not match.
        Assert.AreEqual("disco-dingo superbionic-beaver", result);
        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void AdaptCase_Falls_Back_To_IgnoreCase_For_Non_Identifiers()
    {
        var warned = false;
        var options = new Options { FindString = "<small>", ReplaceString = "<big>", AdaptCase = true };
        var replacer = TextReplacer.Create(options, _ => warned = true);
        var result = replacer.Replace("<small> <SMALL>", out var count);
        Assert.AreEqual("<big> <big>", result);
        Assert.AreEqual(2, count);
        Assert.IsTrue(warned);
    }

    [TestMethod]
    public void Empty_Replacement_Deletes_Matches()
    {
        var options = new Options { FindString = "Old", ReplaceString = "" };
        var result = Replace(options, "XOldY", out _);
        Assert.AreEqual("XY", result);
    }

    [TestMethod]
    public void Invalid_Regex_Throws_ArgumentException()
    {
        var options = new Options { FindString = "(", ReplaceString = "x", Regex = true };
        try
        {
            TextReplacer.Create(options);
            Assert.Fail("Expected an ArgumentException.");
        }
        catch (ArgumentException)
        {
            // RegexParseException derives from ArgumentException, which is what FireEngine catches.
        }
    }
}

[TestClass]
public class TextFileTests
{
    private static string WriteTempFile(byte[] bytes)
    {
        Directory.CreateDirectory(TestHelpers.TestRoot);
        var path = Path.Combine(TestHelpers.TestRoot, "codec-" + Guid.NewGuid().ToString("N")[..8]);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    [TestMethod]
    public void Utf8_Bom_Is_Preserved()
    {
        var path = WriteTempFile([0xEF, 0xBB, 0xBF, .. "Old é"u8]);
        var file = TextFile.Read(path);
        Assert.IsFalse(file.IsBinary);
        Assert.AreEqual("Old é", file.Content);

        file.Write(path, "New é");
        var bytes = File.ReadAllBytes(path);
        CollectionAssert.AreEqual((byte[])[0xEF, 0xBB, 0xBF, .. "New é"u8], bytes);
    }

    [TestMethod]
    public void Utf8_Without_Bom_Stays_Without_Bom()
    {
        var path = WriteTempFile("Old é"u8.ToArray());
        var file = TextFile.Read(path);
        Assert.AreEqual("Old é", file.Content);

        file.Write(path, "New é");
        CollectionAssert.AreEqual("New é"u8.ToArray(), File.ReadAllBytes(path));
    }

    [TestMethod]
    public void Latin1_Bytes_Round_Trip()
    {
        // "caf\xE9" is invalid UTF-8, so this must be read as Latin-1 and written back byte-for-byte.
        var path = WriteTempFile([(byte)'O', (byte)'l', (byte)'d', (byte)' ', 0xE9]);
        var file = TextFile.Read(path);
        Assert.IsFalse(file.IsBinary);
        Assert.AreEqual("Old é", file.Content);

        file.Write(path, "New é");
        CollectionAssert.AreEqual((byte[])[(byte)'N', (byte)'e', (byte)'w', (byte)' ', 0xE9], File.ReadAllBytes(path));
    }

    [TestMethod]
    public void Utf16_Le_Bom_Is_Preserved()
    {
        var encoding = new System.Text.UnicodeEncoding(false, true);
        var path = WriteTempFile([.. encoding.GetPreamble(), .. encoding.GetBytes("Old text")]);

        var file = TextFile.Read(path);
        Assert.IsFalse(file.IsBinary);
        Assert.AreEqual("Old text", file.Content);

        file.Write(path, "New text");
        var bytes = File.ReadAllBytes(path);
        Assert.AreEqual(0xFF, bytes[0]);
        Assert.AreEqual(0xFE, bytes[1]);
        Assert.AreEqual("New text", File.ReadAllText(path)); // ReadAllText detects the BOM
    }

    [TestMethod]
    public void Nul_Bytes_Without_Bom_Mean_Binary()
    {
        var path = WriteTempFile([0x4F, 0x6C, 0x64, 0x00, 0x01, 0xFF]);
        var file = TextFile.Read(path);
        Assert.IsTrue(file.IsBinary);
    }

    [TestMethod]
    public void Empty_File_Is_Text()
    {
        var path = WriteTempFile([]);
        var file = TextFile.Read(path);
        Assert.IsFalse(file.IsBinary);
        Assert.AreEqual("", file.Content);
    }
}

[TestClass]
public class GlobFilterTests
{
    [TestMethod]
    public void Bare_Pattern_Matches_At_Any_Depth()
    {
        var filter = new GlobFilter("*.cs");
        Assert.IsTrue(filter.IsMatch("Program.cs"));
        Assert.IsTrue(filter.IsMatch("src/deep/Program.cs"));
        Assert.IsFalse(filter.IsMatch("Program.txt"));
    }

    [TestMethod]
    public void Directory_Pattern_Is_Anchored()
    {
        var filter = new GlobFilter("src/*.cs");
        Assert.IsTrue(filter.IsMatch("src/Program.cs"));
        Assert.IsFalse(filter.IsMatch("other/Program.cs"));
        Assert.IsFalse(filter.IsMatch("src/deep/Program.cs"));
    }

    [TestMethod]
    public void Globstar_Matches_Nested_Directories()
    {
        var filter = new GlobFilter("**/Views/*.cshtml");
        Assert.IsTrue(filter.IsMatch("App/Views/Index.cshtml"));
        Assert.IsTrue(filter.IsMatch("Views/Index.cshtml"));
        Assert.IsFalse(filter.IsMatch("App/Index.cshtml"));
    }
}
