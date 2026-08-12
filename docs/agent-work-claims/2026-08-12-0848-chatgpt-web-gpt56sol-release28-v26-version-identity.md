# Work claim — release #28 V26 product version identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:48:00+07:00`
- Baseline main SHA: `98469144f23aa55c3a3b715316247138ea73fad2`
- Priority: QS3D Cloud V25 Preview Build & Release #28 exposed a deterministic V26/Core product-version mismatch, and current `main` still has V26 at preview.2 while Core/V25 are preview.3.

## Reserved scope

Synchronize the V26 project product/file/informational version identity to the current shared release version already used by Core/V25. Preserve V26 target framework, assembly identity, source-linking, host references and package/update channel unchanged.

## Expected surfaces

- `src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj`
- this claim file for close-out

## Excluded scope

- No changes to V25/Core versions.
- No V26 runtime, packaging, update-client, release-workflow, installer/signing or .NET SDK changes.
- No weakening of `preflight-v26-package-release.py`; its cross-project identity check is the regression authority.
- No GitHub Actions dispatch.

## Validation plan

- Re-fetch the V26/Core/V25 version declarations immediately before the write.
- Require V26 `Version` and `InformationalVersion` to equal Core/V25 `0.1.0-preview.3` and V26 `FileVersion` to equal `0.1.0.3`.
- Keep `AssemblyVersion` at the existing compatible `0.1.0.0` unless an independent requirement says otherwise.
- Read back the V26 csproj after push and verify the implementation commit remains on current `main`.
- Do not claim runtime/package/release PASS from this source-only correction.

## Coordination

Recent V26 package/release work is historical and no current claim/commit search found an active V26 version-identity owner. Current concurrent health/UI claims do not overlap the V26 csproj version fields. This lane does not take ownership of the other V26 feature-gate failures from run #28.

## Completion condition

V26/Core/V25 product and informational release identities are synchronized at preview.3, V26 file version is `0.1.0.3`, the package-release identity gate remains intact, the change is pushed to `main`, and the claim is closed with exact validation limits.
