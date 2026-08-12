# Work claim — Curtain wall schedule structural read-only result

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-curtain-schedule-readonly-result-20260812-0816`
- Registered: `2026-08-12T08:16:00+07:00`
- Completed: `2026-08-12T08:17:00+07:00`
- Baseline main SHA: `8462d9577de021e56d028f040a3a04264636e317`
- Claim commit: `3b61fc871254b44481a31616fbbebf73588062dd`
- Source commit: `e4842c86e1fb5c64eeb87dc71fc5ad1a1a3115ba`
- Regression commit: `f1ddad53ddd4a1a7515aa2b2c35000aaaee993f8`
- Priority: evidence-driven Reporting result ownership during owner-requested `continue all`

## Confirmed defect fixed

`CurtainWallScheduleBuilder.Build(ProjectState)` declares `IReadOnlyList<CurtainWallScheduleRow>` but previously returned `order.Select(...).ToList()` directly. Callers could cast the returned value to a mutable collection and structurally add, remove or clear rows after schedule aggregation was complete.

## Completed change

The completed curtain schedule list is now wrapped with `.AsReadOnly()` before return. Ordering, grouping keys, wall/panel/frame quantities, min/max values, project/drawing identity, provenance, and existing validation/overflow behavior are unchanged. No deep-immutability redesign of `CurtainWallScheduleRow` was made.

## Regression evidence

`CurtainWallScheduleReadOnlyResultSmoke` builds a minimal one-wall semantic schedule, preserves `WallCount=1` and element identity, requires the returned `ICollection<CurtainWallScheduleRow>` to be read-only, and proves structural `Add` throws `NotSupportedException`.

## Read-back validation

Current `main` source was re-fetched after source/regression publication and still contains `ToList().AsReadOnly()`. The focused smoke was also re-fetched from `main` and retains the expected row/count and mutation-boundary checks.

## Excluded scope respected

No curtain generation/regeneration, frame geometry, ownership metadata, UI/modeless lifetime, XLSX export, CAD/native behavior, persistence or release/update changes were made.

## Validation boundary

Remote source/smoke read-back only. No GitHub Actions were dispatched; no executable Core build/smoke PASS and no BricsCAD V25/V26 runtime qualification are claimed.
