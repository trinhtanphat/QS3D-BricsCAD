# V25 qualification profile sandbox contract

This runbook defines the repository-safe composition boundary for BricsCAD V25 qualification launchers that pass `/P <profile>`. It is source/tooling guidance only; it does not prove licensed BricsCAD runtime behavior or qualify any local result by itself.

## Scope

The shared helper is `scripts/v25-profile-sandbox.ps1`. It is V25-only and protects `HKCU\Software\Bricsys\BricsCAD\V25x64\en_US\Profiles`. V26 launchers must use a separate major-version contract rather than reusing this registry root.

The `SourceProfile` argument is a protected template. A runner must never delete or rewrite that pre-existing profile. The helper clones it into a collision-free runner-owned profile whose name begins with `QS3D-AUTO-`; only that proven-owned nonce may be deleted by cleanup.

## Composition

A V25 launcher that is migrated to this contract dot-sources the helper and allocates the sandbox before invoking any host-launching core:

```powershell
. ./scripts/v25-profile-sandbox.ps1

$sandbox = $null
try {
    Assert-Qs3dNoBricsCadProcess
    $sandbox = New-Qs3dV25ProfileSandbox -SourceProfile $Profile
    $effectiveProfile = $sandbox.NonceProfile

    # Invoke the runner core and pass only $effectiveProfile to /P.
}
finally {
    # Gracefully close the owned BricsCAD host, use bounded owned-process
    # fallback only when needed, then prove zero BricsCAD processes.
    if ($null -ne $sandbox) {
        Restore-Qs3dV25ProfileSandbox -Sandbox $sandbox
    }
}
```

The caller owns the process-lifetime boundary. Cleanup requires zero BricsCAD processes both when protected state is captured and when it is restored. If the helper cannot verify that boundary, cleanup fails closed.

## Protected-state ordering

`Restore-Qs3dV25ProfileSandbox` must restore the exact original `CurProfile` existence/type/value before it deletes the runner-owned nonce profile. This order is intentional: if pointer restoration fails, the nonce still exists; if nonce deletion fails, the original pointer has already been made safe. Final verification then requires the exact profile inventory and pointer state to match the pre-launch snapshot.

In short: restore `CurProfile` before deleting the nonce.

Do not perform legacy cleanup. A pre-existing `QS3D-AUTO-*` name is protected unless the current sandbox object proves that this runner created it after the snapshot.

## Evidence boundary

Repository-safe metadata may contain booleans and hashes such as zero-process status, pointer-restored status, inventory-restored status, nonce-removed status, inventory SHA-256 values, and whether bounded force-close fallback was used. It must not dump raw `CurProfile` values or the machine's complete profile-name inventory.

Cleanup success is cleanup-scoped (`CLEANUP_PASS`). It must never be interpreted as overall runtime PASS when the wrapped runtime core failed or its result will be rethrown.

## Later bounded migrations

Landing this helper around `scripts/test-bricscad-v25-runtime.ps1` does not make other `/P` launchers profile-safe. Each additional launcher must be migrated in a collision-checked bounded batch that preserves its existing runtime assertions while adding the same allocation/finally/zero-process/restore contract. Do not broaden a migration merely because another launcher uses the same `-Profile` parameter.

For every migrated launcher, keep the stable runtime logic in its core where practical and make the wrapper responsible for sandbox allocation, nonce substitution, owned-host shutdown, restoration, and cleanup evidence. Add or update deterministic source guards so future refactors cannot move `CurProfile` restoration after nonce deletion or silently bypass the wrapper.