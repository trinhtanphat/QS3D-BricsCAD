# Review modeless single-instance / wrapper-safe qualification

## Scope

C05 issue #4763 hardens the three document-bound review surfaces launched by `ReviewCommands`:

- `QS3DBBSVIEW` / `RebarScheduleWindow`
- `QS3DRECOGNIZE`, `QS3DRECOGNIZEAUTO`, `QS3DB4D` / `RecognitionWindow`
- `QS3DREVDIFF` / `RevisionWindow`

The source contract is one independently-owned window per review surface. Each publication records a stable non-zero native database identity plus the exact managed BricsCAD `Document` wrapper captured by callbacks.

## Source contract

For each surface:

1. If the published window is loaded and both native database identity and managed `Document` wrapper match the current document, activate/reuse it and do not construct another window.
2. If native DB differs, or BricsCAD has replaced the managed wrapper for the same native DB, request close of the old wrapper-bound window.
3. Close exception or veto fails closed: no replacement may be constructed while the exact old publication remains owned.
4. Stale unloaded publication may be defensively released.
5. Candidate construction is followed by exact `Closed` callback attachment, host `ShowModelessWindow`, `IsLoaded` confirmation, then publication.
6. Only the matching terminal `Closed` callback may clear the current slot; a stale callback cannot clear a newer publication.
7. Failed unpublished candidates are closed best-effort.

The lifecycle change must preserve existing behavior: BBS remains a detached/read-only preview with live freshness checks; Recognition keeps strict atomic/live-project apply semantics; Revision diff remains read-only until explicit locate behavior.

## Hosted deterministic validation

Run from the repository root:

```powershell
python scripts/preflight-review-modeless-single-instance.py
python scripts/preflight-modeless-review-document-binding.py
python scripts/preflight-modeless-review-windows.py
python scripts/preflight-bbs-modeless-project-safety.py
python scripts/preflight-recognition-atomic-batch.py
```

Then run the repository aggregate preflight and normal Core/V25 build path required by shared CI.

## LOCAL_ONLY licensed BricsCAD V25 matrix

This matrix is intentionally `LOCAL_ONLY`; hosted/source evidence must never be promoted to `LOCAL_PASS`.

Use a clean licensed BricsCAD V25 x64 process and the exact candidate DLL/SHA.

### A. Same-wrapper reuse

For each surface independently:

1. Open DWG A and invoke the command once.
2. Keep the review window loaded and invoke the same command again without closing/reopening the drawing.
3. Confirm the existing window activates and no second instance appears.
4. Exercise one safe read/locate action to prove callbacks still target DWG A.

Expected: one live window for that surface; no duplicate owner.

### B. Cross-DWG arbitration

1. With the surface open for DWG A, activate DWG B.
2. Invoke the same review command in DWG B.
3. Confirm the A-bound window reaches terminal close before a B-bound replacement appears.
4. If the old window can veto close in the test harness, trigger the veto and confirm no B replacement is published until the old owner actually closes.

Expected: never two published windows for the same surface.

### C. Managed-wrapper drift / same native database

Using the repository's supported wrapper-drift harness or a reproducible BricsCAD document-wrapper replacement scenario:

1. Open the surface against wrapper A for one native database.
2. Cause the managed `Document` wrapper to be replaced while retaining the same native database identity.
3. Invoke the same command through wrapper B.
4. Confirm wrapper A's window is not blindly reused; it must terminal-close before a B-bound replacement is published.

Expected: source-context callbacks never remain bound to an obsolete managed wrapper.

### D. Close exception/veto fail-closed

With a harness that makes the currently published window's `Close()` throw or remain loaded:

1. Request replacement from another document/wrapper.
2. Confirm command reports failure through existing QS3D error/status surfaces.
3. Confirm no replacement review window is created or published.
4. Release the veto/failure, close the old owner, invoke again, and confirm exactly one replacement opens.

### E. Independent-slot isolation

Open BBS, Recognition, and Revision review windows in the same source document where data prerequisites permit. Reinvoke each command independently.

Expected: each surface has its own publication slot; opening/reusing/closing one does not release or replace either of the other two.

## Evidence recording

Record exact git SHA, built DLL hash, BricsCAD V25 build, DWG identifiers, matrix row, observed window counts, and PASS/FAIL. If licensed host evidence is unavailable, record `PENDING_LOCAL` / `NOT_RUN`; do not infer runtime behavior from source review or CI.
