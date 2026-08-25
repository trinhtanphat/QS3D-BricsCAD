# LOCAL-010 performance / UI / HiDPI qualification

This is the executable LOCAL_ONLY handoff for `LOCAL-010`. Repository-safe preparation is committed so a compatible local worker should fetch/sync the intended exact SHA and run one command, not invent a matrix or patch product source.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-local-v25-local-010.ps1
```

Use a clean checkout, licensed interactive BricsCAD V25 x64, representative **sanitized/disposable** projects, and zero pre-existing BricsCAD processes. Pass `-BricsCadDir` only when V25 cannot be found through `BRICSCAD_V25_DIR` or standard install paths. The runner first executes the canonical licensed `run-local-v25-qualification.ps1` baseline and rejects evidence whose exact SHA does not match the checkout.

## Matrix

Performance rows cover DependencyGraph/regeneration, rooms, wall junctions, Auto Host, Curtain, BQ/BBS/ED2/Interchange, ownership/Health, and representative rebar limits. Record project scale plus elapsed timing and a short responsiveness observation; do not publish customer paths, raw Handles, ProjectIds, drawing contents, or proprietary files.

UI rows cover Start Center and Ribbon at Windows display scaling 100%, 125%, 150%, and 200%; Workspace at narrow, normal, and wide host sizes; and a document-switch/cleanup row. For each DPI row verify text/icon visibility, no clipping that makes required actions unreachable, no duplicate QS3D-owned Ribbon/palette controls after repeated initialization, and command routing to the intended BricsCAD/QS3D action. For Workspace widths verify the native BricsCAD viewport remains the CAD host, modeless surfaces remain usable, and no stale cross-document state is shown or mutated. The document-switch row must verify stale callbacks do not target the prior drawing and that closing the dedicated host leaves zero owned process residue.

The Start Center/Ribbon rows must also execute the already-published detailed acceptance in `docs/LOCAL-010-START-CENTER-HANDOFF-2026-08-17.md`: verify the `Dự án` and `Cấu hình` Ribbon groups, the hosted two-column Start Center shell and bottom-strip `Mô hình` / `BQ` routes, repeated Ribbon initialization without duplicate controls, and Create/Open/Save/Save As/Settings/System Objects/Recent Project plus `Mô hình`/`BQ` routing. Each user action must dispatch exactly once, and the hosted Start Center must use the BricsCAD plugin lifecycle with no standalone QS3D application/process. Repeat those checks across document switching/reopen/reload and representative DPI/window sizes, with explicit no-stale-action and no-cross-DWG results.

V25 and V26 identities are separate. This runner qualifies V25 only; V26 parity evidence, when required by the live LOCAL-010 acceptance set, must use the matching V26 assembly/runtime and must never relabel or load the V25 binary as V26.

For every row type exactly `PASS <case-id>`, `FAIL <case-id>`, or `BLOCKED <case-id>`, followed by a non-trivial sanitized evidence/timing note. The machine-readable result is `artifacts/local-v25-local-010/qualification.json` and includes exact tested SHA, Windows/V25 identity, baseline report, row outcomes, and `localPassClaimedByRunner=false`.

Final runner result is `PASS` only when every row passes. Product failure exits 1, environment/runtime blocker exits 2, and incomplete/unusable evidence exits 3 (`NO_RESULT`). A local worker may update the canonical inbox/evidence only from sanitized evidence tied to the exact tested SHA; the runner itself never claims `LOCAL_PASS`.
