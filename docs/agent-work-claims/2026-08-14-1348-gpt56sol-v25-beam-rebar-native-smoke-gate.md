# Work claim — V25 Beam Rebar native-smoke feature gate

- Status: `ACTIVE`
- Agent: `gpt56sol-v25-beam-rebar-native-smoke-gate-20260814-1348`
- Registered: `2026-08-14T13:48:00+07:00`
- Baseline main SHA: `2f1c78a2ccf2382c7a8ccb7c1e8b733e178b89c1`
- Failure run: `#155` / `31777136878`
- Failure job: `94694842214`
- Failure source SHA: `629091ba0346483d154dc5334c13ae8d45e6d2ff`
- Failing aggregate member: `scripts/preflight-beam-rebar-native-smoke.py`
- Priority: `P0 / current V25 cloud feature-gate blocker`

## Fresh evidence

Run #155 successfully passed release-source preparation, the manual-only CI gate, updater/startup/source-reconcile/obstacle/Level/project-state/automation/Ribbon/native-Core gates and then failed the aggregate Feature Gates step. The aggregate's final fatal list contains only `scripts/preflight-beam-rebar-native-smoke.py`; Core smoke/build/package/publish were therefore skipped.

Recent commit/claim scans did not identify a current claim explicitly owning this exact preflight path. Existing Beam/Rebar work remains outside this reservation unless the failing preflight proves an exact dependency and this claim is amended first.

## Initial reserved scope

- `scripts/preflight-beam-rebar-native-smoke.py`
- this claim file

No production/test helper path is claimed yet. The next step is to read the exact failing log block and this preflight. If the failure is a stale static contract, fix only this preflight. If it exposes a real source/test defect, amend this claim in a claim-only commit with the exact required paths **before** reading/editing those implementation surfaces.

## Excluded scope

- all other `scripts/` preflights;
- production `src/` until explicitly amended after diagnosis;
- Core smoke/test files until explicitly amended after diagnosis;
- active Preview Review XML fixture work;
- #1005/#1106/#1125/#79/#982 lanes;
- release preparation/manual-only dispatcher lane;
- GitHub Actions dispatch/rerun;
- licensed BricsCAD runtime acceptance.

## Validation plan

1. Extract the exact #155 failure text for this member.
2. Compare the failed-head and current preflight contract.
3. Recheck concurrent commits/claims before any write.
4. Make the narrowest evidence-backed correction and add/retain fail-closed coverage.
5. Read back committed content and verify main ancestry.
6. Do not label native V25 runtime PASS from a static/cloud guard.

## Completion condition

The exact aggregate member failure is deterministically corrected or shown stale/resolved by a concurrent change, with exact commit/readback evidence and no ownership collision.
