from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMAND_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ScheduleHubCommands.cs"
WINDOW_SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ScheduleHubWindow.xaml.cs"

text = COMMAND_SOURCE.read_text(encoding="utf-8")
window = WINDOW_SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "private static PublishedManager? _pending;",
    "private static PublishedManager? _published;",
    "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
    "var owner = new PublishedManager(window, document, nativeDatabaseIdentity);",
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
        errors.append("missing Schedule Hub publication contract: " + needle)

if "window.Closed += (_, __) => ReleaseOwnedWindow(owner);" in text:
    errors.append("Schedule Hub Closed callback must not capture the mutable local owner that is nulled after publication")
if "ex.Message" in text:
    errors.append("Schedule Hub launcher must not publish native exception details to UI")

show = text.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
reserve = text.find("_pending = owner;")
loaded = text.find("if (!window.IsLoaded)", show)
exact = text.find("if (!ReferenceEquals(_pending, owner))", loaded)
publish = text.find("_published = owner;", exact)
if min(show, reserve, loaded, exact, publish) < 0 or not (reserve < show < loaded < exact < publish):
    errors.append("Schedule Hub must reserve pending ownership before host show and publish only after loaded/exact-owner proof")

construct = text.find("var window = new ScheduleHubWindow(document);")
release_token = text.find("var releaseOwner = owner;", construct)
closed = text.find("window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);", release_token)
reserve_after_construct = text.find("_pending = owner;", closed)
if min(construct, release_token, closed, reserve_after_construct) < 0 or not (construct < release_token < closed < reserve_after_construct):
    errors.append("Schedule Hub must bind Closed to an immutable exact-owner token before pending publication")

prepare = text.find("if (!PreparePublishedWindow(document, nativeDatabaseIdentity))")
if prepare < 0 or construct < 0:
    errors.append("Schedule Hub admission/construction boundary is missing")
else:
    after_prepare = text.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;", prepare, construct)
    if after_prepare < 0:
        errors.append("Schedule Hub must revalidate exact document generation after destructive published-owner preparation and before construction")

if show >= 0 and publish >= 0:
    after_show = text.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))", show, publish)
    if after_show < 0:
        errors.append("Schedule Hub must revalidate exact document generation after host show and before publication")

if "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)" not in text:
    errors.append("Schedule Hub exact document-generation predicate must include managed active-document identity")
if "database.UnmanagedObject == nativeDatabaseIdentity" not in text:
    errors.append("Schedule Hub exact document-generation predicate must include native database identity")

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
        errors.append("missing bound Schedule Hub window generation contract: " + needle)
if "ex.Message" in window:
    errors.append("Schedule Hub window must not publish exception details to local/global UI")

set_status = window.find("private void SetStatus(string text)")
if set_status < 0:
    errors.append("Schedule Hub window status helper is missing")
else:
    global_status = window.find("PaletteCoordinator.SetStatus(StatusText.Text)", set_status)
    gate = window.rfind("IsBoundActiveDocumentGeneration()", set_status, global_status)
    if global_status < 0 or gate < set_status:
        errors.append("Schedule Hub global Palette status must be gated to the exact bound active document/native generation")

print("QS3D V25 Schedule Hub modeless publication affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: Schedule Hub launcher/window use exact document/native-generation affinity, stable ownership, residue-safe cleanup, and redacted UI failures.")
