"""MkDocs hook: makes every relative link on a page lead somewhere real.

The guides are written for github.com, so they link to anything in the tree: source files,
sheets, catalogue JSON, the scanned PDFs, folders, LICENSE, the .github files. Only the pages and
the pictures are published (exclude_docs in base.yml), so before MkDocs reads a page this hook
rewrites each relative link:

- to a file that is on the site: left alone for MkDocs to resolve (Markdown links). In raw HTML
  (<img src>, <a href>) MkDocs rewrites nothing, so the hook writes the address of the page or
  file on the site.
- to a file or folder git tracks that is not on the site: the file (blob) or folder (tree) on
  GitHub.
- to anything else: left alone, so MkDocs reports it (Markdown) or the hook logs a WARNING
  (raw HTML) - with --strict that fails the build. A broken link never passes quietly.

Links inside fenced code blocks are not touched.
"""
import logging
import os
import posixpath
import re
import subprocess

from mkdocs.utils import get_relative_url

log = logging.getLogger("mkdocs.plugins.retroschematic")

GITHUB = "https://github.com/RetroCoreLabs/RetroSchematic"

_tracked = set()
_tracked_dirs = set()
_site_files = None

MD_LINK = re.compile(r"(!?\[(?:[^\[\]]|\[[^\]]*\])*\]\()(<[^>]+>|[^)\s]+)((?:\s+\"[^\"]*\")?\))")
HTML_ATTR = re.compile(r"""(\b(?:src|href)=)(["'])([^"']+)\2""")
FENCE = re.compile(r"^\s*(```|~~~)")


def on_config(config):
    root = config["docs_dir"]
    out = subprocess.run(["git", "-C", root, "ls-files", "-z"], check=True, capture_output=True).stdout
    _tracked.clear()
    _tracked_dirs.clear()
    for p in out.decode("utf-8").split("\0"):
        if not p:
            continue
        _tracked.add(p)
        d = posixpath.dirname(p)
        while d:
            _tracked_dirs.add(d)
            d = posixpath.dirname(d)
    return config


def on_files(files, config):
    global _site_files
    _site_files = files
    return files


def _external(target):
    return (target.startswith(("#", "/", "mailto:", "data:"))
            or re.match(r"^[a-zA-Z][a-zA-Z0-9+.-]*:", target) is not None)


def _resolve(page_src, target):
    path, sep, anchor = target.partition("#")
    if not path:
        return None, None, None
    joined = posixpath.normpath(posixpath.join(posixpath.dirname(page_src), path))
    return joined, path, (sep + anchor)


def _rewrite(page, target, raw_html):
    if _external(target):
        return target
    resolved, path, anchor = _resolve(page.file.src_uri, target)
    if resolved is None or resolved.startswith(".."):
        return target
    f = _site_files.get_file_from_path(resolved) if _site_files else None
    if f is not None and not f.inclusion.is_included():
        f = None
    if f is not None:
        if raw_html:
            return get_relative_url(f.url, page.url) + anchor
        return target
    if resolved in _tracked:
        return f"{GITHUB}/blob/master/{resolved}{anchor}"
    if resolved.rstrip("/") in _tracked_dirs:
        return f"{GITHUB}/tree/master/{resolved.rstrip('/')}"
    if raw_html:
        log.warning("%s: link to '%s' leads nowhere (not on the site, not tracked by git)",
                    page.file.src_uri, target)
    return target


def on_page_markdown(markdown, page, config, files):
    out = []
    in_fence = False
    for line in markdown.split("\n"):
        if FENCE.match(line):
            in_fence = not in_fence
            out.append(line)
            continue
        if in_fence:
            out.append(line)
            continue

        def md(m):
            target = m.group(2)
            bare = target[1:-1] if target.startswith("<") else target
            new = _rewrite(page, bare, False)
            if new == bare:
                return m.group(0)
            return m.group(1) + new + m.group(3)

        def html(m):
            return m.group(1) + m.group(2) + _rewrite(page, m.group(3), True) + m.group(2)

        line = MD_LINK.sub(md, line)
        line = HTML_ATTR.sub(html, line)
        out.append(line)
    return "\n".join(out)
