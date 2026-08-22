# Work claim — Project quantity report group key collision

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T00:58:00+07:00`
- Completed: `2026-08-12T01:03:00+07:00`
- Baseline main SHA: `96719b870ed03cef9d66b8d278a9eada31a45e20`
- Priority: evidence-driven remote-safe reporting identity correctness

## Reason

`ProjectQuantityReportBuilder` built grouped row identity by concatenating floor id, zone id, category, family id, material and density with the raw `\u001f` delimiter. `ProjectFamily.Id` and project property values are trimmed/nonblank but are not forbidden from containing that character. Therefore distinct logical tuples could produce the same dictionary key and silently merge.

After inspecting the current density sentinel, the verified reachable counterexample uses the adjacent FamilyId/Material dimensions: `(FamilyId = "F\u001fM", Material = "X")` and `(FamilyId = "F", Material = "M\u001fX")`, with the same other grouping dimensions, serialize identically under the old delimiter concatenation. Both family ids are valid `ProjectFamily` identities and both elements can reference their corresponding family.

## Reserved scope

Replace the grouped project quantity report's delimiter-based composite key with an unambiguous length-prefixed key. Preserve detail-mode element identity, grouping dimensions, row order, quantity/mass/density arithmetic, selection behavior and public report types. Add focused CAD-independent regression coverage.

## Changed surfaces

- `src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs` (group key construction/helper only)
- `tests/QS3D.Core.SmokeTests/ProjectQuantityReportGroupKeySmoke.cs`
- this claim file

## Excluded scope

- Did not touch legacy `QuantityReportBuilder`; another agent owned that exact delimiter-collision scope.
- No changes to quantity formulas, negative-quantity policy, density parsing rules, references, UI/export/native behavior or BricsCAD runtime.
- No GitHub Actions dispatch.

## Completion

- Implementation commit: `64fb83263c56560191e25738c8fef20a77f58700` — replace raw grouped-key concatenation with a length-prefixed `CanonicalGroupKey` over the existing six grouping dimensions.
- Regression commit: `a3c794205790ed43cc5a2e1dc9144bdf667ff345` — construct the verified FamilyId/Material delimiter collision and assert two distinct rows retain their own identity/material/length values; also assert equivalent tuples still aggregate count/length normally.
- Final observed `main` before close: `34a9cea7d52c1afede22abb22d4ae8766ba28f1a`.
- Validation actually performed:
  - inspected `ProjectState`/`ProjectFamily` and `ProjectElement` construction/property behavior to establish the separator-bearing values are reachable;
  - re-fetched current `ProjectQuantityReportBuilder` and confirmed grouped mode now uses the length-prefixed canonical key while detail mode and report arithmetic remain unchanged;
  - re-fetched the dedicated smoke source and confirmed both collision separation and legitimate aggregation cases are present;
  - corrected the initial claim's illustrative counterexample after confirming current `DensityKey(null)` uses `<none>`; the implemented fix and verified FamilyId/Material collision were unaffected;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

A separate agent claimed the legacy `QuantityReportBuilder` group-key collision lane (`5508026994952e52aecd5daa68d85070a648d827`). This completed claim is limited to `ProjectQuantityReportBuilder` and does not overlap that file.

## Completion condition

Satisfied: current `main` cannot merge distinct project-backed quantity rows through delimiter injection in the grouped composite key, focused regression coverage is present, and this claim is released as `COMPLETED`.
