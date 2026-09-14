from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT_STATE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectState.cs"
AUDIT_TRAIL = ROOT / "src" / "QS3D.Core" / "Audit" / "AuditTrail.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AuditHistoryBudgetMutationSmoke.cs"

project_text = PROJECT_STATE.read_text(encoding="utf-8")
audit_text = AUDIT_TRAIL.read_text(encoding="utf-8")
smoke_text = SMOKE.read_text(encoding="utf-8")

required_project = [
    "ICatalogMutationObserver<T>",
    "AuditHistoryBudgetObserver",
    "_mutationObserver?.ValidateAdd(item);",
    "_mutationObserver?.CommitAdd(item);",
    "_mutationObserver?.ValidateReplace(previous, value);",
    "_mutationObserver?.CommitReplace(previous, value);",
    "_mutationObserver?.CommitRemove(item);",
    "_mutationObserver?.CommitClear();",
    "AuditTrail.MaxStoredEvents",
    "AuditTrail.MaxStoredTextCharacters",
    "ValidateOwnedMutation",
    "CommitOwnedMutation",
    "ValidateAuditEventCandidate, _auditHistoryBudget",
    "auditEvent.PersistenceTextMutationValidating += ValidateOwnedAuditMutation;",
    "auditEvent.PersistenceMutationRequested += Touch;",
    "_auditHistoryBudget.ValidateStructuralCount(AuditEvents.Count);",
    "_auditHistoryBudget.ValidateOwnedMutation(auditEvent, perOccurrenceDelta);",
    "if (!_restoringSnapshot) _ = checked(ChangeVersion + 1L);",
]
missing_project = [token for token in required_project if token not in project_text]
if missing_project:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: ProjectState must enforce cached "
        "count/text admission and all-owner revision prevalidation before structural or owned mutation; missing token(s): "
        + ", ".join(repr(token) for token in missing_project)
    )

required_audit = [
    "internal const int MaxStoredEvents = 10_000;",
    "internal const long MaxStoredTextCharacters = 8L * 1024L * 1024L;",
    "PersistenceTextMutationValidating?.Invoke(this, delta);",
    "PersistenceTextMutationValidating?.Invoke(this, 0L);",
    "PersistenceMutationRequested?.Invoke();",
    "PersistenceTextMutationCommitted?.Invoke(this, delta);",
    "internal static long CountStoredTextCharacters(AuditEvent item)",
]
missing_audit = [token for token in required_audit if token not in audit_text]
if missing_audit:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: AuditEvent must publish side-effect-free "
        "owner admission before revision publication and commit accounting after field mutation; missing token(s): "
        + ", ".join(repr(token) for token in missing_audit)
    )

required_smoke = [
    "DirectAddAndInsertRejectAtCapacityWithoutMutation();",
    "DirectTextOverflowRejectsWithoutMutation();",
    "ReplacementUsesOldNewTextDelta();",
    "OwnedPropertyGrowthIsAtomicAtTextBudget();",
    "DuplicateReferencePropertyDeltaUsesMultiplicity();",
    "RemovalAndClearReleaseBudget();",
    "SharedAuditEventRevisionOverflowIsAtomicAcrossOwners();",
    "SharedAuditEventCorruptOwnerIsAtomicAcrossOwners();",
]
missing_smoke = [token for token in required_smoke if token not in smoke_text]
if missing_smoke:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: deterministic count/text/delta/multiplicity "
        "and shared-owner atomicity regression coverage is incomplete; missing token(s): "
        + ", ".join(repr(token) for token in missing_smoke)
    )

# All owner-side validation must complete before any project revision is published. Do not regress
# to the combined validation+Touch callback: a later owner can reject a shared AuditEvent mutation
# after an earlier owner has already advanced its revision/timestamp.
if "auditEvent.PersistenceMutationRequested += ValidateAuditHistoryAndTouch;" in project_text:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: owned audit revision publication must "
        "not combine validation and Touch in the multicast mutation callback."
    )

# Prevent accidental regression to a scan-on-every-owned-property-mutation design. The project
# observer must bind owned event changes to reference multiplicity instead of re-enumerating
# AuditEvents to compute aggregate text after a mutation has started.
if "foreach (var auditEvent in AuditEvents)" in project_text:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: owned audit mutation admission must use "
        "incremental accounting rather than rescanning the full audit history."
    )

print("PASS ProjectState audit-history count/text budget admission and shared-owner atomicity source guard")