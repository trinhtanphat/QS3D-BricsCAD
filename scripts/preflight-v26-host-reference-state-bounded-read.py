from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "assert-v26-host-reference-safety.ps1"
text = SCRIPT.read_text(encoding="utf-8")

match = re.search(
    r"function\s+Read-BoundedStrictUtf8\s*\{(?P<body>.*?)\n\}",
    text,
    flags=re.DOTALL,
)
if not match:
    raise SystemExit("Read-BoundedStrictUtf8 function was not found")

body = match.group("body")
open_pos = body.find("[IO.File]::Open(")
stream_bound = re.search(r"\[long\]\$stream\.Length\s+-gt\s+\$MaxBytes", body)
reader_pos = body.find("[IO.StreamReader]::new(")
read_pos = body.find("$reader.ReadToEnd()")

if open_pos < 0:
    raise SystemExit("bounded state read must open one exact FileStream")
if stream_bound is None:
    raise SystemExit("bounded state read must enforce MaxBytes against the exact opened stream")
if reader_pos < 0 or read_pos < 0:
    raise SystemExit("bounded state read must retain strict StreamReader decode on the admitted stream")
if not (open_pos < stream_bound.start() < reader_pos < read_pos):
    raise SystemExit("opened-stream byte admission must happen before decoder creation/read")
if re.search(r"\$file\.Length\s+-gt\s+\$MaxBytes", body):
    raise SystemExit("pathname FileInfo length must not be the authoritative bounded-read admission")
if body.count("[IO.File]::Open(") != 1:
    raise SystemExit("bounded state read must not retry/reopen into another filesystem generation")
if "[Text.UTF8Encoding]::new($false, $true)" not in body:
    raise SystemExit("strict UTF-8 decoding contract must remain enabled")
if "Assert-NoExistingReparseComponent" not in body or "Get-RequiredOrdinaryFile" not in body:
    raise SystemExit("existing non-reparse pathname admission must remain in place")

print("PASS V26 host-reference state bounded read is bound to the exact opened stream")
