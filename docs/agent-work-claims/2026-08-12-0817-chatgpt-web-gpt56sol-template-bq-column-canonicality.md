# Work claim — Template BQ column canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-template-bq-column-canonicality-20260812-0817`
- Registered: `2026-08-12T08:17:00+07:00`
- Baseline main SHA: `e4842c86e1fb5c64eeb87dc71fc5ad1a1a3115ba`
- Priority: evidence-driven persisted-format integrity during owner-requested `continue all`

## Confirmed defect

`TemplateProfileStore.Serialize(...)` emits visible BQ columns as trimmed, case-insensitive-distinct values sorted with `StringComparer.OrdinalIgnoreCase`. `Load(...)` currently accepts each `<column name="...">` through `Required(...)`, which trims the raw XML and appends it without checking duplicate/order canonicality. A persisted template can therefore contain padded names, duplicate/case-duplicate names, or noncanonical order and be silently rewritten on the next save.

## Reserved scope

- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- one isolated Core smoke file for persisted BQ-column canonicality
- this claim file for close-out

## Contract

- Persisted `<bqColumns>` names must already be nonblank, unpadded, case-insensitive unique, and in the exact ordering emitted by `Serialize(...)`.
- Reject lossy persisted variants rather than silently normalizing them during load/save.
- Preserve programmatic profile save behavior and valid canonical template round trips.
- Do not change family/rule category semantics, layer mappings, template import UI/lifecycle, BricsCAD runtime, or release behavior.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Source fix plus deterministic isolated Core smoke coverage are integrated on current `main`, resulting source is re-read, and this claim is marked `COMPLETED` with exact integration SHA/evidence.
