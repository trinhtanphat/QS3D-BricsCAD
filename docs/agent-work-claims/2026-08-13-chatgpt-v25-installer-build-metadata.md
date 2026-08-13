# Work claim — V25 installer SemVer build-metadata compatibility

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T12:06:48+07:00`
- Completed: `2026-08-13T12:15:00+07:00`
- Baseline main SHA: `5a2ac2eec479bc15afdd282dd3a4b0874e6a2a2e`
- Priority: Owner reproduced an install-blocking failure from `v0.1.0-preview.3`: the packaged V25 DLL ProductVersion carries SDK source-revision SemVer build metadata (`+<commit>`) while `PACKAGE-METADATA.productVersion` carries the canonical source SemVer without build metadata. Owner explicitly requested a source fix committed and pushed to `main`.

## Reserved scope

Fix V25 package identity validation so canonical public SemVer identity remains exact while SDK-appended build metadata does not falsely reject a valid same-version package. Preserve strong package provenance by requiring the two packaged managed DLLs to agree on their full ProductVersion. Add focused static regression coverage for this boundary.

## Expected surfaces

- `scripts/install-v25-autoload.ps1`
- `scripts/preflight-installer-package-identity.py`
- this claim file

## Excluded scope

- V26 installer/updater behavior
- changing release tags or package public SemVer
- changing V25/Core release-version numbers
- weakening SHA256SUMS or Authenticode checks
- GitHub Actions dispatch/release publication
- licensed BricsCAD runtime qualification

## Validation plan

- Preserve strict SemVer parsing and canonical public-version equality (`major.minor.patch-prerelease`).
- Accept DLL ProductVersion build metadata such as `0.1.0-preview.3+<sha>` when package metadata is `0.1.0-preview.3`.
- Reject mismatched public versions/prereleases.
- Require `QS3D.BricsCAD.V25.dll` and `QS3D.Core.dll` full ProductVersion text to match each other, including build metadata, so mixed-revision payloads remain fail-closed.
- Extend `scripts/preflight-installer-package-identity.py` to lock the new contract and preserve existing integrity/order guards.
- Do not dispatch GitHub Actions. Existing local packaging/DemandLoad qualification remains covered by `LOCAL-001`.

## Coordination

Recent installer package-identity claim history is already released/completed; current claim/code searches found no overlapping ACTIVE installer build-metadata lane. Concurrent Curtain/BLT3D work is outside this scope. Refresh `main` before each write and never force-push.

## Completion

- Source fix: `84ebcc7c9327954624e02e8ccc6bf4a2343f73a5` — `fix(installer): accept SemVer build metadata safely`.
- Regression guard: `945d0cbdcae3957f5bfd90e6ccc8cf79f7f18a34` — `test(installer): guard SemVer build metadata identity`.
- GitHub readback confirmed the installer now compares canonical/public SemVer identity without `+build-metadata`, while requiring both packaged managed DLLs to agree on the complete ProductVersion including build metadata.
- The focused preflight models the exact owner-reported `0.1.0-preview.3+a99038557bfeab6fd8945cb28a0f890c46480184` case, canonical no-build-metadata packages, public-version mismatch rejection and mixed-source-revision rejection.
- Existing SHA256SUMS, optional/required Authenticode, existing-install ownership and transactional rollback guards remain in place.
- No GitHub Actions were dispatched by this work. No licensed BricsCAD installation/runtime PASS is claimed; runtime packaging/DemandLoad qualification remains under existing `LOCAL-001`.

## Completion condition

Satisfied: current `main` contains the source fix and focused regression guard, source/test commits were read back from GitHub, and this claim is closed without manufacturing CI/runtime/release evidence.
