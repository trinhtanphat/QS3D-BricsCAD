# Work claim — release naming policy

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol`
- Requested by: repository owner in chat on 2026-08-14
- Baseline `main`: `abec4c3b71ebd906bf2ba64ed0883d71ae1886bc`
- Scope: define the canonical public Git tag, GitHub Release title, prerelease-channel, build-metadata, and downloadable-asset naming convention for QS3D for BricsCAD.
- Expected file: `docs/RELEASE-NAMING.md`
- Reserved surface: release/version naming documentation only.
- Exclusions: no workflow/source/test changes, no release deletion or retagging, no mutation of historical tags/releases, no CI dispatch, no product-version bump.
- Validation: reviewed the integrated Markdown for deterministic examples, SemVer ordering safety, V25/V26 compatibility placement, and explicit migration guidance from the historical `v0.1.0-preview.10014`; verified the integration delta was limited to `docs/RELEASE-NAMING.md` plus this claim record.
- Implementation branch: `agent/chatgpt-gpt56sol/release-naming-policy`
- Implementation commit: `8cdeb722f3d6dd43e82d0f565f3a701db15912a8`
- Integration branch: `integration/20260814-release-naming-policy`
- Integration PR: `#1336`
- Final main landing PR: `#1337`
- Final main landing commit: `37ec8233cde56f2c6c0ea4bfb7aa04d25d9d8b8f`

## Owner intent

Prevent ad-hoc public names such as `QS3D for BricsCAD V25 v0.1.0-preview.10014` by documenting a short, deterministic convention that future agents and release automation must follow.

## Completion

The canonical policy is now present on `main` as `docs/RELEASE-NAMING.md`. No workflow, source, test, existing tag, existing release, or CI run was changed by this lane.
