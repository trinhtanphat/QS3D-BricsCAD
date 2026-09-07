# V25 package source-commit canonicality qualification

Scope: REMOTE_SAFE validation of `PACKAGE-METADATA.json` source provenance for the V25 release package. This lane does not sign, publish, dispatch a release, install BricsCAD, or claim licensed runtime PASS.

## Contract

`scripts/assert-v25-release-package-identity.ps1` must treat `gitCommit` as an exact metadata identity, not as user input to normalize. The decoded value must already be one raw 40-character hexadecimal Git SHA. Leading/trailing whitespace or any other normalization-dependent spelling must fail before expected-source comparison.

Hexadecimal letter case may differ from `ExpectedSourceCommit`; comparison may lowercase only after the raw metadata value has passed canonical 40-hex admission. The validator must preserve the existing unique top-level JSON-key admission, strict UTF-8 and bounded held-generation checks, exact product/target identity, ProductVersion/AssemblyVersion binding, and held V25 plugin/Core assembly identity.

## Deterministic regression cases

1. Canonical 40-hex `gitCommit` equal to the expected source SHA: admitted to the remaining package checks.
2. Same SHA with leading whitespace: reject.
3. Same SHA with trailing whitespace: reject.
4. Same SHA surrounded by whitespace: reject.
5. 39/41 characters or non-hex content: reject.
6. Uppercase hexadecimal spelling of the exact expected source SHA: allowed after canonical raw-value admission and compared case-insensitively.
7. Mutation that restores `([string]$metadata.gitCommit).Trim()` normalization: auto-discovered source guard must fail.
8. Mutation that removes the raw-value canonicality equality check: auto-discovered source guard must fail.

## Remote qualification

Run Shared CI on the exact canonical branch head and require both protected `preflight` and `core` to be terminal SUCCESS. `preflight` must keep aggregate feature guards, PowerShell syntax, Reservation/Lane-Key/path-collision validation, and V25 package-integrity checks green. `core` must retain deterministic smoke, trusted V25 compile-reference admission, and the locked V25 plugin build.

Before merge, refresh protected `main`. If it advanced, collision-scan and reconcile this same branch non-force, then obtain fresh exact-head checks. Merge only through the protected PR path with expected-head protection and verify the resulting protected-main merge contains the exact qualified candidate.
