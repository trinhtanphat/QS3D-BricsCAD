# Work claim — Bulk previous Family structural freshness

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bulk-previous-family-structural-freshness`
- Registered: `2026-08-12T13:12:00+07:00`
- Baseline main SHA: `14412456376a50d96caaa4ef9f9e29228a41a581`
- Priority: P2 — fail closed when lazy bulk-Family target enumeration changes previous-Family ownership without advancing `ProjectState.ChangeVersion`.

## Confirmed defect

`BulkEditService.AssignFamily(ProjectState, IEnumerable<string>, string)` already rechecks the target Family and selected Element instances after caller-controlled target-ID enumeration. It does not preserve/recheck ownership of other Families that selected elements may currently reference. Direct replacement of a previous `ProjectFamily` instance in `project.Families` can keep the same ID and leave `ChangeVersion` unchanged. The later inherited-default migration then reads the replacement previous Family and can silently classify an inherited element property as an override, producing the wrong target-Family property result.

Concrete counterexample: an element currently references previous Family `F0` with inherited `Width=0.4`; target Family `F1` has `Width=0.8`. A lazy target enumerable replaces `F0` with a same-ID instance whose `Width=9.9` before yielding completes. Existing freshness checks pass, then assignment treats the element's `0.4` as an override and preserves it instead of migrating to `0.8`.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs`, limited to Family ownership freshness around ID-target enumeration in `AssignFamily(...)`
- focused Core smoke regression + ModuleInitializer registration under `tests/QS3D.Core.SmokeTests/`
- focused static preflight under `scripts/`
- `docs/plans/2026-08-12-bulk-previous-family-structural-freshness.md`
- this claim file

## Intended contract

- Snapshot unique project Family ID -> exact instance ownership before caller-controlled target-ID enumeration.
- Keep the existing `ChangeVersion`, target-Family, and selected-element freshness checks.
- After enumeration, reject Family count/null/duplicate/remove/replace drift before reading previous-Family defaults or computing inherited keys.
- Stable bulk Family assignment remains unchanged.
- No public API signature changes.

## Excluded scope

- Existing target-Family/selected-element structural freshness lane completed by `fff653c64e5891c43519dcd24619766b64d02319`.
- `ProjectFamilyService.Assign(...)`, Family activation, UI, and unrelated bulk property editing.
- GitHub Actions or licensed BricsCAD runtime qualification.
