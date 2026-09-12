from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT_STATE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectState.cs"
AUDIT_TRAIL = ROOT / "src" / "QS3D.Core" / "Audit" / "AuditTrail.cs"

project_text = PROJECT_STATE.read_text(encoding="utf-8")
audit_text = AUDIT_TRAIL.read_text(encoding="utf-8")

required_project = [
    "public IList<AuditEvent> AuditEvents { get; }",
    "AuditEvents = new StructuralRevisionList<AuditEvent>(Touch);",
]
missing_project = [token for token in required_project if token not in project_text]
if missing_project:
    raise SystemExit(
        "ERROR: audit-event revision lifecycle preflight failed: persisted ProjectState.AuditEvents "
        "must use the structural revision-aware collection boundary; missing token(s): "
        + ", ".join(repr(token) for token in missing_project)
    )

if "AuditEvents = new List<AuditEvent>();" in project_text:
    raise SystemExit(
        "ERROR: audit-event revision lifecycle preflight failed: raw List<AuditEvent> bypasses "
        "ProjectState revision tracking for persisted audit-history mutations."
    )

required_audit = [
    "return new AuditTrail(project.AuditEvents);",
    "new AuditTrail(project.AuditEvents).ValidateExistingHistory(",
    "_events.Add(item);",
    "_events.Clear();",
]
missing_audit = [token for token in required_audit if token not in audit_text]
if missing_audit:
    raise SystemExit(
        "ERROR: audit-event revision lifecycle preflight failed: AuditTrail must mutate the "
        "revision-aware project collection directly; missing token(s): "
        + ", ".join(repr(token) for token in missing_audit)
    )

if "_project?.Touch();" in audit_text:
    raise SystemExit(
        "ERROR: audit-event revision lifecycle preflight failed: AuditTrail manual Touch would "
        "double-increment ProjectState revision after AuditEvents became revision-aware."
    )

print("PASS ProjectState audit-event structural mutation revision source guard")
