from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT_STATE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectState.cs"
AUDIT_TRAIL = ROOT / "src" / "QS3D.Core" / "Audit" / "AuditTrail.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AuditHistoryBudgetMutationSmoke.cs"
ATOMICITY_SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "AuditHistoryReferenceAccountingAtomicitySmoke.cs"

project_text = PROJECT_STATE.read_text(encoding="utf-8")
audit_text = AUDIT_TRAIL.read_text(encoding="utf-8")
smoke_text = SMOKE.read_text(encoding="utf-8")
atomicity_smoke_text = ATOMICITY_SMOKE.read_text(encoding="utf-8")

required_project = [
    "ICatalogMutationObserver<T>",
    "AuditHistoryBudgetObserver",
    "ValidateAdd(T item, int existingReferenceCount)",
    "ValidateReplace(T previous, T replacement, int previousReferenceCount, int replacementReferenceCount)",
    "ValidateRemove(T item, int referenceCount)",
    "ValidateClear(IReadOnlyList<T> items)",
    "ValidateReferenceCount(item, existingReferenceCount);",
    "_mutationObserver?.ValidateAdd(item, existingReferenceCount);",
    "_mutationObserver?.CommitAdd(item);",
    "_mutationObserver?.ValidateReplace(previous, value, previousReferenceCount, replacementReferenceCount);",
    "_mutationObserver?.CommitReplace(previous, value);",
    "_mutationObserver?.ValidateRemove(item, referenceCount);",
    "_mutationObserver?.CommitRemove(item);",
    "_mutationObserver?.ValidateClear(_items);",
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
        "count/text/reference admission before structural and owned mutation; missing token(s): "
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

required_atomicity_smoke = [
    "CorruptReferenceAccountingDuplicateAdmissionRejectsBeforeMutation();",
    "CorruptReferenceAccountingRemoveRejectsBeforeMutation();",
    "CorruptReferenceAccountingReplaceRejectsBeforeMutation();",
    "CorruptReferenceAccountingClearRejectsBeforeMutation();",
    "RemoveReferenceAccounting(project, item);",
    "Throws<InvalidOperationException>(() => project.AuditEvents.Add(item));",
    "Throws<InvalidOperationException>(() => project.AuditEvents.Insert(0, item));",
    "Throws<InvalidOperationException>(() => project.AuditEvents.RemoveAt(0));",
    "Throws<InvalidOperationException>(() => project.AuditEvents[0] = replacement);",
    "Throws<InvalidOperationException>(() => project.AuditEvents.Clear());",
]
missing_atomicity_smoke = [token for token in required_atomicity_smoke if token not in atomicity_smoke_text]
if missing_atomicity_smoke:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: corrupt reference-accounting atomicity "
        "regression coverage is incomplete; missing token(s): "
        + ", ".join(repr(token) for token in missing_atomicity_smoke)
    )

# Public structural mutations must prove observer accounting before ProjectState.Touch/list/ownership
# mutation. Keep restore-only repair separate: ClearRestoredPersistenceState intentionally commits its
# rebuilt accounting without traversing the public corruption guard.
add_method = project_text.find("public void Add(T item)", project_text.find("internal sealed class CatalogOwnershipList<T>"))
add_validate = project_text.find("_mutationObserver?.ValidateAdd(item, existingReferenceCount);", add_method)
add_touch = project_text.find("_beforeMutation();", add_validate)
add_commit = project_text.find("_mutationObserver?.CommitAdd(item);", add_validate)
if not (0 <= add_method < add_validate < add_touch < add_commit):
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: Add must validate reference accounting "
        "before revision/list mutation and commit accounting afterward."
    )

remove_validate = project_text.find("_mutationObserver?.ValidateRemove(item, referenceCount);")
remove_touch = project_text.find("_beforeMutation();", remove_validate)
remove_commit = project_text.find("_mutationObserver?.CommitRemove(item);", remove_validate)
if not (0 <= remove_validate < remove_touch < remove_commit):
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: RemoveAt must validate reference "
        "accounting before revision/list mutation and commit accounting afterward."
    )

clear_method = project_text.find("public void Clear()", project_text.find("internal sealed class CatalogOwnershipList<T>"))
clear_validate = project_text.find("_mutationObserver?.ValidateClear(_items);", clear_method)
clear_touch = project_text.find("_beforeMutation();", clear_validate)
clear_commit = project_text.find("_mutationObserver?.CommitClear();", clear_validate)
if not (0 <= clear_method < clear_validate < clear_touch < clear_commit):
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: public Clear must validate complete "
        "audit accounting before revision/list mutation and commit accounting afterward."
    )

replace_method = project_text.find("public T this[int index]", project_text.find("internal sealed class CatalogOwnershipList<T>"))
replace_validate = project_text.find(
    "_mutationObserver?.ValidateReplace(previous, value, previousReferenceCount, replacementReferenceCount);",
    replace_method,
)
replace_touch = project_text.find("_beforeMutation();", replace_validate)
replace_commit = project_text.find("_mutationObserver?.CommitReplace(previous, value);", replace_validate)
if not (0 <= replace_method < replace_validate < replace_touch < replace_commit):
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: replacement must validate reference "
        "accounting before revision/list mutation and commit accounting afterward."
    )

# Prevent accidental regression to a scan-on-every-owned-property-mutation design. The project
# observer must bind owned event changes to reference multiplicity instead of re-enumerating
# AuditEvents to compute aggregate text after a mutation has started.
if "foreach (var auditEvent in AuditEvents)" in project_text:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: owned audit mutation admission must use "
        "incremental accounting rather than rescanning the full audit history."
    )

# Normal structural mutation must also remain incremental. The structural index is deliberately
# independent from AuditHistoryBudgetObserver._referenceCounts so corruption of persisted-budget
# accounting is still detectable rather than being compared with itself.
catalog_start = project_text.find("internal sealed class CatalogOwnershipList<T>")
catalog_end = project_text.find("internal sealed class StructuralRevisionList<T>", catalog_start)
catalog_text = project_text[catalog_start:catalog_end]
required_catalog_index = [
    "private readonly Dictionary<T, int> _referenceCounts = new Dictionary<T, int>(ReferenceComparer.Instance);",
    "private int GetReferenceCount(T item)",
    "return _referenceCounts.TryGetValue(item, out var count) ? count : 0;",
    "IncrementReference(item);",
    "DecrementReference(item);",
    "ValidateReferenceIndex();",
]
missing_catalog_index = [token for token in required_catalog_index if token not in catalog_text]
if missing_catalog_index:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: CatalogOwnershipList must maintain an "
        "independent O(1) reference-identity multiplicity index; missing token(s): "
        + ", ".join(repr(token) for token in missing_catalog_index)
    )
if "private int CountReferences(T item)" in catalog_text:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: normal catalog reference multiplicity "
        "must not be rediscovered by full collection scans."
    )
get_count_start = catalog_text.find("private int GetReferenceCount(T item)")
get_count_end = catalog_text.find("\n        }", get_count_start)
get_count_body = catalog_text[get_count_start:get_count_end]
if "_items" in get_count_body:
    raise SystemExit(
        "ERROR: project audit-history budget preflight failed: normal catalog reference multiplicity "
        "lookup must use the independent index rather than scanning the collection."
    )

print("PASS ProjectState audit-history count/text/reference budget admission source guard")
