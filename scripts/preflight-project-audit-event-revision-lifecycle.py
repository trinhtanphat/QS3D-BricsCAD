from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT_STATE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectState.cs"
AUDIT_TRAIL = ROOT / "src" / "QS3D.Core" / "Audit" / "AuditTrail.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectStateElementStructuralRevisionSmoke.cs"

project_text = PROJECT_STATE.read_text(encoding="utf-8")
audit_text = AUDIT_TRAIL.read_text(encoding="utf-8")
smoke_text = SMOKE.read_text(encoding="utf-8")

# AuditEvents must remain a public persisted list backed by the ownership-aware
# collection boundary. Owned AuditEvent mutation is deliberately three-phase:
# all owners validate first (including budget + revision overflow), then revision
# publication occurs, then cached text accounting commits after the field mutation.
required_project = [
    "public IList<AuditEvent> AuditEvents { get; }",
    "AuditEvents = new CatalogOwnershipList<AuditEvent>(",
    "AttachAuditEvent",
    "DetachAuditEvent",
    "auditEvent.PersistenceTextMutationValidating += ValidateAuditOwnedMutation;",
    "auditEvent.PersistenceMutationRequested += Touch;",
    "auditEvent.PersistenceTextMutationCommitted += _auditHistoryBudget.CommitOwnedMutation;",
    "auditEvent.PersistenceTextMutationValidating -= ValidateAuditOwnedMutation;",
    "auditEvent.PersistenceMutationRequested -= Touch;",
    "auditEvent.PersistenceTextMutationCommitted -= _auditHistoryBudget.CommitOwnedMutation;",
    "_auditHistoryBudget.ValidateOwnedMutation(auditEvent, textDelta);",
    "_ = checked(ChangeVersion + 1L);",
]
missing_project = [token for token in required_project if token not in project_text]
if missing_project:
    raise SystemExit(
        "ERROR: audit-event revision lifecycle preflight failed: persisted ProjectState.AuditEvents "
        "must preserve owner admission, revision publication and budget-accounting lifecycle; missing token(s): "
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
    "PersistenceTextMutationValidating",
    "PersistenceMutationRequested",
    "PersistenceTextMutationCommitted",
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

validation_call = "PersistenceTextMutationValidating?.Invoke(this, delta);"
revision_call = "PersistenceMutationRequested?.Invoke();"
commit_call = "PersistenceTextMutationCommitted?.Invoke(this, delta);"
validation_pos = audit_text.find(validation_call)
revision_pos = audit_text.find(revision_call, validation_pos + len(validation_call))
commit_pos = audit_text.find(commit_call, revision_pos + len(revision_call))
if validation_pos < 0 or revision_pos < 0 or commit_pos < 0 or not (validation_pos < revision_pos < commit_pos):
    raise SystemExit(
        "ERROR: audit-event revision lifecycle preflight failed: owned text mutation must validate "
        "all owners before revision publication and commit accounting only after mutation."
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
