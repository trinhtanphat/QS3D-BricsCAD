# V25 signed ZIP generation-bound publication qualification

## Scope
This qualification covers the final publication boundary in `scripts/finalize-v25-signed-package.ps1` after signed package payload, checksum-manifest, ZIP structure and outer SHA-256 admission. It does not perform signing or publish a GitHub Release.

## Security invariant
The staged ZIP generation whose structure, manifest coverage and outer SHA-256 are admitted must remain the exact generation published as `PackageZip`. Closing the verified staging handle and later publishing the staging pathname is not sufficient: a non-cooperating replacement can change the pathname binding between verification and publication.

Required behavior:

- hold the staged ZIP generation across its final verification and publication commit;
- immediately before publication, revalidate that the held generation still has the admitted SHA-256 and pathname/file identity;
- publish using a Windows handle-bound rename/disposition primitive so the source object cannot be substituted through a pathname race;
- support both a missing destination and an existing ordinary destination without weakening rollback of the prior admitted ZIP;
- if source or destination identity becomes unprovable, fail closed and preserve uncertain generations for manual recovery rather than deleting by historical pathname ownership;
- keep checksum-manifest coverage, ZIP traversal/duplicate rejection, Authenticode signer checks, managed version identity and reparse-chain defenses intact.

## Deterministic adversarial cases

1. **Stable no-existing destination:** verified staged handle is renamed to the canonical ZIP and the published SHA-256 equals the admitted staged SHA-256.
2. **Stable existing destination:** prior ordinary ZIP is retained as the rollback generation while the held verified staging generation atomically takes the canonical name.
3. **Staging pathname replacement:** replacing the staging pathname after verification must not change the object published from the already-open handle.
4. **Destination replacement:** a destination generation change during commit must either be rejected before replacement or handled by the exact handle-bound transaction; never delete an uncertain replacement by pathname.
5. **Post-verification source mutation attempt:** held-generation identity/hash revalidation fails before commit.
6. **Native rename failure:** publication remains uncommitted and rollback preserves the prior admitted canonical ZIP when one existed.
7. **Rollback ambiguity:** report both original failure and rollback failure; do not claim successful finalization.

## Automated guard
Run:

```text
python scripts/preflight-v25-signed-zip-publication-identity.py
```

The guard is auto-discovered by aggregate feature preflight and mutation-locks held-generation consumption, final same-handle validation, the handle-bound rename primitive, and the absence of the old `$tempZip` pathname `File.Move` / `File.Replace` publication calls.

## Admission
Fresh Shared CI must be GREEN on the exact candidate head after reconciliation with protected `main`. Draft GREEN is not final merge evidence; after marking ready, obtain fresh exact-head required `preflight` and `core` success. Do not publish a commercial release solely to qualify this source/static carrier.