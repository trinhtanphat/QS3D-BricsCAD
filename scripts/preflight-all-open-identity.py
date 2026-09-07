#!/usr/bin/env python3
import ast
from pathlib import Path
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "preflight-all.py"


def load_comparator():
    source = TARGET.read_text(encoding="utf-8")
    tree = ast.parse(source, filename=str(TARGET))
    matches = [
        node for node in tree.body
        if isinstance(node, ast.FunctionDef) and node.name == "_same_opened_file"
    ]
    if len(matches) != 1:
        raise SystemExit(f"expected exactly one _same_opened_file in {TARGET}, found {len(matches)}")
    module = ast.Module(body=[matches[0]], type_ignores=[])
    ast.fix_missing_locations(module)
    namespace = {}
    exec(compile(module, str(TARGET), "exec"), namespace, namespace)
    return namespace["_same_opened_file"]


def meta(*, size=100, dev=0, ino=0, mtime_ns=10, ctime_ns=20, include_mtime=True, include_ctime=True):
    values = {"st_size": size, "st_dev": dev, "st_ino": ino}
    if include_mtime:
        values["st_mtime_ns"] = mtime_ns
    if include_ctime:
        values["st_ctime_ns"] = ctime_ns
    return SimpleNamespace(**values)


same_opened_file = load_comparator()

# Stable filesystem identity remains authoritative when usable.
if not same_opened_file(meta(dev=7, ino=11), meta(dev=7, ino=11, size=999, mtime_ns=99, ctime_ns=88)):
    raise SystemExit("aggregate comparator rejected matching usable dev/inode identity")
if same_opened_file(meta(dev=7, ino=11), meta(dev=7, ino=12)):
    raise SystemExit("aggregate comparator accepted mismatched usable dev/inode identity")

# Without usable identity, all fallback generation metadata must be present and equal.
if not same_opened_file(meta(), meta()):
    raise SystemExit("aggregate comparator rejected equal complete fallback metadata")
if same_opened_file(meta(size=100), meta(size=101)):
    raise SystemExit("aggregate comparator accepted fallback size drift")
if same_opened_file(meta(mtime_ns=10), meta(mtime_ns=11)):
    raise SystemExit("aggregate comparator accepted fallback mtime drift")
if same_opened_file(meta(ctime_ns=20), meta(ctime_ns=21)):
    raise SystemExit("aggregate comparator accepted fallback ctime drift")
if same_opened_file(meta(include_mtime=False), meta(include_mtime=False)):
    raise SystemExit("aggregate comparator fail-open: missing mtime metadata was treated as stable")
if same_opened_file(meta(include_ctime=False), meta(include_ctime=False)):
    raise SystemExit("aggregate comparator fail-open: missing ctime metadata was treated as stable")
if same_opened_file(meta(include_mtime=False), meta()):
    raise SystemExit("aggregate comparator accepted one-sided missing mtime metadata")
if same_opened_file(meta(include_ctime=False), meta()):
    raise SystemExit("aggregate comparator accepted one-sided missing ctime metadata")

print("PASS aggregate preflight open identity fails closed when fallback generation metadata is unavailable or changes")
