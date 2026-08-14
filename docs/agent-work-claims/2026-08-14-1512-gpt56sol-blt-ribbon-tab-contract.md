# Work claim — BLT reference Ribbon tab contract hardening

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ribbon-tab-contract`
- Registered: `2026-08-14T15:12:00+07:00`
- Baseline main SHA: `427d029ad834197a43ddfa302e36128334af5ae4`
- Owner request: continue all 100%, fix bugs/update code and commit/push main.

## Concrete gap

The BLT-reference parity preflight already protects Home file/settings mappings and many command/tree surfaces, but it does not protect the canonical top-level Ribbon tab IDs/titles that were implemented for screenshot parity. A parallel merge could silently rename/remove one of those groups while the focused parity preflight still passes.

## Reserved scope

- `scripts/preflight-blt-reference-ui-parity.py`
- this claim file

## Implementation boundary

- Add static regression assertions for the established canonical top-level Ribbon tab ID/title pairs in `RibbonBootstrapper.cs`.
- Cover the requested screenshot groups: KHỞI ĐẦU, THIẾT LẬP DỰ ÁN, MÔ HÌNH BIM, NHẬN DẠNG, VẼ, TOOL, MODELING, XEM, ĐỊNH LƯỢNG, BẢN SỬA ĐỔI.
- Also guard the intentionally-present TẠO MỚI tab so the current canonical layout remains stable.
- No production Ribbon behavior, startup/lifecycle, runtime/native, Level, Curtain, Undo/semantic, rebar, semantic-sheet, or LOCAL_ONLY surfaces are modified.

## Validation

- Read back the updated focused preflight from `main` and verify each asserted pair exists in the current `RibbonBootstrapper.cs` source.
- CI baseline remains release V25 run #160 PASS on `6d834dbadc4c13ce4f7966fbaea00cf1ec8499bb`; do not claim fresh current-HEAD CI unless a new run actually executes.
- BricsCAD V25 visual/click/runtime acceptance remains native evidence, not inferred from this static guard.
