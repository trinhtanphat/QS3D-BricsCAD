# Work claim — Wall Quantity documentation discoverability

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-quantity-doc-discoverability`
- Registered: `2026-08-11T21:25:00+07:00`
- Baseline main SHA: `2a2548a92c0fc14f50ec61317497540a0ad804b5`
- Priority: P2

## Reserved scope

Make the already-merged `QS3DWALLQTY` workflow discoverable from the canonical command reference and compact documentation map, without changing product code or any concurrently owned UX surface.

## Reserved files

- `docs/COMMANDS.md`
- `docs/README.md`
- this claim file for close-out

## Contract

- add `QS3DWALLQTY` to the wall/quantity command catalog with its browser/filter/detail/totals/XLSX and guarded `Bám 3D` / `Định vị 3D` behavior;
- add `WALL-QUANTITY-TAKEOFF.md` to the canonical major-workflow documentation map;
- keep runtime truth explicit: merged source behavior is not licensed V25 interactive qualification;
- do not touch README root, Ribbon, Start Center, Schedule Hub, `Commands.cs`, quantity formulas, local inbox or other active product lanes;
- no GitHub Actions dispatch/re-run.

## Completion condition

Canonical command/docs indexes point to the Wall Quantity workflow and this claim is closed with the exact pushed SHA.
