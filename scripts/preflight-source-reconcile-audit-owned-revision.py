from pathlib import Path

path = Path("src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs")
text = path.read_text(encoding="utf-8")

record = 'AuditTrail.ForProject(project).Record("source.reconcile"'
if record not in text:
    raise SystemExit("FAIL: source reconcile audit event missing")

start = text.find("public static SourceReconcileResult ReconcileSelection")
end = text.find("private static List<Target> ResolveTargets", start)
if start < 0 or end < 0:
    raise SystemExit("FAIL: reconcile method boundary missing")
body = text[start:end]

if "project.Touch();" in body:
    raise SystemExit("FAIL: ReconcileSelection contains standalone project.Touch in addition to audit-owned revision")
if "transaction.Commit();" not in body or "rollback.Restore(project);" not in body:
    raise SystemExit("FAIL: CAD commit / project rollback contract missing")
if body.find("RefreshSourceDerivedState") > body.find("transaction.Commit();"):
    raise SystemExit("FAIL: source refresh must precede CAD commit")

refresh_start = text.find("private static void RefreshSourceDerivedState")
refresh_end = text.find("private static void UpdateOptionalCadMetadata", refresh_start)
refresh_body = text[refresh_start:refresh_end]
if record not in refresh_body:
    raise SystemExit("FAIL: per-target source.reconcile audit record missing")

print("PASS: source reconcile project revision remains audit-owned with transaction rollback intact")
