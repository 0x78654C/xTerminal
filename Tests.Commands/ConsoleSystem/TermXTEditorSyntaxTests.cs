using System.Collections;
using System.Reflection;
using System.Runtime.Versioning;
using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

[SupportedOSPlatform("Windows")]
public class TermXTEditorSyntaxTests
{
    [Theory]
    [InlineData("main.rs")]
    [InlineData("MAIN.RS")]
    public void DetectSyntaxFromPath_RustFiles_ReturnsRust(string path)
    {
        TermXTEditor.DetectSyntaxFromPath(path).Should().Be(TermXTEditorSyntax.Rust);
    }

    [Theory]
    [InlineData("rust")]
    [InlineData("rs")]
    public void TryParseSyntax_RustAliases_ReturnsRust(string value)
    {
        TermXTEditor.TryParseSyntax(value, out TermXTEditorSyntax syntax).Should().BeTrue();
        syntax.Should().Be(TermXTEditorSyntax.Rust);
        TermXTEditor.SyntaxDisplayName(syntax).Should().Be("Rust");
    }

    [Fact]
    public void TokenizeRust_HighlightsCommonRustTokens()
    {
        const string line = @"pub fn main<'a>() { println!(r#""hi""#); let x: i32 = 42; } // comment";
        List<TokenInfo> tokens = TokenizeRust(line, 0);

        TokenForText(tokens, line, "pub").Color.Should().Be(Color("CRustModifier"));
        TokenForText(tokens, line, "fn").Color.Should().Be(Color("CRustDeclaration"));
        TokenForText(tokens, line, "'a").Color.Should().Be(Color("CRustLifetime"));
        TokenForText(tokens, line, "println!").Color.Should().Be(Color("CRustMacro"));
        TokenForText(tokens, line, @"r#""hi""#").Color.Should().Be(Color("CRustString"));
        TokenForText(tokens, line, "let").Color.Should().Be(Color("CRustKeyword"));
        TokenForText(tokens, line, "i32").Color.Should().Be(Color("CRustType"));
        TokenForText(tokens, line, "42").Color.Should().Be(Color("CRustNumber"));
        TokenForText(tokens, line, "// comment").Color.Should().Be(Color("CRustComment"));
    }

    [Fact]
    public void TokenizeRust_ContinuesNestedBlockCommentState()
    {
        const string line = "still /* nested */ commented */ let value = true";
        List<TokenInfo> tokens = TokenizeRust(line, 1);

        tokens[0].Text(line).Should().Be("still /* nested */ commented */");
        tokens[0].Color.Should().Be(Color("CRustComment"));
        TokenForText(tokens, line, "let").Color.Should().Be(Color("CRustKeyword"));
        TokenForText(tokens, line, "true").Color.Should().Be(Color("CRustNumber"));
    }

    [Fact]
    public void TokenizeRust_LeavesRangeDotsAsOperators()
    {
        const string line = "for i in 0..10 { }";
        List<TokenInfo> tokens = TokenizeRust(line, 0);

        TokenForText(tokens, line, "0").Color.Should().Be(Color("CRustNumber"));
        TokenForText(tokens, line, "10").Color.Should().Be(Color("CRustNumber"));

        List<TokenInfo> dots = tokens.Where(token => token.Text(line) == ".").ToList();
        dots.Should().HaveCount(2);
        dots.Should().OnlyContain(token => token.Color == Color("CRustOperator"));
    }

    [Fact]
    public void InsertTextWithoutUndo_MultilinePaste_PreservesPastedIndentation()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            File.WriteAllText(path, "    start");
            var editor = new TermXTEditor(path, TermXTEditorSyntax.CSharp);
            SetPrivateField(editor, "_cursorLine", 0);
            SetPrivateField(editor, "_cursorCol", "    start".Length);

            InvokePrivate(editor, "InsertTextWithoutUndo", "\n        nested\n    done");

            Lines(editor).Should().Equal(
                "    start",
                "        nested",
                "    done");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void TryGetQueuedPasteTextFragment_PasteKeys_ReturnsExactText()
    {
        PasteFragment(new ConsoleKeyInfo('\r', ConsoleKey.Enter, shift: false, alt: false, control: false))
            .Should().Be("\n");
        PasteFragment(new ConsoleKeyInfo('\t', ConsoleKey.Tab, shift: false, alt: false, control: false))
            .Should().Be("\t");
        PasteFragment(new ConsoleKeyInfo('x', ConsoleKey.X, shift: false, alt: false, control: false))
            .Should().Be("x");
    }

    private static List<TokenInfo> TokenizeRust(string line, int blockCommentDepth)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TokenizeRust",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (IEnumerable)method.Invoke(null, new object[] { line, blockCommentDepth })!;
        var tokens = new List<TokenInfo>();
        foreach (object token in result)
        {
            Type tokenType = token.GetType();
            tokens.Add(new TokenInfo(
                (int)tokenType.GetProperty("Start")!.GetValue(token)!,
                (int)tokenType.GetProperty("Length")!.GetValue(token)!,
                (int)tokenType.GetProperty("Color")!.GetValue(token)!));
        }

        return tokens;
    }

    private static string PasteFragment(ConsoleKeyInfo key)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TryGetQueuedPasteTextFragment",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        object?[] arguments = { key, null };
        ((bool)method.Invoke(null, arguments)!).Should().BeTrue();
        return (string)arguments[1]!;
    }

    private static void InvokePrivate(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        method.Invoke(target, arguments);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        field.SetValue(target, value);
    }

    private static List<string> Lines(TermXTEditor editor)
    {
        FieldInfo field = typeof(TermXTEditor).GetField(
            "_lines",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        return (List<string>)field.GetValue(editor)!;
    }

    private static TokenInfo TokenForText(List<TokenInfo> tokens, string line, string text)
    {
        return tokens.Single(token => token.Text(line) == text);
    }

    private static int Color(string name)
    {
        FieldInfo field = typeof(TermXTEditor).GetField(
            name,
            BindingFlags.NonPublic | BindingFlags.Static)!;

        return (int)field.GetRawConstantValue()!;
    }

    private readonly struct TokenInfo
    {
        public TokenInfo(int start, int length, int color)
        {
            Start = start;
            Length = length;
            Color = color;
        }

        public int Start { get; }
        public int Length { get; }
        public int Color { get; }

        public string Text(string line)
        {
            return line.Substring(Start, Length);
        }
    }
}
