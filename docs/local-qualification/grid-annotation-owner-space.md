# Grid annotation owner-space/layout qualification (C05 / #5294)

Status: `LOCAL_ONLY / NO_RESULT`

This matrix qualifies the source hardening for generated Grid endpoint annotation ownership. Remote/static/V25 compile CI is source evidence only and is **not** licensed BricsCAD runtime evidence.

## Candidate identity

Record before execution:

- exact source SHA;
- preview/package identity if applicable;
- `QS3D.BricsCAD.V25.dll` SHA-256;
- BricsCAD V25 build/license state;
- DWG fixture SHA-256.

All cells must run against the same exact candidate unless explicitly recorded otherwise.

## GO01 — ModelSpace baseline

Create/capture and number one finite LINE Grid in ModelSpace, run `QS3DGRIDANNOTATE`, and verify both endpoint annotations are created in the same ModelSpace owner as the authoritative LINE. Save/reopen and verify annotation health remains valid.

Expected: PASS with no semantic identity change and no duplicate generated annotation set.

## GO02 — PaperSpace baseline

Create/capture and number one supported finite Grid source in an active PaperSpace layout, run annotation, and verify generated extension/bubble/text entities are owned by the same PaperSpace block table record as the authoritative source. Save/reopen and refresh.

Expected: PASS; no fallback to ModelSpace and no parallel PaperSpace semantic store.

## GO03 — Same-owner refresh

For both GO01 and GO02, change the Grid label through the canonical naming workflow and refresh annotation.

Expected: prior generated handles are validated, erased, and replaced transactionally in the same authoritative owner space; semantic metadata points only to the new complete set.

## GO04 — Cross-owner generated drift

Starting from a valid annotated Grid, move or otherwise reproduce (using native-host operations only) one generated annotation entity into a different owner space/layout while preserving enough metadata/XData for the handle to remain resolvable.

Expected: refresh fails closed **before any generated annotation is erased**. Existing semantic ProjectState is restored/unchanged. Record command text and before/after handles.

If BricsCAD does not permit this mutation while preserving generated metadata, record `NOT_REPRODUCIBLE_BY_HOST_OPERATION` rather than fabricating a PASS; use GO05 corruption tooling only if an existing repository-approved probe supports it.

## GO05 — Corruption probe, if repository-approved

Use only an existing sanctioned local probe/fixture mechanism to produce a matching-XData generated entity whose OwnerId differs from the authoritative Grid source owner.

Expected: stable fail-closed owner-space/layout diagnostic; zero destructive erase; zero partial new annotation creation.

## GO06 — Ownership/XData mismatch

Corrupt generated ownership/XData without changing owner space.

Expected: existing generated-ownership guard still fails closed; #5294 must not weaken this older safety contract.

## GO07 — Missing/invalid handle metadata

Exercise missing, malformed, stale, and duplicate generated-handle metadata cases already covered by the lifecycle.

Expected: fail closed before erase; no partial replacement.

## GO08 — Undo/Redo

On a successful same-owner refresh, exercise Undo/Redo around annotation replacement.

Expected: CAD/semantic state remains coherent according to existing project persistence semantics; record any host limitation explicitly.

## GO09 — Cold reopen

Save, close, reopen and refresh valid ModelSpace and PaperSpace fixtures.

Expected: exact authoritative source owner is rediscovered from the live source; generated owner-space validation passes only for the matching layout/space.

## GO10 — Multi-layout isolation

Create Grid sources and annotations in two PaperSpace layouts with distinct semantic Grid identities.

Expected: refreshing one Grid does not erase, adopt, or create generated annotation in the other layout.

## GO11 — Multi-DWG isolation

Open two DWGs and exercise annotation refresh in each while switching MDI activation.

Expected: existing MdiActiveDocument/ProjectContext guards remain effective; no cross-document handle or owner adoption.

## GO12 — Failure/UI isolation

Induce a supported annotation failure and separately exercise post-success editor/UI refresh failure where existing local harnesses permit it.

Expected: operation failures do not leak raw exceptions; post-commit UI/editor refresh does not retroactively invalidate a committed native/semantic annotation replacement.

## Acceptance boundary

A runtime PASS may be claimed only when the exact candidate identity and all applicable GO01–GO12 evidence are attached to the canonical issue/PR. `preflight`, Core smoke, trusted V25 reference build, and plugin compilation alone remain `LOCAL_ONLY / NO_RESULT` for licensed native behavior.
