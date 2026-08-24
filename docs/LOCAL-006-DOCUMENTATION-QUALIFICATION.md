# LOCAL-006 licensed documentation qualification

This runbook is the canonical licensed-runtime handoff for `LOCAL-006` after the native documentation source completion merged through PR #3643.

Source/static/hosted CI is **not** `LOCAL_PASS`. Execute this matrix only on licensed BricsCAD hosts, record sanitized evidence against one exact tested commit, and hand any normal source defect back to a bounded remote/source lane instead of patching production source opportunistically from the local lane.

## 1. Exact-SHA and baseline rule

1. Start from a clean checkout of the newest **intended** source SHA named by `docs/LOCAL-AGENT-INBOX.md` / the active LOCAL-006 issue handoff. Do not silently substitute a newer moving `main` after the run starts.
2. Record the exact 40-character commit SHA before building. All V25/V26 evidence in one qualification batch must identify the exact source it exercised.
3. Begin with the repository baseline in `docs/LOCAL-V25-QUALIFICATION.md` and `scripts/run-local-v25-qualification.ps1`. A baseline failure is not a documentation-layer PASS.
4. Build the V25 adapter from that exact SHA and verify the loaded plugin identity before interactive execution.
5. Build/run the repository V26 adapter from the same intended source revision for the representative parity cells below. V26 evidence must name the actual V26 host/plugin identity; do not infer parity only because source is linked/shared.
6. Use only disposable/repository-approved fixtures. Never commit or paste proprietary DLLs, customer/private DWGs, raw local paths, ProjectIds, Handles, fingerprints, credentials, or unsanitized crash captures.

## 2. Result vocabulary

Use exactly one terminal result for the bounded matrix:

- `LOCAL_PASS`: every required V25 cell and the representative V26 parity cells pass on the recorded exact SHA.
- `LOCAL_FAIL`: at least one required licensed cell reproducibly violates a current contract. Record the smallest sanitized repro and create/hand off a bounded source defect.
- `NO_RESULT`: host/license/environment prevented a trustworthy result. Preserve no fake PASS; retry only under the repository's bounded local retry policy.

Cancellation or user refusal is a test input in the cells below; it must not be reclassified as an environment `NO_RESULT` when the expected behavior can be observed.

## 3. Required fixtures and invariants

Prepare disposable DWGs/project state that cover:

- a cold-cache drawing with a valid existing `.qsdb` and at least one semantic element with one authoritative live CAD source;
- an absent-project/absent-sidecar drawing;
- a second independent DWG/project for multi-document isolation;
- at least two semantic schedule definitions plus one definition that intentionally produces zero matching rows;
- a valid title-block block definition with the tags consumed by the current sheet command mapping;
- a controlled foreign/unowned PaperSpace object and a controlled foreign/unowned Table/tag artifact for protection checks;
- Unicode text representative of Vietnamese content;
- a display configuration suitable for a HiDPI UI/rendering check.

For every mutating cell, capture only sanitized before/after invariants sufficient to prove:

- canonical project continuity or intentional refusal;
- no cross-DWG/project mutation;
- ownership is exact before replacement/removal;
- semantic/audit state changes only when the native transaction is intended to commit;
- failure/cancellation leaves no replacement project/cache/sidecar or partial generated artifact;
- save/cold-reopen restores the intended owned artifact state.

## 4. Semantic MText matrix

Use the existing `QS3DTAG`, `QS3DTAGREFRESH`, and `QS3DTAGREMOVE` lifecycle.

### 4.1 Cancellation before mutation

- Cancel source selection for `QS3DTAG`: no project bind/create/cache, no MText, no semantic/audit mutation.
- Select a valid authoritative source, then cancel the placement point: same no-mutation/no-bootstrap result.
- Cancel source selection for `QS3DTAGREFRESH`: no bind/create/cache and no replacement.
- Cancel tag/source selection for `QS3DTAGREMOVE`: no bind/create/cache and no erase.

### 4.2 Freshness and canonical ownership

- With a valid selected source, invalidate/replace/remove the project authority after preview/selection but before the final write boundary. Placement must fail closed on project/source-owner freshness and leave CAD/semantic state unchanged.
- From a cold cache with a valid sidecar, place a tag successfully and prove the final write binds the canonical existing project.
- Refresh must replace only the complete owned tag set.
- Remove must erase only an owned tag. An absent/replaced sidecar or ownership mismatch must refuse without deleting CAD or semantic metadata.

### 4.3 Persistence

- Verify generated ownership/health before save.
- Exercise native Undo then Redo and confirm the semantic/native ownership contract remains coherent.
- Save, fully close the document/host as required by the baseline, cold-reopen, and verify the tag plus canonical project relationship is healthy.

## 5. Semantic MLeader matrix

The source-ready commands include `QS3DTAGLEADER`, `QS3DTAGLEADERBATCH`, `QS3DTAGLEADERREFRESH`, and the shared tag refresh/remove/health paths. Do not replace the native MLeader with line+MText stand-ins.

### 5.1 Create and associative refresh

- Create one MLeader from an authoritative source using a distinct arrow target and text point.
- Verify one owned MLeader is produced, its semantic text is correct, and its stored target/text association is sufficient for refresh.
- Run `QS3DTAGLEADERREFRESH` and the shared `QS3DTAGREFRESH` path for an MLeader-owned element; both must preserve artifact kind and replace only the correct owned artifact.
- Move/drift the native artifact in a controlled way and confirm runtime health reports drift without mutating it.

### 5.2 Batch atomicity

- Run `QS3DTAGLEADERBATCH` with multiple distinct authoritative sources and confirm deterministic bounded placement plus one owner per semantic element.
- Include a replacement case where existing generated tags require the explicit confirmation path.
- Cancel/refuse at confirmation and prove no partial replacement.
- Introduce one invalid/foreign/stale member and verify the batch fails closed without silently committing a prefix of the batch.

### 5.3 Removal, Undo/Redo, reopen

- Remove a valid owned MLeader through the current semantic-tag removal path.
- Verify foreign/corrupt ownership blocks destructive removal.
- Exercise Undo/Redo for successful create/replace/remove cells.
- Save/cold-reopen and rerun health/refresh on a persisted MLeader.

## 6. Native Table and custom schedule matrix

Qualify the already-implemented generic/authoritative BQ, Door/Opening, Room-Finish, Material/BBS Table lifecycles first; do not reimplement them.

For review/modeless paths, preserve the established boundary: true preference/ownership writes bind canonical state, while read-only refresh/export/regeneration paths that are designed as detached snapshots must leave live dirty/change-version/timestamp/audit state unchanged.

Then exercise the custom schedule commands:

- `QS3DSCHEDULETABLE` create/update;
- `QS3DSCHEDULETABLEREFRESH`;
- `QS3DSCHEDULETABLEHEALTH`;
- `QS3DSCHEDULETABLEREMOVE`.

Required cells:

- two or more coexisting schedules with independent owner slots/fingerprints;
- one header-only zero-match schedule;
- prompt cancellation before canonical bind;
- project `ProjectId`/`ChangeVersion` freshness change while a prompt/review is open;
- schedule definition edit and delete;
- partial/corrupt metadata;
- foreign Table ownership;
- stored WCS position drift and content drift reported by health without mutation;
- ModelSpace/UCS restriction behavior;
- Unicode content and HiDPI readability;
- Undo/Redo, save/cold-reopen, and multi-DWG isolation.

Normal save is the expected persistence path after a successful owned mutation. A fallback/recovery path, if the current command reports one, is evidence of a degraded or refused save boundary and must be recorded as such; do not count fallback as equivalent to a verified normal save unless the active source contract explicitly says so. In either mode, a failure must not leave partial semantic/native ownership or silently create a replacement project.

## 7. Sheet / Layout / PaperSpace / Viewport / title-block matrix

Use the source-ready commands:

- `QS3DSHEETBUILD`;
- `QS3DSHEETREFRESH`;
- `QS3DSHEETHEALTH`;
- `QS3DSHEETREMOVE`.

The current command builds a planned overview from project elements with authoritative geometry, creates a `QS3D-<sheet-number>` Layout, materializes PaperSpace Viewport content from the existing Core sheet/view plans, optionally inserts the requested title-block block, and requires explicit confirmation before destructive refresh/remove.

### 7.1 Build

- Build a new sheet from a cold-cache valid project.
- Verify the generated Layout name, PaperSpace ownership marker, expected Viewport count, Viewport center/size/view target/height/custom-scale semantics, and `Locked=true` after configuration.
- With a valid title-block definition, verify one owned BlockReference and mapped attribute values for the current sheet fields.
- With no title-block name, verify the sheet remains valid without one.
- A missing/invalid requested title-block definition must fail closed without leaving a half-owned sheet lifecycle.

### 7.2 Refresh and foreign content

- Add controlled unowned PaperSpace content, then refresh. The refresh path must preserve unowned content while replacing only the validated QS3D-owned Viewport/title-block set.
- Corrupt or cross-project one owned marker and verify refresh refuses before destructive replacement.
- Change project authority/freshness around the interactive boundary and verify no stale sheet mutation commits.
- Confirm health is read-only and reports ownership/geometry/content drift without repairing it.

### 7.3 Remove

- `QS3DSHEETREMOVE` must require explicit confirmation.
- On a fully owned removable layout, remove succeeds and returns away from the target layout when required.
- Presence of foreign/unowned live PaperSpace content must block whole-layout removal rather than deleting user content.
- Malformed/cross-project ownership must block removal.

### 7.4 Viewport transform semantics

For representative transformed rebar/model output used by a semantic view:

- Exercise a planar affine transform that the current native/Core contract supports. Verify the resulting view extents, center/target, scale and locked viewport remain finite, bounded and visually consistent with the authoritative live geometry.
- Exercise a deliberately unsupported/non-affine or otherwise non-representable transform boundary if the current host/source exposes one. The expected result is the current fail-closed contract: refusal/health issue without fabricating transformed geometry or silently accepting corrupt extents. Record the exact command message/code, not a guessed mathematical interpretation.
- If the exact current source does not expose a separate non-affine transform path for this sheet materializer, record the cell as `NOT_APPLICABLE_BY_CURRENT_CONTRACT` with the source SHA and do not invent a PASS condition.

### 7.5 Persistence and isolation

- Undo/Redo successful build/refresh/remove operations where the host supports them and verify ownership remains coherent.
- Save normally, close, cold-reopen, run health, and verify Layout/PaperSpace/Viewport/title-block ownership plus scale/lock state persists.
- Repeat a representative create/health flow in a second DWG and prove no cross-DWG Layout, handle, project or audit mutation.
- Repeat representative sheet text/title-block content with Unicode and visually inspect at the HiDPI configuration.

## 8. V26 representative parity

After the full V25 matrix passes, execute on licensed V26 using the adapter built from the same intended source revision:

1. one successful MText create/refresh/health cycle;
2. one successful MLeader create/refresh/health cycle plus one batch replacement/cancel cell;
3. one native custom schedule create/health/save-cold-reopen cycle including Unicode;
4. one Sheet build/health/save-cold-reopen cycle with title block and a locked Viewport;
5. one foreign/unowned-content destructive-operation refusal;
6. one cancellation/freshness refusal proving no project bootstrap/partial native mutation;
7. one second-DWG isolation check.

Any V25/V26 behavioral mismatch is a `LOCAL_FAIL` for parity until triaged; do not hide it behind linked/shared-source expectations.

## 9. Cleanup and evidence

After each host batch:

- close documents/host through the normal graceful path;
- verify no matching BricsCAD/helper process residue remains;
- restore/delete disposable project/DWG artifacts and any local automation state;
- preserve repository source and protected fixture bytes;
- keep raw logs/screenshots local/ignored when they may contain private data;
- post only sanitized evidence.

Minimum sanitized evidence payload:

```text
LOCAL-006 result: LOCAL_PASS | LOCAL_FAIL | NO_RESULT
Exact QS3D SHA: <40-char SHA>
Host: BricsCAD V25 <build> | V26 <build>
Plugin identity: <sanitized version/hash>
Baseline: PASS | FAIL
MText: PASS | FAIL
MLeader single/batch/refresh/remove/health: PASS | FAIL
Native Tables/custom schedules: PASS | FAIL
Sheet/Layout/PaperSpace/Viewport/title-block: PASS | FAIL
Ownership + foreign-content protection: PASS | FAIL
Atomic rollback/cancellation/freshness: PASS | FAIL
Undo/Redo: PASS | FAIL
Normal save + cold reopen: PASS | FAIL
Multi-DWG isolation: PASS | FAIL
Unicode/HiDPI: PASS | FAIL
V26 representative parity: PASS | FAIL | NOT_RUN
Cleanup/process residue: PASS | FAIL
Sanitized defect reference: <issue or none>
```

Do not include raw local paths, ProjectIds, Handles, fingerprints, customer drawing names/content, credentials, or proprietary binaries/hashes that repository policy forbids publishing.

## 10. Handoff on failure

When a licensed cell fails:

1. stop extending the local scope;
2. confirm the failure belongs to the exact tested SHA and is not a stale binary/host mismatch;
3. reduce it to the smallest sanitized reproducible scenario;
4. open/update one bounded source issue/lane with expected vs actual behavior and exact SHA;
5. leave LOCAL-006 `OPEN` / `PENDING_LOCAL` (or the repository's current blocked form) until a source fix merges and a new exact candidate is explicitly repinned;
6. never patch normal production source opportunistically from the LOCAL_ONLY lane.

When every required V25 cell and V26 parity cell passes, update `docs/LOCAL-AGENT-INBOX.md` with the exact sanitized evidence and only then advance LOCAL-006 to `PASS` under the repository's normal branch/PR policy.
