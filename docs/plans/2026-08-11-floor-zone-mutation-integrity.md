# Floor/Zone mutation integrity plan — 2026-08-11

## Goal

Harden the CAD-independent Floor/Zone Core mutation boundary so semantic identity is evaluated canonically and requested object-target batches are never silently reduced before mutation.

This plan intentionally stays inside `QS3D.Core`. It does not change BricsCAD commands, WPF, persistence format, quantity/reporting semantics, updater/release behavior, or native geometry.

## Baseline and evidence

Baseline inspected before claim: `e7f718ff50569b20c42ba2b894d12cdb06b36746`.

Claim commit: `52ee71a169e743578468edd04e9703ff6c03d80a`.

Source re-fetched after claim from moving `main` at `04c2b48dd420a3a635612876926c64080816f8e1` and confirmed three defects remained:

1. Floor/Zone `SetActive()` compared the mutable stored active id raw against the resolved canonical id.
2. Floor/Zone `Assign()` compared the mutable element relation raw against the resolved canonical id.
3. Caller-supplied null object targets were silently skipped, allowing a partially specified batch to continue.

The completed earlier Floor/Zone canonical-reference lane explicitly excluded assignment semantics, so this plan does not duplicate that completed scope.

## Invariants to preserve

- Floor/Zone lookup remains unique and case-insensitive through `ProjectState`.
- Existing update/reference/delete canonical identity behavior remains unchanged.
- Existing exact project-instance ownership checks remain mandatory.
- Duplicate object targets continue to deduplicate case-insensitively by semantic element id.
- Floor vertical Bottom/Top Level validation and offset semantics remain unchanged.
- Dirty flags for real assignments remain `Relations | Quantity`.
- Real assignments still `Touch()` the project once before applying writes.
- No implicit data migration or normalization pass is introduced.

## Implementation

### 1. Active Floor/Zone no-op identity

Resolve the requested Floor/Zone first, then compare the stored active id after trimming with the resolved canonical id using `OrdinalIgnoreCase`.

Expected result for a stored value such as `"  f-01  "` and requested `"F-01"`:

- return without `ProjectState.Touch()`;
- preserve the stored raw string;
- preserve the same semantic active object.

### 2. Element Floor/Zone assignment no-op identity

After validating the requested object targets, evaluate current `FloorId` / `ZoneId` after trimming against the resolved canonical id using `OrdinalIgnoreCase`.

For a semantic same-target assignment:

- return changed count `0`;
- do not `Touch()` project persistence state;
- do not rewrite the relation string;
- do not change dirty flags;
- do not change `ProjectElement.UpdatedUtc`.

### 3. Null target fail-closed behavior

A caller-supplied null object target represents an incomplete requested target set, not an ignorable item.

- Floor shared owned-target resolution throws before any semantic write. This also strengthens Floor vertical-level and clear-level operations that use the same resolver.
- Zone assignment throws during target resolution before computing/writing changes.
- The existing exact-instance ownership check remains after the null guard.

### 4. Regression coverage

`ProjectFloorZoneMutationIntegritySmoke` covers six deterministic cases:

- Floor active canonical no-op;
- Zone active canonical no-op;
- Floor assignment canonical no-op;
- Zone assignment canonical no-op;
- Floor `[owned, null]` batch rejection with no mutation;
- Zone `[owned, null]` batch rejection with no mutation.

The no-op assignment cases assert `ChangeVersion`, raw relation identity, dirty flags, `UpdatedUtc`, and canonical object ownership. The null cases assert project version, relation, dirty flags and timestamp remain unchanged.

Smoke registration is isolated through a module initializer to avoid the shared registration hotspot used by concurrent agents.

### 5. Static regression gate

`preflight-project-floor-zone-mutation-integrity.py` requires:

- trimmed active-id no-op comparisons;
- trimmed assignment-id no-op comparisons;
- explicit null-target exceptions;
- all six smoke cases and module registration.

It rejects legacy raw equality and `if (element == null) continue;` behavior, and verifies null validation occurs before `project.Touch()` in the assignment methods.

## Integration strategy

- Work on `agent/floor-zone-mutation-integrity-20260811` from the post-claim moving-main baseline.
- Re-fetch `main` before PR/merge.
- Compare the implementation branch against current `main` and check whether either reserved service changed concurrently after the branch point.
- If reserved files changed, do not overwrite the winner; re-read and reconcile only if the scopes remain non-overlapping.
- Otherwise create a focused PR and squash-merge using the branch head as the expected SHA.
- Close the claim on `main` with exact evidence.

## Validation policy

This lane is deterministic pure Core and does not require BricsCAD V25 to establish the semantic contract. Committed smoke/preflight coverage plus source review are the remote evidence. GitHub Actions are not dispatched because repository policy requires separate explicit authorization. No native BricsCAD runtime PASS is claimed.
