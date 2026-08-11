# Work claim — Direct Draw Create Similar

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-11T19:31:00+07:00`
- Baseline main SHA: `0296f6f31e28a598474875805b934edc26c98e60`
- Priority: reduce owner-reported authoring interactions by reusing an existing QS3D element's Family/Type for the next Direct Draw operation

## Reserved scope

Implement a source-safe **Create Similar / Vẽ Tương Tự** Direct Draw lane: select one existing QS3D semantic source or generated object, resolve its canonical semantic owner and Family/Type without creating a second model, activate that existing Family through the repository's current project/family mutation contract, then delegate to the existing `QS3DDRAWACTIVE` or `QS3DDRAWACTIVEADV` workflow. Add static contract coverage and a focused handoff documenting exact LOCAL_ONLY BricsCAD V25 qualification.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/CreateSimilarCommands.cs` (new command surface, exact name may be adjusted only if an existing canonical command file is discovered during source orientation)
- existing semantic ownership / Family activation services only if a small reusable helper is required; no new parallel ownership model
- `scripts/preflight-create-similar.py`
- `docs/DIRECT-DRAW-CREATE-SIMILAR-2026-08-11.md`
- `docs/LOCAL-AGENT-INBOX.md` — extend existing `LOCAL-008`; do not create a second local queue item
- this claim file for close-out status

## Excluded scope

- No DrawJig/transient preview implementation or runtime PASS claim.
- No continuous/repeated authoring implementation owned by `LOCAL-008`.
- No geometry-builder rewrite, second semantic model, or broad Workspace redesign.
- No GitHub Actions dispatch, release operation, signing, installer, or live BricsCAD V25 qualification.
- No competing changes to the agent-registration protocol claim.

## Validation plan

- Static source review against current `main` for semantic-source and generated-output ownership resolution, stale-project handling, Family existence/category checks, and command dispatch.
- Add an auto-discovered preflight that fails if Create Similar bypasses canonical ownership/Family activation or directly invokes geometry builders instead of delegating to Active Family Quick Draw.
- Re-fetch latest `main` before implementation merge and preserve concurrent commits.
- Extend `LOCAL-008` with exact Create Similar selection/cancel/stale-project/generated-owner/document-switch qualification; do not claim those interactive checks remotely.

## Coordination

The repository-wide registration-protocol bootstrap explicitly excludes QS3D product source changes, and the Workspace multi-selection policy lane has separate reserved files. This lane does not overlap either. If a new overlapping Direct Draw or selection-ownership claim appears after this reservation is pushed, stop and re-scope before implementation.

## Completion condition

Create Similar Quick/Advanced commands are merged to current `main` with static contract coverage and handoff documentation, `LOCAL-008` carries the exact interactive qualification delta, this claim is marked `COMPLETED` with the implementation SHA(s), and all live BricsCAD V25-only evidence remains explicitly unclaimed/LOCAL_ONLY.
