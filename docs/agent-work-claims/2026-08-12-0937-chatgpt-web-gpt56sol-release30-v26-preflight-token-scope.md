# Work claim — release #30 V26 preflight token-scope reconciliation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-release30-v26-preflight-token-scope`
- Registered: `2026-08-12T09:37:00+07:00`
- Completed: `2026-08-12T09:39:00+07:00`
- Baseline main SHA: `4658163352e18be52f0fbc3e53d2242571f3ec32`
- Claim commit: `340c88459f312710a6a794ffa8362d19f879c8af`
- Implementation commit: `d2c24e40d3ecfd9c214a28740f8ce22b3a2bc2f1`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reported two V26 compatibility failures caused by over-broad/exact text checks while the actual V26 target and host-major runtime diagnostics remained correct.

## Completed scope

Reconciled only `scripts/preflight-bricscad-v26.py`. V25/V26 project files and runtime diagnostic production source remained unchanged.

## Implemented gate contract

- V25 is still required to target `<TargetFramework>net48</TargetFramework>`.
- V26 is still required to target exactly `<TargetFramework>net8.0-windows</TargetFramework>`.
- V26 now forbids executable/multi-target net48 forms (`<TargetFramework>net48</TargetFramework>` and `<TargetFrameworks>`) rather than forbidding the harmless word `net48` in an architecture comment.
- V26 still forbids V25 SDK/env/update identities and retains all V26-only refs/updater/workflow checks.
- Shared runtime diagnostics still require compile-selected V25/V26 major constants, BrxMgd/TD_Mgd host-major validation, architecture/package checks and the licensed scenario-suite instruction.
- The scenario-suite wording check now pins `plus the licensed ` and `ExpectedRuntimeLabel + " scenario suite` structurally instead of an obsolete string-literal boundary.

## Validation performed

- Verified claim commit `340c88459f312710a6a794ffa8362d19f879c8af` remained an ancestor of moving `main`; intervening commits were unrelated Wall/Grid/Selection work.
- Re-fetched the exact gate before implementation.
- Read back the implemented V26 target section and runtime diagnostics section from `main` at blob `408935eb41a1dd7f03e1bd217b991a8cc2b34855`.
- Confirmed current V26 project remains `net8.0-windows`; bare `net48` is only explanatory text about the separate V25 lane.
- Confirmed runtime diagnostics still emit the licensed host-major scenario-suite requirement.
- No production source was changed.
- No GitHub Actions/build/release dispatch was performed and no BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

Completed. The V26 gate now validates actual build/runtime semantics rather than harmless comment/string-literal boundaries, retains host-major safety checks, and this reservation is released.
