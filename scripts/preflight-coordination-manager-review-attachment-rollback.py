#!/usr/bin/env python3
from pathlib import Path
import sys

# issue-4398 + issue-4699 + issue-5698: deterministic source guard for
# transactional Coordination Manager modeless publication, rollback, and
# exact instance/native-document ownership.
ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/CoordinationManagerCommands.cs"

errors = []

if not COMMAND.exists():
    errors.append(f"missing required file: {COMMAND.relative_to(ROOT)}")
    source = ""
else:
    source = COMMAND.read_text(encoding="utf-8")

required = [
    ("CoordinationManagerWindow? candidate = null;", "local unpublished candidate ownership"),
    ("PublishedManager? published = null;", "exact candidate rollback ownership"),
    ("var previous = _published;", "capture prior published manager ownership"),
    ("candidate = new CoordinationManagerWindow", "construct unpublished candidate"),
    ("var exactPublished = new PublishedManager(", "atomic window/native/project ownership candidate"),
    ("public IntPtr NativeDatabaseIdentity { get; }", "stable native database affinity"),
    ("public string ProjectId { get; }", "canonical project affinity"),
    ("public string DrawingFingerprint { get; }", "canonical drawing affinity"),
    ("public bool Matches(Document document, string projectId, string drawingFingerprint)", "live wrapper plus semantic affinity comparison"),
    ("_publicationInFlight = exactPublished;", "reserve unpublished singleton ownership"),
    ("_nativePublicationCallActive = true;", "native publication-stack reentrancy fence"),
    ("CoordinationManagerReviewUi.Attach(", "review attach under exact publication reservation"),
    ("Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);", "host show under exact reservation"),
    ("if (!publishedWindow.IsLoaded)", "terminal loaded-state publication check"),
    ("ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)", "semantic identity revalidation after host show"),
    ("_published = exactPublished;", "publish only the exact successfully shown manager"),
    ("_publicationInFlight = null;", "release unpublished ownership only after published ownership exists"),
    ("candidate = null;", "transfer local ownership after publication"),
    ("publishedWindow.Closed += (_, __) => ReleaseClosedManager(exactPublished);", "instance-safe terminal Closed cleanup"),
    ("TryCloseManager(published);", "failed publication exact-candidate rollback"),
    ("private static bool TryCloseManager(PublishedManager manager)", "single exact rollback/close helper"),
    ("manager.Window.Close();", "terminal cleanup close attempt"),
    ("ReleaseTerminalManager(manager);", "terminal ownership cleanup"),
]

for needle, label in required:
    if needle not in source:
        errors.append(f"missing {label}: {needle}")

if "public Document Document { get; }" in source:
    errors.append("published modeless owner must not retain a managed Document wrapper across lifetime")

construct_at = source.find("candidate = new CoordinationManagerWindow")
ownership_at = source.find("var exactPublished = new PublishedManager(", construct_at)
closed_at = source.find("publishedWindow.Closed += (_, __) => ReleaseClosedManager(exactPublished);", ownership_at)
reserve_at = source.find("_publicationInFlight = exactPublished;", closed_at)
native_fence_at = source.find("_nativePublicationCallActive = true;", reserve_at)
attach_at = source.find("CoordinationManagerReviewUi.Attach(", native_fence_at)
show_at = source.find("Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);", attach_at)
loaded_at = source.find("if (!publishedWindow.IsLoaded)", show_at)
semantic_at = source.find("ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)", loaded_at)
publish_at = source.find("_published = exactPublished;", semantic_at)
release_reservation_at = source.find("_publicationInFlight = null;", publish_at)
transfer_at = source.find("candidate = null;", release_reservation_at)
if min(
    construct_at,
    ownership_at,
    closed_at,
    reserve_at,
    native_fence_at,
    attach_at,
    show_at,
    loaded_at,
    semantic_at,
    publish_at,
    release_reservation_at,
    transfer_at,
) < 0 or not (
    construct_at
    < ownership_at
    < closed_at
    < reserve_at
    < native_fence_at
    < attach_at
    < show_at
    < loaded_at
    < semantic_at
    < publish_at
    < release_reservation_at
    < transfer_at
):
    errors.append(
        "candidate lifecycle must be construct -> exact owner -> Closed binding -> in-flight reserve -> native fence -> review attach -> host show -> terminal check -> semantic revalidation -> publish -> release reservation -> local transfer"
    )

close_start = source.find("private static bool TryCloseManager(PublishedManager manager)")
close_end = source.find("private static void ReleaseClosedManager", close_start)
if close_start < 0 or close_end < 0:
    errors.append("unable to isolate exact manager rollback helper")
else:
    cleanup = source[close_start:close_end]
    close_call = cleanup.find("manager.Window.Close();")
    loaded_check = cleanup.find("if (manager.Window.IsLoaded)")
    terminal_release = cleanup.find("ReleaseTerminalManager(manager);")
    cleanup_reserve = cleanup.find("_cleanupInFlight = manager;")
    cleanup_release = cleanup.find("if (ReferenceEquals(_cleanupInFlight, manager))")
    if min(close_call, loaded_check, terminal_release, cleanup_reserve, cleanup_release) < 0:
        errors.append("exact rollback helper is missing close/terminal/cleanup-reservation evidence")
    elif not cleanup_reserve < close_call < loaded_check < terminal_release < cleanup_release:
        errors.append(
            "rollback must hold exact cleanup ownership across Close, prove terminal non-loaded state, release exact publication ownership, then drop cleanup reservation"
        )

legacy = [
    "_window = new CoordinationManagerWindow",
    "_window = null;",
    "try { previous.Close(); } catch { }",
    "ReferenceEquals(previous.Document, document)",
    "var published = new PublishedManager(publishedWindow, document);",
    "public bool Matches(Document document)",
    "Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);\n                _published = published;",
]
for token in legacy:
    if token in source:
        errors.append("legacy unsafe publication/affinity pattern must not return: " + token)

if errors:
    print("ERROR: Coordination Manager review attachment rollback source guard failed:")
    for error in errors:
        print(" - " + error)
    sys.exit(1)

print(
    "PASS: Coordination Manager reserves exact publication/cleanup ownership before review/native show, revalidates identity before commit, and rolls failed candidates back without singleton gaps."
)
print("NOTE: this is deterministic source evidence only; licensed BricsCAD modeless behavior is not claimed.")
