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
    [InlineData("app.js")]
    [InlineData("module.MJS")]
    [InlineData("common.cjs")]
    [InlineData("component.jsx")]
    public void DetectSyntaxFromPath_JavaScriptFiles_ReturnsJavaScript(string path)
    {
        TermXTEditor.DetectSyntaxFromPath(path).Should().Be(TermXTEditorSyntax.JavaScript);
    }

    [Theory]
    [InlineData("script.py")]
    [InlineData("windowed.PYW")]
    [InlineData("types.pyi")]
    public void DetectSyntaxFromPath_PythonFiles_ReturnsPython(string path)
    {
        TermXTEditor.DetectSyntaxFromPath(path).Should().Be(TermXTEditorSyntax.Python);
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

    [Theory]
    [InlineData("javascript")]
    [InlineData("js")]
    public void TryParseSyntax_JavaScriptAliases_ReturnsJavaScript(string value)
    {
        TermXTEditor.TryParseSyntax(value, out TermXTEditorSyntax syntax).Should().BeTrue();
        syntax.Should().Be(TermXTEditorSyntax.JavaScript);
        TermXTEditor.SyntaxDisplayName(syntax).Should().Be("JavaScript");
    }

    [Theory]
    [InlineData("python")]
    [InlineData("py")]
    public void TryParseSyntax_PythonAliases_ReturnsPython(string value)
    {
        TermXTEditor.TryParseSyntax(value, out TermXTEditorSyntax syntax).Should().BeTrue();
        syntax.Should().Be(TermXTEditorSyntax.Python);
        TermXTEditor.SyntaxDisplayName(syntax).Should().Be("Python");
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
    public void TokenizeJavaScript_HighlightsCommonJavaScriptTokens()
    {
        const string line = "const rx = /[a-z]+/gi; async function main() { console.log(`hi ${name}`); return null; } // note";
        List<TokenInfo> tokens = TokenizeJavaScript(line, false);

        TokenForText(tokens, line, "const").Color.Should().Be(Color("CJavaScriptDeclaration"));
        TokenForText(tokens, line, "/[a-z]+/gi").Color.Should().Be(Color("CJavaScriptRegex"));
        TokenForText(tokens, line, "async").Color.Should().Be(Color("CJavaScriptKeyword"));
        TokenForText(tokens, line, "function").Color.Should().Be(Color("CJavaScriptDeclaration"));
        TokenForText(tokens, line, "console").Color.Should().Be(Color("CJavaScriptBuiltin"));
        TokenForText(tokens, line, "`hi ${name}`").Color.Should().Be(Color("CJavaScriptString"));
        TokenForText(tokens, line, "return").Color.Should().Be(Color("CJavaScriptFlow"));
        TokenForText(tokens, line, "null").Color.Should().Be(Color("CJavaScriptNumber"));
        TokenForText(tokens, line, "// note").Color.Should().Be(Color("CJavaScriptComment"));
    }

    [Fact]
    public void TokenizeJavaScript_ContinuesBlockCommentState()
    {
        const string line = "still commented */ const value = true";
        List<TokenInfo> tokens = TokenizeJavaScript(line, true);

        tokens[0].Text(line).Should().Be("still commented */");
        tokens[0].Color.Should().Be(Color("CJavaScriptComment"));
        TokenForText(tokens, line, "const").Color.Should().Be(Color("CJavaScriptDeclaration"));
        TokenForText(tokens, line, "true").Color.Should().Be(Color("CJavaScriptNumber"));
    }

    [Fact]
    public void TokenizePython_HighlightsCommonPythonTokens()
    {
        const string line = @"async def main(name): print(f""Hello {name}""); return None # note";
        List<TokenInfo> tokens = TokenizePython(line, 0);

        TokenForText(tokens, line, "async").Color.Should().Be(Color("CPythonKeyword"));
        TokenForText(tokens, line, "def").Color.Should().Be(Color("CPythonDeclaration"));
        TokenForText(tokens, line, "print").Color.Should().Be(Color("CPythonBuiltin"));
        TokenForText(tokens, line, @"f""Hello {name}""").Color.Should().Be(Color("CPythonString"));
        TokenForText(tokens, line, "return").Color.Should().Be(Color("CPythonFlow"));
        TokenForText(tokens, line, "None").Color.Should().Be(Color("CPythonNumber"));
        TokenForText(tokens, line, "# note").Color.Should().Be(Color("CPythonComment"));
    }

    [Fact]
    public void TokenizePython_HighlightsDecoratorsAndContinuesTripleQuotedStringState()
    {
        const string decorator = "@click.command()";
        List<TokenInfo> decoratorTokens = TokenizePython(decorator, 0);
        TokenForText(decoratorTokens, decorator, "@click.command").Color.Should().Be(Color("CPythonDecorator"));

        const string line = "still string''' value = 42";
        List<TokenInfo> tokens = TokenizePython(line, '\'');

        tokens[0].Text(line).Should().Be("still string'''");
        tokens[0].Color.Should().Be(Color("CPythonString"));
        TokenForText(tokens, line, "42").Color.Should().Be(Color("CPythonNumber"));
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

    private static List<TokenInfo> TokenizeJavaScript(string line, bool startsInBlockComment)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TokenizeJavaScript",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (IEnumerable)method.Invoke(null, new object[] { line, startsInBlockComment })!;
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

    private static List<TokenInfo> TokenizePython(string line, int multilineStringQuote)
    {
        MethodInfo method = typeof(TermXTEditor).GetMethod(
            "TokenizePython",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (IEnumerable)method.Invoke(null, new object[] { line, multilineStringQuote })!;
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
