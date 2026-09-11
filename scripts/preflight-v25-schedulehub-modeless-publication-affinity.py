from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ScheduleHubCommands.cs"

text = SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "private static PublishedManager? _pending;",
    "private static PublishedManager? _published;",
    "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
    "var owner = new PublishedManager(window, document, nativeDatabaseIdentity);",
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

if "ex.Message" in text:
    errors.append("Schedule Hub must not publish native exception details to UI")

show = text.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
reserve = text.find("_pending = owner;")
loaded = text.find("if (!window.IsLoaded)", show)
exact = text.find("if (!ReferenceEquals(_pending, owner))", loaded)
publish = text.find("_published = owner;", exact)
if min(show, reserve, loaded, exact, publish) < 0 or not (reserve < show < loaded < exact < publish):
    errors.append("Schedule Hub must reserve pending ownership before host show and publish only after loaded/exact-owner proof")

prepare = text.find("if (!PreparePublishedWindow(document, nativeDatabaseIdentity))")
construct = text.find("var window = new ScheduleHubWindow(document);")
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

print("QS3D V25 Schedule Hub modeless publication affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: Schedule Hub uses pending-first exact document/native-generation publication, residue-safe cleanup, and redacted UI failures.")
