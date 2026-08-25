# Issue #3878 — V26 package-install lifecycle local failure

Status: `LOCAL_FAIL / SOURCE_FIX_REQUIRED`

Parent: #1462

Bounded carrier: #3792

Predecessor source fix: #3873 / PR #3877

Source follow-up: #3878

Lane-Key: `issue-3878`

## Exact execution identity

- Tested source: exact clean `main@6808636f4f6809e44d6c6fcd1f0c73121e1b5dd3`.
- Pinned platform submodule: `external/QS3D-Platform@a5778f4abcf3b5c308c5d6854040dbc0c3082390`.
- Host: licensed BricsCAD V26.2.07 x64 with canonical initialized profile `V26x64/en_US`.
- .NET SDK: 8.0.424.
- Runner: `scripts/test-v26-package-install-lifecycle.ps1` with the production installer/uninstaller and `-ConfirmDisposableInstall`.
- ProductVersion: `0.1.0-preview.10081`.
- Generated package SHA-256: `72E0BB0C6B8584244405E85FC2E65CAC56EEC9475E6BA3B127B935A0344377CC`.

The worktree was detached at the exact tested main SHA, clean including untracked files, and contained the pinned initialized submodule. No BricsCAD process or selected V26 QS3D DemandLoad registration existed before execution.

## Passed stages

- Focused package-install source preflight: PASS.
- Runner PowerShell AST parse: PASS.
- V26 `Release|x64` build: PASS with `0 warnings / 0 errors`.
- Package target/framework identity: PASS.
- Package hash coverage: PASS.
- Packaged runtimeconfig coverage: PASS.
- Real disposable production install: completed.
- Selected DemandLoad registration created: PASS.
- Registration restricted to V26 and bound to the disposable payload: PASS.

The sanitized runner booleans reached:

```text
buildSucceeded=true
packageIdentityValid=true
hashesValid=true
runtimeConfigPackaged=true
registrationCreated=true
registrationV26Only=true
registrationIdentityValid=true
cleanupComplete=true
```

## Exact failure boundary

The generated .NET 8 runtimeconfig has these bounded properties:

```text
runtimeOptions.tfm=net8.0
runtimeOptions.frameworks[0].name=Microsoft.NETCore.App
runtimeOptions.frameworks[1].name=Microsoft.WindowsDesktop.App
```

After the production installer completed, the runner evaluated `runtimeOptions.framework.name` under `Set-StrictMode` and failed because the singular `framework` property does not exist. The sanitized failure was:

```text
The property 'framework' cannot be found on this object.
```

The runner stopped before it could publish installed-payload, runtimeconfig-installed, production-uninstaller and unrelated-state-preservation booleans. Those false result fields therefore are not product assertions and must not be reinterpreted as separate failures. This run does not qualify the bounded package-install cell as `LOCAL_PASS`.

## Cleanup and isolation

Final cleanup was independently rechecked after runner exit:

- selected V26 QS3D DemandLoad registration absent;
- owned disposable qualification install directory absent;
- qualification sentinel residue count zero;
- BricsCAD process count zero;
- unrelated V25 QS3D registration still present with `LoadCtrls=2`;
- unrelated installed V25 loader SHA-256 unchanged at `0D89D8D828BCE5CFC966EC2EF54358DC50E4FED560D5A908F94643AFA1D74E30`;
- exact test worktree remained Git-clean.

Raw build/package/install output and the sanitized runner JSON remain under ignored `artifacts/`; no proprietary BricsCAD binary, private path, registry path, user identity, signing secret, customer drawing, Handle or ProjectId is committed.

## Disposition

Issue #3878 owns the remote/source correction. The assertion must validate the actual .NET 8 runtimeconfig framework set and require `Microsoft.WindowsDesktop.App` while remaining fail-closed for missing or malformed data. Package ownership, hash, cleanup and cross-major guards must remain intact.

The local worker made no production or runner source edit. Resume this exact package-install lifecycle only after the source fix is merged and one exact clean main descendant is published. Broader NETLOAD/DemandLoad runtime, signed finalization, clean update/rollback, SECURELOAD, native workflow, private-DWG and release acceptance remain pending under #1462.
