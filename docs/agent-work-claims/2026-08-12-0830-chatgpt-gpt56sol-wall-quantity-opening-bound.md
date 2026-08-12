# Work claim — Wall Quantity opening enumeration bound

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-wall-quantity-opening-bound`
- Registered: `2026-08-12T08:30:00+07:00`
- Last Updated: `2026-08-12T08:30:00+07:00`
- Baseline main SHA: `3b10e48123bb07db09cc13eef309ea96daa5e35a`
- Priority: deterministic Core availability/resource-bound defect found during owner-requested continue-all audit
- Task Key: `CORE-WALL-QUANTITY-OPENING-ENUMERATION-BOUND`

## Confirmed defect

`WallQuantityCalculator.Calculate(...)` accepts arbitrary `IEnumerable<OpeningCut>` input and currently enumerates it without a count/resource bound. A lazy oversized or non-terminating enumerable can therefore keep the Core takeoff call running indefinitely or consume unbounded work, unlike other hardened Core collection boundaries that cap caller-controlled target inputs.

The recently completed wall-quantity null-opening lane rejects null entries but does not bound enumeration.

## Reserved scope

Bound wall-opening takeoff input to 10,000 entries, matching existing Core bulk/selection/preview safety ceilings. Reject known oversized collections before enumeration and enforce the same cap during lazy enumeration. Preserve valid 0..10,000 opening calculations, null-entry rejection, finite dimension/area checks, clamping and all returned quantity formulas.

## Expected surfaces

- `src/QS3D.Core/Services/WallQuantityCalculator.cs`
- focused Core smoke + isolated registration
- this claim file

## Coordination / exclusions

- Do not modify Wall Quantity WPF/Schedule Hub/viewport/export flows.
- Do not change opening area geometry, overlap semantics, clamping, null-entry behavior or wall formulas.
- Do not modify StructuralRegenerator/WallRegenerator in this lane.
- Do not overwrite any concurrent ACTIVE claim; no force-push, Actions/build/release dispatch, or runtime PASS claim.

## Validation plan

- Exactly 10,000 finite openings remain accepted and produce deterministic clamped takeoff.
- A known-count 10,001 collection is rejected before element enumeration.
- A lazy enumerable is stopped/rejected as soon as the 10,001st item is requested rather than being fully consumed.
- Existing null-entry and finite/overflow behavior remains unchanged.
- Re-fetch `main`, collision state and exact source before each write; read back source/test/registration before closeout.

## Completion condition

Wall quantity takeoff cannot consume unbounded caller-controlled opening enumeration while preserving all existing valid takeoff semantics.