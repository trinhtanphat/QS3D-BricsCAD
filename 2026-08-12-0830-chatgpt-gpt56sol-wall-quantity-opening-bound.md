# Work claim — Wall Quantity opening enumeration bound

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-wall-quantity-opening-bound`
- Registered: `2026-08-12T08:30:00+07:00`
- Last Updated: `2026-08-12T08:32:22+07:00`
- Baseline main SHA: `3b10e48123bb07db09cc13eef309ea96daa5e35a`
- Priority: deterministic Core availability/resource-bound defect found during owner-requested continue-all audit
- Task Key: `CORE-WALL-QUANTITY-OPENING-ENUMERATION-BOUND`

## Confirmed defect

`WallQuantityCalculator.Calculate(...)` accepted arbitrary `IEnumerable<OpeningCut>` input and enumerated it without a count/resource bound. A lazy oversized or non-terminating enumerable could therefore keep the Core takeoff call running indefinitely or consume unbounded work, unlike other hardened Core collection boundaries that cap caller-controlled target inputs.

The previously completed wall-quantity null-opening lane rejected null entries but did not bound enumeration.

## Implemented scope

Wall-opening takeoff input is now capped at 10,000 entries, matching existing Core bulk/selection/preview safety ceilings. Known `ICollection<OpeningCut>` / `IReadOnlyCollection<OpeningCut>` inputs above the cap fail before enumeration. Lazy inputs are counted in-stream and fail when the 10,001st item is encountered, without materializing the whole sequence.

Valid 0..10,000 opening calculations, null-entry rejection, finite dimension/area checks, gross/opening clamping and all returned wall quantity formulas remain unchanged.

## Committed evidence

- Claim registration: `605bff9c05e37727f0b2f2dac6ccc86138d23e11` — `chore(agent): claim wall quantity opening bound`
- Core fix: `84a48c237072098763cfac564e2399f9e214c08c` — `fix(quantity): bound wall opening enumeration`
- Focused smoke: `fbd5edf8c14c3c7547ac040172450e31add73cff` — `test(quantity): guard wall opening enumeration bound`
- Isolated smoke registration: `c9f97eec01d55ad78129e4b675c7487769e0b62b` — `test(quantity): register wall opening bound smoke`
- Read-back at `c9f97eec01d55ad78129e4b675c7487769e0b62b` confirmed source, smoke and registration on `main`.

The smoke locks exact-bound acceptance, known-count rejection before `GetEnumerator()`, lazy rejection at the first item beyond the cap, and preservation of the existing null-opening guard.

## Preserved behavior / exclusions

- Wall Quantity WPF/Schedule Hub/viewport/export flows were not modified.
- Opening geometry/overlap semantics, clamping, null-entry behavior and wall formulas were not changed.
- StructuralRegenerator/WallRegenerator were not modified.
- No force-push or GitHub Actions/build/release dispatch was used.
- No local .NET smoke execution or BricsCAD V25 runtime qualification is claimed.

## Completion condition

Satisfied: wall quantity takeoff cannot consume unbounded caller-controlled opening enumeration while preserving existing valid takeoff semantics.