# Work claim — ProjectSession recovery integrity

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:36:00+07:00`
- Baseline main SHA: `09e3749a856b8d246f46f42e121289df5f3ecb8f`
- Priority: production-stabilization / Core persistence recovery correctness

## Confirmed defect

`QsdbProjectStore` already exposes the recovery-safe pair `LoadWithBackupFallback(...)` and `SavePreservingValidatedBackup(...)`, but `ProjectSession.Reload()` still calls plain `Load(...)` and `ProjectSession.Save()` still calls plain `Save(...)`.

That disconnect means the session abstraction does not participate in the store's backup-recovery contract. In particular, a session that should recover from a valid `.bak` cannot carry recovery provenance forward to the next save, and a plain `Save(...)` is allowed to rotate the current primary into `.bak`, which is unsafe when that primary is the corrupt file that caused recovery.

## Reserved scope

- `src/QS3D.Core/Services/ProjectSession.cs`
- focused Core smoke/regression files for ProjectSession recovery semantics
- one focused auto-discovered source preflight if needed to lock the session/store recovery contract
- `docs/PROJECT-SESSION-RECOVERY-INTEGRITY-PLAN-2026-08-11.md`
- this claim file

`src/QS3D.Core/Persistence/QsdbProjectStore.cs` is read-only context for this lane unless a newly-proven defect in its existing recovery API makes a minimal change unavoidable. No speculative store redesign is reserved.

## Intended contract

1. `Reload()` stages the candidate through `LoadWithBackupFallback(...)` before replacing the live session project/audit objects.
2. A successful backup fallback records session recovery provenance without mutating the primary file during reload.
3. A failed reload leaves the current `Project`, `Audit`, and recovery provenance unchanged.
4. While recovery provenance is active, `Save()` uses `SavePreservingValidatedBackup(...)` so the validated `.bak` cannot be replaced by the corrupt/stale primary.
5. A successful recovery-safe save clears recovery provenance only after the primary and preserved backup have both been validated by the store API.
6. A failed save restores the in-memory snapshot and retains recovery provenance so a retry remains recovery-safe.
7. A later successful primary reload clears recovery provenance because the canonical primary has been validated again.
8. Existing write-lock requirements, audit events, `ProjectStateSnapshot` rollback, and ordinary non-recovery save behavior remain intact.

## Regression requirements

- normal primary reload remains canonical and non-recovery;
- corrupt primary + valid backup reload succeeds from backup;
- recovery provenance remains active after fallback;
- recovery-mode save does not replace the known-good backup with corrupt primary bytes;
- successful recovery-mode save produces a valid primary and preserves a valid backup;
- failed recovery-mode save does not clear recovery provenance or leak the `PROJECT_SAVE` audit mutation;
- subsequent successful primary reload clears recovery provenance;
- corrupt primary + corrupt backup fails closed and leaves the existing session object graph unchanged;
- smoke registration cannot silently disappear.

## Coordination / exclusions

- No overlap with active Bulk Edit, Build3D Touch/selection, native Table freshness/Touch, Quantity Rule/Settings, Grid annotation, Level runtime qualification, uninstall/release SemVer, UI, Direct Draw, rebar, Curtain, or documentation-sheet lanes.
- No BricsCAD adapter/native geometry/UI changes.
- No GitHub Actions dispatch.
- No release publication.
- No claim of licensed BricsCAD V25 runtime qualification; this is CAD-independent Core persistence work.

## Validation plan

- Re-fetch current `main` and exact target blobs immediately before implementation.
- Add deterministic Core smoke coverage using repository-owned temporary files only.
- Add/extend an auto-discovered preflight only when it protects the architectural recovery contract without duplicating smoke semantics.
- Review the final diff against all concurrent commits since this claim baseline before updating `main`.
- Close this claim only after the implementation commit is on `main`; record exact SHA and what was/was not executed.

## Completion condition

The session abstraction consumes the existing recovery-safe store contract end-to-end, regression coverage prevents a good `.bak` from being replaced by a corrupt primary after fallback, source changes are merged to `main`, and all remaining host/runtime-only evidence stays explicitly LOCAL_ONLY.
