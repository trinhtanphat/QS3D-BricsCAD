import base64
import hashlib
import re
import struct
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
RIBBON_DIR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon"
GROUP = RIBBON_DIR / "Qs3dRibbonTabGroupCoordinator.cs"
SHELL = RIBBON_DIR / "Blt3dShellChromeCoordinator.cs"
BOOTSTRAP = RIBBON_DIR / "RibbonBootstrapper.cs"
INIT = RIBBON_DIR / "RibbonInitializationCoordinator.cs"
V26_ENTRY = ROOT / "src" / "QS3D.BricsCAD.V26" / "PluginEntry.cs"

CANONICAL_IDS = (
    "QS3D_HOME",
    "QS3D_PROJECT",
    "QS3D_BIM",
    "QS3D_RECOGNIZE",
    "QS3D_DRAW",
    "QS3D_TOOL",
    "QS3D_MODELING",
    "QS3D_VIEW",
    "QS3D_QTY",
    "QS3D_REV",
)

CANONICAL_TITLES = (
    "KHỞI ĐẦU",
    "THIẾT LẬP DỰ ÁN",
    "MÔ HÌNH BIM",
    "NHẬN DẠNG",
    "VẼ",
    "TOOL",
    "MODELING",
    "XEM",
    "ĐỊNH LƯỢNG",
    "BẢN SỬA ĐỔI",
)

REFERENCE_ICO_SHA256 = "630223de090522ad22fea64a530163d28c6ce8b4bdffcdcaa519491e6a6f23d7"
REFERENCE_PNG_SHA256 = "d4229cc06d2804ee3f80bd5d4aef2561b06dbd8bbe2cc01536ee347f682c8032"


def require(text: str, needle: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: BLT3D topbar contract missing: {needle}")


def require_order(text: str, values: tuple[str, ...]) -> None:
    positions = [text.find(f'"{value}"') for value in values]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        raise SystemExit(f"FAIL: BLT3D topbar ordering mismatch: {values}")


def main() -> int:
    group = GROUP.read_text(encoding="utf-8")
    shell = SHELL.read_text(encoding="utf-8")
    bootstrap = BOOTSTRAP.read_text(encoding="utf-8")
    init = INIT.read_text(encoding="utf-8")
    v26_entry = V26_ENTRY.read_text(encoding="utf-8")

    require_order(group, CANONICAL_IDS)
    require(group, 'private const string OwnedTabPrefix = "QS3D_";')
    require(group, "var ownedTabs = snapshot.Where(IsOwnedTab).ToList();")
    require(group, "TryGroupOwnedTabs(tabs, ownedTabs, orderedOwnedTabs)")
    require(group, "RecoverMissingOwnedTabs(tabs, ownedTabs);")
    require(group, "return snapshot.Count(IsOwnedTab) == orderedOwnedTabs.Count;")

    require_order(bootstrap, CANONICAL_TITLES)

    require(init, "Qs3dRibbonTabGroupCoordinator.TryInitialize()")
    require(init, "Blt3dShellChromeCoordinator.TryInitialize()")
    require(init, "Blt3dShellChromeCoordinator.Reset()")
    require(v26_entry, "RibbonInitializationCoordinator.Start();")
    require(v26_entry, "RibbonInitializationCoordinator.Stop();")

    for token in (
        "QuickAccess",
        "ApplicationButton",
        "ApplicationMenuButton",
        "RibbonSearch",
        "SearchBox",
        "InfoCenter",
        "WmSetIcon",
        "LoadEmbeddedIcon",
        "ResolveHostWindowHandle",
    ):
        require(shell, token)

    match = re.search(r'Blt3dIconIcoBase64\s*=\s*\n\s*"([A-Za-z0-9+/=]+)";', shell)
    if not match:
        raise SystemExit("FAIL: embedded BLT3D reference icon is missing")

    ico = base64.b64decode(match.group(1), validate=True)
    if hashlib.sha256(ico).hexdigest() != REFERENCE_ICO_SHA256:
        raise SystemExit("FAIL: embedded BLT3D ICO payload changed")
    if ico[:4] != b"\x00\x00\x01\x00" or len(ico) < 22:
        raise SystemExit("FAIL: embedded BLT3D logo is not a single-image ICO")

    width = ico[6] or 256
    height = ico[7] or 256
    if (width, height) != (16, 16):
        raise SystemExit(f"FAIL: BLT3D ICO directory must remain 16x16, got {width}x{height}")

    payload_length = int.from_bytes(ico[14:18], "little")
    payload_offset = int.from_bytes(ico[18:22], "little")
    png = ico[payload_offset : payload_offset + payload_length]
    if hashlib.sha256(png).hexdigest() != REFERENCE_PNG_SHA256:
        raise SystemExit("FAIL: embedded BLT3D reference pixels changed")
    if png[:8] != b"\x89PNG\r\n\x1a\n":
        raise SystemExit("FAIL: embedded BLT3D ICO does not contain the reference PNG")

    png_width, png_height = struct.unpack(">II", png[16:24])
    if (png_width, png_height) != (16, 16):
        raise SystemExit(
            f"FAIL: BLT3D reference PNG must remain 16x16, got {png_width}x{png_height}"
        )

    print(
        "PASS: BLT3D topbar keeps the canonical QS3D tab order, preserves host tabs, hides host "
        "topbar chrome, uses the reference 16x16 logo, and initializes through the V25/V26 retry path."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
