# V25 Beam Rebar post-commit outcome

Issue: #7010
Lane: C03

## Defect
`BeamRebarSolidBuilder.BuildSelected` mutates project semantic state and commits the native CAD transaction before nested `Transaction` / `DocumentLock` cleanup completes. A cleanup exception after `transaction.Commit()` used to be rethrown into `BeamRebarCommands`, which displayed the generic mutation-failure message even though the CAD/project result was already durable. Retrying could duplicate an already-applied authoring operation.

## Contract
- Before CAD commit, any failure restores the captured `ProjectStateSnapshot`; rollback failure remains terminal and preserves both errors internally.
- After CAD commit, cleanup/dispose failure must not trigger project rollback and must not be classified as mutation failure.
- The builder returns the committed bar count plus a `PostCommitCleanupWarning` bit.
- The command publishes the committed success message and adds the bounded `UiSyncWarning` when that bit is set.
- UI publication remains fenced to the exact active managed document + captured non-zero native database generation.
- No raw post-commit cleanup exception text is exposed to the user.

## REMOTE_SAFE qualification
Run the focused source guard:

```text
python scripts/preflight-v25-beam-rebar-postcommit-outcome.py
```

Then run repository aggregate feature guards, deterministic smoke applicable to the candidate, and the admitted/locked-reference BricsCAD V25 plugin compile through Shared CI. Green remote/source/build evidence is not licensed native runtime evidence.

## LOCAL_ONLY scenarios
Licensed BricsCAD V25 may later validate real Teigha `Transaction.Dispose` / `DocumentLock.Dispose` failure timing, MDI switching around post-commit UI refresh, visible status text and subsequent authoring retries. Until such an exact-SHA licensed run occurs, classify these native scenarios as `LOCAL_ONLY / NO_RESULT`; never call hosted CI `LOCAL_PASS`.
