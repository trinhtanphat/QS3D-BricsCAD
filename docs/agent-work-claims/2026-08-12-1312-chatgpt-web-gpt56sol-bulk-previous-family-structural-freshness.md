# Work claim — Bulk previous Family structural freshness

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bulk-previous-family-structural-freshness`
- Registered: `2026-08-12T13:12:00+07:00`
- Completed: `2026-08-12T13:25:00+07:00`
- Baseline main SHA: `14412456376a50d96caaa4ef9f9e29228a41a581`
- Priority: P2 — fail closed when lazy bulk-Family target enumeration changes previous-Family ownership without advancing `ProjectState.ChangeVersion`.

## Confirmed defect

`BulkEditService.AssignFamily(ProjectState, IEnumerable<string>, string)` already rechecked the target Family and selected Element instances after caller-controlled target-ID enumeration. It did not preserve/recheck ownership of other Families that selected elements may currently reference. Direct replacement of a previous `ProjectFamily` instance in `project.Families` could keep the same ID and leave `ChangeVersion` unchanged. The later inherited-default migration then read the replacement previous Family and could silently classify an inherited element property as an override, producing the wrong target-Family property result.

Concrete counterexample: an element references previous Family `F0` with inherited `Width=0.4`; target Family `F1` has `Width=0.8`. A lazy target enumerable replaces `F0` with a same-ID instance whose `Width=9.9`. Before this fix, the existing semantic-version, target-Family, and selected-element checks could all pass, and assignment could preserve `0.4` as an apparent override instead of migrating it to `0.8`.

## Delivered contract

- Snapshot unique project Family ID -> exact instance ownership before caller-controlled target-ID enumeration.
- Keep the existing `ChangeVersion`, target-Family, and selected-element freshness checks.
- After enumeration, reject Family count/null/duplicate/remove/same-ID replacement drift before reading previous-Family defaults or computing inherited keys.
- Stable lazy bulk Family assignment continues to migrate inherited defaults to the target Family.
- A previous-Family removal performed by an empty lazy input also fails closed before any assignment/no-op result.
- No public API signature changes.

## Evidence

- Claim: `a9f29988cd15f0f8e53b8402f145b69690b1a7aa`
- Plan: `b5daac1da22bee96fedb740c7b340b023ec96c9e`
- Source fix: `9744caa462ed7ce88d5e23bd60ad3ab6047d8204`
- Focused smoke: `22ce49693dfa0c3c8597761c85e115ff0ec8fcff`
- Smoke registration: `832bfdda38fd2e3ff467e97a44deadc61cdce29e`
- Static preflight: `0c21bc71c6d10481a5d8a4d2368d11c2e8673467`

Readback on current `main` confirmed Family ownership snapshot before target enumeration, the existing semantic-version check followed by full Family ownership recheck before previous-Family reads, exact-instance/count/duplicate guards, the stable/same-ID-replacement/removal-empty smoke cases, ModuleInitializer registration, and the static preflight after concurrent writes.

## Validation limits

The GitHub connector session did not execute the Core smoke executable, Python preflight, GitHub Actions, or licensed BricsCAD runtime. No PASS is claimed for those execution environments.

## Excluded scope

- Existing target-Family/selected-element structural freshness lane completed by `fff653c64e5891c43519dcd24619766b64d02319`.
- `ProjectFamilyService.Assign(...)`, Family activation, UI, and unrelated bulk property editing.
- GitHub Actions or licensed BricsCAD runtime qualification.
