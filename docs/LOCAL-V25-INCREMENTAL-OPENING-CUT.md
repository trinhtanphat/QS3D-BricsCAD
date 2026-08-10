# Local V25 handoff — incremental selected opening cut

Updated: 2026-08-10 (UTC+7)

This checklist is **local-only runtime qualification** for Windows x64 + licensed BricsCAD V25. Source/static review must not mark these scenarios PASS.

Scope: straight supported hosts using `QS3DCUTSELECTEDOPENINGS` / `QS3DCUTOPENINGS`. Curved-host incremental subset behavior is not claimed by this handoff.

## Preconditions

- Checkout the exact candidate SHA with a clean working tree.
- Build Release/x64 against the locally installed BricsCAD V25 managed assemblies.
- Run the repository local qualification runner and all discovered source preflights first.
- Use only disposable/sanitized DWGs for corruption/failure-injection tests.

## Required regression matrix

1. **A -> B on the same host**
   - Create one supported straight host with two linked openings A and B.
   - Run `QS3DCUTSELECTEDOPENINGS` with only A selected.
   - Verify only A is physically cut and the generated host solid remains the owned current solid.
   - Then select only B and run the command again.
   - Verify B is added without rebuilding the host and A is not double-cut.
   - Verify semantic physical-cut count/state now represents A+B, not only B.

2. **Idempotent reselect A**
   - After A+B are already cut, reselect A and run `QS3DCUTSELECTEDOPENINGS` again.
   - Native geometry must be a no-op; no duplicate subtraction and no new owned solid may appear.
   - Health/release diagnostics must remain clean for the physical-cut state.

3. **all-linked after selected-cut**
   - Create A, B and C on one host.
   - Cut only A through the selected command.
   - Run legacy `QS3DCUTOPENINGS`.
   - Verify only B and C are newly subtracted, while the final physical state/fingerprint represents A+B+C.

4. **Geometry/property change after cut**
   - Cut A, then change A width/sill/clearance or move its CAD source without rebuilding the host.
   - A later selected/broad cut must fail closed as stale and must not mutate the host solid further.
   - `QS3DHEALTHALL` / release diagnostics must surface the stale physical-cut condition.

5. **Delete/relink an already-cut opening**
   - Cut A, then delete A semantically or relink it to a different host using a disposable fixture.
   - Further incremental cutting on the original host must fail closed and require host rebuild.
   - No foreign host or unrelated solid may be erased or modified.

6. **Partial/malformed/oversized metadata**
   - In a disposable fixture, create a host where only one of `PhysicalOpeningCutSolidHandle` / `PhysicalOpeningCutFingerprint` is present, or corrupt the accumulated target-state payload.
   - Also test target-state data exceeding at least one guarded bound: more than 4096 opening IDs, an element ID longer than 128 characters, an encoded token longer than 1024 characters, or a serialized target-state larger than 4 MiB.
   - The next cut must fail before native boolean mutation; oversized metadata must not be partially normalized into a trusted cut set.
   - Rebuild must clear the stale `PhysicalOpeningCut*` state and allow a clean cut afterwards.

7. **Legacy metadata upgrade**
   - Use a fixture representing a pre-target-state straight cut: solid handle + fingerprint are valid, but accumulated opening-id state is absent.
   - If and only if the existing fingerprint exactly matches the requested opening set, the command may upgrade metadata without re-subtracting geometry.
   - If the requested set differs, it must fail closed and require rebuild.

8. **Failure injection / transaction atomicity**
   - Arrange multiple newly requested openings where a later BooleanSubtract fails.
   - Verify the BricsCAD transaction rolls native mutations back and project semantic metadata is restored to its prior snapshot.
   - No half-updated count/fingerprint/target-state may remain.

9. **save, close, reopen**
   - After A+B are successfully cut, save the DWG/project, close BricsCAD, reopen and rerun Health/Release checks.
   - Reselect A: no-op.
   - Add C: only C is newly cut and final state becomes A+B+C.
   - Repeat in a representative millimetre drawing and metre drawing.

10. **UCS / ownership safety**
    - Repeat a representative straight-host case under World UCS and supported rotated planar UCS authoring inputs.
    - Verify generated-solid ownership remains the original semantic host and no unrelated `Solid3d` is touched.

## Evidence

Record each scenario as **PASS / FAIL / NOT TESTED** with:

- exact 40-character Git SHA;
- BricsCAD V25 edition/build;
- Windows version/x64;
- command sequence;
- sanitized error text if failed;
- screenshot or disposable DWG evidence kept outside Git when useful.

Do not commit customer/private DWGs, BricsCAD proprietary DLLs, signing keys, machine secrets or unsanitized paths.

## Release rule

This source change is not customer-release-qualified until the applicable cases above pass on the exact release candidate SHA in real BricsCAD V25. GitHub Actions remain manual-only and are not authorized by this handoff.
