# LOCAL-006 — native documentation licensed qualification

**Owner boundary:** LOCAL_ONLY / licensed BricsCAD runtime  
**Source-ready baseline:** PR #3643 / `main@b3212c11ba1dfed04f4a7e1f1e0fd8670e3561a5`  
**Parent trackers:** #77, #72  
**Handoff reconciliation:** #3646  
**Lane-Key:** issue-3646

This runbook qualifies the native documentation source that is already implemented. It does **not** authorize a local worker to reimplement semantic tags, Tables, MLeader, Sheet/Layout/PaperSpace/Viewport or title-block source. A deterministic product defect found here is recorded as sanitized evidence and handed to a separate remote/source lane.

Remote/static CI, mock tests, source review, `-SkipRuntime`, or a successful managed build are never `LOCAL_PASS`.

## 1. Exact-SHA and safety rules

Before every qualification pass:

1. Read current `AGENTS.md`, `docs/AGENT-RUNTIME-CONTRACT.md`, `docs/LOCAL-AGENT-INBOX.md` and this file from current `origin/main`.
2. Refresh `origin/main` and record the exact intended SHA. Do not inherit an older result after production source materially changes the tested scenario.
3. Use a clean tracked worktree. Runtime helpers, evidence, disposable DWGs and screenshots remain ignored/local.
4. Close all unrelated BricsCAD processes before automation. Do not run against an operator-owned drawing/session.
5. Use repository-generated fixtures or explicitly authorized disposable copies only. Never modify the reference/original DWG.
6. Never commit BricsCAD proprietary DLLs, private/customer DWGs, raw local paths, ProjectIds, Handles, fingerprints, signing keys, credentials or unsanitized runtime captures.
7. Record plugin/Core ProductVersion and SHA-256 against the exact tested Git SHA.

## 2. Baseline gate — run first

From an interactive PowerShell session on the exact clean SHA:

```powershell
.\scripts\run-local-v25-qualification.ps1 `
  -BricsCadDir "C:\Program Files\Bricsys\BricsCAD V25 en_US" `
  -Profile "QS3D-V25-TEST"
```

The baseline must complete its applicable source guards, Core Release build, deterministic Core smoke, exact V25 adapter build and licensed load/runtime checks before the LOCAL-006 matrix proceeds. `-SkipRuntime` is diagnostic only and cannot qualify this lane.

If the baseline exposes a normal source/build defect, stop the LOCAL-006 runtime matrix, preserve sanitized evidence, and hand the defect to a remote/source owner. Do not patch general source opportunistically from a `local002`/`local003` role.

## 3. Test fixture preparation

Prepare at least two disposable DWG copies:

- **A**: valid existing QS3D project/sidecar with authoritative semantic source geometry, at least one geometry-backed semantic element and enough data to render the existing documentation Tables/schedules.
- **B**: a second independent valid QS3D project for multi-DWG isolation.

For sheet tests, prepare a disposable title-block block definition with representative attribute tags when title-block coverage is exercised. Keep a control copy with user/foreign PaperSpace content so refresh/remove protection can be tested.

Record the reference and disposable hashes before execution. Restore/remove all disposable state during cleanup.

## 4. Semantic MText regression

Existing MText behavior remains supported and shares the canonical generated semantic-tag ownership surface with MLeader.

Run `QS3DTAG`, `QS3DTAGREFRESH` and `QS3DTAGREMOVE` and verify:

- source-selection cancel returns before project bind/create/cache;
- placement-point cancel creates no MText and leaves semantic/audit state unchanged;
- cold-cache success binds the canonical same ProjectId and creates exactly the intended owned tag;
- refresh replaces only the complete validated owned tag set;
- remove validates ownership before erase;
- absent/replaced sidecar after preview/selection fails closed with no CAD/semantic deletion;
- generated/foreign/ambiguous source selection is refused;
- Undo/Redo, save/close and fresh-process reopen retain coherent ownership and placement metadata.

## 5. Semantic MLeader lifecycle

Use the production commands:

- `QS3DTAGLEADER`
- `QS3DTAGLEADERBATCH`
- `QS3DTAGLEADERREFRESH`
- shared `QS3DTAGREFRESH`, `QS3DTAGREMOVE` and tag Health/Release diagnostics where applicable.

### 5.1 Single MLeader

Verify:

1. source-selection cancel returns without project bind/create/cache;
2. cancel at arrow target creates no object/mutation;
3. cancel at text point creates no object/mutation;
4. valid cold-cache placement binds the same canonical ProjectId;
5. the live artifact is a native MLeader and carries current QS3D ownership plus stored target/text WCS metadata;
6. refresh reconstructs from the stored MLeader metadata and replaces only the validated complete owned set;
7. remove validates project/element/artifact ownership before erase;
8. changing/removing/replacing the sidecar or changing source ownership between preview and final mutation fails closed;
9. foreign, missing, malformed or wrong-version ownership is never erased or silently adopted;
10. supported planar UCS placement behaves coherently; unsupported/non-planar context refuses without residue;
11. Undo/Redo and save/cold-reopen preserve the intended artifact kind and ownership.

### 5.2 Bounded batch MLeader

Exercise `QS3DTAGLEADERBATCH` with zero, one, multiple and the practical bounded-selection cases. Verify deterministic ordering, one authoritative source per semantic element, replacement confirmation, complete-set validation before mutation and whole-batch atomicity. Cancel confirmation and injected/precondition failures must leave all prior tag objects and semantic metadata unchanged.

Do not treat a partial visual result as PASS when semantic ownership or the batch result is incomplete.

## 6. Existing native Table/schedule regression

Requalify the existing native documentation paths on the same exact binary so #3643 does not regress them:

- generic semantic Table;
- authoritative BQ Table;
- Door/Opening Table;
- Room Finish Table;
- Material Usage Table;
- BBS Table;
- persisted custom semantic schedules through `QS3DSCHEDULETABLE`, `QS3DSCHEDULETABLEREFRESH`, `QS3DSCHEDULETABLEHEALTH` and `QS3DSCHEDULETABLEREMOVE`.

Required observations include:

- at least two coexisting custom schedule owner slots;
- header-only zero-match schedule;
- prompt cancel before canonical bind;
- stale `ProjectId` / `ChangeVersion` refusal;
- definition edit/delete behavior;
- partial/corrupt metadata and foreign Table protection;
- stored position/content drift detected by Health;
- save/reopen, Undo/Redo and multi-DWG isolation;
- BQ preference writes bind canonical state while read-only/detached export/refresh paths do not dirty unrelated live project state.

## 7. Semantic Sheet / Layout / PaperSpace lifecycle

Use the production commands:

- `QS3DSHEETBUILD`
- `QS3DSHEETREFRESH`
- `QS3DSHEETREMOVE`
- `QS3DSHEETHEALTH`

The current source materializes existing Core `SemanticSheetPlan` / `SemanticViewPlan`; it is not a second sheet-planning engine.

### 7.1 Build

On a project with authoritative geometry:

1. create a new sheet number/name through `QS3DSHEETBUILD`;
2. verify the expected owned Layout exists;
3. verify the system paper viewport and every semantic viewport are classified correctly;
4. verify viewport center/size/view target/view height/custom scale are finite and consistent with the plan;
5. verify semantic viewports are locked after configuration;
6. verify project/sheet/view ownership and persisted handle metadata resolve to the live objects;
7. with a valid title-block definition, verify the BlockReference is created and mapped attributes reflect `SemanticTitleBlockParameterMapBuilder` output;
8. with no title block requested, verify no fabricated title-block artifact appears;
9. with missing/invalid required native input, verify failure is explicit and leaves no half-owned sheet state.

### 7.2 Refresh

Before refresh, add one clearly unowned/user PaperSpace object and retain a control hash/count. Run `QS3DSHEETREFRESH` and verify:

- confirmation precedes destructive mutation;
- the complete old QS3D-owned set is validated before erase/replacement;
- owned Viewports/title block are replaced coherently;
- unowned PaperSpace content is preserved;
- stale/missing/malformed/duplicate ownership fails closed before destructive partial replacement;
- semantic snapshot/native transaction rollback restores the pre-operation state on pre-commit failure;
- post-commit UI warning does not falsely report a valid committed native result as rolled back.

### 7.3 Remove

Run `QS3DSHEETREMOVE` on a clean fully-owned sheet and verify complete owned removal. Repeat with unowned/foreign PaperSpace content and verify removal refuses instead of deleting user content. Repeat after ownership drift/corruption and require fail-closed behavior.

### 7.4 Health

`QS3DSHEETHEALTH` is read-only. Exercise healthy state plus controlled drift for Layout, system paper viewport, semantic viewport, title block, scale/lock and ownership metadata. Before/after CAD and semantic state must be unchanged by Health itself.

## 8. Atomicity / failure injection

Using a repository-approved local test hook, debugger boundary or disposable harness, inject failures at representative stages:

- after semantic preparation but before native commit;
- after Layout creation but before all semantic viewports are complete;
- during viewport replacement;
- during title-block materialization/attribute binding;
- after native commit but during UI/palette refresh.

Required contract:

- every pre-commit failure leaves no half-created/half-erased owned set and restores semantic state;
- no foreign/user object is deleted;
- a post-commit UI failure may surface a warning but must not claim that the already committed CAD transaction was rolled back;
- a second attempt after recovery must not create duplicate ownership solely because the first UI refresh failed.

## 9. Lifecycle and isolation

On the same exact binary:

1. native Undo then Redo representative MLeader and Sheet operations;
2. `QS3DSAVE` / native DWG save as applicable;
3. close the document and reopen it in the same process;
4. close BricsCAD completely and perform a fresh-process cold reopen;
5. verify tag/Table/Sheet ownership, content and Health remain coherent;
6. perform Save As and verify drawing/project identity boundaries remain correct;
7. open A and B simultaneously, switch repeatedly, and verify no operation from A mutates B or vice versa;
8. keep relevant modeless documentation/review surfaces open across document switching/reload and verify stale callbacks cannot mutate a replaced project.

## 10. Unicode / DPI / visual acceptance

On licensed V25, inspect representative MText, MLeader, Table, title-block attributes and sheet UI with Vietnamese/long text at 100%, 125%, 150% and a representative 200% DPI setting. Verify no clipped critical command state, unreadable labels, misplaced leader text, unusable Table content or broken viewport/title-block presentation.

Only make visual source changes from reproducible real-host evidence under a separate source lane.

## 11. V26 representative parity

PR #3643 explicitly shares the V25 source into the V26 adapter path. V25 PASS is **not** V26 PASS.

When a licensed BricsCAD V26 host is available, rerun at least the representative subset on the same intended source lineage:

- NETLOAD / exact plugin identity;
- one MText and one MLeader create/refresh/remove cycle;
- one batch MLeader cycle;
- one custom schedule Table cycle;
- one Sheet build/Health/refresh/remove cycle with Viewport lock/scale and title block;
- Undo/Redo, save/cold-reopen and two-DWG isolation.

Record the exact V26 build/CLR/plugin identity separately from V25.

## 12. Evidence record

A sanitized result must include at minimum:

- exact Git SHA and whether it was current `origin/main` at start;
- Windows build and BricsCAD edition/version/build;
- plugin/Core ProductVersion and SHA-256;
- baseline qualification result;
- PASS/FAIL/NOT_RUN for every required matrix section;
- before/after aggregate native object counts and ownership-health status where useful;
- canonical-project continuity as a boolean/result, without publishing raw ProjectIds;
- Undo/Redo, save/reopen, fresh-process reopen and multi-DWG outcomes;
- exact-PID Application Error/Hang/.NET Runtime/WER observations where the runner audits them;
- reference/disposable hash preservation and cleanup result;
- zero remaining BricsCAD/helper processes and no probe environment/script residue.

Do not publish raw Handles, ProjectIds, drawing fingerprints, private paths, private screenshots or private/customer drawings.

## 13. Completion rule

`LOCAL-006` may be marked `PASS` only from actual licensed-host evidence tied to the exact tested SHA. A bounded subset may be recorded as `LOCAL_PASS` for that subset while the overall item remains `OPEN`/`IN_PROGRESS`.

If a runtime failure is reproducible:

1. leave the local item open;
2. post only sanitized exact-SHA evidence;
3. create/hand off a bounded remote/source defect lane;
4. do not broaden the local worker into opportunistic general source editing;
5. after the source fix merges, rerun the smallest affected licensed cell on the new exact intended SHA, then resume the remaining matrix.
