# Work claim — Quantity revision summary null-row integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-revision-null-row`
- Registered: `2026-08-12T00:19:00+07:00`
- Completed: `2026-08-12T00:22:00+07:00`
- Baseline main SHA: `fa73d76c8d76de5c53ebaa458a492d4b1716f0f0`
- Reservation commit: `f5f2ec2295c464f65b7ce7fb6209f2c094a0cf44`
- Priority: P2 — malformed public revision report input must not be silently dropped from summary processing.

## Defect fixed

`QuantityRevisionReport.Summarize(IEnumerable<QuantityRevisionRow>)` previously began with `rows.Where(x => x != null && !string.IsNullOrWhiteSpace(x.QuantityName))`, so a null row was silently ignored. This could hide corruption in caller-supplied review data.

Summary preprocessing now walks the input explicitly, rejects a null row with its index, and still skips non-null rows whose `QuantityName` is blank so element-only add/remove rows retain their prior behavior.

## Reserved scope

- `src/QS3D.Core/Revisions/QuantityRevisionReport.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRevisionSummaryNullRowSmoke.cs`
- this claim file

## Published commits

- `f844e03d38827b6b4986f60b21d4bf517a32245a` — reject null summary rows before grouping while preserving blank-quantity filtering.
- `32cdb7d547b95a42e3367a6385dfff395b1f6b9e` — add isolated auto-registered smoke covering null rejection, blank-row behavior, and case-insensitive aggregation.

## Delivered contract

- A null `QuantityRevisionRow` cannot disappear silently from summary input.
- Blank `QuantityName` rows remain intentionally ignored.
- Valid grouping, case-insensitive quantity names, finite/overflow math, ordering, and report build behavior are unchanged.

## Validation notes

- Exact source/test diffs were fetched after publication and are limited to reserved surfaces.
- The first source write encountered a concurrent 409; the target file was re-fetched, verified unchanged semantically, and the retry used the current blob without force-push or overwrite.
- Dedicated smoke auto-registers via `ModuleInitializer`; no shared test registry was edited.
- No GitHub Actions dispatch.
- This hosted environment does not provide the repository .NET/BricsCAD V25 qualification toolchain, so executable/native runtime PASS is not claimed.

## Completion condition

Satisfied for the remote-safe source/static contract. Exact executable/native qualification remains separate.
