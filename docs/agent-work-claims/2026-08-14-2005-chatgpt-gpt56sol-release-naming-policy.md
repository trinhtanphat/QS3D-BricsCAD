# Work claim — release naming policy

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol`
- Requested by: repository owner in chat on 2026-08-14
- Baseline `main`: `abec4c3b71ebd906bf2ba64ed0883d71ae1886bc`
- Scope: define the canonical public Git tag, GitHub Release title, prerelease-channel, build-metadata, and downloadable-asset naming convention for QS3D for BricsCAD.
- Expected file: `docs/RELEASE-NAMING.md`
- Reserved surface: release/version naming documentation only.
- Exclusions: no workflow/source/test changes, no release deletion or retagging, no mutation of historical tags/releases, no CI dispatch, no product-version bump.
- Validation: review the final Markdown for deterministic examples, SemVer ordering safety, V25/V26 compatibility placement, and explicit migration guidance from the historical `v0.1.0-preview.10014` tag; verify the implementation diff is limited to the reserved documentation surface.

## Owner intent

Prevent ad-hoc public names such as `QS3D for BricsCAD V25 v0.1.0-preview.10014` by documenting a short, deterministic convention that future agents and release automation must follow.
