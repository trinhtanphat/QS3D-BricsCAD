from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallHubCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
    "CloseOwnedCandidateOnFailure(candidate)",
    "if (candidate.IsLoaded) return false;",
    "ReleaseOwnedWindow(candidate);",
    "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
    "database.UnmanagedObject == nativeDatabaseIdentity",
]
for needle in required:
    if needle not in text:
        errors.append("missing Curtain Wall Hub affinity/residue contract: " + needle)

for forbidden in (
    "ReleaseOwnedWindow(candidate);\n                    TryClose(candidate);",
    "ReleaseOwnedWindow(candidate);\n                    TryClose(candidate);\n                }\n                ReportFailure(document);",
):
    if forbidden in text:
        errors.append("Curtain Wall Hub must not forget candidate ownership before terminal Close")

close_helper = text.find("private static bool CloseOwnedCandidateOnFailure")
release_in_helper = text.find("ReleaseOwnedWindow(candidate);", close_helper)
loaded_fence = text.find("if (candidate.IsLoaded) return false;", close_helper, release_in_helper)
if min(close_helper, loaded_fence, release_in_helper) < 0 or not (close_helper < loaded_fence < release_in_helper):
    errors.append("Curtain Wall Hub failure cleanup must release ownership only after terminal !IsLoaded proof")

prepare = text.find("if (!PreparePublishedWindow(document, nativeDatabaseIdentity))")
construct = text.find("candidate = new CurtainWallWindow(document);")
show = text.find("Application.ShowModelessWindow(IntPtr.Zero, candidate, true);")
promote = text.find("if (!PromotePendingWindow(candidate, document, nativeDatabaseIdentity))")
if min(prepare, construct, show, promote) < 0:
    errors.append("Curtain Wall Hub publication boundaries are missing")
else:
    if text.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;", prepare, construct) < 0:
        errors.append("Curtain Wall Hub must revalidate after published-owner close before construction")
    if text.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))", show, promote) < 0:
        errors.append("Curtain Wall Hub must revalidate after host show before promotion")

success = text.find("TrySetStatus(\"Vách Kính Hub: Family • panel grid • schedule • workflow 3D.\");")
if success >= 0:
    gate = text.rfind("IsActiveDocumentGeneration(document, nativeDatabaseIdentity)", promote, success)
    if gate < promote:
        errors.append("Curtain Wall Hub success status must be gated to the same exact document generation")

print("QS3D V25 Curtain Wall Hub modeless publication affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: Curtain Wall Hub keeps exact document/native affinity and retains loaded candidates until terminal close.")
