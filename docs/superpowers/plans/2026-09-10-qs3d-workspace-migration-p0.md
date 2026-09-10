# QS3D Workspace C-to-D Migration P0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Relocate the canonical QS3D-BricsCAD repository and every C:-hosted linked worktree to D: without losing branch identity, dirty/untracked work, Git worktree administration, runtime compatibility paths, or rollback evidence.

**Architecture:** Use a copy-first, verify-before-cutover migration. Capture an immutable pre-migration ledger, copy to `D:\QS3D-Workspace`, repair linked-worktree administration only after byte-level/file-state evidence is available, create a full Git bundle, then replace verified legacy C: paths with NTFS junctions deepest-first while preserving already-D worktrees untouched. Any mismatch, locked required file, Git integrity error, or active mutation conflict is a fail-closed stop before deletion.

**Tech Stack:** Windows PowerShell 5.1+, Git for Windows, `robocopy`, NTFS junctions, SHA-256, WMI/CIM process inspection.

**Spec:** `docs/superpowers/specs/2026-09-10-blt3d-full-parity-design.md`

## Global Constraints

- Canonical destination: `D:\QS3D-Workspace\QS3D-BricsCAD`.
- External C: worktrees map under `D:\QS3D-Workspace\worktrees\Documents` or `D:\QS3D-Workspace\worktrees\Temp`.
- Baseline observed worktree count is 101; execution must re-read the live count and use that live value as the comparison baseline.
- Never run `git reset --hard`, `git clean`, forced checkout, destructive stash, or any command that discards dirty/untracked state.
- Preserve already-D linked worktrees, including `D:\Projects\QS3D-BricsCAD\artifacts\worktrees\c02-6298`, without rewriting their working files.
- Never kill an unrelated owner/agent/CAD process merely to obtain a file lock.
- A required file that cannot be copied is a migration blocker, not an ignorable warning. The known example is `artifacts\locked-evidence\issue1462-v26-20260822-artifacts\v26-netload-16152.dmp`.
- Do not remove the deliberate safety interlock directory `D:\QS3D-Workspace\all-refs-backup.bundle` until all copy/worktree/status verification passes.
- Do not delete old C: content until the real `all-refs-backup.bundle` exists and passes `git bundle verify`.
- Legacy C: paths remain usable after cutover through NTFS junctions to D:.
- If any cutover path is actively mutating and cannot be renamed safely, stop that path's cutover and leave both verified copies intact.

---

### Task 1: Capture a live migration ledger

**Files:**
- Create outside repo: `D:\QS3D-Workspace\evidence\p0-before.json`
- Create outside repo: `D:\QS3D-Workspace\evidence\p0-processes-before.txt`
- Read: `D:\QS3D-Workspace-Migrate.ps1`

**Interfaces:**
- Consumes: live canonical repo `C:\Users\Admin\Documents\QS3D-BricsCAD` and `git worktree list --porcelain`.
- Produces: a JSON ledger containing `SourcePath`, `DestinationPath`, `Head`, `Branch`, and a base64-encoded porcelain status snapshot for every worktree.

- [ ] **Step 1: Create the evidence directory and capture active process command lines**

```powershell
$Evidence = 'D:\QS3D-Workspace\evidence'
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
Get-CimInstance Win32_Process |
  Where-Object { $_.CommandLine -like '*QS3D*' } |
  Select-Object ProcessId, Name, CommandLine |
  Format-List | Out-File "$Evidence\p0-processes-before.txt" -Encoding utf8
```

Expected: file exists and records processes currently referencing QS3D paths; no process is terminated.

- [ ] **Step 2: Capture every worktree's identity and dirty/untracked status**

```powershell
$Root = 'C:\Users\Admin\Documents\QS3D-BricsCAD'
$raw = git -C $Root worktree list --porcelain
$blocks = ($raw -join "`n") -split "`n`n" | Where-Object { $_.Trim() }
$ledger = foreach ($block in $blocks) {
  $path = ([regex]::Match($block, '(?m)^worktree (.+)$')).Groups[1].Value.Trim()
  $head = ([regex]::Match($block, '(?m)^HEAD ([0-9a-f]+)$')).Groups[1].Value
  $branchRef = ([regex]::Match($block, '(?m)^branch refs/heads/(.+)$')).Groups[1].Value
  $statusBytes = [Text.Encoding]::UTF8.GetBytes(((git -C $path status --porcelain=v1 --untracked-files=all) -join "`n"))
  $dest = if ($path.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase)) {
    'D:\QS3D-Workspace\QS3D-BricsCAD' + $path.Substring($Root.Length)
  } elseif ($path.StartsWith('C:\Users\Admin\Documents\', [StringComparison]::OrdinalIgnoreCase)) {
    'D:\QS3D-Workspace\worktrees\Documents\' + $path.Substring('C:\Users\Admin\Documents\'.Length)
  } elseif ($path.StartsWith('C:\Temp\', [StringComparison]::OrdinalIgnoreCase)) {
    'D:\QS3D-Workspace\worktrees\Temp\' + $path.Substring('C:\Temp\'.Length)
  } else { $path }
  [pscustomobject]@{ SourcePath=$path; DestinationPath=$dest; Head=$head; Branch=$branchRef; StatusBase64=[Convert]::ToBase64String($statusBytes) }
}
$ledger | ConvertTo-Json -Depth 4 | Set-Content "$Evidence\p0-before.json" -Encoding utf8
"WORKTREE_COUNT=$($ledger.Count)"
```

Expected: count equals the current live `git worktree list` count. Do not hard-fail merely because the live count has changed from the earlier observation of 101; the live captured count is authoritative for this execution.

- [ ] **Step 3: Assert known dirty/untracked work remains represented**

```powershell
$ledger | Where-Object {
  $_.SourcePath -match 'issue72-current-main-20260821|QS3D-C02-6290|QS3D-C02-6301|c02-6298'
} | Format-Table SourcePath,Head,Branch,StatusBase64 -AutoSize
```

Expected: the relevant entries are visible. If a previously dirty path is now clean, inspect why before proceeding; do not assume another agent intended the change.

- [ ] **Step 4: Commit no repository changes for this task**

This task creates only migration evidence on D:. Record completion in the execution log rather than making a Git commit.

---

### Task 2: Complete the copy without ignoring locked required files

**Files:**
- Modify outside repo: `D:\QS3D-Workspace\migration.log`
- Populate: `D:\QS3D-Workspace\QS3D-BricsCAD\...`
- Populate: `D:\QS3D-Workspace\worktrees\...`

**Interfaces:**
- Consumes: Task 1 ledger.
- Produces: D: copies of every C:-hosted worktree while already-D worktrees remain in place.

- [ ] **Step 1: Check whether the earlier migration process is still running before starting another copy**

```powershell
Get-CimInstance Win32_Process |
  Where-Object { $_.CommandLine -like '*QS3D-Workspace-Migrate.ps1*' } |
  Select-Object ProcessId,Name,CommandLine
```

Expected: either one known migration process exists and should be observed to completion, or none exists. Never start a second destructive/copy phase concurrently with the first.

- [ ] **Step 2: Run/re-run copy-only Robocopy for the canonical tree**

```powershell
$src='C:\Users\Admin\Documents\QS3D-BricsCAD'
$dst='D:\QS3D-Workspace\QS3D-BricsCAD'
robocopy $src $dst /E /COPY:DAT /DCOPY:DAT /XJ /R:1 /W:1 /MT:8 /NFL /NDL /NP /LOG+:D:\QS3D-Workspace\migration.log
$rc=$LASTEXITCODE
if ($rc -ge 8) { throw "Canonical robocopy failed with exit code $rc" }
```

Expected: exit code 0-7. Exit code 8+ stops the task.

- [ ] **Step 3: If the known crash dump remains locked, prove it explicitly and stop rather than excluding it**

```powershell
$locked='C:\Users\Admin\Documents\QS3D-BricsCAD\artifacts\locked-evidence\issue1462-v26-20260822-artifacts\v26-netload-16152.dmp'
$lockedDest='D:\QS3D-Workspace\QS3D-BricsCAD\artifacts\locked-evidence\issue1462-v26-20260822-artifacts\v26-netload-16152.dmp'
try {
  Copy-Item -LiteralPath $locked -Destination $lockedDest -Force -ErrorAction Stop
} catch {
  throw "Required evidence file is still locked; leave C: intact and retry after its owning process releases the handle. $($_.Exception.Message)"
}
```

Expected: successful copy. Do not terminate a CAD/agent process to force success.

- [ ] **Step 4: Copy each external C: worktree from the ledger**

```powershell
$items = Get-Content "$Evidence\p0-before.json" -Raw | ConvertFrom-Json
foreach ($item in $items) {
  if ($item.SourcePath -like 'C:*' -and -not $item.SourcePath.StartsWith($Root,[StringComparison]::OrdinalIgnoreCase)) {
    New-Item -ItemType Directory -Force -Path $item.DestinationPath | Out-Null
    robocopy $item.SourcePath $item.DestinationPath /E /COPY:DAT /DCOPY:DAT /XJ /R:1 /W:1 /MT:8 /NFL /NDL /NP /LOG+:D:\QS3D-Workspace\migration.log
    if ($LASTEXITCODE -ge 8) { throw "Robocopy failed: $($item.SourcePath) -> $($item.DestinationPath), rc=$LASTEXITCODE" }
  }
}
```

Expected: every C:-hosted external worktree has a D: copy; already-D paths are skipped.

---

### Task 3: Repair Git worktree administration and verify identities

**Files:**
- Git admin data under: `D:\QS3D-Workspace\QS3D-BricsCAD\.git\worktrees\...`
- Create outside repo: `D:\QS3D-Workspace\evidence\p0-after-repair.json`

**Interfaces:**
- Consumes: fully copied D: trees and Task 1 ledger.
- Produces: valid linked worktrees whose Git administrative pointers resolve on D:.

- [ ] **Step 1: Repair all relocated linked worktrees from the D: canonical repository**

```powershell
$DMain='D:\QS3D-Workspace\QS3D-BricsCAD'
$items = Get-Content "$Evidence\p0-before.json" -Raw | ConvertFrom-Json
$relocated = @($items | Where-Object { $_.SourcePath -like 'C:*' } | ForEach-Object { $_.DestinationPath })
git -C $DMain worktree repair @relocated
if ($LASTEXITCODE -ne 0) { throw 'git worktree repair failed' }
```

Expected: exit 0.

- [ ] **Step 2: Verify each worktree HEAD, branch, and dirty/untracked status exactly matches the ledger**

```powershell
foreach ($item in $items) {
  $path = if ($item.SourcePath -like 'C:*') { $item.DestinationPath } else { $item.SourcePath }
  $actualHead=(git -C $path rev-parse HEAD).Trim()
  $actualBranch=(git -C $path branch --show-current).Trim()
  $actualStatus=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(((git -C $path status --porcelain=v1 --untracked-files=all) -join "`n")))
  if ($actualHead -ne $item.Head) { throw "HEAD mismatch: $path" }
  if ($actualBranch -ne $item.Branch) { throw "Branch mismatch: $path expected=$($item.Branch) actual=$actualBranch" }
  if ($actualStatus -ne $item.StatusBase64) { throw "Working-tree status mismatch: $path" }
}
```

Expected: no mismatch.

- [ ] **Step 3: Verify worktree count and eliminate C: admin references**

```powershell
$after = git -C $DMain worktree list --porcelain
$afterCount = (($after -join "`n") -split "`n`n" | Where-Object { $_.Trim() }).Count
if ($afterCount -ne $items.Count) { throw "Worktree count mismatch before=$($items.Count) after=$afterCount" }
if (($after -join "`n") -match '(?im)^worktree C:\\') { throw 'Git worktree administration still references C:' }
```

Expected: identical count and no `worktree C:\...` entries.

---

### Task 4: Prove repository object integrity and create rollback bundle

**Files:**
- Create outside repo: `D:\QS3D-Workspace\refs-after.txt`
- Replace safety interlock directory with file: `D:\QS3D-Workspace\all-refs-backup.bundle`
- Create outside repo: `D:\QS3D-Workspace\evidence\git-fsck.txt`

**Interfaces:**
- Consumes: repaired D: canonical repo.
- Produces: integrity evidence and an independently verifiable all-refs bundle before any C: deletion.

- [ ] **Step 1: Run full Git object verification**

```powershell
git -C $DMain fsck --full --no-dangling 2>&1 | Tee-Object "$Evidence\git-fsck.txt"
if ($LASTEXITCODE -ne 0) { throw 'git fsck failed; do not cut over' }
```

Expected: exit 0.

- [ ] **Step 2: Persist all refs and remove only the deliberate bundle interlock directory**

```powershell
git -C $DMain show-ref | Set-Content 'D:\QS3D-Workspace\refs-after.txt' -Encoding ascii
$bundle='D:\QS3D-Workspace\all-refs-backup.bundle'
if (Test-Path $bundle -PathType Container) { Remove-Item -LiteralPath $bundle -Recurse -Force }
if (Test-Path $bundle) { throw 'Bundle path is occupied by an unexpected file' }
```

Expected: refs file exists and bundle path is free. This is the first point at which the deliberate interlock may be removed.

- [ ] **Step 3: Create and verify the all-refs Git bundle**

```powershell
git -C $DMain bundle create $bundle --all
if ($LASTEXITCODE -ne 0) { throw 'git bundle create failed' }
git -C $DMain bundle verify $bundle
if ($LASTEXITCODE -ne 0) { throw 'git bundle verify failed' }
```

Expected: both commands exit 0.

---

### Task 5: Cut over legacy C: paths with junctions, deepest-first

**Files:**
- Replace verified C: repository/worktree directories with NTFS junctions to their D: destinations.
- Create outside repo: `D:\QS3D-Workspace\evidence\p0-cutover.txt`

**Interfaces:**
- Consumes: verified D: copies and valid rollback bundle.
- Produces: C: compatibility paths that resolve to D: without retaining duplicate source data on C:.

- [ ] **Step 1: Re-scan active QS3D process command lines immediately before cutover**

```powershell
$active = Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match 'C:\\.*QS3D' }
$active | Select-Object ProcessId,Name,CommandLine | Format-List | Out-File "$Evidence\p0-processes-cutover.txt" -Encoding utf8
```

Expected: evidence captured. Active readers are not killed. If a process is actively mutating a specific worktree, defer that path until its operation reaches a safe boundary.

- [ ] **Step 2: Cut over external C: worktrees deepest-first using rename-before-junction**

```powershell
$cItems = @($items | Where-Object { $_.SourcePath -like 'C:*' -and -not $_.SourcePath.Equals($Root,[StringComparison]::OrdinalIgnoreCase) } | Sort-Object { $_.SourcePath.Length } -Descending)
foreach ($item in $cItems) {
  $src=$item.SourcePath; $dst=$item.DestinationPath; $quarantine="$src.__qs3d_migrated__"
  if (Test-Path $quarantine) { throw "Existing quarantine path blocks cutover: $quarantine" }
  Rename-Item -LiteralPath $src -NewName ([IO.Path]::GetFileName($quarantine)) -ErrorAction Stop
  New-Item -ItemType Junction -Path $src -Target $dst -ErrorAction Stop | Out-Null
  $junctionHead=(git -C $src rev-parse HEAD).Trim()
  if ($junctionHead -ne $item.Head) { throw "Junction verification failed: $src" }
  Remove-Item -LiteralPath $quarantine -Recurse -Force -ErrorAction Stop
}
```

Expected: each legacy path is a junction and resolves to the exact expected HEAD. If rename/remove is blocked, stop and leave the quarantine/copy intact; do not force-close unrelated processes.

- [ ] **Step 3: Cut over the canonical root last**

```powershell
$rootItem=$items | Where-Object { $_.SourcePath.Equals($Root,[StringComparison]::OrdinalIgnoreCase) }
$rootQuarantine="$Root.__qs3d_migrated__"
Rename-Item -LiteralPath $Root -NewName ([IO.Path]::GetFileName($rootQuarantine)) -ErrorAction Stop
New-Item -ItemType Junction -Path $Root -Target $rootItem.DestinationPath -ErrorAction Stop | Out-Null
if ((git -C $Root rev-parse HEAD).Trim() -ne $rootItem.Head) { throw 'Canonical C: junction resolves to wrong HEAD' }
Remove-Item -LiteralPath $rootQuarantine -Recurse -Force -ErrorAction Stop
```

Expected: old canonical C: location is now a junction to D:.

---

### Task 6: Final migration verification and evidence closure

**Files:**
- Create outside repo: `D:\QS3D-Workspace\evidence\p0-final.txt`

**Interfaces:**
- Consumes: D: canonical repo, legacy C: junctions, Task 1 ledger.
- Produces: final proof that source storage moved to D: and all identities remain valid.

- [ ] **Step 1: Verify all D: and legacy compatibility paths**

```powershell
$lines = New-Object System.Collections.Generic.List[string]
foreach ($item in $items) {
  $primary = if ($item.SourcePath -like 'C:*') { $item.DestinationPath } else { $item.SourcePath }
  $head=(git -C $primary rev-parse HEAD).Trim()
  $branch=(git -C $primary branch --show-current).Trim()
  if ($head -ne $item.Head -or $branch -ne $item.Branch) { throw "Final identity mismatch: $primary" }
  if ($item.SourcePath -like 'C:*') {
    $attr=(Get-Item -LiteralPath $item.SourcePath -Force).Attributes
    if (($attr -band [IO.FileAttributes]::ReparsePoint) -eq 0) { throw "Legacy path is not a reparse point: $($item.SourcePath)" }
    if ((git -C $item.SourcePath rev-parse HEAD).Trim() -ne $item.Head) { throw "Legacy junction HEAD mismatch: $($item.SourcePath)" }
  }
  $lines.Add("PASS`t$($item.Head)`t$($item.Branch)`t$primary")
}
$lines | Set-Content "$Evidence\p0-final.txt" -Encoding utf8
```

Expected: every ledger entry reports PASS.

- [ ] **Step 2: Re-run Git fsck and bundle verification after cutover**

```powershell
git -C $DMain fsck --full --no-dangling
if ($LASTEXITCODE -ne 0) { throw 'Post-cutover git fsck failed' }
git -C $DMain bundle verify 'D:\QS3D-Workspace\all-refs-backup.bundle'
if ($LASTEXITCODE -ne 0) { throw 'Post-cutover bundle verification failed' }
```

Expected: both exit 0.

- [ ] **Step 3: Record disk-space change without treating a specific number as correctness**

```powershell
Get-PSDrive C,D,E | Select-Object Name,@{N='FreeGB';E={[math]::Round($_.Free/1GB,2)}},@{N='UsedGB';E={[math]::Round($_.Used/1GB,2)}} |
  Format-Table -AutoSize | Out-File "$Evidence\p0-disk-after.txt" -Encoding utf8
```

Expected: C: free space should normally increase materially, but functional verification above is authoritative.

- [ ] **Step 4: Completion criterion**

Mark P0 complete only when: every required file exists on D:, all worktree HEAD/branch/status snapshots match, Git has no C: worktree admin refs, the all-refs bundle verifies, all legacy C: source paths are junctions to D:, and no dirty/untracked working state was lost.