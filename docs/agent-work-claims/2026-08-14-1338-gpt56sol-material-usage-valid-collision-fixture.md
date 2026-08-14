# Work claim — Material Usage valid delimiter-collision fixture

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-material-usage-collision`
- Registered: `2026-08-14T13:38:00+07:00`
- Baseline main SHA: `3695aaf91ba50aad825e084c5056e47b970551dc`
- Owner request: continue all; reconcile remaining remote-safe blockers without weakening production validation.

## Concrete blocker

The complete registered Core smoke has advanced past the corrected Door/Opening grouping fixture and now stops in `MaterialUsageScheduleGroupKeyCollisionSmoke`. The fixture still uses `U+001F` in a `FloorDefinition.Id`, but current persistability guards correctly reject control characters before the grouping assertion can run.

Production `MaterialUsageScheduleBuilder` already uses length-prefixed grouping tokens and must remain unchanged. The smoke should demonstrate the historical delimiter-only collision with values that are valid under the current domain contract.

## Reserved scope

- `tests/QS3D.Core.SmokeTests/MaterialUsageScheduleGroupKeyCollisionSmoke.cs`
- this claim file

## Implementation boundary

- Test-fixture reconciliation only; do not relax `FloorDefinition`, `ProjectElement`, ID/property persistability or reporting identity validation.
- Replace the unreachable control-character collision data with printable delimiter-bearing values accepted by current domain rules.
- Add an explicit test-local legacy delimiter-key equality assertion so collision intent remains deterministic and visible.
- Preserve production assertions that identical material tuples aggregate while the two distinct current grouping tuples remain separate.
- Do not touch Level/rebar LOCAL-003 production/probe/runner surfaces, Source Reconcile, Curtain geometry, release preparation, or GitHub Actions workflow code.

## Validation plan

- Read back current production `MaterialUsageScheduleBuilder.GroupKey` to confirm length-prefixed production grouping stays unchanged.
- Read back the updated smoke and verify the two test tuples collide only under the test-local delimiter-only serializer.
- Re-check the next available Core/CI evidence after concurrent work settles; do not claim licensed BricsCAD runtime evidence from this test-only lane.
