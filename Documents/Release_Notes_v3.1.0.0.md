# xTerminal v3.1.0.0

xTerminal 3.1.0.0 adds Tab completion for built-in command parameters and improves completion cycling at the terminal prompt.

Changes since v3.0.9.1.

## Highlights

- Type a command and part of a supported parameter, then press **Tab** to complete it. For example, `hash -sha2` completes to `hash -sha256`, and `wget --n` completes to `wget --noping`.
- Press **Tab** repeatedly to cycle through matching parameters and wrap back to the first match. For example, `ls -c` cycles through `-c`, `-cf`, `-cd`, and `-ct`.
- Parameter completion replaces only the final argument, preserving earlier arguments, spacing, and the prompt. Editing the input starts a fresh set of matches.
- Added parameter lists for built-in commands, including supported subcommands such as `ctx list`.
- Expanded `ls` file-path completion to suggest both files and directories.

## Fixes and improvements

- Fixed repeated Tab presses losing the original parameter matches or changing earlier input.
- Unrecognized parameters beginning with `-` leave the input unchanged instead of triggering file completion.
- Aligned `note` parameter suggestions with its dash-prefixed command syntax.
- Improved file-signature matching for some extensions in the `file` command.

## Upgrade notes

The `chain`, `env`, and `note` commands now require a leading dash on their subcommands. Update saved scripts and aliases that use the old forms:

| Previous syntax | New syntax |
| --- | --- |
| `chain create`, `chain run`, `chain add` | `chain -create`, `chain -run`, `chain -add` |
| `chain show`, `chain list`, `chain del` | `chain -show`, `chain -list`, `chain -del` |
| `env get`, `env set`, `env del` | `env -get`, `env -set`, `env -del` |
| `env addval`, `env delval` | `env -addval`, `env -delval` |
| `env load`, `env export` | `env -load`, `env -export` |
| `note add`, `note list`, `note del`, `note clear` | `note -add`, `note -list`, `note -del`, `note -clear` |

The `env -t <scope>` option is unchanged; for example, use `env -t user -get PATH`.

**Full changelog:** https://github.com/0x78654C/xTerminal/compare/v3.0.9.1...v3.1.0.0
