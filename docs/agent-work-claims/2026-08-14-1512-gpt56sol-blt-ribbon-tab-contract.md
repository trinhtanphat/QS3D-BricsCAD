# Work claim — BLT reference Ribbon tab contract hardening

- Status: `CLOSED`
- Agent: `chatgpt-web-gpt56sol-ribbon-tab-contract`
- Registered: `2026-08-14T15:12:00+07:00`
- Baseline main SHA: `427d029ad834197a43ddfa302e36128334af5ae4`
- Owner request: continue all 100%, fix bugs/update code and commit/push main.

## Concrete gap

The BLT-reference parity preflight already protected Home file/settings mappings and many command/tree surfaces, but it did not protect the canonical top-level Ribbon tab IDs/titles that were implemented for screenshot parity. A parallel merge could silently rename/remove one of those groups while the focused parity preflight still passed.

## Reserved scope

- `scripts/preflight-blt-reference-ui-parity.py`
- this claim file

## Implementation

- Added stable ID/title contract assertions for the established canonical top-level Ribbon tabs in `RibbonBootstrapper.cs`.
- Covered KHỞI ĐẦU, THIẾT LẬP DỰ ÁN, TẠO MỚI, MÔ HÌNH BIM, NHẬN DẠNG, VẼ, TOOL, MODELING, XEM, ĐỊNH LƯỢNG, BẢN SỬA ĐỔI.
- Each pair must occur exactly once; matching is whitespace-tolerant so formatting-only changes do not break the guard.
- No production Ribbon behavior, startup/lifecycle, runtime/native, Level, Curtain, Undo/semantic, rebar, semantic-sheet, or LOCAL_ONLY surfaces were modified.

## Evidence

- Claim commit: `79a9706c49ef84beea8efb5d1bf5fe09472713b9`.
- Source branch head: `ec60c24cbdd1d83062e7fa62507bfcb78f438590`.
- PR: `#1299`.
- Main merge commit: `b1f74290bbb92da59de384dede86e7165810e9b1`.
- Read-back of `RibbonBootstrapper.cs` confirmed every asserted ID/title pair exists in the current canonical source.
- Release V25 run #160 remains the last previously verified PASS baseline on `6d834dbadc4c13ce4f7966fbaea00cf1ec8499bb`; fresh current-HEAD CI is tracked separately and is not inferred by this claim.
- BricsCAD V25 visual/click/runtime acceptance remains native evidence, not inferred from this static guard.
