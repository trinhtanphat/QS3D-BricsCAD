# Work claim — Generated Curtain Frame health null-element fail-visible

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-curtain-frame-null-health`
- Registered: `2026-08-12`
- Baseline main SHA: `3a766aeb9192ae12d42fc4f9bd2d27b05baaae37`
- Priority: P1 — standalone generated health must not silently treat malformed ProjectState entries as clean.
- Task Key: `CORE-CURTAIN-FRAME-NULL-HEALTH`

## Confirmed defect

`GeneratedCurtainFrameHealthService.Inspect(ProjectState)` and its ownership-index traversal still execute `if (element == null) continue;`. A malformed project containing a null semantic element can therefore be silently normalized by this standalone provider instead of failing visibly. Neighboring generated-health providers already reject null semantic elements.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs`
- one focused auto-discovered `scripts/preflight-*.py` regression gate
- this claim file

Do not modify curtain frame builders, BricsCAD runtime commands, curtain panel health, Grid health, wall quantity, or `ComprehensiveModelHealthService`.

## Intended contract

- Direct Curtain Frame health inspection throws `InvalidOperationException` on a null project element instead of silently skipping it.
- Ownership-index traversal follows the same fail-visible contract.
- Valid projects retain existing handle/count/config/mode/staleness diagnostics.
- Composite health continues to rely on its existing safe-provider boundary; no aggregate changes in this lane.
- Inspection remains read-only.
- No GitHub Actions/build/release dispatch and no BricsCAD runtime PASS claim from this remote lane.

## Completion condition

Standalone Curtain Frame health can no longer return clean solely because a null semantic element was skipped, focused regression coverage pins both traversal sites and valid-path compatibility, and this claim is closed after merged-main readback.
