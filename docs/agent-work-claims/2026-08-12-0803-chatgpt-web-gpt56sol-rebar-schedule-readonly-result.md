# Work claim — Rebar schedule structural read-only result

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rebar-schedule-readonly-result-20260812-0803`
- Registered: `2026-08-12T08:03:00+07:00`
- Completed: `2026-08-12T08:04:00+07:00`
- Baseline main SHA: `e391d9c2f44d48e6b66daa7e2e75736ed5eadd97`
- Claim commit: `c7a9e3c6c7bb78dca9c43c2001355b8e2492d957`
- Source commit: `9fde7bfd8097051aa160d1d33377157865bac8d7`
- Regression commit: `d1d42891bb6d4de3f81b8d6b4faeca557a175a24`
- Pre-close verification SHA: `d1d42891bb6d4de3f81b8d6b4faeca557a175a24`
- Priority: evidence-driven public result ownership during owner-requested `continue all`

## Confirmed defect

`RebarScheduleBuilder.Build(IEnumerable<RebarScheduleInput>)` returned an `IReadOnlyList<RebarScheduleRow>` but exposed the mutable backing `List<RebarScheduleRow>` directly. A caller could cast the result back to a mutable collection and structurally add, remove or clear rows after the schedule had been built.

## Completed change

`RebarScheduleBuilder.Build` now returns `rows.AsReadOnly()` only after the existing aggregate validation completes. Row ordering, row instances, notation parsing, quantity/spacing arithmetic, aggregate overflow checks and project schedule behavior are unchanged.

## Regression coverage

`RebarScheduleReadOnlyResultSmoke` builds one count row and one spacing row, preserves their expected order and quantities, asserts the returned `ICollection<RebarScheduleRow>` reports `IsReadOnly`, and verifies structural `Add` throws `NotSupportedException`.

## Scope respected

No BBS export UI, modeless ownership, quantity arithmetic, notation parsing, generated rebar, CAD/native behavior, Level placement, release/update or persistence changes were made. No deep-immutability redesign of mutable `RebarScheduleRow` objects was attempted.

## Validation evidence

The source was re-fetched from `main@d1d42891bb6d4de3f81b8d6b4faeca557a175a24` and the read-only return boundary is present. This web session performed source/static read-back only: no GitHub Actions dispatch, local `dotnet`/Core smoke execution, private-DWG execution or BricsCAD V25/V26 runtime qualification is claimed.
