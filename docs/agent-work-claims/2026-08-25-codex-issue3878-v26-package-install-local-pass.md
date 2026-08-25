# Issue #3878 — V26 package-install lifecycle local pass

Status: `LOCAL_PASS` for the bounded V26 package install/uninstall lifecycle only

Parent: #1462

Original local carrier: #3792

Source correction: #3878

Prior failure evidence: PR #3879

Lane-Key: `issue-3878-v26-package-install-local-pass`

## Exact execution identity

- Tested source: exact clean `main@e90c6aba7ef7bf903042d42dd991f9e7112fe659`.
- Required post-#3878 source-ready carrier `5da966686826a350d8babc8f22a390ab29ec824b` is an ancestor of the tested main SHA.
- Pinned platform submodule: `external/QS3D-Platform@a5778f4abcf3b5c308c5d6854040dbc0c3082390`.
- Host: licensed BricsCAD V26.2.07 x64 with canonical initialized profile `V26x64/en_US`.
- .NET SDK: 8.0.424.
- Runner: unchanged `scripts/test-v26-package-install-lifecycle.ps1` with `-ConfirmDisposableInstall`.
- ProductVersion: `0.1.0-preview.10081`.
- Generated package SHA-256: `60F5239611B13F424BAE49922E5D34ADF3FC12C3064BF7506FE06CD27B8B3F7C`.

The execution used a new detached worktree at the exact tested main SHA. The worktree was clean including untracked files, the pinned submodule was initialized, no BricsCAD process was running, and the selected V26 profile had no pre-existing QS3D DemandLoad registration.

## Validation before execution

- `scripts/preflight-v26-package-install-lifecycle.py`: PASS.
- `scripts/preflight-local016-v26-package-source-ready-handoff.py`: PASS.
- Runner PowerShell AST parse: PASS.
- Exact HEAD and completely clean worktree checks: PASS.
- Canonical V26 host/profile identity and zero-process prerequisites: PASS.

## Licensed-local result

The V26 `Release|x64` build passed with `0 warnings / 0 errors`. Package generation produced the expected `BricsCAD V26 x64` / `net8.0-windows` identity, exact hash coverage and the required runtimeconfig. The production installer created only the selected disposable V26 registration, and the production uninstaller removed that registration and its owned files.

The sanitized runner JSON reported `status=PASS` with every bounded result true:

```text
buildSucceeded=true
packageIdentityValid=true
hashesValid=true
runtimeConfigPackaged=true
registrationCreated=true
registrationV26Only=true
registrationIdentityValid=true
installedPayloadValid=true
installedPayloadHashesMatch=true
runtimeConfigInstalled=true
uninstallRemovedRegistration=true
uninstallRemovedFiles=true
unrelatedV25RegistrationPreserved=true
unrelatedSentinelPreserved=true
cleanupComplete=true
```

The post-#3878 runtimeconfig validation accepted the actual .NET 8 `runtimeOptions.frameworks` array and required `Microsoft.WindowsDesktop.App`. No singular `runtimeOptions.framework` compatibility shortcut was used.

## Independent cleanup and isolation readback

After runner exit:

- BricsCAD process count was zero;
- selected V26 QS3D DemandLoad registration was absent;
- disposable qualification install directory was absent;
- qualification sentinel residue count was zero;
- unrelated V25 QS3D registration remained present with `LoadCtrls=2`;
- unrelated installed V25 loader SHA-256 remained `0D89D8D828BCE5CFC966EC2EF54358DC50E4FED560D5A908F94643AFA1D74E30`;
- exact test worktree remained Git-clean.

Raw build/package/install output and sanitized runner JSON remain under ignored `artifacts/`. No proprietary BricsCAD binary, local/private path, registry path, disposable identifier, user identity, signing secret, customer drawing, Handle or ProjectId is committed.

## Scope and disposition

This evidence promotes only the bounded package install/uninstall lifecycle described by `docs/LOCAL-V26-PACKAGE-INSTALL-LIFECYCLE.md` to `LOCAL_PASS`. The runner intentionally does not launch BricsCAD or perform NETLOAD, and the package remains an unsigned preview. It does not prove signed finalization, update/rollback, SECURELOAD/trust, clean customer-machine behavior, interactive native commands, UI/DPI, private-DWG acceptance or release publication.

LOCAL-016 and parent #1462 therefore remain `IN_PROGRESS` for their broader documented matrix. The current source-ready inbox guard still hard-codes `PENDING_LICENSED_V26`; this local evidence branch does not edit that remote-safe guard. A source/status lane must promote the canonical inbox/runbook truth using this exact evidence without broadening the result.
