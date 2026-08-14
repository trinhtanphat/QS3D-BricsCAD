# Work claim — Material Usage valid delimiter-collision fixture

- Status: `COMPLETED_BY_CONCURRENT_MERGE`
- Agent: `chatgpt-web-gpt56sol-material-usage-collision`
- Registered: `2026-08-14T13:38:00+07:00`
- Baseline main SHA: `3695aaf91ba50aad825e084c5056e47b970551dc`
- Owner request: continue all; reconcile remaining remote-safe blockers without weakening production validation.
- Concurrent claim PR: `#1171` / merge `68f6aa217fee054f5b1e8bc9425d36086dcc013a`
- Concurrent implementation PR: `#1173` / merge `0ca306c1eced3f199e98fcbeaa21794d3ce460a0`

## Concrete blocker

The complete registered Core smoke had advanced past the corrected Door/Opening grouping fixture and stopped in `MaterialUsageScheduleGroupKeyCollisionSmoke`. The fixture still used `U+001F` in a `FloorDefinition.Id`, but current persistability guards correctly reject control characters before the grouping assertion can run.

Production `MaterialUsageScheduleBuilder` already uses length-prefixed grouping tokens and remains unchanged.

## Collision handling and completion

This claim was published while GitHub code/claim search had not yet surfaced an earlier concurrent claim. When this agent attempted the test-file update, GitHub correctly returned a blob-SHA conflict. Immediate readback showed the same intended printable-delimiter fixture had landed concurrently, so no duplicate/overwrite was attempted.

PR `#1173` completed the exact test-only contract:

- replace the unreachable U+001F Floor identity data with printable `|` values;
- explicitly prove the two five-token fixtures collide under a test-local delimiter-only serializer;
- retain the two-row separation plus identical-tuple count/length aggregation assertions;
- leave production/domain/Level/probe/runner/BricsCAD/Actions surfaces unchanged.

Its reported validation is Core smoke Release build `0 warnings / 0 errors`, all five `preflight-material-usage*.py` gates PASS, and the complete registered Core smoke advances to the next independent stale fixture: `MeasurementWorkItemCoverageSmoke.CorruptProjectStateFailsClosed` line 143 constructs a control-character `ProjectElement` ID.

## Preserved boundary

No production validation was weakened. No Level/rebar LOCAL-003, Source Reconcile, Curtain geometry or release workflow code was changed by this claim. No licensed BricsCAD runtime evidence is claimed here.
