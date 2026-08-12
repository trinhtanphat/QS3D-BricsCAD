# Work claim — Rebar notation finite input bounds

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-notation-bounds-20260812-0741`
- Registered: `2026-08-12T07:41:00+07:00`
- Baseline main SHA: `aa8c696ce3f4d538ed81dddb1f76d43b4da4c13a`
- Priority: P2 evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Confirmed defect

`RebarNotationParser.Parse(string)` currently calls `notation.Split('+')` before enforcing any notation-length or compound-group capacity. The public parser is also reached by `ProjectRebarScheduleBuilder` from persisted `RebarNotation` values, while `ProjectElement.SetProperty()` accepts arbitrary string values. A very large semantic notation can therefore force unbounded split-array/group allocation and regex work before the existing integer/finite guards run.

## Reserved scope

- `src/QS3D.Core/Rebar/RebarNotationParser.cs`
- isolated focused Core smoke regression for notation capacity boundaries
- this claim file for close-out

## Contract

- reject notation longer than 4096 UTF-16 characters before splitting or regex matching;
- reject more than 128 compound `+` groups;
- accept exactly the supported length/group boundaries when the notation is otherwise valid;
- preserve existing grammar, whitespace behavior, finite-positive diameter/spacing checks and checked integer multiplication;
- do not change `ProjectElement`'s general property-value policy or any CAD/native/runtime path.

## Validation plan

Add isolated module-initializer smoke coverage for 4096-character boundary acceptance, 4097-character rejection, 128-group acceptance, 129-group rejection, and one ordinary spacing/count parse. Re-fetch moving `main` before source integration and preserve concurrent history.

No GitHub Actions dispatch and no BricsCAD V25 runtime PASS claim from this web session.
