# Work claim — release prerelease ordinal validation

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol`
- Registered: `2026-08-14T20:38:00+07:00`
- Baseline main SHA: `ce29bc89113961a4cd3874f5b5352ca50af5e260`
- Implementation branch: `agent/chatgpt-gpt56sol/release-preview-ordinal`
- Implementation commit: `4e7e42570e4250217de89130e0789dbb86645294`
- Integration batch: `integration/20260814-release-preview-ordinal`
- Final integration candidate: `1ed70f1a613f1dda4caed9c3d454675883048014`
- Final main landing PR: `#1338`
- Final main landing commit: `0ea20bdc09359a286270f97a567eea9b180b2a6e`
- Priority: remote-safe contract bug found during owner-requested whole-repository review; `docs/RELEASE-NAMING.md` requires prerelease ordinal `N >= 1`, while the V25 cloud workflow and both release-preparation version parsers accepted `.0`.

## Reserved scope

Align every automated preview-release tag/version validator in the V25 cloud release path with the canonical release naming policy so zero-valued prerelease ordinals such as `preview.0` are rejected before source synchronization, release preparation or publishing.

## Expected surfaces

- `.github/workflows/release-v25-cloud.yml`
- `scripts/prepare-v25-cloud-release.ps1`
- `scripts/sync-preview-release-version.ps1`
- `scripts/preflight-release-preview-ordinal.py`
- this claim record for implementation/integration close-out

## Excluded scope

- No BricsCAD LOCAL_ONLY/runtime qualification, NETLOAD, native UI, private DWG, signing or licensing work.
- No product-version bump, release/tag deletion, historical release retagging, or mutation of already-published releases.
- No unrelated release-title redesign or changes outside the zero-ordinal contract unless required by a directly adjacent deterministic regression guard.
- No overlap with the `slabOpen`, drawing-fingerprint, or other feature/source claims.

## Validation evidence

- Agent diff from `2d47dc07bec19e652ce85fb2ad34f4d61bfa888a` to `4e7e42570e4250217de89130e0789dbb86645294` is limited to the three release validators plus the new auto-discovered preflight guard.
- Each existing validator changes only the preview-ordinal regex from zero-or-positive to strictly positive; no release flow step was removed or reordered.
- Deterministic semantic cases accept `v0.1.0-preview.1`, `v1.0.0-preview.12`, `v10.20.30-preview.65535` and reject `preview.0`, `preview.01`, empty ordinal, `rc.1`, and a leading-zero major.
- `scripts/preflight-all.py` auto-discovers `scripts/preflight-*.py`, so `preflight-release-preview-ordinal.py` participates in the existing aggregate source-guard gate without changing the aggregator.
- Before final landing, the integration branch was repeatedly reconciled with current `main`, including the concurrent drawing-fingerprint source landing and its docs-only claim closeout. The final PR remained limited to the four reserved implementation surfaces.
- PR `#1338` was merged with expected head SHA `1ed70f1a613f1dda4caed9c3d454675883048014`.
- Post-merge read-back of `main` resolved exactly to `0ea20bdc09359a286270f97a567eea9b180b2a6e`; that merge commit has the previous current `main` and the final integration candidate as its two parents, proving the reserved implementation is represented in the integrated result.

## Coordination

The completed release-naming-policy claim reserved documentation only and explicitly excluded workflow/source/test changes, so this was a separate follow-up implementation lane. Concurrent drawing-fingerprint work was preserved and reconciled before the final landing rather than overwritten.

## Completion

The remote-safe source/integration portion of this lane is complete on `main` at `0ea20bdc09359a286270f97a567eea9b180b2a6e`. Automatic post-integration CI is tracked separately under repository CI policy; this claim does not manufacture LOCAL_ONLY BricsCAD runtime evidence.
