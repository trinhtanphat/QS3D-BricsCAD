# V26 candidate held final-path identity qualification

## Purpose

Qualify the V26 release candidate admission invariant introduced by issue #6180: every admitted ZIP, checksum, provenance, optional update manifest, and publication script must prove that the already-open held `FileStream` resolves to the same canonical final pathname that passed ordinary-file/reparse admission.

This closes the pathname check-to-open window where a different reparse/path generation could otherwise be opened while retaining metadata compatible with the pre-open pathname observation.

## Automated qualification

Run the auto-discovered source guard from the repository root:

```text
python scripts/preflight-v26-candidate-held-final-path.py
```

Expected result:

```text
PASS: V26 candidate admission binds every held input to the opened handle final-path identity
```

The guard mutation-locks the Windows `GetFinalPathNameByHandleW` declaration and invocation, use of the exact held stream `SafeFileHandle`, extended DOS/UNC normalization, canonical-path comparison, fail-closed mismatch handling, and ordering after the file is opened but before held state is returned.

Also run the normal aggregate/shared CI. The new guard is intentionally auto-discovered; no manual registration is permitted.

## Windows functional qualification

Perform this qualification on Windows, matching the `release-v26.yml` runner platform.

1. Build or stage a normal V26 candidate set and invoke `scripts/assert-v26-candidate-identity.ps1` with the same inputs used by release qualification. A valid ordinary-file candidate must continue through candidate identity admission.
2. Confirm the candidate files are opened with held read streams and that `GetFinalPathNameByHandleW` is evaluated from each stream's `SafeFileHandle` before that held input is consumed.
3. Exercise a pathname whose final path is exposed through the normal Windows extended DOS prefix (`\\?\C:\...`). The helper must normalize it to the admitted canonical DOS pathname before comparison.
4. Where a UNC candidate location is supported by the release environment, exercise the extended UNC form (`\\?\UNC\server\share\...`) and confirm it normalizes to `\\server\share\...` before comparison.
5. In a controlled test fixture, arrange for the pathname/reparse target to differ from the path resolved by the opened handle across the check-to-open boundary. Admission must fail with the opened-handle final-path mismatch error; no checksum, provenance, package metadata, or publication script from that candidate may be trusted afterward.
6. Confirm pre-existing protections remain active: reparse components are rejected, held streams retain `FileShare.Read`, length/last-write generation checks still run, SHA-256 is computed from held streams, and post-admission/post-publication `Assert-Held` checks still execute.

## Failure expectations

Any zero/error result from `GetFinalPathNameByHandleW`, unsupported/oversized final path, invalid or closed held handle, or canonical/final-path mismatch is a release-admission failure. Do not fall back to pathname-only metadata comparison, repeat a pathname reparse check as a substitute for handle identity, or continue publication after a failed proof.

This qualification tests admission only. Do not publish a production release while deliberately exercising race/reparse failure fixtures.