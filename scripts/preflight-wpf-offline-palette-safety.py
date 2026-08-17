from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "scripts" / "test-wpf-palettes-runtime.ps1"
WRAPPER = ROOT / "scripts" / "run-local-v25-wpf-smoke.ps1"
QUALIFICATION = ROOT / "scripts" / "run-local-v25-qualification.ps1"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"FAIL: {message}")


def read(path: Path) -> str:
    require(path.is_file(), f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def palette_contract_errors(source: str) -> list[str]:
    errors: list[str] = []
    required = (
        "$uiRoot = Join-Path $root 'src\\QS3D.BricsCAD.V25\\UI'",
        "$maxXamlBytes = 1MB",
        "UTF8Encoding]::new($false, $true)",
        "Resolve-Path -LiteralPath $uiRoot",
        "Resolve-Path -LiteralPath $Path",
        "StartsWith($uiPrefix, [System.StringComparison]::OrdinalIgnoreCase)",
        "[System.IO.FileAttributes]::ReparsePoint",
        "$item.Length -gt $maxXamlBytes",
        "[System.IO.File]::Open(",
        "($memory.Length + $read) -gt $maxXamlBytes",
        "$utf8Strict.GetString($bytes)",
        "Palette XAML source escapes the canonical UI root",
        "Palette XAML source must not be a reparse point",
        "Palette XAML source exceeds the 1 MiB safety limit",
        "Palette XAML source is not valid strict UTF-8",
        "return ,$manager",
    )
    for token in required:
        if token not in source:
            errors.append(f"offline palette bounded-input contract missing: {token}")
    for forbidden in (
        "Get-Content -LiteralPath $Path -Raw",
        "Assembly]::LoadFrom",
        "AssemblyResolve",
        "Activator]::CreateInstance",
        "Start-Process",
        "System.Diagnostics.Process",
    ):
        if forbidden in source:
            errors.append(f"offline palette smoke retained forbidden load/read primitive: {forbidden}")
    return errors


palette = read(PALETTE)
wrapper = read(WRAPPER)
qualification = read(QUALIFICATION)
v26_project = read(V26_PROJECT)

errors = palette_contract_errors(palette)
require(not errors, "; ".join(errors))

# Mutation self-checks ensure the preflight itself detects removal of each critical
# source-input safety boundary rather than merely documenting the desired tokens.
for token in (
    "$maxXamlBytes = 1MB",
    "[System.IO.FileAttributes]::ReparsePoint",
    "UTF8Encoding]::new($false, $true)",
    "Resolve-Path -LiteralPath $uiRoot",
    "($memory.Length + $read) -gt $maxXamlBytes",
):
    mutated = palette.replace(token, token + "__REMOVED__", 1)
    require(palette_contract_errors(mutated), f"mutation self-check did not detect removal of: {token}")

for token in (
    "WorkspacePanel.xaml",
    "RightPanel.xaml",
    "WorkspaceOverflow",
    "WorkspaceContentRoot",
    "FamilySearch",
    "PropertySearch",
    "DrawingList",
    "LayerList",
    "Theme.xaml",
    "bounded source/XAML checks only",
    "Licensed in-host BricsCAD V25 runtime remains the authority",
):
    require(token in palette, f"offline palette source contract missing: {token}")

for token in (
    '<UseWPF>true</UseWPF>',
    '<RootNamespace>QS3D.BricsCAD.V25</RootNamespace>',
    '<Page Include="..\\QS3D.BricsCAD.V25\\UI\\**\\*.xaml">',
    '<Link>UI\\%(RecursiveDir)%(Filename)%(Extension)</Link>',
):
    require(token in v26_project, f"V26 shared palette-XAML assumption missing: {token}")

require('test-wpf-palettes-runtime.ps1' in wrapper, "local WPF wrapper must execute source-only palette checks")
require(
    "offline PowerShell must not load BricsCAD native UI dependencies" in wrapper,
    "local WPF wrapper must document the standalone/native-load boundary",
)

wpf_index = qualification.find("run-local-v25-wpf-smoke.ps1")
runtime_index = qualification.find("test-bricscad-v25-runtime.ps1")
require(wpf_index >= 0 and runtime_index >= 0 and wpf_index < runtime_index,
        "offline WPF smoke must remain before the licensed in-host runtime probe")

print(
    "PASS: offline palette qualification uses bounded, root-contained, non-reparse strict-UTF8 XAML ingestion, "
    "remains source-only, and preserves the separate licensed in-host runtime gate."
)
