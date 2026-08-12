# Work claim — Grid renumber structural read-only result

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-grid-renumber-readonly-result-20260812-0838`
- Registered: `2026-08-12T08:38:00+07:00`
- Completed: `2026-08-12T08:39:00+07:00`
- Baseline main SHA: `7805ab5d978147ce01f083dc98f4393e9537af04`
- Claim commit: `bbbca9ea8674dacef9a471aff2916eb1d044de1e`
- Source commit: `15e4c9556ac5c5d137610742a99ec984f64e3fd5`
- Regression commit: `35518d53f8be7d092612446fe5c2598fdece6250`
- Priority: evidence-driven public result ownership during owner-requested `continue all`

## Confirmed defect fixed

`GridNamingService.Renumber(...)` declares `IReadOnlyList<GridLabelAssignment>` but previously returned its mutable backing `List<GridLabelAssignment>` directly. A caller could cast the returned plan to `ICollection<GridLabelAssignment>` and structurally add, remove or clear assignments after the renumber result had been published.

## Completed change

The completed renumber plan is now returned through `plan.AsReadOnly()`. Input cap, sequence/label validation, target resolution, reserved-label collision checks, canonical no-op behavior, project Touch semantics and element property mutations are unchanged. `GridLabelAssignment` remains immutable as-is.

## Regression evidence

`GridRenumberReadOnlyResultSmoke` renumbers two ordinary Grid elements in explicit order, verifies labels and semantic properties, requires the returned `ICollection<GridLabelAssignment>` to be read-only, and proves structural `Add` throws `NotSupportedException`.

## Read-back validation

Current `main` source was re-fetched after source/regression publication and contains `return plan.AsReadOnly();`. The focused smoke was also re-fetched from `main` with assignment/property and mutation-boundary checks intact.

## Coordination respected

Recent Grid naming bounded-enumeration, reserved-label-integrity and null-health contracts remain unchanged. No health provider, Grid annotation/native generation, command lifecycle or existing smoke/preflight file was edited.

## Validation boundary

Remote source/smoke read-back only. No GitHub Actions were dispatched; no executable Core build/smoke PASS and no BricsCAD V25/V26 runtime qualification are claimed.
