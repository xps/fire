Fire
===============================

A git-aware command line tool to **FI**nd and **RE**place text in files — in file *contents* and in file/directory *names*.

Inspired by the excellent [fart][0] (Find And Replace Text) command line utility by Lionello Lunesu.

The main intended use case is renaming things across a source tree: point Fire at a directory,
give it a find string and a replacement, and it consistently updates code, file names and
directory names in one pass.

**Features**

  - **Git-friendly file discovery.** Files are found by walking the directory tree;
    `.git` folders are always skipped and `.gitignore` files (including nested ones) are
    respected by default. Use `--no-ignore` to process ignored files too.
  - **Smart adapt-case.** With `-a`, `BionicBeaver` also matches `bionicBeaver`,
    `bionic-beaver`, `bionic_beaver`, `BIONIC_BEAVER`, `bionicbeaver`, `BIONICBEAVER`...
    and each match is replaced with the same-style variant of the replacement
    (`discoDingo`, `disco-dingo`, `DISCO_DINGO`, ...).
  - **Safe renames.** If a rename target already exists, the rename is skipped with a
    warning — nothing is ever overwritten, merged or deleted. Case-only renames
    (`foo` → `Foo`) work.
  - **Encoding preserved.** BOMs (UTF-8/16/32) are detected and kept, BOM-less files stay
    BOM-less, non-UTF-8 files round-trip byte-for-byte, and line endings are untouched.
    Binary files (NUL bytes) are skipped.
  - **Regular expressions** with the .NET syntax, including `$1`/`${name}` substitutions
    in the replacement.
  - **Preview mode** (`-p`) shows every change without touching the disk.


Usage
===============================

`C:\> fire [options] <find_string> <replace_string> [files]`

**Options**

    -v, --verbose              Output more information
    -n, --no-filenames         Don't replace in file/directory names
    -i, --ignore-case          Case insensitive text comparison
    -a, --adapt-case           Match case variants of find_string and adapt replace_string to each match
    -w, --whole-word           Match whole word only
    -x, --regex                Match find_string as a .NET regular expression
    -p, --preview              Preview changes only, don't touch any files
    -u, --no-ignore            Also process files excluded by .gitignore

    --help                     Display this help screen
    --version                  Display version information

`files` is an optional glob pattern (e.g. `*.cs`, `src/**/*.cshtml`). A pattern without a
directory separator matches at any depth. When a pattern is given, only matching files are
edited/renamed and directory names are left alone.


Examples
===============================

```Shell
# Replace OldText with NewText everywhere under the current directory
fire OldText NewText

# Smart-case rename of a project: also converts oldText, old-text, OLD_TEXT, ...
fire -a OldText NewText

# Preview a whole-word replacement in .cs files only
fire -pw Speech Voice *.cs

# Regex with group substitution
fire -x "gr([ae])y" "r$1d"
```


Notes
===============================

  - `-a` (adapt-case) expects identifier-like strings (letters, digits, `-`, `_`).
    For anything else it falls back to a case-insensitive literal replacement.
  - `-a` and `-x` are incompatible.
  - Nested `.gitignore` files are combined with union semantics: a path excluded at any
    level stays excluded (a nested `!pattern` cannot re-include something a parent
    `.gitignore` excluded).
  - Regular expressions use the [.NET syntax][1].


Development
===============================

    Fire.Core/    engine library (discovery, replacement, rename planning)
    Fire/         command line executable
    Fire.Tests/   MSTest suite (unit + integration): dotnet test


License
===============================

Copyright © 2026 Xavier Poinas

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


[0]: http://fart-it.sourceforge.net/
[1]: https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference
