# Work claim — Safe generated ownership malformed-project visibility

- Status: `COMPLETE`
- State: `COMPLETE`
- Agent: `chatgpt-gpt56sol-20260812-safe-generated-ownership-null-health`
- Registered: `2026-08-12T00:53:00+07:00`
- Completed: `2026-08-12` (source-side)
- Last Updated: `2026-08-12`
- Baseline main SHA: `57b032b615de4d8a92c1ffbe3380dd66457269ea`
- Priority: P1 — health must not report false-clean for a malformed ProjectState that the canonical ownership index rejects.
- Task Key: `CORE-SAFE-GENERATED-OWNERSHIP-MALFORMED-PROJECT`

## Confirmed defect

`GeneratedHandleOwnershipIndex.Build(project)` already fails closed when `project.Elements` contains a null element, a blank semantic element id, or a duplicate canonical element id. `SafeGeneratedHandleOwnershipHealthService.Inspect(project)` independently scanned the same collection and executed `if (element == null) continue;`. A malformed project containing a null element could therefore produce no ownership issue from the safe health surface even though the canonical raw ownership index rejected that project.

That was a false-clean health result: the safe wrapper is intended to make ownership diagnostics consumable, not silently weaken the underlying project-integrity contract.

## Reserved scope

- `src/QS3D.Core/Diagnostics/SafeGeneratedHandleOwnershipHealthService.cs`
- `tests/QS3D.Core.SmokeTests/SafeGeneratedHandleOwnershipMalformedProjectSmoke.cs`
- this claim file

## Implemented contract

- `SafeGeneratedHandleOwnershipHealthService.Inspect(project)` now invokes `GeneratedHandleOwnershipIndex.Build(project)` before its existing conflict scan so the same canonical element-set validation is authoritative for both surfaces.
- Only canonical `InvalidOperationException` validation failures are converted into a project-level `HealthSeverity.Error` with code `GENERATED_HANDLE_OWNERSHIP_INVALID_PROJECT`; unrelated exception classes are not swallowed.
- The previous `if (element == null) continue;` false-clean path is removed after successful canonical validation.
- Existing valid ownership conflict scanning and same-logical-slot de-duplication remain unchanged.
- `GeneratedHandleOwnershipIndex`, DependencyHealth, native builders and BricsCAD adapter/runtime code are untouched.
- Inspection remains read-only.

## Regression source

Added auto-registered `SafeGeneratedHandleOwnershipMalformedProjectSmoke` covering:

- null semantic element -> one visible invalid-project Error;
- duplicate semantic element id -> one visible invalid-project Error;
- valid non-conflicting ownership -> clean result;
- valid cross-owner conflict -> existing conflict reporting preserved;
- `ProjectState.ChangeVersion` unchanged across every inspection case.

The source fix commit on the implementation branch is `38da4e4cca66a91188a550210c0419b97a7dd437`; focused smoke source commit is `3a808f945bd77bb1b1b5dc6e9ccab5c56ac3ce00`.

## Validation / coordination

- Claim was committed to `main` first as `baa8c639c1ec29bb0f33f582af367401558b22eb` before any source edit.
- Re-read current `main` after concurrent activity: at `8b81c0041f07789c4eb044bcfe44be470fc589b7`, the claimed source still had original blob `6c192da2d29dabf1f4bdc276f7af34163b480387`, so no concurrent source overlap was overwritten.
- Historical raw-index null/blank/duplicate hardening remains unchanged and authoritative.
- No GitHub Actions/build/release workflow was dispatched. The new smoke source was reviewed but no executable Core smoke/build PASS is claimed from this remote session.
- No BricsCAD V25 runtime qualification is required for this pure-Core diagnostic change, and no BricsCAD runtime PASS is claimed.

## Result

PR #597 was squash-merged to `main` as `318d39766eaa93da84d698e638e526bb18ad752f`. Post-merge readback confirmed `SafeGeneratedHandleOwnershipHealthService.cs` on `main` at blob `215f0fd76574e0f14f1be95ae75e7dd70d4c200c` with canonical validation/Error mapping and no silent null skip, and the auto-registered smoke on `main` at blob `4239f690e3f123808106e627be5a550391c75ff8`. The claim is released. No executable smoke/build/runtime PASS is inferred from source readback.