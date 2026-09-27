using System;
using System.Collections.Generic;

namespace Core.DirFiles
{
    public sealed partial class TermXTEditor
    {
        private bool _rustCompletionManual;

        // Immediate fallback while Cargo analysis loads, also usable without a toolchain.
        private static readonly Dictionary<string, string[]> s_rustCompletionCatalog =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["std"] = new[] { "collections", "env", "fmt", "fs", "io", "iter", "mem", "option", "path", "process", "result", "string", "sync", "thread", "time", "vec" },
                ["std::collections"] = new[] { "HashMap", "HashSet", "BTreeMap", "BTreeSet", "VecDeque", "BinaryHeap" },
                ["std::io"] = new[] { "Read", "Write", "BufRead", "BufReader", "BufWriter", "Error", "ErrorKind", "Result", "stdin() -> Stdin", "stdout() -> Stdout", "stderr() -> Stderr" },
                ["std::fs"] = new[] { "File", "OpenOptions", "read(path) -> io::Result<Vec<u8>>", "read_to_string(path) -> io::Result<String>", "write(path, contents) -> io::Result<()>", "read_dir(path) -> io::Result<ReadDir>", "create_dir_all(path) -> io::Result<()>" },
                ["std::env"] = new[] { "args() -> Args", "var(key) -> Result<String, VarError>", "current_dir() -> io::Result<PathBuf>" },
                ["std::fmt"] = new[] { "Debug", "Display", "Formatter", "Result", "Write" },
                ["std::path"] = new[] { "Path", "PathBuf" },
                ["std::option"] = new[] { "Option" },
                ["std::result"] = new[] { "Result" },
                ["std::string"] = new[] { "String", "ToString" },
                ["std::vec"] = new[] { "Vec" },
                ["std::sync"] = new[] { "Arc", "Mutex", "RwLock", "mpsc", "atomic" },
                ["std::thread"] = new[] { "spawn(f) -> JoinHandle<T>", "sleep(duration)", "current() -> Thread" },
                ["std::time"] = new[] { "Duration", "Instant", "SystemTime" },
                ["std::mem"] = new[] { "drop(value)", "replace(dest, src) -> T", "swap(x, y)", "size_of<T>() -> usize", "take(dest) -> T" },
                ["std::iter"] = new[] { "Iterator", "IntoIterator", "empty<T>() -> Empty<T>", "once(value) -> Once<T>" },
                ["std::process"] = new[] { "Command", "ExitCode", "exit(code) -> !", "id() -> u32" },
                ["String::"] = new[] { "new() -> String", "from(value) -> String", "with_capacity(capacity) -> String", "from_utf8(vec) -> Result<String, FromUtf8Error>" },
                ["Vec::"] = new[] { "new() -> Vec<T>", "with_capacity(capacity) -> Vec<T>", "from(value) -> Vec<T>" },
                ["HashMap::"] = new[] { "new() -> HashMap<K, V>", "with_capacity(capacity) -> HashMap<K, V>" },
                ["HashSet::"] = new[] { "new() -> HashSet<T>", "with_capacity(capacity) -> HashSet<T>" },
                ["Option::"] = new[] { "Some(value)", "None" },
                ["Result::"] = new[] { "Ok(value)", "Err(error)" },
                ["Box::"] = new[] { "new(value) -> Box<T>" },
                ["Arc::"] = new[] { "new(value) -> Arc<T>", "clone(value) -> Arc<T>" },
                ["Rc::"] = new[] { "new(value) -> Rc<T>", "clone(value) -> Rc<T>" },
                ["Path::"] = new[] { "new(path) -> &Path" },
                ["PathBuf::"] = new[] { "new() -> PathBuf", "from(path) -> PathBuf" },
                ["String."] = new[] { "len() -> usize", "is_empty() -> bool", "push(ch)", "push_str(string)", "pop() -> Option<char>", "clear()", "capacity() -> usize", "as_str() -> &str", "as_bytes() -> &[u8]", "chars() -> Chars", "trim() -> &str", "split(pattern) -> Split", "contains(pattern) -> bool", "starts_with(pattern) -> bool", "ends_with(pattern) -> bool", "replace(from, to) -> String", "to_lowercase() -> String", "to_uppercase() -> String", "clone() -> String" },
                ["str."] = new[] { "len() -> usize", "is_empty() -> bool", "as_bytes() -> &[u8]", "chars() -> Chars", "lines() -> Lines", "trim() -> &str", "split(pattern) -> Split", "contains(pattern) -> bool", "starts_with(pattern) -> bool", "ends_with(pattern) -> bool", "replace(from, to) -> String", "to_string() -> String", "to_owned() -> String", "parse<F>() -> Result<F, F::Err>" },
                ["Vec."] = new[] { "push(value)", "pop() -> Option<T>", "len() -> usize", "is_empty() -> bool", "capacity() -> usize", "clear()", "insert(index, value)", "remove(index) -> T", "get(index) -> Option<&T>", "first() -> Option<&T>", "last() -> Option<&T>", "iter() -> Iter<T>", "iter_mut() -> IterMut<T>", "into_iter() -> IntoIter<T>", "as_slice() -> &[T]", "sort()", "retain(f)", "extend(iter)" },
                ["HashMap."] = new[] { "insert(key, value) -> Option<V>", "get(key) -> Option<&V>", "get_mut(key) -> Option<&mut V>", "contains_key(key) -> bool", "remove(key) -> Option<V>", "entry(key) -> Entry<K, V>", "keys() -> Keys<K, V>", "values() -> Values<K, V>", "iter() -> Iter<K, V>", "len() -> usize", "is_empty() -> bool", "clear()" },
                ["HashSet."] = new[] { "insert(value) -> bool", "contains(value) -> bool", "remove(value) -> bool", "iter() -> Iter<T>", "len() -> usize", "is_empty() -> bool", "clear()" },
                ["Option."] = new[] { "is_some() -> bool", "is_none() -> bool", "unwrap() -> T", "expect(message) -> T", "unwrap_or(default) -> T", "unwrap_or_else(f) -> T", "unwrap_or_default() -> T", "map(f) -> Option<U>", "and_then(f) -> Option<U>", "as_ref() -> Option<&T>", "take() -> Option<T>", "ok_or(error) -> Result<T, E>" },
                ["Result."] = new[] { "is_ok() -> bool", "is_err() -> bool", "unwrap() -> T", "expect(message) -> T", "unwrap_or(default) -> T", "unwrap_or_else(f) -> T", "map(f) -> Result<U, E>", "map_err(f) -> Result<T, F>", "and_then(f) -> Result<U, E>", "ok() -> Option<T>", "err() -> Option<E>", "as_ref() -> Result<&T, &E>" },
                ["PathBuf."] = new[] { "push(path)", "pop() -> bool", "as_path() -> &Path", "join(path) -> PathBuf", "exists() -> bool", "is_file() -> bool", "is_dir() -> bool", "file_name() -> Option<&OsStr>", "extension() -> Option<&OsStr>", "display() -> Display" }
            };

        private static readonly string[] s_rustCompletionMacros =
        {
            "println!(format, args...)", "print!(format, args...)", "eprintln!(format, args...)",
            "eprint!(format, args...)", "format!(format, args...) -> String", "vec![elements] -> Vec<T>",
            "dbg!(value)", "assert!(condition)", "assert_eq!(left, right)", "assert_ne!(left, right)",
            "debug_assert!(condition)", "panic!(message)", "todo!(message)", "unimplemented!(message)",
            "unreachable!(message)", "matches!(expression, pattern) -> bool", "write!(writer, format, args...)",
            "writeln!(writer, format, args...)", "include_str!(path) -> &str", "include_bytes!(path) -> &[u8]"
        };

        private bool RefreshRustCompletion(bool manual)
        {
            if (_mode != Mode.Insert || HasSelection() || _cursorLine < 0 || _cursorLine >= _lines.Count)
            {
                DismissCompletion();
                return false;
            }

            string line = CurrentLine();
            int start = _cursorCol;
            if (start < 0 || start > line.Length)
            {
                DismissCompletion();
                return false;
            }
            while (start > 0 && IsRustWordPart(line[start - 1])) start--;
            if (start >= 2 && line[start - 2] == 'r' && line[start - 1] == '#') start -= 2;
            string prefix = line.Substring(start, _cursorCol - start);
            bool reuse = _completionActive && _completionStartLine == _cursorLine && _completionStartCol == start;
            manual |= reuse && _rustCompletionManual;
            // Most keystrokes cannot open a popup. Avoid scanning the document for them.
            int beforeLine = _cursorLine;
            int beforeColumn = start - 1;
            while (beforeLine >= 0)
            {
                while (beforeColumn >= 0 && char.IsWhiteSpace(_lines[beforeLine][beforeColumn])) beforeColumn--;
                if (beforeColumn >= 0) break;
                if (--beforeLine >= 0) beforeColumn = _lines[beforeLine].Length - 1;
            }
            char before = beforeLine >= 0 ? _lines[beforeLine][beforeColumn] : '\0';
            if (!manual && prefix.Length < 2 && before != '.' && before != ':')
            {
                DismissCompletion();
                return false;
            }
            int position = start;
            for (int i = 0; i < _cursorLine; i++) position += _lines[i].Length + 1;

            string source = string.Join("\n", _lines);
            List<RustCompletionToken> tokens = TokenizeRustCompletion(source, position + prefix.Length, out bool suppressed);
            int previous = tokens.FindLastIndex(token => token.End <= position);
            string separator = RustTokenText(tokens, previous);
            bool member = separator == "." || separator == "::";
            if (suppressed || (prefix.Length > 0 && !IsRustWordStart(prefix[0])) ||
                separator == "'" || separator.StartsWith("'", StringComparison.Ordinal) ||
                IsRustDeclarationName(tokens, previous) || (!manual && !member && prefix.Length < 2))
            {
                DismissCompletion();
                return false;
            }

            string selected = reuse ? _completionItems[_completionSelectedIndex].Label : null;
            var items = new List<CompletionItem>();
            var labels = new HashSet<string>(StringComparer.Ordinal);
            if (member)
            {
                string target = ReadRustPathBackward(tokens, previous - 1);
                if (separator == ".") target = InferRustReceiverType(tokens, previous - 1);
                else if (target == "Self") target = FindRustEnclosingImplType(tokens, previous);
                AddRustCatalogCompletions(items, labels, target, separator);
                AddRustDeclaredMembers(tokens, source, target, separator == ".", items, labels);
            }
            else
            {
                AddRustGlobalCompletions(tokens, position, source, items, labels);
            }

            List<CompletionItem> filtered = FilterCompletionItems(items, prefix);
            if (filtered.Count == 0)
            {
                DismissCompletion();
                if (manual) Status("No Rust completions");
            }
            else
            {
                SetCompletionSession(new CompletionSession(_cursorLine, start, member, items, filtered), selected);
                _rustCompletionManual = manual;
                if (manual) Status("IntelliSense " + filtered.Count + " " + Pluralize("item", filtered.Count));
            }
            bool projectPending = QueueRustProjectCompletion(source, start, manual);
            return filtered.Count > 0 || projectPending;
        }

        private static bool IsRustDeclarationName(List<RustCompletionToken> tokens, int previous)
        {
            string word = RustTokenText(tokens, previous);
            if (word == "mut" || word == "ref") word = RustTokenText(tokens, --previous);
            return word == "let" || word == "fn" || word == "struct" || word == "enum" || word == "trait" ||
                word == "mod" || word == "type" || word == "const" || word == "static" || word == "union" ||
                (word == "!" && RustTokenText(tokens, previous - 1) == "macro_rules");
        }

        private static void AddRustCompletion(List<CompletionItem> items, HashSet<string> labels,
            string name, string kind, string detail, int priority)
        {
            if (!string.IsNullOrEmpty(name) && name != "_" && labels.Add(name))
                items.Add(new CompletionItem(name, name, kind, detail, priority));
        }

        private static string RustSignatureName(string signature)
        {
            int end = signature.IndexOfAny(new[] { '(', '[', '<' });
            return end < 0 ? signature : signature.Substring(0, end);
        }

        private static void AddRustCatalogCompletions(List<CompletionItem> items, HashSet<string> labels,
            string target, string separator)
        {
            if (string.IsNullOrEmpty(target)) return;
            bool module = separator == "::" && s_rustCompletionCatalog.ContainsKey(target);
            string key = module ? target : RustLastPathPart(target) + separator;
            if (!s_rustCompletionCatalog.TryGetValue(key, out string[] entries)) return;
            foreach (string signature in entries)
            {
                string name = RustSignatureName(signature);
                string kind = signature.Contains("(") ? (separator == "." ? "method" : "function") :
                    (char.IsUpper(name[0]) ? "type" : "module");
                AddRustCompletion(items, labels, name, kind, target + separator + signature, 10);
            }
        }

        private static void AddRustGlobalCompletions(List<RustCompletionToken> tokens, int position,
            string source, List<CompletionItem> items, HashSet<string> labels)
        {
            // Declarations precede generic names so the popup can show their signatures.
            for (int i = 0; i < tokens.Count; i++)
            {
                string word = tokens[i].Text;
                if (!tokens[i].Identifier || tokens[i].Start == position) continue;
                string previous = RustTokenText(tokens, i - 1);
                if (previous == "fn")
                    AddRustCompletion(items, labels, word, "function", RustDeclarationDetail(tokens, i - 1, source), 0);
                else if (previous == "struct" || previous == "enum" || previous == "trait" || previous == "type" || previous == "union")
                    AddRustCompletion(items, labels, word, "type", previous + " " + word, 5);
                else if (previous == "mod")
                    AddRustCompletion(items, labels, word, "module", "mod " + word, 5);
                else if (previous == "!" && RustTokenText(tokens, i - 2) == "macro_rules")
                    AddRustCompletion(items, labels, word + "!", "macro", "macro_rules! " + word, 0);
                else if (previous == "let" || previous == "mut" || previous == "ref" || previous == "const" || previous == "static" ||
                    RustTokenText(tokens, i + 1) == ":")
                    AddRustCompletion(items, labels, word, "variable", "current file: " + word, 5);
            }

            foreach (string signature in s_rustCompletionMacros)
                AddRustCompletion(items, labels, RustSignatureName(signature), "macro", signature, 20);
            foreach (string word in s_rustStdIdentifiers)
            {
                if (char.IsUpper(word[0])) AddRustCompletion(items, labels, word, "type", "Rust standard library", 20);
            }
            foreach (string word in new[] { "std", "core", "alloc" })
                AddRustCompletion(items, labels, word, "module", "Rust " + word + " crate", 25);
            foreach (HashSet<string> words in new[] { s_rustFlowKeywords, s_rustDeclarationKeywords,
                s_rustModifierKeywords, s_rustKeywords, s_rustTypeKeywords, s_rustLiteralKeywords })
                foreach (string word in words)
                    AddRustCompletion(items, labels, word, s_rustTypeKeywords.Contains(word) ? "type" : "keyword", "Rust " + word, 30);

            foreach (RustCompletionToken token in tokens)
            {
                if (token.Identifier && token.Start != position && !IsRustCompletionKeyword(token.Text) &&
                    !s_rustStdIdentifiers.Contains(token.Text))
                    AddRustCompletion(items, labels, token.Text, "identifier", "name in current file", 15);
            }
        }

        private static bool IsRustCompletionKeyword(string word)
        {
            return s_rustFlowKeywords.Contains(word) || s_rustDeclarationKeywords.Contains(word) ||
                s_rustModifierKeywords.Contains(word) || s_rustKeywords.Contains(word) ||
                s_rustReservedKeywords.Contains(word) || s_rustLiteralKeywords.Contains(word) || s_rustTypeKeywords.Contains(word);
        }

        private static string RustDeclarationDetail(List<RustCompletionToken> tokens, int start, string source)
        {
            int end = start;
            while (end + 1 < tokens.Count && tokens[end + 1].Text != "{" && tokens[end + 1].Text != ";" &&
                tokens[end + 1].Start - tokens[start].Start < 160) end++;
            return source.Substring(tokens[start].Start, tokens[end].End - tokens[start].Start)
                .Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
        }

        private static string ReadRustPathBackward(List<RustCompletionToken> tokens, int index)
        {
            if (RustTokenText(tokens, index) == ">")
            {
                int depth = 0;
                do
                {
                    if (tokens[index].Text == ">") depth++;
                    if (tokens[index].Text == "<") depth--;
                    index--;
                } while (index >= 0 && depth > 0);
                if (RustTokenText(tokens, index) == "::") index--;
            }
            if (index < 0 || !tokens[index].Identifier) return string.Empty;
            string path = tokens[index].Text;
            while (index >= 2 && tokens[index - 1].Text == "::" && tokens[index - 2].Identifier)
            {
                index -= 2;
                path = tokens[index].Text + "::" + path;
            }
            return path;
        }

        private static string RustLastPathPart(string path)
        {
            int separator = path.LastIndexOf("::", StringComparison.Ordinal);
            return separator < 0 ? path : path.Substring(separator + 2);
        }

        private static string ReadRustType(List<RustCompletionToken> tokens, int index)
        {
            while (index < tokens.Count && (tokens[index].Text == "&" || tokens[index].Text == "mut" ||
                tokens[index].Text.StartsWith("'", StringComparison.Ordinal))) index++;
            if (index >= tokens.Count || !tokens[index].Identifier) return string.Empty;
            string name = tokens[index].Text;
            while (index + 2 < tokens.Count && tokens[index + 1].Text == "::" && tokens[index + 2].Identifier)
            {
                index += 2;
                name = tokens[index].Text;
            }
            return name;
        }

        private static string InferRustReceiverType(List<RustCompletionToken> tokens, int receiver)
        {
            string name = RustTokenText(tokens, receiver);
            if (name == "\"\"") return "str";
            if (name == "self") return FindRustEnclosingImplType(tokens, receiver);
            if (receiver < 0 || !tokens[receiver].Identifier) return string.Empty;
            for (int i = receiver - 1; i >= 0; i--)
            {
                if (tokens[i].Text != name || !IsRustBindingVisible(tokens, i, receiver)) continue;
                if (RustTokenText(tokens, i + 1) == ":") return ReadRustType(tokens, i + 2);
                if (RustTokenText(tokens, i + 1) != "=") continue;
                string value = RustTokenText(tokens, i + 2);
                if (value == "vec" && RustTokenText(tokens, i + 3) == "!") return "Vec";
                if (value == "Some" || value == "None") return "Option";
                if (value == "Ok" || value == "Err") return "Result";
                if (value == "\"\"")
                    return RustTokenText(tokens, i + 3) == "." &&
                        (RustTokenText(tokens, i + 4) == "to_string" || RustTokenText(tokens, i + 4) == "to_owned") ? "String" : "str";
                int typeEnd = i + 2;
                while (typeEnd + 2 < tokens.Count && tokens[typeEnd + 1].Text == "::" && tokens[typeEnd + 2].Identifier)
                    typeEnd += 2;
                // The final path segment of a constructor call is the function name.
                if (typeEnd > i + 2 && RustTokenText(tokens, typeEnd + 1) == "(") typeEnd -= 2;
                return RustTokenText(tokens, typeEnd);
            }
            return string.Empty;
        }

        private static bool IsRustBindingVisible(List<RustCompletionToken> tokens, int declaration, int receiver)
        {
            for (int parent = tokens[declaration].Parent; parent >= 0; parent = tokens[parent].Parent)
            {
                if (tokens[parent].Text == "{" && tokens[parent].Match >= 0 && tokens[parent].Match < receiver)
                    return false;
                if (tokens[parent].Text == "(" && tokens[parent].Match >= 0)
                {
                    int body = tokens[parent].Match + 1;
                    while (body < tokens.Count && tokens[body].Text != "{" && tokens[body].Text != ";") body++;
                    if (body < tokens.Count && tokens[body].Match >= 0 && tokens[body].Match < receiver)
                        return false;
                }
            }
            return true;
        }

        private static string RustImplType(List<RustCompletionToken> tokens, int declaration, int body)
        {
            int typeStart = declaration + 1;
            if (RustTokenText(tokens, typeStart) == "<")
            {
                int depth = 0;
                do
                {
                    if (tokens[typeStart].Text == "<") depth++;
                    if (tokens[typeStart].Text == ">") depth--;
                    typeStart++;
                } while (typeStart < body && depth > 0);
            }
            for (int p = typeStart; p < body && tokens[p].Text != "where"; p++)
                if (tokens[p].Text == "for") typeStart = p + 1;
            return ReadRustType(tokens, typeStart);
        }

        private static string FindRustEnclosingImplType(List<RustCompletionToken> tokens, int index)
        {
            for (int i = index - 1; i >= 0; i--)
            {
                if (tokens[i].Text != "impl") continue;
                int body = i + 1;
                while (body < index && tokens[body].Text != "{" && tokens[body].Text != ";") body++;
                if (body < index && tokens[body].Text == "{" && (tokens[body].Match < 0 || tokens[body].Match > index))
                    return RustImplType(tokens, i, body);
            }
            return string.Empty;
        }

        private static void AddRustDeclaredMembers(List<RustCompletionToken> tokens, string source,
            string target, bool instance, List<CompletionItem> items, HashSet<string> labels)
        {
            if (string.IsNullOrEmpty(target)) return;
            target = RustLastPathPart(target);
            for (int i = 0; i + 1 < tokens.Count; i++)
            {
                string kind = tokens[i].Text;
                if (kind != "struct" && kind != "enum" && kind != "impl" && kind != "mod") continue;
                int body = i + 2;
                while (body < tokens.Count && tokens[body].Text != "{" && tokens[body].Text != ";") body++;
                if (body >= tokens.Count || tokens[body].Text != "{") continue;
                string owner = tokens[i + 1].Text;
                if (kind == "impl")
                    owner = RustImplType(tokens, i, body);
                if (owner != target) continue;
                int end = tokens[body].Match < 0 ? tokens.Count : tokens[body].Match;
                for (int p = body + 1; p < end; p++)
                {
                    if (tokens[p].Parent != body || !tokens[p].Identifier) continue;
                    string name = tokens[p].Text;
                    string previous = RustTokenText(tokens, p - 1);
                    if (kind == "struct" && instance && RustTokenText(tokens, p + 1) == ":")
                        AddRustCompletion(items, labels, name, "field", name + ": " + ReadRustType(tokens, p + 2), 0);
                    else if (kind == "enum" && !instance && (previous == "{" || previous == ","))
                        AddRustCompletion(items, labels, name, "variant", target + "::" + name, 0);
                    else if (previous == "fn" && (kind == "impl" || kind == "mod"))
                    {
                        int parameters = p + 1;
                        while (parameters < end && tokens[parameters].Text != "(" && tokens[parameters].Text != "{") parameters++;
                        bool hasSelf = false;
                        if (parameters < end && tokens[parameters].Match >= 0)
                            for (int arg = parameters + 1; arg < tokens[parameters].Match; arg++)
                                hasSelf |= tokens[arg].Text == "self";
                        if (!instance || hasSelf)
                            AddRustCompletion(items, labels, name, hasSelf ? "method" : "function", RustDeclarationDetail(tokens, p - 1, source), 0);
                    }
                    else if (!instance && (previous == "const" || previous == "type" || previous == "struct" || previous == "enum" || previous == "mod"))
                        AddRustCompletion(items, labels, name, previous == "const" ? "constant" : "type", previous + " " + name, 0);
                }
            }
        }

        private static string RustTokenText(List<RustCompletionToken> tokens, int index)
        {
            return index >= 0 && index < tokens.Count ? tokens[index].Text : string.Empty;
        }

        private sealed class RustCompletionToken
        {
            public string Text;
            public int Start;
            public int End;
            public int Parent;
            public int Match = -1;
            public bool Identifier;
        }

        // Keep literal/comment boundaries across lines, including nested comments and
        // raw strings. Highlighting's line-local scanner cannot suppress completion here.
        private static List<RustCompletionToken> TokenizeRustCompletion(string source, int cursor, out bool suppressed)
        {
            suppressed = false;
            var tokens = new List<RustCompletionToken>();
            var delimiters = new Stack<int>();
            for (int i = 0; i < source.Length;)
            {
                if (char.IsWhiteSpace(source[i])) { i++; continue; }
                int start = i;
                bool comment = false;
                bool literal = false;
                bool closed = true;
                string text = null;
                if (i + 1 < source.Length && source[i] == '/' && source[i + 1] == '/')
                {
                    comment = true;
                    i += 2;
                    while (i < source.Length && source[i] != '\n') i++;
                    closed = false;
                }
                else if (i + 1 < source.Length && source[i] == '/' && source[i + 1] == '*')
                {
                    comment = true;
                    i += 2;
                    int depth = 1;
                    while (i < source.Length && depth > 0)
                    {
                        if (i + 1 < source.Length && source[i] == '/' && source[i + 1] == '*') { depth++; i += 2; }
                        else if (i + 1 < source.Length && source[i] == '*' && source[i + 1] == '/') { depth--; i += 2; }
                        else i++;
                    }
                    closed = depth == 0;
                }
                else if (TryReadRustCompletionString(source, start, out int stringEnd, out closed))
                {
                    i = stringEnd;
                    literal = true;
                    text = "\"\"";
                }
                else if (TryReadRustChar(source, i, out int charLength))
                {
                    i += charLength;
                    literal = true;
                    text = "''";
                }
                else if (source[i] == '\'' && i + 1 < source.Length && source[i + 1] == '\\')
                {
                    // Escaped character being edited (including \xNN and \u{NNNN}).
                    i += 2;
                    while (i < source.Length && source[i] != '\'' && source[i] != '\n') i++;
                    closed = i < source.Length && source[i] == '\'';
                    if (closed) i++;
                    literal = true;
                    text = "''";
                }
                else if (source[i] == '\'' && i + 1 < source.Length && IsRustWordStart(source[i + 1]))
                {
                    i += 2;
                    while (i < source.Length && IsRustWordPart(source[i])) i++;
                    // A lifetime may have no closing quote; it is not a string.
                    if (cursor > start && cursor <= i) suppressed = true;
                }
                else if (char.IsDigit(source[i])) i = ReadRustNumberEnd(source, i);
                else if (IsRustWordStart(source[i]))
                {
                    if (source[i] == 'r' && i + 2 < source.Length && source[i + 1] == '#' && IsRustWordStart(source[i + 2])) i += 2;
                    i++;
                    while (i < source.Length && IsRustWordPart(source[i])) i++;
                }
                else
                {
                    i++;
                    if (i < source.Length && ((source[start] == ':' && source[i] == ':') ||
                        (source[start] == '.' && source[i] == '.') || (source[start] == '-' && source[i] == '>'))) i++;
                }

                if ((comment || literal) && cursor > start && (cursor < i || (!closed && cursor == i))) suppressed = true;
                if (comment) continue;
                text = text ?? source.Substring(start, i - start);
                var token = new RustCompletionToken
                {
                    Text = text, Start = start, End = i,
                    Identifier = !literal && IsRustWordStart(text[0]),
                    Parent = delimiters.Count == 0 ? -1 : delimiters.Peek()
                };
                if (text == "{" || text == "(" || text == "[") delimiters.Push(tokens.Count);
                else if (delimiters.Count > 0 && (text == "}" || text == ")" || text == "]"))
                {
                    string open = tokens[delimiters.Peek()].Text;
                    if ((open == "{" && text == "}") || (open == "(" && text == ")") || (open == "[" && text == "]"))
                    {
                        token.Match = delimiters.Pop();
                        tokens[token.Match].Match = tokens.Count;
                    }
                }
                tokens.Add(token);
            }
            return tokens;
        }

        private static bool TryReadRustCompletionString(string source, int start, out int end, out bool closed)
        {
            int quote = start;
            end = start;
            closed = false;
            if (source[quote] == 'b' || source[quote] == 'c') quote++;
            bool raw = quote < source.Length && source[quote] == 'r';
            int hashes = 0;
            if (raw)
            {
                quote++;
                while (quote < source.Length && source[quote] == '#') { hashes++; quote++; }
            }
            if (quote >= source.Length || source[quote] != '"') return false;
            end = quote + 1;
            while (end < source.Length)
            {
                if (!raw && source[end] == '\\') { end = Math.Min(source.Length, end + 2); continue; }
                if (source[end++] != '"') continue;
                int h = 0;
                while (h < hashes && end + h < source.Length && source[end + h] == '#') h++;
                if (h == hashes) { end += h; closed = true; break; }
            }
            return true;
        }
    }
}
