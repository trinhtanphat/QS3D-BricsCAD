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
    "private static PublishedReviewWindow? _bbsPending;",
    "private static PublishedReviewWindow? _recognitionPending;",
    "private static PublishedReviewWindow? _revisionPending;",
    "private static PublishedReviewWindow? _bbsPublished;",
    "private static PublishedReviewWindow? _recognitionPublished;",
    "private static PublishedReviewWindow? _revisionPublished;",
    "private readonly WeakReference<Document> _document;",
    "public IntPtr NativeDatabaseIdentity { get; }",
    "database.UnmanagedObject == NativeDatabaseIdentity",
    "ReferenceEquals(ownedDocument, document)",
    "private static PublishedReviewWindow? GetPending",
    "private static void SetPending",
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
            label + " must arbitrate pending/published modeless ownership before constructing a replacement")
    require("ShowAndPublish(" + surface in source,
            label + " must publish through the shared pending-first transactional host-show path")

pending_lookup = source.find("var pending = GetPending(surface);")
pending_native = source.find("pending.MatchesNativeDatabase(document)", pending_lookup)
pending_wrapper = source.find("pending.MatchesManagedWrapper(document)", pending_native)
pending_block = source.find("không mở instance thứ hai", pending_wrapper)
published_lookup = source.find("var previous = GetPublished(surface);", pending_block)
require(min(pending_lookup, pending_native, pending_wrapper, pending_block, published_lookup) >= 0,
        "pending-owner arbitration structure is incomplete")
require(pending_lookup < pending_native < pending_wrapper < pending_block < published_lookup,
        "pending exact native+wrapper reuse/fail-closed arbitration must happen before published replacement handling")

reuse_native = source.find("previous.MatchesNativeDatabase(document)")
reuse_wrapper = source.find("previous.MatchesManagedWrapper(document)")
activate = source.find("previous.Window.Activate();")
close = source.find("previous.Window.Close();")
retained = source.find("ReferenceEquals(GetPublished(surface), previous)", close if close >= 0 else 0)
require(min(reuse_native, reuse_wrapper, activate, close, retained) >= 0,
        "published review close arbitration structure is incomplete")
require(reuse_native < reuse_wrapper < activate < close < retained,
        "exact native+wrapper reuse must precede fail-closed terminal replacement arbitration")
require("try { previous.Window.Close(); } catch { }" not in source,
        "replacement close failures must not be swallowed")

owner = source.find("var owner = new PublishedReviewWindow(candidate, document);")
reserve = source.find("SetPending(surface, owner);", owner)
closed = source.find("candidate.Closed += (_, __) =>", reserve)
show = source.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);", closed)
loaded = source.find("if (!candidate.IsLoaded)", show)
owner_check = source.find("ReferenceEquals(GetPending(surface), owner)", loaded)
publish = source.find("SetPublished(surface, owner);", owner_check)
release_pending = source.find("SetPending(surface, null);", publish)
require(min(owner, reserve, closed, show, loaded, owner_check, publish, release_pending) >= 0,
        "pending-first show/loaded/publication structure is incomplete")
require(owner < reserve < closed < show < loaded < owner_check < publish < release_pending,
        "review owner must reserve pending before host show and promote only after loaded + exact-owner confirmation")
require(source.count("if (ReferenceEquals(GetPending(surface), owner))") >= 2,
        "matching pending owner must be released from Closed/failure paths")
require(source.count("if (ReferenceEquals(GetPublished(surface), owner))") >= 2,
        "matching published owner must be released from Closed/failure paths")

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

print("PASS review modeless surfaces reserve exact pending ownership before host show and promote loaded-only without duplicate publication")
