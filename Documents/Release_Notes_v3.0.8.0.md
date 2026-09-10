# xTerminal v3.0.8.0

xTerminal 3.0.8.0 adds IntelliSense for TermXT scripts in the built-in `xte` editor, improves C# editing responsiveness, and updates the TermXT scripting language to 1.0.2.

## Highlights

- Added release notes to new-version notifications, displayed before the update confirmation prompt.
- Added automatic TermXT IntelliSense in `xte` for keywords, built-in functions, declared functions after `call`, and variables inside `{...}` or after `lines:`.
- Added usage hints, case-insensitive filtering, and automatic closing braces for variable completions. Variable suggestions also work inside quoted text.
- Added completion keyboard controls for TermXT and C#:
  - **Ctrl+Space** opens suggestions manually in insert mode.
  - **Up/Down** selects a suggestion.
  - **Enter/Tab** accepts the selected completion.
  - **Esc** closes suggestions; pressing it again returns to normal mode.
- Updated TermXT to version 1.0.2, adding `{argc}`, parenthesized conditions, and compact expressions such as `{count}>=2&&{count}<5`.
- Added the TermXT 1.0.2 Markdown and PDF manuals and a new `language_features.xt` example.

## Fixes and improvements

- Moved C# semantic diagnostics into background processing and cached runtime assembly metadata to reduce pauses in `xte`. Outdated analysis results are discarded when the document changes.
- Fixed TermXT logical operator precedence, quoted condition text, and repeated condition interpolation. Operators inside substituted values remain literal.
- Improved argument parsing for quoted and empty arguments, tab separators, and literal `-p` text inside quotes.
- Fixed TermXT function argument leakage and stale return values. Function calls preserve whitespace and quotes in variable arguments and restore the caller's positional arguments and `{argc}`.
- Fixed TermXT nested loop counters and integer-range overflow. Excessive function or condition nesting now reports an error.
- Improved `try/catch` handling in TermXT, failure propagation, and cleanup after exceptions. Failed pipeline stages stop later stages.
- Fixed quoted file paths, empty file output, and substring bounds handling in TermXT.
- Shared structural validation between the interpreter and editor. `xt -check` now reports failure through the command error state, including when a script is missing.
- Updated example scripts to use `{argc}` for optional arguments and adjusted port-status matching in `netaudit.xt`.

## Upgrade notes

Use `{argc}` to check for optional script arguments; missing arguments remain literal placeholders. Write empty comparison operands as `""`.

Conditions apply comparisons/`not`, then `&&`, then `||`. Use parentheses when negating a compound condition. Each interpolated variable in a function call now remains one argument, even when its value contains spaces.

Scripts with invalid block structure, assignments, or duplicate function definitions are rejected before execution. Run `xt -check <script.xt>` to check existing scripts.

• xTerminal v3.0.8.0 is here!

  This release adds IntelliSense for TermXT scripts, improves C# editor responsiveness, and brings
  TermXT 1.0.2 with better conditions, argument handling, and error recovery. Update notifications now
  show release notes before installation.

  Built for a smoother scripting experience on Windows.

  https://github.com/0x78654C/xTerminal

  #xTerminal #OpenSource #DeveloperTools #Scripting #Windows