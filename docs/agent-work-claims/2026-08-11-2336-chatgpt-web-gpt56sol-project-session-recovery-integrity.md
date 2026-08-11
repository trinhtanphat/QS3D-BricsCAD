# Work claim — ProjectSession recovery integrity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:36:00+07:00`
- Completed: `2026-08-12`
- Baseline main SHA: `09e3749a856b8d246f46f42e121289df5f3ecb8f`
- Priority: production-stabilization / Core persistence recovery correctness

## Confirmed defect

`QsdbProjectStore` already exposed the recovery-safe pair `LoadWithBackupFallback(...)` and `SavePreservingValidatedBackup(...)`, while the claimed baseline still had `ProjectSession.Reload()` on plain `Load(...)` and `ProjectSession.Save()` on plain `Save(...)`.

That disconnect meant the session abstraction did not participate in the store's backup-recovery contract. In particular, a session that should recover from a valid `.bak` could not carry recovery provenance forward to the next save, and a plain `Save(...)` was allowed to rotate the current primary into `.bak`, which is unsafe when that primary is the corrupt file that caused recovery.

## Reserved scope

- `src/QS3D.Core/Services/ProjectSession.cs`
- focused Core smoke/regression files for ProjectSession recovery semantics
- one focused auto-discovered source preflight to lock the session/store recovery contract
- `docs/plans/2026-08-11-project-session-recovery-integrity.md`
- this claim file

`src/QS3D.Core/Persistence/QsdbProjectStore.cs` remained read-only context for this lane. No speculative store redesign was performed.

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

## Completion evidence

- Source implementation landed concurrently on `main`: `0ad2b89d9f9e7ae9665912182fd040409e00ad37` (`fix(persistence): preserve ProjectSession backup recovery`). It introduces session recovery provenance, `LoadWithBackupFallback(...)`, recovery-safe save publication, post-success provenance clearing, and keeps staged reload binding / save rollback semantics.
- Focused smoke expansion: `8969906068553899c75f440cf759465133ff7fa3` (`test(persistence): cover ProjectSession recovery lifecycle`). It covers both-invalid reload atomicity, corrupt-primary recovery, validated-backup preservation, recovery-mode clearing after successful save, primary-reload clearing, and failed recovered-save rollback/retry semantics.
- Focused source gate: `94a54181a75c470a818ab83653cc760ea964a6bb` (`test(persistence): guard ProjectSession recovery contract`). It removes the stale plain-`Load(Path)` requirement and statically guards fallback reload, staged binding, recovery-safe publication, post-success provenance clearing, smoke registration, and the recovery regression cases.
- `scripts/preflight-all.py` auto-discovers `scripts/preflight-*.py`, so the updated ProjectSession gate remains part of aggregate preflight discovery.
- Final source/test/gate contents were re-read from moving `main` after the above commits and remained present after concurrent commits.
- The updated Python gate was syntax-compiled successfully during remote review. The full repository smoke executable and aggregate preflight were not executed in this connector session because no repository checkout/runner was available, and no GitHub Actions workflow was dispatched.

## Coordination / exclusions

- No concurrent source implementation was overwritten; the existing `0ad2b89d...` implementation was reviewed and retained.
- No overlap was introduced with active adapter/UI/release/geometry lanes.
- No BricsCAD adapter/native geometry/UI changes.
- No release publication.
- No claim of licensed BricsCAD V25 runtime qualification; this lane is CAD-independent Core persistence work.

## Completion condition

Completed for source/regression ownership: the session abstraction consumes the existing recovery-safe store contract end-to-end, the known-good `.bak` lifecycle is guarded by focused smoke cases and source preflight, and the claim is released. Exact BricsCAD V25 runtime qualification remains outside this Core-only lane and is not claimed here.
