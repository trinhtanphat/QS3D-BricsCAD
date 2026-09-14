from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT_STATE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectState.cs"
AUDIT_TRAIL = ROOT / "src" / "QS3D.Core" / "Audit" / "AuditTrail.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectStateElementStructuralRevisionSmoke.cs"

project_text = PROJECT_STATE.read_text(encoding="utf-8")
audit_text = AUDIT_TRAIL.read_text(encoding="utf-8")
smoke_text = SMOKE.read_text(encoding="utf-8")

# Guard semantics rather than a single formatting/source-shape spelling. AuditEvents must
# remain a public persisted list backed by the ownership-aware boundary, and ProjectState
# must wire/unwire AuditEvent persistence-mutation notifications to its revision lifecycle.
required_project = [
    "public IList<AuditEvent> AuditEvents { get; }",
    "AuditEvents = new CatalogOwnershipList<AuditEvent>(",
    "AttachAuditEvent",
    "DetachAuditEvent",
    "auditEvent.PersistenceMutationRequested += Touch",
    "auditEvent.PersistenceMutationRequested -= Touch",
]
missing_project = [token for token in required_project if token not in project_text]
if missing_project:
    raise SystemExit(
        "ERROR: audit-event revision lifecycle preflight failed: persisted ProjectState.AuditEvents "
        "must use the ownership/revision-aware collection boundary; missing token(s): "
        + ", ".join(repr(token) for token in missing_project)
    )

for stale in (
    "AuditEvents = new List<AuditEvent>();",
    "AuditEvents = new StructuralRevisionList<AuditEvent>(Touch);",
):
    if stale in project_text:
        raise SystemExit(
            "ERROR: audit-event revision lifecycle preflight failed: AuditEvents storage must "
            "track both structural and owned-entry persistence mutations: " + repr(stale)
        )

required_audit = [
    "PersistenceMutationRequested",
    "PersistenceMutationRequested?.Invoke();",
    "return new AuditTrail(project.AuditEvents);",
    "new AuditTrail(project.AuditEvents).ValidateExistingHistory(",
    "_events.Add(item);",
    "_events.Clear();",
]
missing_audit = [token for token in required_audit if token not in audit_text]
if missing_audit:
    raise SystemExit(
        "ERROR: audit-event revision lifecycle preflight failed: AuditEvent/AuditTrail must publish "
        "owned entry changes through the revision-aware project collection; missing token(s): "
        + ", ".join(repr(token) for token in missing_audit)
    )

if "_project?.Touch();" in audit_text:
    raise SystemExit(
        "ERROR: audit-event revision lifecycle preflight failed: AuditTrail manual Touch would "
        "double-increment ProjectState revision after AuditEvents became revision-aware."
    )

required_smoke = [
    "AuditEventStructuralMutationsAdvanceExactlyOnce();",
    "AuditEventInvalidStructuralCandidatesFailAtomically();",
    "AuditEventInvalidOwnedPropertyMutationsFailAtomically();",
    "AuditEventRevisionOverflowFailsBeforeMutation();",
]
missing_smoke = [token for token in required_smoke if token not in smoke_text]
if missing_smoke:
    raise SystemExit(
        "ERROR: audit-event revision lifecycle preflight failed: deterministic regression coverage "
        "for structural/owned mutation atomicity is missing token(s): "
        + ", ".join(repr(token) for token in missing_smoke)
    )

print("PASS ProjectState audit-event ownership and revision lifecycle source guard")
