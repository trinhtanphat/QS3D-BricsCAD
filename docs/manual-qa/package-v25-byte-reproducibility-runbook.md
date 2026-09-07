# V25 byte-reproducible package runbook

Lane: C05 CI / Release / Deterministic Build Validation

## Contract

For one exact Git source generation and the same admitted V25 build inputs, `scripts/package-v25.ps1` must not inject wall-clock or host-culture ordering into the release artifact. `PACKAGE-METADATA.json.generatedUtc` is bound to the exact HEAD commit timestamp. ZIP entry names are repository-relative, normalized with `/`, sorted with `StringComparer.Ordinal`, and receive the same source-bound timestamp. ZIP entries use `NoCompression` so release bytes do not depend on a deflate implementation. `COMMANDS.txt` and `SHA256SUMS.txt` are also ordinal-sorted.

The deterministic ZIP writer keeps each staging file open while copying that admitted generation into its ZIP entry and rechecks pathname/generation binding before releasing it. Existing reparse/path-containment guards remain fail-closed.

## Automated guard

Run:

```text
python scripts/preflight-package-v25-byte-reproducibility.py
```

The guard is auto-discovered by `scripts/preflight-all.py` and mutation-probes source timestamp binding, explicit `System.IO.Compression` loading for Windows PowerShell 5.1, normalized entry names, ordinal ordering, stored ZIP entries, deterministic metadata, and the final deterministic writer call.

## Manual reproducibility qualification

On a clean Windows release worker with the exact same checked-out commit and identical admitted V25 build outputs:

1. Run the normal V25 package build once and record SHA-256 of `dist/QS3D-BricsCAD-V25.zip`.
2. Remove `dist/` only; do not rebuild or mutate source/build outputs.
3. Change the process culture if practical, then run the package build again.
4. Confirm the ZIP SHA-256 is identical.
5. Inspect both archives and confirm identical ordered entry-name sets, `/` separators, source-bound entry timestamps, `PACKAGE-METADATA.json`, `COMMANDS.txt`, and `SHA256SUMS.txt`.
6. Run the existing V25 package-integrity verifier against the resulting ZIP and require PASS.

A mismatch is release-blocking; do not refresh an expected checksum to hide it. Determine whether the difference comes from a changed input generation, a signing/build input, package ordering/metadata, or runtime/toolchain behavior.

## Scope and release implication

This fence makes packaging deterministic for identical admitted package inputs and source generation. It does not claim that separately rebuilt signed DLLs are byte-identical unless their own build/signing pipeline is reproducible. Release admission, Authenticode, package SHA-256, internal manifest coverage, path safety, and exact-head CI requirements remain unchanged.

REMOTE_SAFE.
