# Work claim — Wall Quantity documentation discoverability

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-wall-quantity-doc-discoverability`
- Registered: `2026-08-11T21:25:00+07:00`
- Completed: `2026-08-11T21:29:00+07:00`
- Baseline main SHA: `2a2548a92c0fc14f50ec61317497540a0ad804b5`
- Documentation commits: `38919ce0f7dfe6b278a89a1a6b98cf8cbea2f96f`, `94ca82cc3b9e5dbbf9ae49e3cdef0074f5b108e1`
- Priority: P2

## Delivered scope

Made the already-merged `QS3DWALLQTY` workflow discoverable from the canonical command reference and compact documentation map, without changing product code or any concurrently owned UX surface.

## Delivered files

- `docs/COMMANDS.md`
- `docs/README.md`

## Delivered contract

- `docs/COMMANDS.md` now registers `QS3DWALLQTY` under the Wall workflow and describes browser/filter/detail/totals, detached recompute, filtered XLSX export, guarded default-on `Bám 3D` and explicit `Định vị 3D`, with the licensed V25 runtime boundary kept explicit;
- `docs/README.md` now links `WALL-QUANTITY-TAKEOFF.md` from the canonical Quantity/BQ workflow map;
- root README, Ribbon, Start Center, Schedule Hub, `Commands.cs`, quantity formulas and product code were not touched;
- no GitHub Actions workflow was dispatched/re-run by this documentation lane.

## Completion

Canonical command/docs indexes now point to the Wall Quantity workflow. Licensed modeless/viewport runtime qualification remains local-only and is not represented as completed by these documentation commits.
