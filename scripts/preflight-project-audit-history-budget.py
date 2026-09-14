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
]
missing_project = [token for token in required_project if token not in project_text]
if missing_project:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: ProjectState must enforce cached "
        "count/text admission before structural and owned mutation; missing token(s): "
        + ", ".join(repr(token) for token in missing_project)
    )

required_audit = [
    "internal const int MaxStoredEvents = 10_000;",
    "internal const long MaxStoredTextCharacters = 8L * 1024L * 1024L;",
    "PersistenceTextMutationValidating?.Invoke(this, delta);",
    "PersistenceMutationRequested?.Invoke();",
    "PersistenceTextMutationCommitted?.Invoke(this, delta);",
    "internal static long CountStoredTextCharacters(AuditEvent item)",
]
missing_audit = [token for token in required_audit if token not in audit_text]
if missing_audit:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: AuditEvent must publish pre-admission "
        "text delta before revision and commit accounting after field mutation; missing token(s): "
        + ", ".join(repr(token) for token in missing_audit)
    )

required_smoke = [
    "DirectAddAndInsertRejectAtCapacityWithoutMutation();",
    "DirectTextOverflowRejectsWithoutMutation();",
    "ReplacementUsesOldNewTextDelta();",
    "OwnedPropertyGrowthIsAtomicAtTextBudget();",
    "DuplicateReferencePropertyDeltaUsesMultiplicity();",
    "RemovalAndClearReleaseBudget();",
]
missing_smoke = [token for token in required_smoke if token not in smoke_text]
if missing_smoke:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: deterministic count/text/delta/multiplicity "
        "regression coverage is incomplete; missing token(s): "
        + ", ".join(repr(token) for token in missing_smoke)
    )

# Prevent accidental regression to a scan-on-every-owned-property-mutation design. The project
# observer must bind owned event changes to reference multiplicity instead of re-enumerating
# AuditEvents to compute aggregate text after a mutation has started.
if "foreach (var auditEvent in AuditEvents)" in project_text:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: owned audit mutation admission must use "
        "incremental accounting rather than rescanning the full audit history."
    )

print("PASS ProjectState audit-history count/text budget admission source guard")
