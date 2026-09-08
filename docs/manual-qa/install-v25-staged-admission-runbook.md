# V25 installer staged-payload admission qualification

## Scope

This carrier hardens the standalone V25 DemandLoad installer against package-source replacement between source admission and the atomic staging commit. It is a REMOTE_SAFE source/CI qualification only; it does not claim a licensed BricsCAD runtime install PASS, production signing, or LOCAL_PASS.

## Security contract

1. `SHA256SUMS.txt` is opened as one held read generation with write/delete sharing denied. Its SHA-256 and parsed manifest lines come from that same held stream.
2. Source package admission retains the validated payload hash snapshot and the admitted `COMMANDS.txt` command snapshot.
3. The installer copies the fixed standalone payload into a fresh stage, then validates staged bytes before any existing installation is moved aside and before the stage is committed.
4. Staged admission requires an ordinary non-reparse stage root and exactly eight ordinary top-level files. Directories, nested entries, reparse points, duplicate/case-colliding paths, unexpected files, and missing files fail closed.
5. Every staged file, including `SHA256SUMS.txt`, must match its exact admitted SHA-256 snapshot.
6. Staged managed DLL/package metadata identity is revalidated. When signatures are required or an expected signer is supplied, staged executable payloads are Authenticode-validated after copy/unblock and before commit.
7. Staged `COMMANDS.txt` must reproduce the admitted command snapshot before DemandLoad registration can use it.
8. Existing install backup/rollback and per-user update mutex behavior remain unchanged. No test/gate is disabled and no fail-open fallback is introduced.

## Deterministic regression

Run:

```text
python scripts/preflight-install-v25-staged-admission.py
```

The guard is auto-discovered by the aggregate feature-source preflight. It verifies source-admission retention, held-manifest generation binding, staged hash/signature/identity rebinding, ordinary stage-root/entry handling, ordering after copy and before atomic stage commit, and mutation sensitivity for those markers.

## Hosted acceptance

The canonical PR may merge only after reconciliation with current protected `main` and fresh exact-head required CI is terminal GREEN. Required source validation includes aggregate feature guards and tracked PowerShell syntax. Any aggregate failure must be attributed to a concrete child guard before changing production or expectations.

## Runtime follow-up

A real signed release package can later be exercised on a licensed Windows/BricsCAD V25 machine with BricsCAD closed. Confirm successful DemandLoad registration, correct installed DLL/package identity, rollback behavior under injected stage validation failure, and no security-policy weakening. Until such a run is captured against the exact released artifact, runtime status remains NO_RESULT/LOCAL_ONLY.