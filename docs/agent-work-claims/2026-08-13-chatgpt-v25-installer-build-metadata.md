# Work claim — V25 installer SemVer build-metadata compatibility

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol`
- Registered: `2026-08-13T12:06:48+07:00`
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

## Completion condition

Current `main` contains the source fix and focused regression guard, final source is read back from GitHub, this claim is marked `COMPLETED`, and no CI/runtime/release PASS is claimed without separate evidence.
