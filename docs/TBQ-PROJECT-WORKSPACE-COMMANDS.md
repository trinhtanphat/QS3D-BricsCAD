# TBQ project-bound workspace commands

Status date: 2026-08-15

## Scope

This lane completes the BricsCAD V25 command surface for the existing project-bound TBQ workspace introduced by issue #1674. It intentionally reuses the canonical QS3D `.qsdb` project and does not introduce a second TBQ archive, detached shell, or separate persistence model.

The Core workspace is stored in reserved versioned project metadata under `QS3D.TBQ.v1.Workspace`. The codec remains deterministic and fail-closed for malformed or unsupported reserved TBQ payloads. Public project metadata mutation participates in `ProjectState.ChangeVersion`, so TBQ metadata changes are visible to persistence/freshness tracking.

## Implemented command surface

| Command | Behavior | Mutation |
| --- | --- | --- |
| `QS3DTBQSTATUS` | Shows project id, currency, CFA, item/build-up/reference/library counts, base total and currently adjusted total. | No |
| `QS3DTBQRATEREFERENCE` | Lists deterministic rate-reference edges from the bound workspace. | No |
| `QS3DTBQBUILDUPANALYSIS` | Runs Core build-up analysis, including adopted and unused rates plus reverse references. | No |
| `QS3DTBQTRADECFA` | Runs Core trade analysis and cost-per-CFA calculation. | No |
| `QS3DTBQBQLIBRARY` | Lists the project-bound BQ library and reference rates. | No |
| `QS3DTBQADJUSTPREVIEW` | Prompts for adjustment/markup ratios and previews the adjusted total without changing project state. | No |
| `QS3DTBQADJUSTAPPLY` | Applies adjustment/markup ratios to the bound workspace and persists the existing QS3D project. | Yes |

Large deterministic listings are capped at the first 200 rows in the command line to avoid flooding the BricsCAD editor; the complete data remains in the project workspace.

## Existing-project bind and freshness contract

Every command binds through `ExistingProjectMutationContext.Require(...)` and therefore requires a QS3D project that already exists. A missing project or a project without a TBQ workspace fails closed; these commands never create a replacement project or invent TBQ data.

The adapter uses `ProjectContextCoordinator.RequireBackingStoreUnchanged(...)` before consuming or mutating the canonical cached project. `QS3DTBQADJUSTPREVIEW` rechecks freshness after interactive prompts before reporting a preview.

`QS3DTBQADJUSTAPPLY` relies on `ProjectContextCoordinator.Save(...)` for the final persistence transaction. That save path acquires the project file lock and repeats the backing-store freshness check while the same lock is held, preventing two sessions from both validating an old generation and silently overwriting each other.

## Save failure and rollback

Before applying an adjustment, the command captures `ProjectStateSnapshot` from the canonical project. If project save fails:

1. the in-memory project is restored from the snapshot;
2. the document project cache is discarded;
3. the command reports failure and requires the next operation to rebind from the actual sidecar.

Discarding the cache is deliberate. It covers both pre-commit save failures and uncertain post-write failures: a later command must inspect the real `.qsdb/.bak` generation rather than continuing from an in-memory assumption.

If snapshot restoration itself fails, the cache is still discarded and the command reports both the save and rollback errors.

## Core evidence already present in this lane

- Project-bound persistence: commit `4e7d31cad1a8fa6eaa156b0ba91e3b464903d6ac`.
- Public metadata `ChangeVersion` correction: commit `88c1d0a329eb1715ad6a513702d9a196d1767e8c`.
- Metadata version smoke coverage: commit `887db1b43bea269885bb4eb8981f4fd9616623bf`.
- Core smoke coverage includes project-bound mutation/analysis, adjustment preview/apply semantics, snapshot rollback, `.qsdb` round-trip, and fail-closed reserved metadata handling.

## Qualification status

- Core source/smoke contract: implemented in source.
- BricsCAD V25 command source: implemented in this lane.
- Licensed BricsCAD V25 runtime execution for these new commands: **PENDING_LOCAL** until run against an actual licensed BricsCAD V25 host.
- Do not convert `PENDING_LOCAL` into `PASS` from source review, cloud compile, or Core-only smoke tests.

## Remaining qualification

Before declaring issue #1674 production-complete, run the repository's applicable source guards/build/smoke checks on the exact integration SHA, then exercise the command set in licensed BricsCAD V25 against an existing project containing a bound TBQ workspace. The local qualification should verify read commands, preview non-mutation, apply persistence across reopen, stale-sidecar rejection, and recovery behavior after an induced save failure.
