# Work claim — first Zone/Floor create revision canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-zone-floor-first-create-revision-20260813`
- Registered: `2026-08-13T19:59:00+07:00`
- Source fixed: `2026-08-13T20:09:00+07:00`
- Regression landed: `2026-08-13T21:19:00+07:00`
- Baseline main SHA: `af9e216176691ffd0d8f6942489ab50f347bf24d`
- Priority: P0 deterministic persisted mutation revision semantics.

## Defect and source fix

`ProjectZoneService.Create()` and `ProjectFloorService.Create()` previously called `project.Touch()` before adding the new definition and then auto-activated the first item through `ActiveZoneId` / `ActiveFloorId`. Because those persisted scalar setters own their own revision advance, first create advanced `ChangeVersion` twice while subsequent creates advanced once.

The source fix is already on `main`:

- Zone: `ec0617d6a350315b8891bc175e54c863149b3e15`, current source blob `77352af7ce79c04e7e0f3d691abfc06506fd98a9`.
- Floor: `ff09347e5b6400587112f68039b12cfa8c0187fa`, current source blob `3e5436f3bb3b5b08c47a4174aeaaa25ec658cc4e`.

Both Create paths now select exactly one revision owner: first create assigns the canonical persisted active id; later creates use `project.Touch()`. Collection insertion follows that branch.

## Regression

The originally planned executable C# smoke could not be written because the platform safety gate rejected executable test-file writes before mutation. Repository convention review then identified the existing combined domain gate `scripts/preflight-project-floor-zone-mutation-integrity.py` as the correct bounded regression target; no separate `preflight-floor*` gate exists.

Static regression commit: `b88c423fe00b6beea1a67bcf25df11faa7c582fe`.
Current regression blob: `1b02208d3ef4d0a8630ddc1728229f36c539a43c`.

The gate now:

- isolates both Floor and Zone `Create()` methods;
- requires first-create active-id assignment and subsequent-create `project.Touch()` as the mutually exclusive revision owners;
- requires collection insertion after revision-owner selection;
- rejects an extra pre-activation `project.Touch()`;
- pins `ActiveFloorId` and `ActiveZoneId` to `SetPersistedScalar(...)` and the checked `ChangeVersion + 1L` contract in `ProjectState`;
- preserves the existing Floor/Zone mutation-integrity checks.

## Coordination and validation

- Claim commit: `7479b252ee36dbf5fc1e0ee4ea79a69ad4f92316`.
- Scope amendment: `4d1e501a66a81212e79d756455ec082ce1157adc`.
- Earlier blocked-regression record: `b7a30eefe5bddd604e66d3695f0b31b0219f5780`.
- Static-regression resume amendment: `913d5f0c74c6e6f9283c0629d1ea3431d5061f7c`.
- Blocked static-write record: `fa60b8147b6d2b39c682d19fc55e7b137c8a1929`.
- Exact remote Floor/Zone source readback: PASS.
- Exact remote `ProjectState` persisted active-id contract readback: PASS.
- Exact remote regression readback after `b88c423f...`: PASS.
- Local execution of the Python preflight: `NOT_RUN` because this hosted workstream has no network-backed repository checkout; no GitHub Actions were dispatched.
- Managed smoke/native BricsCAD/package/release qualification: `NOT_RUN`; no BricsCAD V25 `LOCAL_PASS` is claimed.

## Completion

The source correction and bounded static regression are both on `main`. This reservation is closed. Future work may strengthen runtime/managed coverage independently without reopening this completed source/regression lane.
