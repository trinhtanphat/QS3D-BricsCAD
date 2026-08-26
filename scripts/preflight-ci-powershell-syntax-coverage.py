#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"


def fail(message: str) -> None:
    raise SystemExit(f"ERROR: CI PowerShell syntax coverage preflight failed: {message}")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"missing {label}: {token}")


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")

    start_token = "      - name: Validate tracked PowerShell syntax\n"
    start = text.find(start_token)
    if start < 0:
        fail("Shared CI must expose the tracked PowerShell syntax step")
    next_step = text.find("\n      - name: ", start + len(start_token))
    if next_step < 0:
        fail("could not bound the tracked PowerShell syntax step")
    step = text[start:next_step]

    require(step, "$scriptJson = & python scripts/list-tracked-powershell.py --root .", "lossless tracked-file discovery helper")
    require(step, "if ($LASTEXITCODE -ne 0)", "tracked discovery failure guard")
    require(step, "$scripts = @($scriptJson | ConvertFrom-Json -ErrorAction Stop)", "strict helper JSON decode")
    require(step, "if ($scripts.Count -eq 0)", "empty-discovery failure guard")
    require(step, "Test-Path -LiteralPath $script -PathType Leaf", "tracked-file existence guard")
    require(step, "[System.Management.Automation.Language.Parser]::ParseFile", "PowerShell AST parser")
    require(step, "if ($parseErrors.Count -gt 0)", "parser-error rejection")
    require(step, "foreach ($script in $scripts)", "dynamic parse loop")

    if "foreach ($script in @(" in step:
        fail("syntax coverage regressed to a hardcoded script allowlist")
    if "git ls-files -- 'scripts'" in step or "Where-Object { $_ -match '\\.ps1$' }" in step:
        fail("syntax coverage regressed to line-oriented or case-sensitive PowerShell discovery")

    representatives = (
        ROOT / "scripts" / "package-v25.ps1",
        ROOT / "scripts" / "package-v26.ps1",
        ROOT / "scripts" / "run-local-v25-qualification.ps1",
    )
    for path in representatives:
        if not path.is_file():
            fail(f"representative tracked PowerShell script is missing: {path.relative_to(ROOT)}")
        rel = path.relative_to(ROOT).as_posix()
        if rel in step:
            fail(f"syntax step must cover {rel} through tracked discovery, not a literal allowlist")

    helper = ROOT / "scripts" / "list-tracked-powershell.py"
    if not helper.is_file():
        fail("lossless tracked PowerShell discovery helper is missing")

    if "Validate release PowerShell syntax" in text:
        fail("legacy partial syntax-step name remains in Shared CI")

    print("CI PowerShell syntax coverage preflight: PASS")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
