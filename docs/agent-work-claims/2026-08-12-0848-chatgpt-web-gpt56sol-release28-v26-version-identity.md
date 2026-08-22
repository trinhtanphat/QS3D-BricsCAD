# Work claim — release #28 V26 product version identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:48:00+07:00`
- Completed: `2026-08-12T08:50:00+07:00`
- Baseline main SHA: `98469144f23aa55c3a3b715316247138ea73fad2`
- Priority: QS3D Cloud V25 Preview Build & Release #28 exposed a deterministic V26/Core product-version mismatch, and current source still had V26 at preview.2 while Core/V25 were preview.3.

## Completed implementation

Implementation commit: `daf5c6a6009e635b519ab5baf551fb0d05c40bbe` (`fix(bricscad): sync V26 preview version identity`).

The V26 project now declares:

- `Version` = `0.1.0-preview.3`
- `InformationalVersion` = `0.1.0-preview.3`
- `FileVersion` = `0.1.0.3`
- `AssemblyVersion` remains `0.1.0.0`

No V26 target-framework, Library/assembly identity, shared-source links, BricsCAD references, update client, packaging or release workflow changed.

## Validation performed

- Verified the claim-only commit `fd5823ed36f91ce2312f8338eff6d204aa92241f` was current `main` before implementation.
- Re-read the current V26 csproj immediately before the write and confirmed preview.2 / file version `.2` were still present.
- Confirmed Core current product/informational version is `0.1.0-preview.3`; V25 is also preview.3.
- Read back the V26 csproj after push and confirmed preview.3 / file version `.3` while `AssemblyVersion` stays `0.1.0.0`.
- `scripts/preflight-v26-package-release.py` was left unchanged; its V26/Core `Version` and `InformationalVersion` equality checks remain the regression authority.

## Validation boundary

- Run #28 remains tied to `fbd5edf8c14c3c7547ac040172450e31add73cff`; it cannot validate `daf5c6a6009e635b519ab5baf551fb0d05c40bbe`.
- No GitHub Actions workflow was dispatched or rerun.
- No V26 build, licensed runtime, package, signing, installer/update or release PASS is claimed remotely.
- Other run #28 failures remain separate and must be re-evaluated against moving `main` before edits.
