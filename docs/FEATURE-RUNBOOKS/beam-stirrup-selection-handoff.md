# Beam Stirrup selection snapshot handoff

Issue: #6413  
Lane: C03 — BricsCAD V25 UI / Authoring  
Runtime classification: REMOTE_SAFE source/static/V25 compile; licensed interaction cells remain LOCAL_ONLY.

## Defect

`QS3DREBARSTIRRUP3D` already acquired and semantically validated one command-level selection, but the geometry builder discarded that admitted snapshot and read/prompted native selection again. A changed PICKFIRST/current selection could therefore produce geometry for Beam targets that were never covered by the command's project/target freshness checks.

## Source contract

1. `CadSelectionGuard.AcquireCurrentSelection(document)` is the only selection acquisition for one command invocation.
2. The exact admitted `ObjectId[]` and expected semantic Beam target IDs flow into `BeamStirrupSolidBuilder`.
3. The builder never calls `SelectImplied`, `GetSelection`, or `SetImpliedSelection`.
4. The builder derives source handles only from the admitted ObjectId snapshot and revalidates the exact semantic target set before native mutation.
5. The exact managed `Document` must still be the active MDI document before lock and after `LockDocument()` before opening Model Space for write.
6. After the native lock boundary, the exact cached `ProjectState` must still be canonical, its backing store must remain fresh, and the semantic Beam target set must still equal the admitted target generation.
7. Existing project snapshot rollback, generated-rebar ownership checks, native transaction commit ordering and post-commit UI warning semantics remain unchanged.

## REMOTE_SAFE verification

- `python scripts/preflight-beam-stirrup-selection-handoff.py`
- repository generic + discovered feature source guards
- deterministic Core smoke where invoked by protected CI
- trusted/admitted BricsCAD V25 reference validation
- BricsCAD V25 plugin compilation against the locked reference generation

These prove source/compile contracts only. They are not native BricsCAD interaction evidence.

## LOCAL_ONLY licensed BricsCAD V25 matrix

Use an exact pushed candidate and record the exact SHA. Do not infer PASS from hosted CI.

- Start with Beam A selected, enter `QS3DREBARSTIRRUP3D`, then exercise a real PICKFIRST/current-selection change before native mutation; only the originally admitted selection may be eligible.
- Exercise document A → B and A → B → A timing around command/lock boundaries; stale/background document mutation must be refused.
- Exercise project reload/rebind or backing-sidecar change around the document-lock boundary; stale project generations must fail before Model Space write access.
- Exercise stale/deleted selected source ObjectIds and generated-stirrup replacement; failure must leave native/project state rolled back before CAD commit.
- Exercise duplicate source ownership and multi-selected-source refusal.
- Confirm successful same-document execution preserves generated ownership metadata, semantic properties, project save behavior and post-commit UI refresh/status.
- Confirm cancellation or refusal leaves no newly generated geometry and no false success status.

Until those licensed cells actually run, report `LOCAL_ONLY / NO_RESULT`, never `LOCAL_PASS`.
