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
    reserve = source.find("_pending = reserved;")
    closed = source.find("window.Closed += (_, __) =>", reserve)
    closed_pending_release = source.find("if (ReferenceEquals(_pending, reserved)) _pending = null;", closed)
    closed_published_release = source.find("if (ReferenceEquals(_published, reserved)) _published = null;", closed_pending_release)
    show = source.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);", closed)
    loaded = source.find("if (!window.IsLoaded)", show)
    failure = source.find("host show returned without a loaded window.", loaded)
    exact_owner = source.find("if (!ReferenceEquals(_pending, reserved))", failure)
    clear_pending = source.find("_pending = null;", exact_owner)
    publish = source.find("_published = reserved;", clear_pending)
    release_candidate = source.find("candidate = null;", publish)
    release_window = source.find("window = null;", release_candidate)

    required = [
        reserve,
        closed,
        closed_pending_release,
        closed_published_release,
        show,
        loaded,
        failure,
        exact_owner,
        clear_pending,
        publish,
        release_candidate,
        release_window,
    ]
    if min(required) < 0:
        errors.append(f"{label} pending-first loaded-publication tokens are incomplete")
        continue

    if not (
        reserve < closed < closed_pending_release < closed_published_release < show < loaded < failure <
        exact_owner < clear_pending < publish < release_candidate < release_window
    ):
        errors.append(
            f"{label} must reserve pending ownership, install exact Closed release, show, require Loaded + exact owner, then publish and release local ownership"
        )

    if source.find("_published = reserved;", show, loaded) >= 0:
        errors.append(f"{label} publishes before Loaded admission")
    if source.find("_pending = reserved;", show) >= 0:
        errors.append(f"{label} reserves pending ownership after host show")

    old_close = source.find("previous.Window.Close();")
    retained = source.find("if (ReferenceEquals(_published, previous))", old_close)
    if min(old_close, retained) < 0 or old_close > retained:
        errors.append(f"{label} must preserve terminal-close/veto arbitration before replacement")

    generation_call = "IsActiveDocumentGeneration(document, nativeDatabaseIdentity)"
    affinity_before_show = source.rfind(generation_call, reserve, show)
    affinity_after_show = source.find(generation_call, show, loaded)
    if affinity_before_show < reserve:
        errors.append(f"{label} must revalidate exact active document generation after reservation and before host show")
    if affinity_after_show < show:
        errors.append(f"{label} must revalidate exact active document generation after host show before Loaded/publication admission")

material = CASES["Material Catalog"].read_text(encoding="utf-8") if CASES["Material Catalog"].is_file() else ""
if "ExistingProjectMutationContext.TryGet(document, out var project)" not in material:
    errors.append("Material Catalog must retain existing-project admission")
if "new MaterialCatalogWindow(document, project)" not in material:
    errors.append("Material Catalog must retain the exact admitted project binding")
if "catch (Exception ex)" in material or "ex.Message" in material:
    errors.append("Material Catalog must not expose caught host exception details")
if "CloseCandidateOnAffinityDrift(reserved);" not in material:
    errors.append("Material Catalog must close its exact pending candidate on document-affinity drift")

project_tools = CASES["Project Tools"].read_text(encoding="utf-8") if CASES["Project Tools"].is_file() else ""
if "new ProjectToolsWindow(document)" not in project_tools:
    errors.append("Project Tools must retain exact source-document construction")
if "ClosePendingOnAffinityDrift(reserved);" not in project_tools:
    errors.append("Project Tools must close its exact pending candidate on document-affinity drift")

if errors:
    for error in errors:
        print("ERROR:", error)
    raise SystemExit(f"FAILED with {len(errors)} manager loaded-publication error(s).")

print("PASS Material Catalog and Project Tools pending-first Loaded/exact-owner host-show admission with exact active-document generation fences")
