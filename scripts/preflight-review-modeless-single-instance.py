#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
source = COMMAND.read_text(encoding="utf-8") if COMMAND.exists() else ""
errors = []


def require(condition: bool, message: str) -> None:
    if not condition:
        errors.append(message)


for token in (
    "private static PublishedReviewWindow? _bbsPublished;",
    "private static PublishedReviewWindow? _recognitionPublished;",
    "private static PublishedReviewWindow? _revisionPublished;",
    "private readonly WeakReference<Document> _document;",
    "public IntPtr NativeDatabaseIdentity { get; }",
    "database.UnmanagedObject == NativeDatabaseIdentity",
    "ReferenceEquals(ownedDocument, document)",
    "private static bool ReuseOrClosePublished",
    "private static void ShowAndPublish",
):
    require(token in source, "review publication contract missing token: " + token)

require("public Document Document { get; }" not in source,
        "static review publication must not strongly retain a managed Document wrapper")

for surface, label in (
    ("ReviewSurface.Bbs", "BBS"),
    ("ReviewSurface.Recognition", "Recognition"),
    ("ReviewSurface.Revision", "Revision"),
):
    require("ReuseOrClosePublished(" + surface in source,
            label + " must arbitrate the existing modeless owner before constructing a replacement")
    require("ShowAndPublish(" + surface in source,
            label + " must publish through the shared transactional host-show path")

reuse_native = source.find("previous.MatchesNativeDatabase(document)")
reuse_wrapper = source.find("previous.MatchesManagedWrapper(document)")
activate = source.find("previous.Window.Activate();")
close = source.find("previous.Window.Close();")
retained = source.find("ReferenceEquals(GetPublished(surface), previous)", close if close >= 0 else 0)
require(min(reuse_native, reuse_wrapper, activate, close, retained) >= 0,
        "review close arbitration structure is incomplete")
require(reuse_native < reuse_wrapper < activate < close < retained,
        "exact native+wrapper reuse must precede fail-closed terminal replacement arbitration")
require("try { previous.Window.Close(); } catch { }" not in source,
        "replacement close failures must not be swallowed")

closed = source.find("candidate.Closed += (_, __) =>")
show = source.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);")
loaded = source.find("if (!candidate.IsLoaded)", show if show >= 0 else 0)
publish = source.find("SetPublished(surface, published);", loaded if loaded >= 0 else 0)
require(min(closed, show, loaded, publish) >= 0,
        "transactional show/loaded/publication structure is incomplete")
require(closed < show < loaded < publish,
        "Closed handler must attach before host show and publication must occur only after loaded confirmation")
require("if (ReferenceEquals(GetPublished(surface), published))" in source and
        "SetPublished(surface, null);" in source,
        "only the matching terminal Closed callback may release a live review publication")

for old_call in (
    "Application.ShowModelessWindow(IntPtr.Zero, new RebarScheduleWindow",
    "Application.ShowModelessWindow(IntPtr.Zero, new RecognitionWindow",
    "Application.ShowModelessWindow(IntPtr.Zero, new RevisionWindow",
):
    require(old_call not in source, "direct duplicate-prone review show remains: " + old_call)

for candidate_type in ("RebarScheduleWindow? candidate", "RecognitionWindow? candidate", "RevisionWindow? candidate"):
    require(candidate_type in source, "failed candidate cleanup owner missing: " + candidate_type)
require(source.count("try { candidate.Close(); } catch { }") >= 3,
        "each review surface must best-effort close an unpublished failed candidate")

# Product semantics that must survive the lifecycle fix.
for token in (
    "ProjectStateSnapshot.CreateDetachedCopy(project)",
    "ProjectContextCoordinator.TryGetReadOnly(doc, out var project)",
    "RecognitionApplyBatchService.PrepareStrict",
    "RecognitionApplyBatchService.Commit(doc, reviewProjectId, plan)",
    "ProjectContextCoordinator.TryGetReadOnly(doc, out _)",
    "LocateCurrentElement(doc, row.ElementId, \"Revision Locate\")",
):
    require(token in source, "existing review safety/product semantic was lost: " + token)

if errors:
    print("Review modeless single-instance preflight FAILED:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print("PASS review modeless surfaces are single-instance per exact native+managed document owner with fail-closed replacement")
