using Core.DirFiles;
using FluentAssertions;
using Xunit;

namespace Tests.Commands.ConsoleSystem;

public partial class TermXTEditorSyntaxTests
{
    [Theory]
    [InlineData("ma$$", "match", "keyword")]
    [InlineData("pri$$", "println!", "macro")]
    [InlineData("let value: St$$", "String", "type")]
    [InlineData("use std::co$$", "collections", "module")]
    [InlineData("use std::collections::$$", "HashMap", "type")]
    [InlineData("std::fs::read_$$", "read_to_string", "function")]
    [InlineData("String::$$", "new", "function")]
    [InlineData("std::vec::Vec::wi$$", "with_capacity", "function")]
    [InlineData("Vec::<String>::ne$$", "new", "function")]
    [InlineData("fn greet(name: &str) {}\nfn main() { gr$$ }", "greet", "function")]
    [InlineData("let user_name = 1;\nus$$", "user_name", "variable")]
    [InlineData("macro_rules! greeting { () => {} }\ngr$$", "greeting!", "macro")]
    [InlineData("struct Account {}\nAc$$", "Account", "type")]
    public void RustCompletion_AutomaticSuggestions(string text, string label, string kind)
    {
        var editor = RustEditorAtMarker(text);
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == label && item.Kind == kind);
        InvokePrivate<bool>(editor, "IsCSharpCompletionContext").Should().BeFalse();
    }

    [Theory]
    [InlineData("let mut names = Vec::new();\nnames.$$")]
    [InlineData("let mut names: Vec<String> = Vec::new();\nnames.pu$$")]
    [InlineData("let names = vec![1, 2];\nnames.$$")]
    [InlineData("fn add(names: &mut Vec<String>) { names.$$ }")]
    [InlineData("let names = std::vec::Vec::new();\nnames.$$")]
    [InlineData("let names = Vec::<String>::new();\nnames.$$")]
    [InlineData("let names = Vec::new(); { let names = String::new(); }\nnames.$$")]
    public void RustCompletion_VectorReceiverSuggestsMethods(string text)
    {
        var editor = RustEditorAtMarker(text);
        InvokePrivate(editor, "RefreshCompletionAfterText", ".");
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "push" && item.Kind == "method");
        ActiveCSharpCompletions(editor).Should().NotContain(item => item.Label == "println!" || item.Label == "new");
    }

    [Theory]
    [InlineData("let text = String::from(\"hello\");\ntext.pu$$", "push_str")]
    [InlineData("let text = \"hello\".to_owned();\ntext.pu$$", "push_str")]
    [InlineData("let text = \"hello\";\ntext.tr$$", "trim")]
    [InlineData("\"hello\".tr$$", "trim")]
    [InlineData("let result: Result<i32, String> = Ok(1);\nresult.ma$$", "map_err")]
    [InlineData("let value = Some(1);\nvalue.un$$", "unwrap")]
    [InlineData("let values = HashMap::new();\nvalues.in$$", "insert")]
    [InlineData("struct User { name: String, age: u32 }\nlet user = User { name: String::new(), age: 2 };\nuser.na$$", "name")]
    [InlineData("struct User {}\nimpl User { fn greet(&self) {} fn new() -> Self { Self {} } }\nlet user: User;\nuser.gr$$", "greet")]
    [InlineData("struct User {}\nimpl User { fn new() -> Self { Self {} } }\nUser::ne$$", "new")]
    [InlineData("struct User<T> { value: T }\nimpl<T> User<T> { fn value(&self) -> &T { &self.value } }\nlet user: User<u32>;\nuser.va$$", "value")]
    [InlineData("enum Color { Red, Green, Blue }\nColor::Gr$$", "Green")]
    [InlineData("mod helpers { pub fn greet() {} }\nhelpers::gr$$", "greet")]
    [InlineData("struct User { name: String }\nimpl User { fn greet(&self) { self.na$$ } }", "name")]
    [InlineData("struct User {}\nimpl User { fn greet(&self) { self.gr$$ } }", "greet")]
    [InlineData("struct User {}\nimpl User { fn new() -> Self { Self {} } fn create() { Self::ne$$ } }", "new")]
    public void RustCompletion_SuggestsKnownMembers(string text, string label)
    {
        var editor = RustEditorAtMarker(text);
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == label);
    }

    [Theory]
    [InlineData("p$$")]
    [InlineData("// pri$$")]
    [InlineData("/* comment\npri$$\n*/")]
    [InlineData("/* outer /* nested */\npri$$\n*/")]
    [InlineData("let text = \"pri$$\";")]
    [InlineData("let text = \"start\npri$$\";")]
    [InlineData("let text = \"escaped \\\" pri$$\";")]
    [InlineData("let text = r##\"first \"#\npri$$\"##;")]
    [InlineData("let text = br#\"pri$$\"#;")]
    [InlineData("let text = cr#\"pri$$\"#;")]
    [InlineData("let text = b\"pri$$\";")]
    [InlineData("let ch = 'a$$';")]
    [InlineData("let ch = '\\x6$$1';")]
    [InlineData("fn main() { let pri$$ }")]
    [InlineData("let mut pri$$")]
    [InlineData("fn pri$$")]
    [InlineData("struct Str$$")]
    [InlineData("fn f<'pri$$>() {}")]
    [InlineData("let x = 12u$$")]
    [InlineData("unknown.$$ ")]
    public void RustCompletion_SuppressesUnrelatedContexts(string text)
    {
        var editor = RustEditorAtMarker(text);
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [Theory]
    [InlineData("/* outer /* inner */ end */ pri$$")]
    [InlineData("let text = r##\"hello\nworld\"##; pri$$")]
    [InlineData("fn greet<'a>(text: &'a str) { pri$$ }")]
    public void RustCompletion_ResumesAfterCommentsAndStrings(string text)
    {
        var editor = RustEditorAtMarker(text);
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "println!");
    }

    [Fact]
    public void RustCompletion_IgnoresNamesInCommentsAndStrings()
    {
        var editor = RustEditorAtMarker("// fn hidden() {}\nlet text = r#\"hidden_name\"#;\n/* hidden_field */\nhi$$");
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [Theory]
    [InlineData("pri$$", "println!", "println!")]
    [InlineData("pri$$ntln!(\"hello\");", "println!", "println!(\"hello\");")]
    [InlineData("String::ne$$w()", "new", "String::new()")]
    [InlineData("let r#type = 1;\nr#ty$$pe + 2", "r#type", "let r#type = 1;\nr#type + 2")]
    public void RustCompletion_CommitReplacesTokenAndSupportsUndo(string text, string label, string expected)
    {
        var editor = RustEditorAtMarker(text);
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        int selected = ActiveCSharpCompletions(editor).FindIndex(item => item.Label == label);
        selected.Should().BeGreaterThanOrEqualTo(0);
        SetPrivateField(editor, "_completionSelectedIndex", selected);
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        string.Join("\n", Lines(editor)).Should().Be(expected);
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
        InvokePrivate(editor, "Undo");
        string.Join("\n", Lines(editor)).Should().Be(text.Replace("$$", ""));
        InvokePrivate(editor, "Redo");
        string.Join("\n", Lines(editor)).Should().Be(expected);
    }

    [Fact]
    public void RustCompletion_KeyboardFilteringAndManualCompletion()
    {
        var editor = RustEditorAtMarker("$$");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('p', ConsoleKey.P, false, false, false));
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('r', ConsoleKey.R, false, false, false));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "println!");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "pub");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('r', ConsoleKey.R, false, false, false));
        ActiveCSharpCompletions(editor).Should().NotContain(item => item.Label == "pub");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "pub");
        int previousSelection = GetPrivateField<int>(editor, "_completionSelectedIndex");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        GetPrivateField<int>(editor, "_completionSelectedIndex").Should().Be(previousSelection + 1);
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\x1b', ConsoleKey.Escape, false, false, false));
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
        GetPrivateField<object>(editor, "_mode").ToString().Should().Be("Insert");
    }

    [Fact]
    public void RustCompletion_SyntaxSwitchDismissesPopup()
    {
        var editor = RustEditorAtMarker("pr$$");
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        InvokePrivate(editor, "ExecuteEditorCommand", "syntax py");
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [Fact]
    public void RustCompletion_CtrlSpaceOnEmptyBufferAndInComment()
    {
        var editor = RustEditorAtMarker("$$");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "fn");

        editor = RustEditorAtMarker("/* $$ */");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\0', ConsoleKey.Spacebar, false, false, true));
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [Fact]
    public void RustCompletion_ParametersFromOtherFunctionsDoNotDetermineReceiverType()
    {
        var editor = RustEditorAtMarker("fn other(names: Vec<String>) {}\nfn main() { names.$$ }");
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        GetPrivateField<bool>(editor, "_completionActive").Should().BeFalse();
    }

    [Fact]
    public void RustCompletion_PopupShowsFunctionSignatureAndEnterAccepts()
    {
        var editor = RustEditorAtMarker("fn greet(name: &str) -> String { name.to_owned() }\ngr$$");
        InvokePrivate(editor, "RefreshCompletionAfterEdit");
        var frame = GetPrivateField<System.Text.StringBuilder>(editor, "_frame");
        frame.Clear();
        InvokePrivate(editor, "RenderCompletionPopup", 2, 5, 20, 100, 105);
        frame.ToString().Should().Contain("greet").And.Contain("name: &str");
        InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
        Lines(editor).Should().HaveCount(2);
        Lines(editor)[1].Should().Be("greet");
    }

    [Fact]
    public void RustCompletion_DetectsRsExtensionAndTypingPathOpensPopup()
    {
        var editor = new TermXTEditor(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".rs"));
        GetPrivateField<TermXTEditorSyntax>(editor, "_syntax").Should().Be(TermXTEditorSyntax.Rust);
        SetPrivateEnumField(editor, "_mode", "Insert");
        foreach (char c in "String::")
            InvokePrivate(editor, "HandleKey", new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false));
        ActiveCSharpCompletions(editor).Should().Contain(item => item.Label == "new");
    }

    private static TermXTEditor RustEditorAtMarker(string text)
    {
        var editor = TermXtEditorAtMarker(text);
        SetPrivateField(editor, "_syntax", TermXTEditorSyntax.Rust);
        return editor;
    }
}
