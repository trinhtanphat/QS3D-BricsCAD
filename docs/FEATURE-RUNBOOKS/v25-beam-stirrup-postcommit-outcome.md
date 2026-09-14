# V25 Beam Stirrup post-commit outcome

## Scope
`QS3DREBARSTIRRUP3D` must distinguish mutation failure before native commit from cleanup failure after a durable native commit.

## Required contract
- Capture the project rollback snapshot before native mutation.
- Before `transaction.Commit()`, any operation failure restores the project snapshot.
- If project rollback also fails, preserve both operation and restore exceptions as the internal aggregate cause.
- After `transaction.Commit()` succeeds, transaction/document-lock cleanup failure must not be reported as mutation failure.
- Return committed element/stirrup counts plus a bounded `PostCommitCleanupWarning` discriminator.
- Never expose raw cleanup exception text to user-facing UI.
- Keep existing active-document and native-database-generation admission before mutation.
- Keep post-commit refresh, Regen, palette status and editor output fenced to the exact invocation generation.
- Cleanup warning publication must not perform rollback, start a new native transaction, or retry the authoring command.

## Verification
Run `python scripts/preflight-v25-beam-stirrup-postcommit-outcome.py`, all Beam Stirrup preflights, generic `scripts/preflight.py`, aggregate feature preflight, deterministic smoke and admitted locked-reference V25 compile.

Licensed Teigha cleanup timing and visible editor/modeless behavior remain `LOCAL_ONLY / NO_RESULT` unless executed in licensed BricsCAD V25.