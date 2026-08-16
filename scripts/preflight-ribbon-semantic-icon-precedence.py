from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonBootstrapIconAugmenter.cs"


def require(text: str, needle: str) -> int:
    index = text.find(needle)
    if index < 0:
        raise SystemExit(f"FAIL: ribbon semantic icon contract missing: {needle}")
    return index


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    opening = require(
        text,
        'ContainsAny(normalized, "AUTO_HOST", "AUTOLINKHOST", "LINKHOST", "CUT_OPENINGS", "OPENING", "LỖ MỞ", "HOST")',
    )
    door = require(text, 'normalized.Contains("DOOR")')
    wall = require(text, 'ContainsAny(normalized, "GLASS_WALL", "_WALL", "TƯỜNG", "VÁCH")')
    structure = require(
        text,
        'ContainsAny(normalized, "CURTAIN", "PIER", "JUNCTION", "BEAM", "SLAB", "COLUMN", "FOUNDATION", "STAIR", "RAILING", "EARTHWORK", "CẦU THANG", "LAN CAN", "ĐÀO ĐẤT", "KẾT CẤU")',
    )
    rebar = require(text, 'ContainsAny(normalized, "REBAR", "BBS", "MESH")')
    generic_draw = require(text, 'ContainsAny(normalized, "_POINT", "_LINE", "_ARC", "_RECTANGLE", "DRAW")')

    for label, semantic in (
        ("opening", opening),
        ("door", door),
        ("wall", wall),
        ("structure", structure),
        ("rebar", rebar),
    ):
        if semantic >= generic_draw:
            raise SystemExit(
                f"FAIL: {label} semantic icon resolution must precede the broad DRAW fallback"
            )

    require(text, 'ContainsAny(normalized, "MEASURE", "_DIST", "DISTANCE", "KHOẢNG CÁCH")')
    require(text, 'ContainsAny(normalized, "QS3DBQ", "_BQ", " BQ", "QUANTITY", "QTY", "BÓC TÁCH")')
    require(text, 'normalized.Contains("_RECTANG")')
    require(text, "Generic drawing is deliberately the last semantic fallback")
    require(text, "return RibbonIconKind.Qs3dLogo;")

    if "return RibbonIconKind.Objects;" in text:
        raise SystemExit("FAIL: generic Objects placeholder must not be the final QS3D ribbon fallback")

    print(
        "PASS: semantic BIM/rebar/measure/quantity/draw icon resolution precedes the broad DRAW "
        "fallback, and unknown QS3D commands use the brand mark instead of the generic Objects glyph."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
