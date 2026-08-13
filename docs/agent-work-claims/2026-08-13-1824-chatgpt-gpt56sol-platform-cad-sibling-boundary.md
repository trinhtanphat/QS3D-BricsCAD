# Work claim — Platform/CAD sibling product boundary and migration inventory

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T18:24:00+07:00`
- Baseline main SHA: `8c1d96e0915a31b34130c954c056b73b53f2dbba`
- Priority: owner explicitly created `trinhtanphat/QS3D-Platform` and `trinhtanphat/QS3D-CAD` and requested coordinated planning/implementation while preserving this repository as the BricsCAD product.

## Reserved scope

Document the new sibling-repository architecture without changing this repository's shipping form: `QS3D-BricsCAD` remains the licensed BricsCAD V25/V26 hosted adapter/product, `QS3D-Platform` becomes the future vendor-neutral shared domain/contracts repository, and `QS3D-CAD` is the separate standalone product. Create a migration inventory and update canonical product-boundary/readme wording only where needed to prevent agents from interpreting the new standalone effort as a request to convert this repo into `QS3D.exe`.

## Expected surfaces

- `docs/QS3D-PLATFORM-MIGRATION.md` (new)
- `docs/PRODUCT-BOUNDARY.md` (sibling-product clarification only)
- `README.md` (sibling-product clarification only)
- this claim file for close-out

## Excluded scope

- no BricsCAD source, WPF, command, geometry, persistence, quantity, mapping, responsive-UI or runtime changes;
- no V25/V26 build, `NETLOAD`, local qualification, CI/Actions, packaging, installer or release dispatch;
- no deletion/move of existing `QS3D.Core` code in this batch;
- no takeover of any ACTIVE/BLOCKED feature, bug, UI, mapping, runtime or LOCAL_ONLY claim;
- no vendor SDK integration in this repository.

## Validation plan

- refresh `main` before implementation/commit and preserve concurrent commits;
- read back the changed Markdown from `main` after push;
- verify wording keeps BricsCAD product/runtime qualification independent from the standalone CAD effort;
- no GitHub Actions dispatch.

## Coordination

Recent active work is concentrated in responsive UI, mapping, startup/runtime and feature-specific lanes. This reservation is intentionally documentation-only and does not own those capabilities. `QS3D-Platform` and `QS3D-CAD` implementation occurs in their own repositories.

## Completion condition

The three-repository ownership/migration boundary is documented on current `main`, this claim is marked `COMPLETED`, and no BricsCAD implementation/runtime claim is made.