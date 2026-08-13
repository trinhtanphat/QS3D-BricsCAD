# Work claim — MTR-02A adjustment rule identity provenance

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-mtr02-adjustment-rule-identity-20260813-1305`
- Workstream: `MeasurementTrace / MTR-02 / P0` — deduction/addition rule identity sub-lane only
- Claimed UTC: `2026-08-13T06:05:00Z`
- Last updated UTC: `2026-08-13T06:15:00Z`
- Baseline main SHA: `76a1e760c78f1146fa528dcf11e906fecaa532e0`

## Confirmed gap

The baseline canonical `MeasurementTrace` already carried optional trace-level `RuleId` / `RuleVersion`, but `MeasurementTraceAdjustment` recorded only kind, amount, unit, reason and source identity. Therefore a deduction/addition could explain which source was adjusted without identifying which versioned rule/policy authorized that adjustment. This was the deterministic deduction-identity part of MTR-02, distinct from profile-definition work and from rule evaluation itself.

Baseline readback also showed canonical serialization was explicitly versioned as `MTR1`, so the implementation had to preserve byte-for-byte `MTR1` serialization and the legacy public constructor contract for existing/no-adjustment-rule callers.

## Reserved files

- `src/QS3D.Core/Measurement/MeasurementTrace.cs`
- `tests/QS3D.Core.SmokeTests/MeasurementTraceContractSmoke.cs`
- this claim file

## Implemented scope

- Extended `MeasurementTraceAdjustment` with optional canonical `RuleId` + `RuleVersion` metadata using the existing token validation rules.
- The rule pair is fail-closed: supplying only one side throws before a valid adjustment can be created.
- Adjustment equality includes rule identity/version. Hashing includes the pair only when present, preserving the prior no-rule adjustment hash path.
- Preserved the exact public five-argument `MeasurementTraceAdjustment` constructor as a delegating overload for binary/source compatibility; the rule-aware constructor is an additive overload.
- Preserved legacy `MTR1` canonical serialization whenever every adjustment has no rule identity. Rule-aware traces switch to the explicit `MTR2` schema and serialize nullable rule-id/version slots for every adjustment.
- Extended canonical adjustment ordering with `RuleId` then `RuleVersion` so `MTR2` output is independent of caller enumeration order when otherwise-identical adjustments differ only by rule provenance.
- Added focused smoke coverage for paired validation, rule-aware equality/hash, exact legacy `MTR1` bytes, deterministic `MTR2` ordering, and reflection verification that the legacy five-argument constructor still exists.
- No quantity calculation, rule evaluation, Takeoff, report/UI, persistence, BricsCAD/native or other engine ownership changed.

## Compatibility correction before closeout

The first implementation commit used a single seven-parameter constructor whose final two parameters were optional. Current-main readback before claim close identified that this preserved C# source-call syntax but not the exact precompiled five-argument constructor signature. The claim therefore remained `ACTIVE`; a follow-up commit restored the original five-argument overload and added a reflection regression before this claim was closed. No ABI PASS is inferred from source inspection alone, but the legacy constructor signature is explicitly present in current source and guarded by the smoke surface.

## Coordination / overlap reconciliation

- Claim-only commit: `bd5c54a54f35ed4723925551f19f31a05a95fcf7` — `chore(agent): claim MTR-02 adjustment rule identity`.
- `main` changed repeatedly during the lane through Quantity Rule provenance/collision work and licensed Curtain/Level work. Before every main ref update, the new head was fetched and compared against the implementation base.
- All intervening commits were outside the two reserved MTR files; candidate commit objects based on stale heads were discarded rather than attached. Final commits were recreated on the then-current `main` and pushed with `force=false`.
- The concurrent Quantity Rule claims explicitly excluded MTR files; `LOCAL-003` remained a separate licensed native Level-Z lane.

## Implementation commits

- `c8df3610dbb4c5dee172486f23a998eb59845c66` — `feat(measurement): trace adjustment rule identity`.
  - Parent compare confirmed exactly the two reserved source/smoke files changed.
  - Introduced optional adjustment rule provenance, MTR1/MTR2 compatibility behavior, deterministic rule-aware ordering, and focused regression coverage.
- `36db4723b311b5f40fbde8983877d6965ef0ed9b` — `fix(measurement): preserve adjustment constructor ABI`.
  - Parent `d1b2d08dc014dba21d16ed2d1c6fcdcb12c43405` compare confirmed exactly the same two reserved files changed, with +10 lines each.
  - Restored the exact five-argument constructor overload and added the reflection compatibility regression.
- Final pre-close current-main refresh confirmed `main` at `36db4723b311b5f40fbde8983877d6965ef0ed9b` and direct file readback confirmed the final source/smoke blobs are present.

## Validation actually executed

- Executed: required governance/workstream/product-boundary/research reads before source changes; Gemini research was treated as research/reference only.
- Executed: source/test readback proving the missing adjustment rule/version identity before claim creation.
- Executed: claim-only publication to `main`, then current-main refresh and overlap comparison before source implementation.
- Executed: repeated current-main refresh + GitHub compare reconciliation across every observed concurrent main advance; no reserved-file overlap was found and no force push was used.
- Executed: exact parent-to-implementation GitHub compares; both implementation commits were limited to the two reserved MTR files.
- Executed: direct current-main readback of `MeasurementTrace.cs` and `MeasurementTraceContractSmoke.cs` after the ABI follow-up. Readback shows the legacy five-argument overload, additive rule-aware overload, paired validation, MTR1/MTR2 selection and the focused compatibility/provenance smoke cases.
- Executed: an independent deterministic reconstruction of the legacy canonical encoder inputs produced exactly `4:MTR110:SEM-WALL-18:SRC-WALL9:NetAreaM22:122:112:m24:none-;-;1:01:11:01:12:m27:opening11:SRC-OPENING1:01:0` (107 characters), matching the literal regression fixture. This is a static encoding cross-check, not execution of the C# smoke binary.
- Environment capability remains unchanged from the preceding MTR lane: `dotnet` is not installed and no `csc`, `mcs` or `msbuild` executable is available in this container. Therefore no managed build/smoke PASS is claimed.
- Not executed: GitHub Actions, repository `dotnet build`, registered Core smoke executable, installed-reference BricsCAD V25 build, licensed BricsCAD runtime probe, save/reopen/Undo/multi-DWG qualification. No PASS is claimed for any of those gates.

## Remaining gates / split work

- A host with the repository .NET toolchain must run the warnings-as-errors build and registered Core smoke to execute the new MTR2/constructor compatibility regression in the real test binary.
- BricsCAD native qualification remains owned by the separate `LOCAL-003` lane; this pure Core provenance change does not imply a native PASS.
- Measurement Profile identity/version is intentionally not implemented in this sub-lane. Repository/source inspection did not expose a canonical profile source-of-truth; inventing a second profile/rule engine would be speculative and contrary to the product boundary. That remaining MTR-02 profile portion needs a separate narrowly claimed owner/design decision when a canonical source-of-truth is established.

## Completion condition

Satisfied for this bounded MTR-02A sub-lane: the gap was proven from current source, ownership was published before source changes, implementation stayed within the reserved Core contract/smoke files, concurrent main changes were reconciled without overwrite or force push, compatibility was corrected before closeout, final source/tests were read back from current `main`, and all unexecuted managed/native gates remain explicitly outstanding rather than represented as PASS.
