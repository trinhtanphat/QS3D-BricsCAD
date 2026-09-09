"""Run actual V25 held decoder and .NET Framework identity admission regressions."""
from pathlib import Path
import os
import shutil
import subprocess

root = Path(__file__).resolve().parents[1]
source = (root / "scripts/assert-v25-release-package-identity.ps1").read_text(encoding="utf-8")
start = source.index("function Read-HeldStrictUtf8Metadata")
end = source.index("function Open-HeldAssemblyFile", start)
reader = source[start:end]
for token in (
    "$Held.Stream.Length -gt $script:MaxMetadataBytes",
    "$Held.Stream.Read($bytes, $offset, $bytes.Length - $offset)",
    "$Held.Stream.ReadByte() -ne -1",
    "$preambleLength = 0",
    "$bytes.Length -ge 3 -and $bytes[0] -eq 0xef -and $bytes[1] -eq 0xbb -and $bytes[2] -eq 0xbf",
    "$preambleLength = 3",
    "$script:StrictUtf8.GetString($bytes, $preambleLength, $bytes.Length - $preambleLength)",
    "catch [Text.DecoderFallbackException]",
):
    if token not in reader:
        raise SystemExit("Missing exact held UTF-8 preamble contract: " + token)
if reader.index("$preambleLength = 0") < reader.index("$Held.Stream.ReadByte() -ne -1"):
    raise SystemExit("Preamble admission must follow the bounded complete held read")
if "TrimStart(" in reader or ".Replace(" in reader:
    raise SystemExit("Metadata decoding must not remove arbitrary U+FEFF text")
if os.name != "nt":
    print("PASS source contract; full V25 .NET Framework verifier execution requires Windows PowerShell")
else:
    shell = shutil.which("powershell.exe")
    if not shell:
        raise SystemExit("Windows PowerShell is required for the actual V25 verifier regression")
    completed = subprocess.run(
        [shell, "-NoLogo", "-NoProfile", "-File", str(root / "scripts/test-v25-held-metadata-utf8-preamble.ps1")],
        cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=60,
    )
    if completed.returncode != 0:
        raise SystemExit((completed.stdout + completed.stderr)[-12000:])
    if "PASS V25 held strict UTF-8 preamble and full identity verifier" not in completed.stdout:
        raise SystemExit("Actual verifier regression omitted its completion marker")
    print("PASS V25 held strict UTF-8 preamble and full identity verifier")
