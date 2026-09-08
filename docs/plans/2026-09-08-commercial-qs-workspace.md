# Commercial QS Workspace implementation plan

Issue: #6101

## Goal

Expose the merged commercial-QS domain through a single BricsCAD modeless product surface for Variation, IPC, and Final Account while keeping all monetary authority in Core.

## Architecture

- Entry point: `QS3DCOMMERCIAL` in the V25 adapter, guarded exactly like existing production modeless commands.
- UI: one document-bound modeless WPF window with persistent selected-variation context and three tabs: Variation, IPC, Final Account.
- Variation authority: `CommercialVariationRegister` / `CommercialVariation`.
- Progress authority feeding IPC: `ProgressClaimService.Evaluate(...)` creates the non-publicly-constructible `ProgressClaimResult`.
- IPC authority: `InterimPaymentCertificateService.Create(...)`.
- Final Account authority: `FinalAccountService.Reconcile(...)`.
- Adapter responsibility is input parsing, constructing Core request/domain objects, invoking Core services, and rendering returned results. No commercial arithmetic is duplicated in UI code.

## TDD sequence

1. Add `CommercialQsWorkspaceSurfaceSmoke` and register it. It must fail while the product surface is absent.
2. Add `CommercialQsCommands.cs` with production hardening, license, support-window, and modeless-window gates.
3. Add `CommercialQsWindow.xaml` with persistent variation context and Variation / IPC / Final Account sections.
4. Add `CommercialQsWindow.xaml.cs` using the existing single-instance/document-bound modeless-window conventions and the Core authorities above.
5. Run the smoke suite/build and require the new source guard to pass.
6. Run fresh exact-head CI. Do not treat hosted CI as licensed V25/V26 runtime evidence.
7. Open PR referencing #6101, keep it current with `main`, and merge only after required exact-head checks are green and the PR is mergeable.

## Verification truth

Hosted source/tests/build can prove deterministic integration and compile-time adapter wiring. Interactive BricsCAD V25/V26 behavior remains `PENDING_NATIVE` until exercised on the corresponding licensed host; no hosted check may be reported as native runtime PASS.
