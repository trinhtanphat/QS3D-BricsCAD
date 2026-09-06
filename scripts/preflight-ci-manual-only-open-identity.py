#!/usr/bin/env python3
from pathlib import Path
import ast
from types import SimpleNamespace

root = Path(__file__).resolve().parents[1]
target = root / "scripts" / "preflight-ci-manual-only.py"
source = target.read_text(encoding="utf-8")
module = ast.parse(source, filename=str(target))
function = next(
    (node for node in module.body if isinstance(node, ast.FunctionDef) and node.name == "_same_opened_file"),
    None,
)
if function is None:
    raise SystemExit("manual-only workflow identity guard is missing _same_opened_file")

isolated = ast.Module(body=[function], type_ignores=[])
ast.fix_missing_locations(isolated)
namespace: dict[str, object] = {}
exec(compile(isolated, str(target), "exec"), namespace, namespace)
same_opened_file = namespace["_same_opened_file"]


def meta(*, dev=0, ino=0, size=100, mtime=1000, ctime=2000):
    return SimpleNamespace(
        st_dev=dev,
        st_ino=ino,
        st_size=size,
        st_mtime_ns=mtime,
        st_ctime_ns=ctime,
    )


# Stable IDs are authoritative when available.
if not same_opened_file(meta(dev=7, ino=11), meta(dev=7, ino=11)):
    raise SystemExit("manual-only workflow identity guard rejects an unchanged stable file ID")
if same_opened_file(meta(dev=7, ino=11), meta(dev=7, ino=12)):
    raise SystemExit("manual-only workflow identity guard accepts a changed stable file ID")

# Some filesystems/runtimes do not expose usable dev/inode identity. The comparator
# must not fail open there: metadata generation changes must still be detected.
if not same_opened_file(meta(), meta()):
    raise SystemExit("manual-only workflow identity fallback rejects unchanged metadata")
for label, opened in (
    ("size", meta(size=101)),
    ("mtime", meta(mtime=1001)),
    ("ctime", meta(ctime=2001)),
):
    if same_opened_file(meta(), opened):
        raise SystemExit(f"manual-only workflow identity fallback accepts changed {label}")

print("PASS manual-only workflow source admission detects identity changes even when stable file IDs are unavailable")
