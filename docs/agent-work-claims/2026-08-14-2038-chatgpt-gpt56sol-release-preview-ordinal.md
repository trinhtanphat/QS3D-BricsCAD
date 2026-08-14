# Work claim — release prerelease ordinal validation

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol`
- Registered: `2026-08-14T20:38:00+07:00`
- Baseline main SHA: `ce29bc89113961a4cd3874f5b5352ca50af5e260`
- Implementation branch: `agent/chatgpt-gpt56sol/release-preview-ordinal`
- Integration batch: `integration/20260814-release-preview-ordinal`
- Priority: remote-safe contract bug found during owner-requested whole-repository review; `docs/RELEASE-NAMING.md` requires prerelease ordinal `N >= 1`, while the V25 cloud workflow and both release-preparation version parsers currently accept `.0`.

## Reserved scope

Align every automated preview-release tag/version validator in the V25 cloud release path with the canonical release naming policy so zero-valued prerelease ordinals such as `preview.0` are rejected before source synchronization, release preparation or publishing.

## Expected surfaces

- `.github/workflows/release-v25-cloud.yml`
- `scripts/prepare-v25-cloud-release.ps1`
- `scripts/sync-preview-release-version.ps1`
- existing remote-safe/static release validation guard or a narrowly scoped new guard if needed
- this claim record for implementation/integration close-out

## Excluded scope

- No BricsCAD LOCAL_ONLY/runtime qualification, NETLOAD, native UI, private DWG, signing or licensing work.
- No product-version bump, release/tag deletion, historical release retagging, or mutation of already-published releases.
- No unrelated release-title redesign or changes outside the zero-ordinal contract unless required by a directly adjacent deterministic regression guard.
- No overlap with the active `slabOpen`, drawing-fingerprint, or other feature/source claims.

## Validation plan

- Verify valid positive ordinals such as `preview.1` and `preview.12` remain accepted by every validator in the V25 preview path.
- Verify `preview.0`, leading-zero ordinals, malformed or mismatched tags are rejected by the workflow and both release scripts.
- Run/inspect the repository's relevant static/preflight guard for release workflow policy when available; otherwise add a deterministic remote-safe guard and validate its positive/negative fixtures.
- Read back the agent/integration/main trees and compare exact SHAs before declaring integration complete.

## Coordination

The completed release-naming-policy claim reserved documentation only and explicitly excluded workflow/source/test changes, so this is a separate follow-up implementation lane. Current recent active work on `slabOpen` and drawing-fingerprint canonicality is unrelated and must not be touched. Scope was expanded before editing after read-only inspection showed the same zero-ordinal regex in both release-preparation scripts.

## Completion condition

The workflow, both release scripts and regression guard enforce positive preview ordinals, the implementation is integrated through the declared integration branch and one final main landing, the claim is closed only after the exact final main SHA contains the fix, and any automatically dispatched CI is reported separately from LOCAL_ONLY runtime evidence.
