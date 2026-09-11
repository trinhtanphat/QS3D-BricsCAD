from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMAND_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DoorOpeningScheduleWindowCommands.cs"
WINDOW_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DoorOpeningScheduleWindow.xaml.cs"

text = COMMAND_SOURCE.read_text(encoding="utf-8")
window = WINDOW_SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "private static PublishedWindow? _pending;",
    "private static PublishedWindow? _published;",
    "nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
    "owner = new PublishedWindow(window, document, nativeDatabaseIdentity);",
    "var releaseOwner = owner;",
    "window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);",
    "_pending = owner;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "if (!ReferenceEquals(_pending, owner))",
    "_pending = null;",
    "_published = owner;",
    "ClosePendingOnFailure(owner);",
    "ReleaseOwnedWindow(owner);",
]
for needle in required:
    if needle not in text:
        errors.append("missing Door/Opening publication contract: " + needle)

if "window.Closed += (_, __) => ReleaseOwnedWindow(owner);" in text:
    errors.append("Door/Opening Closed callback must not capture a mutable local owner")
if "ex.Message" in text:
    errors.append("Door/Opening launcher must not publish native exception details to UI")

show = text.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
reserve = text.find("_pending = owner;")
loaded = text.find("if (!window.IsLoaded)", show)
exact = text.find("if (!ReferenceEquals(_pending, owner))", loaded)
publish = text.find("_published = owner;", exact)
if min(show, reserve, loaded, exact, publish) < 0 or not (reserve < show < loaded < exact < publish):
    errors.append("Door/Opening must reserve pending ownership before host show and publish only after loaded/exact-owner proof")

construct = text.find("var window = new DoorOpeningScheduleWindow(document);")
release_token = text.find("var releaseOwner = owner;", construct)
closed = text.find("window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);", release_token)
reserve_after_construct = text.find("_pending = owner;", closed)
if min(construct, release_token, closed, reserve_after_construct) < 0 or not (construct < release_token < closed < reserve_after_construct):
    errors.append("Door/Opening must bind Closed to an immutable exact-owner token before pending publication")

prepare = text.find("if (!PreparePublishedWindow(document, nativeDatabaseIdentity))")
if prepare < 0 or construct < 0:
    errors.append("Door/Opening admission/construction boundary is missing")
else:
    after_prepare = text.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;", prepare, construct)
    if after_prepare < 0:
        errors.append("Door/Opening must revalidate exact document generation after destructive owner preparation and before construction")

if show >= 0 and publish >= 0:
    after_show = text.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))", show, publish)
    if after_show < 0:
        errors.append("Door/Opening must revalidate exact document generation after host show and before publication")

if "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)" not in text:
    errors.append("Door/Opening exact document-generation predicate must include managed active-document identity")
if "database.UnmanagedObject == nativeDatabaseIdentity" not in text:
    errors.append("Door/Opening exact document-generation predicate must include native database identity")

window_required = [
    "private readonly IntPtr _nativeDatabaseIdentity;",
    "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(_document);",
    "private bool IsBoundActiveDocumentGeneration()",
    "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)",
    "database.UnmanagedObject == _nativeDatabaseIdentity",
    "if (!IsBoundActiveDocumentGeneration())",
]
for needle in window_required:
    if needle not in window:
        errors.append("missing bound Door/Opening window generation contract: " + needle)
if "ex.Message" in window:
    errors.append("Door/Opening window must not publish exception details to local/global UI")

set_status = window.find("private void SetStatus(string text)")
if set_status < 0:
    errors.append("Door/Opening window status helper is missing")
else:
    global_status = window.find("PaletteCoordinator.SetStatus(StatusText.Text)", set_status)
    gate = window.rfind("IsBoundActiveDocumentGeneration()", set_status, global_status)
    if global_status < 0 or gate < set_status:
        errors.append("Door/Opening global Palette status must be gated to the exact bound active document/native generation")

print("QS3D V25 Door/Opening Schedule modeless publication affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: Door/Opening launcher/window use exact document/native-generation affinity, stable ownership, residue-safe cleanup, and redacted UI failures.")
