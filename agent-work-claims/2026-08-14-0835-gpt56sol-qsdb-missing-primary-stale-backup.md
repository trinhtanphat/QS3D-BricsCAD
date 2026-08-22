# QSDB missing-primary stale-backup publication integrity claim
Status: ACTIVE
Agent: gpt56sol-qsdb-missing-primary-stale-backup-20260814-0835
Baseline: eb6d5666936f9484f38e195a3b11fad947d58892
Scope: src/QS3D.Core/Persistence/AtomicFileCommit.cs; tests/QS3D.Core.SmokeTests/AtomicFileCommitPathIdentitySmoke.cs.
Goal: fail closed when normal backup-preserving replacement is asked to publish a new primary while the primary is absent but the configured backup sidecar already exists, so QSDB cannot silently pair a new primary generation with stale backup data. Preserve ordinary replace-with-backup when a primary exists, create-new semantics, and the explicit validated-backup recovery path; add focused regression. QSDB schema/migrations, mappings and host UI are out of scope.