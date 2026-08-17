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

    forbidden_palette_tokens = (
        "Assembly]::LoadFrom",
        "AssemblyResolve",
        "add_AssemblyResolve",
        "Activator]::CreateInstance",
        "LoadFile(",
        "LoadFrom(",
        "Start-Process",
        "System.Diagnostics.Process",
        "PresentationFramework",
        "PresentationCore",
        "WindowsBase",
        "System.Xaml",
        "System.Windows.Controls",
        "System.Windows.UIElement",
        "Get-Content -LiteralPath $Path -Raw",
    )
    for token in forbidden_palette_tokens:
        if token in source:
            errors.append(f"offline palette smoke must not contain hosted/unbounded source primitive: {token}")

    required_source_contracts = (
        "WorkspacePanel.xaml",
        "RightPanel.xaml",
        "Read-XamlDocument",
        "$maximumXamlBytes = 1MB",
        "[System.IO.Path]::GetFullPath",
        "StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)",
        "Palette XAML source escaped the canonical UI root",
        "Test-Path -LiteralPath $fullPath -PathType Leaf",
        "Get-Item -LiteralPath $fullPath -Force",
        "[System.IO.FileAttributes]::ReparsePoint",
        "Palette XAML source must not be a reparse point",
        "$sourceFile.Length -gt $maximumXamlBytes",
        "Palette XAML source exceeds the $maximumXamlBytes-byte offline qualification limit",
        "[System.Text.UTF8Encoding]::new($false, $true)",
        "[System.IO.File]::ReadAllText($fullPath, $strictUtf8)",
        "Palette XAML is not valid strict UTF-8 / well-formed XML",
        "WorkspaceOverflow",
        "WorkspaceContentRoot",
        "FamilySearch",
        "PropertySearch",
        "DrawingList",
        "LayerList",
        "Theme.xaml",
        "bounded strict source/XAML checks only",
        "Licensed in-host BricsCAD V25 runtime remains the authority",
    )
    for token in required_source_contracts:
        if token not in source:
            errors.append(f"offline palette source/failure contract missing: {token}")

    ordering_contracts = (
        (
            "StartsWith($rootPrefix",
            "Test-Path -LiteralPath $fullPath -PathType Leaf",
            "canonical-root containment must be checked before source existence/read",
        ),
        (
            "[System.IO.FileAttributes]::ReparsePoint",
            "$sourceFile.Length -gt $maximumXamlBytes",
            "reparse-point rejection must precede source materialization/size acceptance",
        ),
        (
            "$sourceFile.Length -gt $maximumXamlBytes",
            "[System.IO.File]::ReadAllText($fullPath, $strictUtf8)",
            "source size must be bounded before materialization",
        ),
    )
    for first, second, message in ordering_contracts:
        first_index = source.find(first)
        second_index = source.find(second)
        if first_index < 0 or second_index < 0 or first_index >= second_index:
            errors.append(message)

    return errors


def require_palette_contract(source: str) -> None:
    errors = palette_contract_errors(source)
    require(not errors, errors[0] if errors else "offline palette source contract failed")


def require_mutation_rejected(source: str, old: str, new: str, description: str) -> None:
    require(old in source, f"mutation fixture missing source token: {description}")
    mutated = source.replace(old, new, 1)
    require(bool(palette_contract_errors(mutated)), f"mutation unexpectedly survived offline palette guard: {description}")


palette = read(PALETTE)
wrapper = read(WRAPPER)
qualification = read(QUALIFICATION)
v26_project = read(V26_PROJECT)

# This script executes in standalone PowerShell. It must remain incapable of
# constructing WPF controls, constructing the hosted plugin, resolving BricsCAD
# managed/native UI dependencies, or materializing unbounded/redirected XAML.
require_palette_contract(palette)

for old, new, description in (
    ("$maximumXamlBytes = 1MB", "$maximumXamlBytes = [int]::MaxValue", "remove bounded XAML source ceiling"),
    ("[System.IO.FileAttributes]::ReparsePoint", "[System.IO.FileAttributes]::Hidden", "remove reparse-point rejection"),
    ("[System.Text.UTF8Encoding]::new($false, $true)", "[System.Text.UTF8Encoding]::new($false, $false)", "disable strict UTF-8 decoding"),
    ("StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)", "StartsWith('', [System.StringComparison]::OrdinalIgnoreCase)", "remove canonical-root containment"),
    ("[System.IO.File]::ReadAllText($fullPath, $strictUtf8)", "Get-Content -LiteralPath $Path -Raw -Encoding UTF8", "restore unbounded Get-Content materialization"),
):
    require_mutation_rejected(palette, old, new, description)

# V26 deliberately links the V25 UI XAML as its shared source of truth. Keep the
# offline parser pointed at that canonical source and fail CI if V26 silently
# switches to a different palette-XAML tree without updating qualification.
for token in (
    '<UseWPF>true</UseWPF>',
    '<RootNamespace>QS3D.BricsCAD.V25</RootNamespace>',
    '<Page Include="..\\QS3D.BricsCAD.V25\\UI\\**\\*.xaml">',
    '<Link>UI\\%(RecursiveDir)%(Filename)%(Extension)</Link>',
):
    require(token in v26_project, f"V26 shared palette-XAML assumption missing: {token}")

require(
    'test-wpf-palettes-runtime.ps1' in wrapper,
    "local WPF wrapper must execute the source-only palette contract",
)
for token in (
    "Assembly]::LoadFrom",
    "add_AssemblyResolve",
    "Activator]::CreateInstance",
    "Start-Process",
    "System.Diagnostics.Process",
):
    require(token not in wrapper, f"local WPF wrapper must not load/construct hosted UI directly: {token}")
require(
    "offline PowerShell must not load BricsCAD native UI dependencies" in wrapper,
    "local WPF wrapper must document the standalone/native-load boundary",
)

wpf_index = qualification.find('run-local-v25-wpf-smoke.ps1')
runtime_index = qualification.find('test-bricscad-v25-runtime.ps1')
require(wpf_index >= 0, "aggregate V25 qualification must retain the offline WPF/source smoke")
require(runtime_index >= 0, "aggregate V25 qualification must retain the licensed in-host runtime probe")
require(
    wpf_index < runtime_index,
    "offline source smoke must remain an early detector before licensed in-host runtime qualification",
)

print(
    "PASS: offline palette smoke remains source/XAML-only with bounded repository-contained strict-UTF8 input, "
    "deterministic missing/malformed/reparse diagnostics, mutation coverage, shared V26 XAML, and the separate licensed in-host runtime gate."
)
