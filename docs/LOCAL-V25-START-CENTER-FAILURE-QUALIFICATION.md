# LOCAL V25 — Start Center failure redaction qualification

Status: `LOCAL_ONLY / SOURCE_READY / NO_RESULT`

This runbook qualifies the native BricsCAD V25 behavior for Issue #5105. Remote/static CI must never be reported as `LOCAL_PASS`.

## Candidate binding

Before execution, record the exact fetched commit SHA, built V25 DLL SHA-256, BricsCAD V25 version, Windows version, and disposable DWG identity. Build and load only that exact candidate. If the source SHA changes, restart the matrix on the new candidate.

## Safety boundary

Use a disposable drawing/profile. Do not use customer/private DWGs. Do not alter signing, licensing, release, or global BricsCAD settings. Capture only sanitized command/editor observations. A native host exception may be intentionally induced only through an existing supported test/probe path; do not corrupt BricsCAD installation state or patch proprietary binaries.

## Matrix

| Row | Scenario | Required result |
| --- | --- | --- |
| SC01 | NETLOAD exact V25 plugin | Plugin loads; no startup exception escapes. |
| SC02 | `QS3DSTART` normal open | Native embedded Start Center PaletteSet opens and remains dockable. |
| SC03 | Hide and reopen | Palette hides/reopens without duplicate visible host or stranded callback symptoms. |
| SC04 | Switch between two disposable DWGs while visible | Start Center refresh follows document activation and remains responsive. |
| SC05 | Close active disposable DWG while visible | No exception escapes document activation/close lifecycle. |
| SC06 | Supported command-show failure injection/probe | Editor may show exactly `QS3DSTART could not open the Start Center.`; raw exception message/type/path/stack text is absent. |
| SC07 | Supported activation-refresh failure injection/probe | Activated document editor may show exactly `QS3DSTART refresh could not update the Start Center.`; raw exception message/type/path/stack text is absent. |
| SC08 | Repeat SC06 twice | Failure remains contained and a later successful `QS3DSTART` can open the same Start Center normally. |
| SC09 | Repeat SC07 across two document activations | Callback remains contained; no duplicate warning burst attributable to duplicate subscription. |
| SC10 | Hide after a successful recovery | Hidden Start Center produces no refresh warning on subsequent document activation. |
| SC11 | Save/cold reopen disposable DWG then open Start Center | Normal Start Center behavior remains intact after reopen. |
| SC12 | Exit BricsCAD after visible + hidden cycles | Host exits cleanly with no QS3D-owned UI/process residue observable after shutdown. |

Rows SC06/SC07 are `NO_RESULT` if the current local harness has no safe supported failure-injection path. Do not fabricate a failure by damaging the installation or private state. In that case record the missing probe capability precisely; all other rows may still be reported individually.

## PASS contract

`LOCAL_PASS` for this matrix requires SC01–SC12 to have real executed PASS evidence on one exact candidate. If SC06/SC07 cannot be safely executed, the overall verdict remains `NO_RESULT / PARTIAL`, even when normal-path rows pass. Source/static guards and hosted V25 compilation are `REMOTE_PASS` evidence only.

## Failure handoff

For a reproducible product defect, record only sanitized scenario, exact candidate identity, BricsCAD V25 version, expected/actual behavior, and whether the failure escaped the command/callback boundary. Mark `SOURCE_FIX_REQUIRED`; do not patch production source in the local evidence run.
