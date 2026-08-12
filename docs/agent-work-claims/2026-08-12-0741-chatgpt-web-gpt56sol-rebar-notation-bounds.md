# Work claim — Rebar notation finite input bounds

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-rebar-notation-bounds-20260812-0741`
- Registered: `2026-08-12T07:41:00+07:00`
- Baseline main SHA: `aa8c696ce3f4d538ed81dddb1f76d43b4da4c13a`
- Priority: P2 evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Confirmed defect

`RebarNotationParser.Parse(string)` called `notation.Split('+')` before enforcing any notation-length or compound-group capacity. The public parser is also reached by `ProjectRebarScheduleBuilder` from persisted `RebarNotation` values, while `ProjectElement.SetProperty()` accepts arbitrary string values. A very large semantic notation could therefore force unbounded split-array/group allocation and regex work before the existing integer/finite guards ran.

## Reserved scope

- `src/QS3D.Core/Rebar/RebarNotationParser.cs`
- isolated focused Core smoke regression for notation capacity boundaries
- this claim file for close-out

## Contract implemented

- notation longer than 4096 UTF-16 characters is rejected before splitting or regex matching;
- more than 128 compound `+` groups are rejected before regex parsing;
- exactly 4096 characters and exactly 128 otherwise-valid groups remain accepted;
- existing grammar, whitespace behavior, finite-positive diameter/spacing checks and checked integer multiplication remain unchanged;
- `ProjectElement`'s general property-value policy and CAD/native/runtime paths are unchanged.

## Validation implemented

- focused module-initializer smoke covers 4096-character boundary acceptance and 4097-character rejection;
- focused smoke covers 128-group acceptance and 129-group rejection;
- ordinary `2x3D16+D12@200` count/spacing parsing remains covered;
- source and regression were re-read from moving `main` at `a4c44a495d5cf5a4c5977fa21565c496a8b16307`, confirming both changes remain integrated after concurrent commits.

## Integration commits

- Claim: `bcd93e77ef1c1232ee5d799e34f14f6f087ed590`
- Plan: `3cb79d13f2e2bdda30e4655f66fb43d5622ebc1c`
- Source fix: `2f94d74343d56886b1ba44a45cb76208fdbcadc2`
- Focused smoke regression: `6c80e26dbcfa99a9d56e1fd2728b7d00819eddc3`
- Pre-close main verification: `a4c44a495d5cf5a4c5977fa21565c496a8b16307`

## Validation boundary

Remote source/regression read-back only. No .NET build, GitHub Actions workflow, licensed BricsCAD V25/V26 runtime qualification or private-DWG/native execution is claimed by this web session.

## Completion condition

Completed: parser-local finite capacities are enforced before the expensive allocation/regex stages, exact boundaries and ordinary semantics are regression-locked in source, current `main` read-back confirms the integration, and the claim records the exact integration SHAs.
