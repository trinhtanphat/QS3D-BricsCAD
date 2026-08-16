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


palette = read(PALETTE)
wrapper = read(WRAPPER)
qualification = read(QUALIFICATION)
v26_project = read(V26_PROJECT)

# This script executes in standalone PowerShell. It must remain incapable of
# constructing WPF controls, constructing the hosted plugin, or resolving
# BricsCAD managed/native UI dependencies.
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
)
for token in forbidden_palette_tokens:
    require(token not in palette, f"offline palette smoke must not contain hosted/native UI load primitive: {token}")

required_source_contracts = (
    "WorkspacePanel.xaml",
    "RightPanel.xaml",
    "Read-XamlDocument",
    "Test-Path -LiteralPath $Path -PathType Leaf",
    "Get-Content -LiteralPath $Path -Raw -Encoding UTF8",
    "Required palette XAML source is missing",
    "Palette XAML is not well-formed XML",
    "WorkspaceOverflow",
    "WorkspaceContentRoot",
    "FamilySearch",
    "PropertySearch",
    "DrawingList",
    "LayerList",
    "Theme.xaml",
    "source/XAML checks only",
    "Licensed in-host BricsCAD V25 runtime remains the authority",
)
for token in required_source_contracts:
    require(token in palette, f"offline palette source/failure contract missing: {token}")

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
    "PASS: offline palette smoke remains source/XAML-only with deterministic missing/malformed-file diagnostics, "
    "V26 still consumes the same canonical palette XAML, and aggregate qualification retains the separate licensed in-host runtime gate."
)
