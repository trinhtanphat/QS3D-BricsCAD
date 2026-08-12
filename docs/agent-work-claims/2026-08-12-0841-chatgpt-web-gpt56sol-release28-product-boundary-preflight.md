# Work claim — release #28 product-boundary preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:41:00+07:00`
- Baseline main SHA: `232dfe41ee4e43b3ce215dabc89da46340c30b2b`
- Priority: QS3D Cloud V25 Preview Build & Release #28 still fails `preflight-product-boundary.py` on three exact markers that describe the superseded V25-only product wording while the canonical product boundary is now V25 + V26 hosted plugins.

## Reserved scope

Reconcile `scripts/preflight-product-boundary.py` with the current locked V25 + V26 hosted-plugin product decision without weakening the core invariant that QS3D is not a standalone CAD/EXE product. Strengthen source checks so both host-major projects remain managed Library plugins.

## Expected surfaces

- `scripts/preflight-product-boundary.py`
- this claim file for close-out

## Excluded scope

- No edits to README, PRODUCT-BOUNDARY, ARCHITECTURE, REQUIREMENTS, UI, Direct Draw docs or historical handoffs unless an independent current-document defect is proven.
- No changes to V25/V26 production source, versions, build/package/update behavior or runtime qualification.
- No removal of the standalone-EXE prohibition or BLT clean-room boundary.
- No GitHub Actions dispatch.

## Validation plan

- Require current README wording that QS3D is a V25/V26 x64 hosted plugin and that a matching licensed host is required at runtime.
- Require current PRODUCT-BOUNDARY wording for Windows x64 host-specific managed assemblies and the explicit no-`QS3D.exe` invariant.
- Require current ARCHITECTURE hosted-plugin/V25+V26 adapter wording rather than the obsolete architecture-level `DemandLoad or NETLOAD` literal.
- Keep legacy workflow/handoff markers only where those documents are intentionally V25-scoped.
- Add V26 csproj checks for `net8.0-windows`, `<OutputType>Library</OutputType>` and host-major assembly identity alongside the existing V25 Library/extension checks.
- Read back the script after push. Do not claim aggregate PASS without a new current-SHA run.

## Coordination

Current active Floor/Zone, Start Center and other concurrent claims observed during refresh do not own `scripts/preflight-product-boundary.py` or the product-form contract. This lane changes only the stale static gate and deliberately excludes V26 version/release failures from run #28.

## Completion condition

The product-boundary gate enforces the current V25 + V26 hosted-plugin architecture, both host assemblies remain Library targets, standalone/BLT boundaries remain fail-closed, the implementation is pushed to `main`, and this claim is closed with the actual SHA and validation limits.
