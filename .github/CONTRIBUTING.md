# Contributing to RetroSchematic

Thanks for looking. This file says how to get a build running, what the code is expected to look
like, and what will get a change sent back.

---

## Getting a build

| # | Requirement | How to check |
|---|---|---|
| 1 | The .NET SDK version pinned in `src/global.json` | `dotnet --info` |
| 2 | Git LFS, for the scanned drawings and manuals under `src/docs/sources/` | `git lfs version` |

```powershell
# from the src folder
dotnet test RetroSchematic.slnx -nodeReuse:false
```

> [!IMPORTANT]
> Pass **`-nodeReuse:false`** on every `dotnet build` and `dotnet test`. Node reuse leaves MSBuild
> worker processes behind that lock the output DLL; the next build then keeps the old binary and
> the tests report green against stale code.

## Reading a drawing

A new or changed sheet file follows `src/docs/FORMAT.md`. Every part, gate and pin carries how sure
the reading is (`read`, `inferred`, `open`); an `inferred` one says in its `note` what it was
worked out from. Something that cannot be read stays in the file as `open` - never leave it out and
never guess it. Check the file with `retroschematic check <file>` before sending it.

---

## What the code is expected to look like

 - **No LINQ, and avoid `foreach`.** Use index-based `for` loops. Where a hot path allocates, use
   `Span<T>`, `ArrayPool<T>` or `stackalloc`.
 - **Unit tests only**, in the existing test projects. Never add a standalone program that prints
   something and exits - it cannot fail a build.
 - **No FluentAssertions.** Use the test framework's own asserts.
 - **Keep the comments, and add more.** A comment that records a datasheet reference, a
   specification clause, or the reason a thing is NOT done the obvious way is the most valuable
   line in the file. Never delete one because a style guide prefers less; replace one only when it
   has become factually wrong.
 - **Plain words.** In code, comments, commit messages and documentation alike. No jargon where a
   normal word works.
 - **Run `dotnet format`** if whitespace drifts.

### Naming

Package id = root namespace = folder name. The standard is `NAMING.md`, kept beside the
repositories. The organisation is **RetroCoreLabs**; the old name `HackerCorpLabs` is retired and
must not appear in anything new.

---

## Documentation changes

```powershell
python src\eng\check-docs.py   # from the repository root
```

It must exit 0. It fails on a relative link that does not resolve, a relative link that escapes
the repository root, a machine-specific absolute path, and a Mermaid diagram that is malformed or
breaks `MERMAID_COLOR_STANDARDS.md`.

> [!WARNING]
> **Never put an absolute path in anything committed here.** A drive letter or a home directory is
> correct on exactly one machine, and this repository is public. A path inside the repository goes
> repo-relative; a sibling repository is named, not located; anything outside is described. A
> variable such as `%USERPROFILE%` or `~` is fine - that is the portable way to name a per-user
> location.

### The documentation site

<https://retrocorelabs.github.io/RetroSchematic/> is built by MkDocs from the Markdown in this
repository (`.github/workflows/docs.yml`, on every push to `master`). The chip, bus signal and
reference pages under `src/docs/reference/` and the page tree in `src/docs-site/mkdocs.yml` are
generated from the catalogue files. After changing a file in `src/catalog/`, a sheet, or adding
or moving a guide, regenerate them and commit the result - CI runs the same script with
`--check` and fails when they are out of date:

```powershell
# from the src folder
python -I docs-site/gen_site.py
```

A new guide is placed in the page tree by the policy at the top of `src/docs-site/gen_site.py`;
one the policy does not name lands under "More". To build the site locally (Python 3.12, a
virtual environment outside the checkout; MkDocs refuses an output folder inside the repository):

```powershell
# from the src folder
python -m venv $env:TEMP\retroschematic-site
& $env:TEMP\retroschematic-site\Scripts\pip install -r docs-site/requirements.txt
& $env:TEMP\retroschematic-site\Scripts\mkdocs build --strict -f docs-site/mkdocs.yml -d $env:TEMP\retroschematic-site-out
```

It must finish with no warning. Git LFS must be installed: the site shows the symbol pictures
and the icon, which are LFS files.

A README follows `README-TEMPLATE.md`, beside the repositories: one sentence at the top that a
stranger to the subject understands, four badges that can each be wrong, install before
build-from-source, every application in a table linking its own README, licence last.

---

## Submitting a change

 1. Branch off `master`. Do not commit to `master` directly.
 2. One logical change per commit. A commit message says **what changed and why**, in plain
    words - the subject line in the imperative, then a blank line, then the reasoning. If you
    measured something, put the number in the message.
 3. `dotnet test RetroSchematic.slnx` (from `src`) passes before you open the pull request.
 4. Say in the pull request what you verified and how. "Tests pass" on its own is not useful;
    "the netlist reader now refuses a gate in an unknown part, 16 tests, all green" is.
 5. If something is still broken or unfinished, say so plainly. A known gap that is written down
    is fine. One that is hidden is not.

---

## Licence

By contributing you agree your work is licensed under the MIT licence in [LICENSE](../LICENSE).
