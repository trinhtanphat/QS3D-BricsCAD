#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CASES = {
    "Material Catalog": ROOT / "src/QS3D.BricsCAD.V25/MaterialCatalogCommands.cs",
    "Project Tools": ROOT / "src/QS3D.BricsCAD.V25/ProjectToolsCommands.cs",
}
errors = []

for label, path in CASES.items():
    if not path.is_file():
        errors.append(f"missing {label} publisher: {path.relative_to(ROOT)}")
        continue

    source = path.read_text(encoding="utf-8")
    show = source.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
    loaded = source.find("if (!window.IsLoaded)", show)
    failure = source.find("host show returned without a loaded window.", loaded)
    publish = source.find("_published = published;", failure)
    release_local = source.find("window = null;", publish)
    cleanup = source.find("if (window != null)", release_local)
    cleanup_close = source.find("try { window.Close(); } catch { }", cleanup)

    if min(show, loaded, failure, publish, release_local, cleanup, cleanup_close) < 0:
        errors.append(f"{label} loaded-publication tokens are incomplete")
        continue

    if not (show < loaded < failure < publish < release_local < cleanup < cleanup_close):
        errors.append(
            f"{label} must show, reject non-Loaded host return, publish, release local cleanup ownership, then preserve catch cleanup"
        )

    if source.find("_published = published;", show, loaded) >= 0:
        errors.append(f"{label} publishes before Loaded admission")

    closed = source.find("window.Closed += (_, __) =>")
    exact_release = source.find("if (ReferenceEquals(_published, published)) _published = null;", closed)
    if min(closed, exact_release) < 0 or closed > show:
        errors.append(f"{label} must install exact-instance Closed release before host show")

    old_close = source.find("previous.Window.Close();")
    retained = source.find("if (ReferenceEquals(_published, previous))", old_close)
    if min(old_close, retained) < 0 or old_close > retained:
        errors.append(f"{label} must preserve terminal-close/veto arbitration before replacement")

material = CASES["Material Catalog"].read_text(encoding="utf-8") if CASES["Material Catalog"].is_file() else ""
if "ExistingProjectMutationContext.TryGet(document, out var project)" not in material:
    errors.append("Material Catalog must retain existing-project admission")
if "new MaterialCatalogWindow(document, project)" not in material:
    errors.append("Material Catalog must retain the exact admitted project binding")

project_tools = CASES["Project Tools"].read_text(encoding="utf-8") if CASES["Project Tools"].is_file() else ""
if "new ProjectToolsWindow(document)" not in project_tools:
    errors.append("Project Tools must retain exact source-document construction")

if errors:
    for error in errors:
        print("ERROR:", error)
    raise SystemExit(f"FAILED with {len(errors)} manager loaded-publication error(s).")

print("PASS Material Catalog and Project Tools require Loaded host-show admission before authoritative publication")
