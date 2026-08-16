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

    opening = require(text, 'ContainsAny(normalized, "AUTO_HOST", "CUT_OPENINGS", "OPENING", "LỖ MỞ")')
    door = require(text, 'normalized.Contains("DOOR")')
    wall = require(text, 'ContainsAny(normalized, "GLASS_WALL", "_WALL", "TƯỜNG", "VÁCH")')
    structure = require(text, 'ContainsAny(normalized, "CURTAIN", "PIER", "JUNCTION", "BEAM", "SLAB", "COLUMN", "FOUNDATION", "KẾT CẤU")')
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

    require(text, "Generic drawing is deliberately the last semantic fallback")
    print(
        "PASS: semantic BIM/rebar icon resolution precedes the broad DRAW fallback, so commands "
        "such as DRAW_WALL, DRAW_DOOR and DRAW_REBAR keep domain-specific QS3D icons."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
