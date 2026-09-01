#!/usr/bin/env python3
"""Source-safe guard for Material Catalog modeless publication and error redaction."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "MaterialCatalogCommands.cs"


def fail(message: str) -> None:
    print("ERROR:", message)
    raise SystemExit(1)


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"{label} missing token: {token}")


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    for token in (
        "private static PublishedManager? _pending;",
        "private static PublishedManager? _published;",
        "pending.Matches(document) && pending.MatchesManagedWrapper(document)",
        "_pending = reserved;",
        "ReferenceEquals(_pending, reserved)",
        "ReferenceEquals(_published, reserved)",
        "if (!window.IsLoaded)",
        "if (!ReferenceEquals(_pending, reserved))",
        "_pending = null;",
        "_published = reserved;",
        'const string message = "QS3DMATERIALS không thể mở Material Catalog an toàn; trạng thái hiện tại được giữ nguyên.";',
    ):
        require(text, token, "MaterialCatalogCommands")

    if "ex.Message" in text:
        fail("MaterialCatalogCommands must not surface raw exception detail")

    reserve_at = text.index("_pending = reserved;")
    show_at = text.index("Application.ShowModelessWindow", reserve_at)
    loaded_at = text.index("if (!window.IsLoaded)", show_at)
    exact_owner_at = text.index("if (!ReferenceEquals(_pending, reserved))", loaded_at)
    pending_clear_at = text.index("_pending = null;", exact_owner_at)
    publish_at = text.index("_published = reserved;", pending_clear_at)
    if not (reserve_at < show_at < loaded_at < exact_owner_at < pending_clear_at < publish_at):
        fail("publication ordering must remain pending reserve -> host show -> loaded -> exact owner -> clear pending -> publish")

    pending_at = text.index("var pending = _pending;")
    published_at = text.index("var previous = _published;", pending_at)
    if pending_at >= published_at:
        fail("pending-owner arbitration must happen before published-owner replacement")

    closed_at = text.index("window.Closed +=")
    closed_pending_at = text.index("ReferenceEquals(_pending, reserved)", closed_at)
    closed_published_at = text.index("ReferenceEquals(_published, reserved)", closed_pending_at)
    show_at = text.index("Application.ShowModelessWindow", closed_published_at)
    if not (closed_at < closed_pending_at < closed_published_at < show_at):
        fail("exact-owner Closed cleanup must be wired before host show")

    print("PASS: Material Catalog publication is pending-first, exact-owner, reentrancy-safe and user-facing failures are redacted")
    return 0


if __name__ == "__main__":
    sys.exit(main())
