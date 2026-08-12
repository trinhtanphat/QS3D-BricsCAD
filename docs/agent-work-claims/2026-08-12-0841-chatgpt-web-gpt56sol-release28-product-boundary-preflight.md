# Work claim — release #28 product-boundary preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-12T08:41:00+07:00`
- Expanded: `2026-08-12T08:43:00+07:00`
- Baseline main SHA: `232dfe41ee4e43b3ce215dabc89da46340c30b2b`
- Priority: QS3D Cloud V25 Preview Build & Release #28 still fails `preflight-product-boundary.py` on exact markers that describe the superseded V25-only product wording while the canonical product boundary is now V25 + V26 hosted plugins.

## Reserved scope

Reconcile the product-form contract with the current locked V25 + V26 hosted-plugin decision without weakening the invariant that QS3D is not a standalone CAD/EXE product. The current audit also found two current policy/requirements surfaces that still state the product is V25-only even though `docs/PRODUCT-BOUNDARY.md` explicitly applies the V25+V26 boundary to requirements and agent wording; those two surfaces are included in this expansion.

## Expected surfaces

- `scripts/preflight-product-boundary.py`
- `AGENTS.md` — only the locked product-form paragraph
- `docs/REQUIREMENTS.md` — only the Product/runtime boundary paragraph/bullets
- this claim file for close-out

## Excluded scope

- README, `docs/PRODUCT-BOUNDARY.md` and `docs/ARCHITECTURE.md` are canonical evidence and are not to be rewritten in this lane.
- No edits to UI, V25-specific install docs, Direct Draw implementation docs or historical handoffs whose V25 wording is intentionally scoped to those legacy/runtime lanes.
- No changes to V25/V26 production source, versions, build/package/update behavior or runtime qualification.
- No removal of the standalone-EXE prohibition or BLT clean-room boundary.
- No GitHub Actions dispatch.

## Validation plan

- Update AGENTS' locked product-form statement to V25+V26 host-specific managed Library plugins and retain the no-standalone / BLT clean-room rules.
- Update the REQUIREMENTS product/runtime boundary to V25+V26, with V25 net48 and V26 net8.0-windows host adapters, while preserving BricsCAD-owned viewport/database/editor semantics and no `QS3D.exe` requirement.
- Require current README wording that QS3D is a V25/V26 x64 hosted plugin and that a matching licensed host is required at runtime.
- Require current PRODUCT-BOUNDARY wording for Windows x64 host-specific managed assemblies and the explicit no-`QS3D.exe` invariant.
- Require current ARCHITECTURE hosted-plugin/V25+V26 adapter wording rather than the obsolete architecture-level `DemandLoad or NETLOAD` literal.
- Keep V25-specific workflow/handoff markers only where those documents are intentionally V25-scoped.
- Add V26 csproj checks for `net8.0-windows`, `<OutputType>Library</OutputType>` and host-major assembly identity alongside the existing V25 Library/extension checks.
- Read back all three edited surfaces after push. Do not claim aggregate PASS without a new current-SHA run.

## Coordination

Current active Floor/Zone, Start Center and other concurrent claims observed during refresh do not own these product-form paragraphs or `scripts/preflight-product-boundary.py`. This lane deliberately excludes the separate V26 version/release mismatch from run #28.

## Completion condition

Current policy/requirements wording and the product-boundary gate all enforce the V25 + V26 hosted-plugin architecture, both host assemblies remain Library targets, standalone/BLT boundaries remain fail-closed, the implementation is pushed to `main`, and this claim is closed with actual SHAs and validation limits.
