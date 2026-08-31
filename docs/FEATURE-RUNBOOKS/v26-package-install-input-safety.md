# V26 package install lifecycle input safety

Lane-Key: `issue-4428`

## Scope

This repository-safe package hardens `scripts/test-v26-package-install-lifecycle.ps1`, the source-ready V26 disposable install/uninstall qualification runner. It does not execute licensed BricsCAD in hosted CI, sign packages, publish releases, or claim `LOCAL_PASS`.

## Defect boundary

The runner previously accepted generated package paths and manifest identities, but later reopened package metadata, manifest payloads, ZIP content, installer/uninstaller scripts, and installed payloads with direct `Get-Content`, `Get-FileHash`, or recursive filesystem enumeration. That left two trust gaps:

1. a same-path replacement could change bytes after admission/verification and before a later consumer reopened the path;
2. recursive enumeration could descend through a reparse-backed directory before a leaf-level check observed the escape.

## Required contract

For package metadata, `SHA256SUMS.txt`, package ZIP, every package payload, installer/uninstaller scripts, and installed payloads:

1. resolve only ordinary files/directories with no reparse-backed path component;
2. capture streaming SHA-256, length, and UTC last-write ticks;
3. re-resolve and re-hash before publishing a stable file state;
4. perform bounded strict-UTF8 reads from the admitted file state;
5. revalidate the full state after reads and before/after installer or uninstaller consumption;
6. enumerate package/install trees with an explicit stack and reject reparse entries before descent;
7. refuse recursive cleanup if the disposable install root itself becomes reparse-backed.

The existing V26-only registry isolation, exact manifest coverage, runtimeconfig identity, installed hash parity, V25 registration preservation, unrelated sentinel preservation, and cleanup truth remain authoritative.

## Deterministic source guard

Run:

```text
python scripts/preflight-v26-package-install-input-safety.py
```

The guard is auto-discovered by aggregate preflight. It pins stable generation capture/recheck, strict UTF-8 reads, explicit reparse-safe traversal, installer/uninstaller generation binding, installed payload binding, forbidden direct path-reopening hashes/reads, and mutation probes that must fail if those boundaries are weakened.

## Validation boundary

Hosted validation may prove source/static/PowerShell/build readiness. Actual disposable V26 package installation remains a local licensed-host qualification operation and retains its exact-SHA, zero-BricsCAD-process, V26-only registry and cleanup requirements. Hosted success is never `LOCAL_PASS`.
