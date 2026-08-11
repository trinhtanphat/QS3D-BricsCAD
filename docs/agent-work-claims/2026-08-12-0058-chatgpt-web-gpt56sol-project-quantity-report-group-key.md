# Work claim — Project quantity report group key collision

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:58:00+07:00`
- Baseline main SHA: `96719b870ed03cef9d66b8d278a9eada31a45e20`
- Priority: evidence-driven remote-safe reporting identity correctness

## Reason

`ProjectQuantityReportBuilder` builds grouped row identity by concatenating floor id, zone id, category, family id, material and density with the raw `\u001f` delimiter. Project property values are not forbidden from containing that character. Therefore distinct logical tuples can produce the same dictionary key. A reachable example with no project references required is `(Material = "M\u001f1", DensityKgPerM3 absent)` versus `(Material = "M", DensityKgPerM3 = "1")`: both serialize the final material/density key components identically, so distinct report rows can be silently merged.

## Reserved scope

Replace the grouped project quantity report's delimiter-based composite key with an unambiguous length-prefixed key. Preserve detail-mode element identity, grouping dimensions, row order, quantity/mass/density arithmetic, selection behavior and public report types. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs` (group key construction/helper only)
- `tests/QS3D.Core.SmokeTests/ProjectQuantityReportGroupKeySmoke.cs`
- this claim file

## Excluded scope

- Do not touch legacy `QuantityReportBuilder`; another active/recent agent lane owns that exact delimiter-collision scope.
- No changes to quantity formulas, negative-quantity policy, density parsing rules, references, UI/export/native behavior or BricsCAD runtime.
- No GitHub Actions dispatch.

## Validation plan

- Build two valid `ProjectElement` rows with identical category/reference dimensions but colliding legacy material/density text and assert grouped output contains two distinct rows rather than one merged row.
- Assert each row retains its own material/density/quantity values.
- Preserve a normal same-key aggregation case to verify legitimate grouping still combines quantities.
- Re-fetch current source blob before write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

A separate agent has claimed the legacy `QuantityReportBuilder` group-key collision lane (`5508026994952e52aecd5daa68d85070a648d827`). This claim is limited to `ProjectQuantityReportBuilder` and does not overlap that file.

## Completion condition

Current `main` cannot merge distinct project-backed quantity rows through delimiter injection, focused regression coverage is present, and this claim is marked `COMPLETED`.
