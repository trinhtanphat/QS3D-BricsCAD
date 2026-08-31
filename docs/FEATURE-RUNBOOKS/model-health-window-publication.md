# Model Health window publication / refresh lifecycle

Carrier: #4773  
Lane-Key: `issue-4773`

## Source contract

`ModelHealthWindow` is the single process-wide publication owner for Model Health review snapshots. Each Health invocation still recomputes a fresh snapshot. A new snapshot does **not** reuse an older document-bound window: the previous owner must reach terminal close before the new candidate may reserve publication.

The ownership lifecycle is:

1. constructor attaches its own publication `Loaded` / `Closed` callbacks;
2. candidate reserves `_pendingPublication` only after any previous pending/published owner has terminally released;
3. only after reservation does document-bound lifetime attach, with rollback/close if setup fails;
4. host `ShowModelessWindow` loads the candidate;
5. matching `Loaded` promotes exact pending candidate to `_published`;
6. matching terminal `Closed` releases exact pending/published ownership;
7. close exception/veto is fail-closed and replacement is refused;
8. an unloaded **published** stale owner may be defensively released, but an unloaded pending candidate is never cleared merely because `IsLoaded == false`.

Migrated Health commands use `ModelHealthWindowPresenter.Show(...)`. The presenter does not keep a second static ownership registry; it only guarantees candidate cleanup when host publication fails. Legacy/base `QS3DHEALTH` remains an explicit-document caller and is covered by the ownership contract enforced inside `ModelHealthWindow` itself.

## Hosted deterministic acceptance

Run the discovered feature guards, including:

```text
python scripts/preflight-model-health-window-publication.py
python scripts/preflight-modeless-review-document-binding.py
python scripts/preflight-health-all-command-error-redaction.py
python scripts/preflight-rebar-health-all-locate-lifecycle.py
python scripts/preflight-generated-ownership-health-readonly.py
python scripts/preflight-room-finish-health.py
```

Then run the repository protected Shared CI matrix. Hosted acceptance covers source contracts, Core deterministic smoke, trusted V25 references and V25 adapter/plugin compilation. It is not licensed BricsCAD runtime evidence.

## LOCAL_ONLY licensed BricsCAD V25 matrix

Record exact repository SHA, BricsCAD V25 build, DWG identity and result for every executed cell. Do not convert NOT_RUN into PASS.

### A. Fresh same-DWG replacement

1. Open a DWG with an existing QS3D project.
2. Run `QS3DHEALTH`; keep Model Health open.
3. Change semantic state through a normal QS3D workflow so a recomputed health snapshot is observably different.
4. Run `QS3DHEALTH` again.
5. Verify the original review window reaches terminal close before the replacement appears, only one Model Health window remains, and the replacement shows the new counts/state.
6. Repeat with `QS3DHEALTHALL`, `QS3DREBARHEALTH`, `QS3DREBARHEALTHALL`, `QS3DREBARTIEHEALTH`, `QS3DREBARMODEHEALTH`, `QS3DROOMFINISHHEALTH`, `QS3DREBARSHAPEHEALTH`, `QS3DFOUNDATIONREBARHEALTH`, `QS3DCURTAINFRAMEHEALTH`, `QS3DHANDLEHEALTH`, `QS3DOWNERSHIPHEALTH`, and `QS3DRELEASECHECK` where applicable.

Expected: every invocation computes a fresh snapshot; no duplicate/stale Model Health window accumulates.

### B. Cross-DWG replacement

1. Open DWG A and DWG B, each with an existing QS3D project.
2. In A run a Health command and leave its window open.
3. Activate B and run a Health command.
4. Verify A's window terminally closes before B's replacement is published.
5. In B use Locate and verify it resolves against B's current semantic project and CAD handles.

Expected: one Model Health surface process-wide; no stale A callback is invoked from B.

### C. Managed-wrapper drift, when reproducible

If the licensed host/harness can reproduce a managed `Document` wrapper replacement for the same native database, leave a Health window open across the drift and rerun Health.

Expected: the old wrapper-bound snapshot is terminally replaced; it is never blindly reused merely because the native database is unchanged.

If wrapper drift cannot be reproduced deterministically, record `NOT_RUN` with host/build reason.

### D. Close veto / close failure

Using the approved local UI harness if available, attach a temporary WPF `Closing` cancellation/failure probe to the currently published Model Health window, then invoke another Health command.

Expected: replacement is refused, the original owner remains authoritative, and no second Model Health window is published. Remove the probe and retry; replacement should then succeed.

If the approved harness cannot inject this condition, record `NOT_RUN`; source preflight remains the deterministic remote contract.

### E. Host-show failure cleanup

Using an approved local host harness if available, force/observe `ShowModelessWindow` failure after candidate creation.

Expected: presenter closes/abandons the candidate and a subsequent normal Health invocation can publish successfully without a stranded pending owner.

If the failure cannot be injected safely, record `NOT_RUN`.

### F. No-project read-only boundary

On a DWG without an existing QS3D project/sidecar, run the Health commands that advertise read-only behavior.

Expected: command reports BLOCKED/no-project as before, does not create/cache a project merely to inspect health, and no Model Health window is published.

## Evidence boundary

Only a licensed local BricsCAD V25 execution of the applicable matrix may be reported as `LOCAL_PASS`. Hosted CI/source guards/adapter builds must be reported separately and never promoted to native runtime evidence.
