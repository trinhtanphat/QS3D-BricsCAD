# Work claim — Curtain opening pieces read-only result parity

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T08:50:00+07:00`
- Completed: `2026-08-12T08:54:00+07:00`
- Baseline main SHA observed: `953bc91e46bfbcbb2e089080e1d647f6529c74ac`
- Priority: P2 — Core planner result structural immutability

## Confirmed defect

`CurtainWallOpeningFramePlan.Pieces` and `CurtainWallOpeningPanelPlan.Pieces` were exposed as `IReadOnlyList<...>`, but both planners populated those properties with `.ToArray()`. Callers could cast returned `Pieces` back to the concrete array and replace elements, so the advertised read-only collection boundary was structurally mutable.

## Implemented contract

1. Frame and panel planner `Pieces` results now use read-only collection wrappers and reject structural/index mutation.
2. Piece ordering, values, counts, areas and interruption semantics are unchanged.
3. Piece DTO property mutability is unchanged; this lane only protects the returned collection structure.
4. Input limits, opening subtraction math and native CAD integration are unchanged.

## Integration evidence

- Claim registration: `54d47c7f4d2d1dab4080a0c0d70ac0a65467a2ae`.
- Planning: `fc26083a68df9adbabd2659f785b94e98f821e4a`.
- Frame source fix on `main`: `53d167f5745396dba71e27b17ca43f6267dad869` (`+2/-2`, only wraps the ordered array with `Array.AsReadOnly`).
- Panel source fix on `main`: `4c88546c99c6894d41ad496748de763030896213` (`+2/-2`, same structural wrapper only).
- Focused smoke on `main`: `73f68af0791c80b0058af8b522f147190a25683b`.
- Moving-main ancestry at observed HEAD `f3a1868156bc7b8f846886d9a03d34ee0db801e4`: frame fix remained an ancestor; the only reserved source listed after it was this lane's panel fix. After the panel fix, no later Frame/Panel source overlap occurred. The smoke remained an ancestor with only an unrelated claim update after it.

## Validation boundary

Committed deterministic Core smoke coverage plus exact source/diff and ancestry review. GitHub Actions were not dispatched and no executable smoke, local .NET build, or licensed BricsCAD runtime PASS is claimed from this connector-only environment.
