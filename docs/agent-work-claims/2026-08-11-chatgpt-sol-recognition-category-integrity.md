# Work claim — recognition category integrity

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-recognition-category-integrity-20260811-2152`
- Registered: `2026-08-11T21:52:51+07:00`
- Completed: `2026-08-11T21:57:28+07:00`
- Baseline main SHA: `b33459e74ce1229e4b42cfe78190c5d3e6063ef7`
- Claim commit: `5e26d0c92d65abda2091c35af704bd738a14a144`
- Implementation commit: `17d6e2f74943b9bfec1fe353355994ee29d7f1af`
- Regression-test commit: `9de0c39b0c8b58f209d97205fd92b82dcb48fea0`
- Priority: evidence-driven Core recognition defect found during owner-requested repository review

## Reserved scope

Harden Core recognition so undefined `ElementCategory` enum values cannot enter a `RecognitionRule` or survive mutable `RecognitionCandidate` validation into capture-readiness / auto-accept decisions.

## Implemented

- `RecognitionRule` now rejects undefined `ElementCategory` enum values at construction.
- `RecognitionCandidate.Category` now fails closed on assignment of an undefined enum value.
- `RecognitionResult.ValidateCandidates` independently revalidates category integrity as defense-in-depth before batch auto-accept logic.
- `ProxyCaptureEligibilitySmoke` now covers invalid rule category, invalid mutable candidate category, and the unchanged valid non-proxy auto-accept path.

## Changed surfaces

- `src/QS3D.Core/Recognition/RecognitionEngine.cs`
- `tests/QS3D.Core.SmokeTests/ProxyCaptureEligibilitySmoke.cs`
- this claim file for completion metadata

## Excluded scope

- No BricsCAD V25 adapter/UI/runtime changes.
- No generated native ownership redesign or changes to the completed generated-source-recognition lane.
- No persistence, interchange, reporting, quantity settings, Direct Draw, ribbon, updater, rebar parser, or geometry work.
- No GitHub Actions dispatch/re-run and no LOCAL_PASS/V25 runtime qualification claim.

## Defect evidence

Before the fix, `RecognitionCandidate.Category` was mutable while `RecognitionResult.ValidateCandidates` validated only confidence. `RecognitionBatch` revalidated candidates and then used `RecognitionResult.IsCaptureReady`; for a non-`ProxyEntity`, `EntitySnapshotCaptureEligibility.IsReady` could return ready before any category-specific switch. Therefore an undefined enum value could survive to recognition readiness/auto-accept logic instead of failing closed. `RecognitionRule` also accepted undefined enum values at construction.

## Validation performed

- Re-fetched current `main` immediately before each product/test write.
- Re-fetched both changed files from current `main` after implementation and confirmed the category guards and regression coverage are present.
- Compared claim commit `5e26d0c92d65abda2091c35af704bd738a14a144` to then-current `main` `d947a51af188a9c99da65729c4e863779f0fb8cd`: status `ahead`, `ahead_by=8`, `behind_by=0`; both changed files were included in the compare.
- No GitHub Actions workflow was dispatched or re-run.
- This remote environment does not provide the repo's local BricsCAD V25 runtime and no LOCAL_PASS is claimed. Build/smoke execution is not claimed here; validation is source/static plus Git ancestry/content verification.

## Outcome

The confirmed Core recognition integrity gap is closed without changing normal defined-category recognition behavior. Undefined category values now fail closed at both rule/candidate boundaries, with batch validation retained as defense-in-depth.
