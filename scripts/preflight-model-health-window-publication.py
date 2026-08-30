#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/ModelHealthWindow.xaml.cs"
PRESENTER = ROOT / "src/QS3D.BricsCAD.V25/UI/ModelHealthWindowPresenter.cs"
COMMAND_ROOT = ROOT / "src/QS3D.BricsCAD.V25"
errors = []

for path in (WINDOW, PRESENTER):
    if not path.is_file():
        errors.append("missing Model Health publication source: " + str(path.relative_to(ROOT)))

window = WINDOW.read_text(encoding="utf-8") if WINDOW.is_file() else ""
presenter = PRESENTER.read_text(encoding="utf-8") if PRESENTER.is_file() else ""

for token in (
    "private static ModelHealthWindow? _pendingPublication;",
    "private static ModelHealthWindow? _published;",
    "Loaded += OnPublicationLoaded;",
    "Closed += OnPublicationClosed;",
    "ReservePublication(this);",
    "DocumentBoundWindowLifetime.Attach(this, _document);",
    "AbandonPublication(this);",
    'CloseOwnerBeforeReplacement(pending, "pending")',
    'CloseOwnerBeforeReplacement(published, "published")',
    'if (!owner.IsLoaded && string.Equals(state, "published", StringComparison.Ordinal))',
    "owner.Close();",
    "owner.IsLoaded || ReferenceEquals(_pendingPublication, owner) || ReferenceEquals(_published, owner)",
    "if (!ReferenceEquals(_pendingPublication, this))",
    "_pendingPublication = null;",
    "_published = this;",
    "private void OnPublicationClosed(object? sender, EventArgs e) => AbandonPublication(this);",
    "ProjectContextCoordinator.TryGetReadOnly(_document, out var current)",
    "ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, _document)",
):
    if token not in window:
        errors.append("ModelHealthWindow publication contract missing token: " + token)

# Candidate owns no external document-lifetime subscription until it has won reservation.
reserve = window.find("ReservePublication(this);")
attach = window.find("DocumentBoundWindowLifetime.Attach(this, _document);")
if reserve < 0 or attach < 0 or reserve >= attach:
    errors.append("ModelHealthWindow must reserve publication before attaching document lifetime so refused candidates do not leak external subscriptions")

# Loaded/Closed handlers must exist before reservation; constructor setup after reservation must roll ownership back on failure.
loaded_attach = window.find("Loaded += OnPublicationLoaded;")
closed_attach = window.find("Closed += OnPublicationClosed;")
if min(loaded_attach, closed_attach) < 0 or max(loaded_attach, closed_attach) >= reserve:
    errors.append("ModelHealthWindow must attach publication Loaded/Closed handlers before reservation")
constructor_tail = window[reserve:attach + 2000] if reserve >= 0 and attach >= 0 else ""
for token in ("try", "catch", "AbandonPublication(this);", "try { Close(); } catch { }", "throw;"):
    if token not in constructor_tail:
        errors.append("ModelHealthWindow post-reservation setup rollback missing token: " + token)

# Pending ownership is fail-closed: unlike a formerly published stale/unloaded owner it may not be manually cleared merely for IsLoaded=false.
close_method_start = window.find("private static void CloseOwnerBeforeReplacement")
close_method_end = window.find("private static void AbandonPublication", close_method_start)
close_method = window[close_method_start:close_method_end] if close_method_start >= 0 and close_method_end >= 0 else ""
if 'string.Equals(state, "published", StringComparison.Ordinal)' not in close_method:
    errors.append("stale-unloaded defensive release must be restricted to published ownership")
if "owner.Close();" not in close_method:
    errors.append("pending/live owner replacement must attempt terminal Close")

for token in (
    "candidate = new ModelHealthWindow(document, issues, locate);",
    "Application.ShowModelessWindow(IntPtr.Zero, candidate, true);",
    "if (!candidate.IsLoaded)",
    "candidate = null;",
    "finally",
    "try { candidate.Close(); } catch { }",
):
    if token not in presenter:
        errors.append("ModelHealthWindowPresenter host-show rollback contract missing token: " + token)
if "private static ModelHealthWindow?" in presenter:
    errors.append("presenter must not carry a second Model Health ownership registry")
show_at = presenter.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);")
loaded_at = presenter.find("if (!candidate.IsLoaded)", show_at)
release_at = presenter.find("candidate = null;", loaded_at)
if min(show_at, loaded_at, release_at) < 0 or not (show_at < loaded_at < release_at):
    errors.append("presenter ordering must be host show -> loaded confirmation -> relinquish local candidate")

migrated = (
    "HealthAllCommands.cs",
    "RebarHealthCommands.cs",
    "RebarHealthAllCommands.cs",
    "ColumnTieHealthCommands.cs",
    "RebarModeHealthCommands.cs",
    "RoomFinishHealthCommands.cs",
    "ShapeRebarHealthCommands.cs",
    "FoundationMeshHealthCommands.cs",
    "CurtainWallFrameHealthCommands.cs",
    "GeneratedHandleOwnershipHealthCommands.cs",
    "SafeGeneratedHandleOwnershipHealthCommands.cs",
    "ReleaseReadinessCommands.cs",
)
for filename in migrated:
    path = COMMAND_ROOT / filename
    if not path.is_file():
        errors.append("missing migrated Model Health command: " + filename)
        continue
    text = path.read_text(encoding="utf-8")
    if "ModelHealthWindowPresenter.Show(" not in text:
        errors.append(filename + ": must route fresh Model Health snapshot through presenter")
    if "Application.ShowModelessWindow(" in text or "new ModelHealthWindow(" in text:
        errors.append(filename + ": direct Model Health construction/show bypass remains")

# The monolithic base QS3DHEALTH remains an explicit-document legacy caller; window-boundary ownership protects it too.
commands = (COMMAND_ROOT / "Commands.cs").read_text(encoding="utf-8")
for token in (
    '[CommandMethod("QS3DHEALTH", CommandFlags.Modal)]',
    "var window = new ModelHealthWindow(doc, issues, issue =>",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
):
    if token not in commands:
        errors.append("base QS3DHEALTH explicit-document compatibility contract missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Model Health uses one window-owned pending/published lifecycle, fail-closed terminal replacement, rollback-safe host publication, and presenter routing for migrated fresh-snapshot commands.")