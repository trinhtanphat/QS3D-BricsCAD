# V26 package construction held-generation binding

## Scope

Lane-Key: `issue-4570`.

This repository-safe package-integrity lane binds V26 package-construction inputs to the exact file generations admitted for semantic consumption. It covers source build artifacts, project/command text, generated-script inputs, synthetic samples, and staged managed plugin/Core identity reads in `scripts/package-v26.ps1`.

It does not dispatch or publish a release, sign artifacts, execute licensed BricsCAD, or claim `LOCAL_PASS`.

## Defect boundary

Ordinary-file and reparse checks prove filesystem topology at admission, but they do not prove a later pathname consumer read the same file generation. The previous package constructor admitted paths and then used operations such as `Get-Content`, `Copy-Item`, Authenticode, `AssemblyName.GetAssemblyName(path)`, and file-version APIs later by pathname. A same-path swap-and-restore can therefore make admission evidence describe different bytes from the bytes copied or interpreted.

This boundary is distinct from the downstream V26 release-package identity verifier: package construction must itself preserve the generation it admitted.

## Required contract

`scripts/package-v26.ps1` must preserve all existing containment/reparse/version/host-major/package checks and additionally:

1. open source files as read-only streams with `FileShare.Read` after ordinary non-reparse admission;
2. keep those handles alive while copying, reading text, or invoking pathname-based consumers, so write/delete/replace is denied for the consumption interval;
3. copy required build artifacts and synthetic samples from held source streams rather than `Copy-Item` pathname reopens;
4. read project and command-source text from bounded held streams rather than `Get-Content` pathname reopens;
5. keep each generated-script source input held while the existing V26 transformer consumes it;
6. hold staged plugin/Core files while Authenticode, AssemblyName, ProductVersion and cross-identity checks consume those paths;
7. reassert canonical path/length/write-time binding around pathname-only consumers and dispose all held handles in `finally`.

`FileShare.Read` intentionally allows concurrent readers but denies write/delete/replace of the admitted generation.

## Deterministic regression

Run:

```text
python scripts/preflight-v26-package-held-generations.py
python scripts/preflight-package-source-input-safety.py
```

The dedicated guard rejects legacy pathname copy/text reopen shapes and mutation-tests held-generation markers. The cross-guard must continue enforcing repository containment, ordinary/non-reparse admission, and source-scan safety while accepting only the stronger held-generation consumption boundary.

These checks are source/static evidence. They are not signing, publication, clean-machine installation, or licensed V26 runtime evidence.
