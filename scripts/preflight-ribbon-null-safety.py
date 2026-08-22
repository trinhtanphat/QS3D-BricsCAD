#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(rel: str) -> str:
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text: str, needle: str, rel: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing nullable-safety contract: {needle}")


def forbid(text: str, needle: str, rel: str) -> None:
    if needle in text:
        raise SystemExit(f"FAIL: {rel} suppresses nullable diagnostics instead of fixing them: {needle}")


def main() -> int:
    mirror_rel = "src/QS3D.BricsCAD.V25/Ribbon/BltBimRibbonMirrorAugmenter.cs"
    draw_rel = "src/QS3D.BricsCAD.V25/Ribbon/BltDrawRibbonLayoutRefiner.cs"
    topbar_rel = "src/QS3D.BricsCAD.V25/Ribbon/BltTopbarTabContract.cs"
    workspace_rel = "src/QS3D.BricsCAD.V25/Ribbon/BltBimWorkspaceActivationCoordinator.cs"

    sources = {
        mirror_rel: read(mirror_rel),
        draw_rel: read(draw_rel),
        topbar_rel: read(topbar_rel),
        workspace_rel: read(workspace_rel),
    }
    mirror = sources[mirror_rel]
    draw = sources[draw_rel]
    topbar = sources[topbar_rel]
    workspace = sources[workspace_rel]

    # These explicit null branches are required because the V25 compile-reference lane does not
    # provide the modern nullable flow annotations for every framework helper. Keep the source
    # semantically safe instead of suppressing CS8602/CS8603/CS8604 warnings.
    require(mirror, 'if (sourceId == null || string.IsNullOrWhiteSpace(sourceId))', mirror_rel)
    require(mirror, 'if (id != null && !string.IsNullOrWhiteSpace(id))', mirror_rel)

    require(draw, 'if (id == null || string.IsNullOrWhiteSpace(id))', draw_rel)
    require(draw, 'if (result.ContainsKey(id))', draw_rel)

    require(topbar, 'if (id == null || string.IsNullOrWhiteSpace(id))', topbar_rel)
    require(topbar, 'if (id.StartsWith(OwnedPrefix, StringComparison.OrdinalIgnoreCase)', topbar_rel)

    # Tab-id resolution must stay non-null on every path so the polling coordinator can compile
    # cleanly under the V25 nullable-as-errors build while remaining presentation-only.
    require(workspace, 'private static string TabId(object? tab)', workspace_rel)
    require(workspace, 'if (tab == null) return string.Empty;', workspace_rel)
    require(workspace, '?? GetProperty(tab, "Name") as string ?? string.Empty;', workspace_rel)

    # Do not hide this class of regression with compiler switches. CI should keep catching future
    # unsafe changes at the exact source line, not accept a broad nullable opt-out.
    for rel, text in sources.items():
        forbid(text, '#nullable disable', rel)
        forbid(text, '#pragma warning disable CS860', rel)
        forbid(text, '<NoWarn>$(NoWarn);CS860', rel)

    print(
        "PASS: BLT3D ribbon mirror/layout/topbar/workspace null-state guards remain explicit for "
        "the BricsCAD V25 nullable-as-errors compile lane, with nullable suppression forbidden."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
