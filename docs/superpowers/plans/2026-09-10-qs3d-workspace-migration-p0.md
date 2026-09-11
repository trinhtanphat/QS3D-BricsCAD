# QS3D Workspace C-to-D Migration P0 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Relocate the canonical QS3D-BricsCAD repository and every C:-hosted linked worktree to D: without losing branch identity, dirty/untracked work, Git worktree administration, compatibility paths, or rollback evidence.

**Architecture:** Copy first, verify exact Git/worktree state, repair worktree administration, create a verified all-refs bundle, then cut over only verified legacy C: paths with NTFS junctions. Nested worktrees below the canonical root are copied with the root and inherit the root junction; only C: worktrees outside the root receive their own junctions. Any mismatch, required locked file, active mutation conflict, or Git integrity error stops before deletion.

**Tech Stack:** Windows PowerShell 5.1+, Git for Windows, `robocopy`, NTFS junctions, WMI/CIM process inspection.

**Spec:** `docs/superpowers/specs/2026-09-10-blt3d-full-parity-design.md`

## Global Constraints

- Canonical source: `C:\Users\Admin\Documents\QS3D-BricsCAD`.
- Canonical destination: `D:\QS3D-Workspace\QS3D-BricsCAD`.
- External C: worktrees map to `D:\QS3D-Workspace\worktrees\Documents\...` or `D:\QS3D-Workspace\worktrees\Temp\...`.
- Earlier observation found 101 worktrees; execution must capture and use the live count instead of assuming 101 remains current.
- Never run `git reset --hard`, `git clean`, destructive stash, forced checkout, or any discard operation.
- Preserve already-D worktrees untouched, especially `D:\Projects\QS3D-BricsCAD\artifacts\worktrees\c02-6298` and its dirty files.
- Never kill an unrelated CAD/agent/owner process merely to obtain a lock.
- A required file that cannot be copied is a blocker. Known example: `artifacts\locked-evidence\issue1462-v26-20260822-artifacts\v26-netload-16152.dmp`.
- Keep the deliberate directory interlock `D:\QS3D-Workspace\all-refs-backup.bundle` until copy/state/integrity verification passes.
- Do not delete C: data until a real `all-refs-backup.bundle` exists and `git bundle verify` passes.
- Do not cut over a worktree while another process is actively mutating it.
- C: compatibility paths must work after migration via junctions to D:.

---

### Task 1: Capture a live immutable migration ledger

**Files:**
- Create outside repo: `D:\QS3D-Workspace\evidence\p0-before.json`
- Create outside repo: `D:\QS3D-Workspace\evidence\p0-processes-before.txt`

**Interfaces:**
- Consumes: live `git worktree list --porcelain` and each worktree's `HEAD`, branch, and porcelain status.
- Produces: the comparison baseline for every later verification step.

- [ ] **Step 1: Capture process evidence without terminating anything**

```powershell
$Root='C:\Users\Admin\Documents\QS3D-BricsCAD'
$DMain='D:\QS3D-Workspace\QS3D-BricsCAD'
$Evidence='D:\QS3D-Workspace\evidence'
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
Get-CimInstance Win32_Process |
  Where-Object { $_.CommandLine -like '*QS3D*' } |
  Select-Object ProcessId,Name,CommandLine |
  Format-List | Out-File "$Evidence\p0-processes-before.txt" -Encoding utf8
```

Expected: evidence file exists; no process is killed.

- [ ] **Step 2: Capture every worktree identity and dirty/untracked state**

```powershell
$raw=git -C $Root worktree list --porcelain
$blocks=(($raw -join "`n") -split "`n`n") | Where-Object { $_.Trim() }
$ledger=foreach($block in $blocks){
  $path=([regex]::Match($block,'(?m)^worktree (.+)$')).Groups[1].Value.Trim()
  $head=([regex]::Match($block,'(?m)^HEAD ([0-9a-f]+)$')).Groups[1].Value
  $branch=([regex]::Match($block,'(?m)^branch refs/heads/(.+)$')).Groups[1].Value
  $status=((git -C $path status --porcelain=v1 --untracked-files=all) -join "`n")
  $dest=if($path.StartsWith($Root,[StringComparison]::OrdinalIgnoreCase)){
    $DMain+$path.Substring($Root.Length)
  }elseif($path.StartsWith('C:\Users\Admin\Documents\',[StringComparison]::OrdinalIgnoreCase)){
    'D:\QS3D-Workspace\worktrees\Documents\'+$path.Substring('C:\Users\Admin\Documents\'.Length)
  }elseif($path.StartsWith('C:\Temp\',[StringComparison]::OrdinalIgnoreCase)){
    'D:\QS3D-Workspace\worktrees\Temp\'+$path.Substring('C:\Temp\'.Length)
  }else{$path}
  [pscustomobject]@{SourcePath=$path;DestinationPath=$dest;Head=$head;Branch=$branch;StatusBase64=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($status))}
}
$ledger | ConvertTo-Json -Depth 4 | Set-Content "$Evidence\p0-before.json" -Encoding utf8
"WORKTREE_COUNT=$($ledger.Count)"
```

Expected: live count is nonzero and becomes the authoritative baseline.

- [ ] **Step 3: Confirm known dirty/untracked lanes are represented**

```powershell
$ledger | Where-Object { $_.SourcePath -match 'issue72-current-main-20260821|QS3D-C02-6290|QS3D-C02-6301|c02-6298' } |
  Format-Table SourcePath,Head,Branch,StatusBase64 -AutoSize
```

Expected: relevant entries are visible. If a previously dirty path changed, inspect the live state before proceeding.

---

### Task 2: Finish copy-only migration with fail-closed locked-file handling

**Files:**
- Populate: `D:\QS3D-Workspace\QS3D-BricsCAD\...`
- Populate external copies: `D:\QS3D-Workspace\worktrees\...`
- Append: `D:\QS3D-Workspace\migration.log`

**Interfaces:**
- Consumes: Task 1 ledger.
- Produces: complete D: copies while C: remains authoritative and intact.

- [ ] **Step 1: Ensure only one migration copier is active**

```powershell
Get-CimInstance Win32_Process |
  Where-Object { $_.CommandLine -like '*QS3D-Workspace-Migrate.ps1*' } |
  Select-Object ProcessId,Name,CommandLine
```

Expected: observe one existing migration process or none. Never launch a second copier concurrently.

- [ ] **Step 2: Copy the canonical root**

```powershell
robocopy $Root $DMain /E /COPY:DAT /DCOPY:DAT /XJ /R:1 /W:1 /MT:8 /NFL /NDL /NP /LOG+:D:\QS3D-Workspace\migration.log
$rc=$LASTEXITCODE
if($rc -ge 8){throw "Canonical robocopy failed, rc=$rc"}
```

Expected: return code 0-7.

- [ ] **Step 3: Prove the known required dump can be copied**

```powershell
$locked="$Root\artifacts\locked-evidence\issue1462-v26-20260822-artifacts\v26-netload-16152.dmp"
$lockedDest="$DMain\artifacts\locked-evidence\issue1462-v26-20260822-artifacts\v26-netload-16152.dmp"
Copy-Item -LiteralPath $locked -Destination $lockedDest -Force -ErrorAction Stop
```

Expected: success. `Access is denied` stops P0; do not kill the owning process or exclude the file.

- [ ] **Step 4: Copy C: worktrees outside the canonical root**

```powershell
$items=Get-Content "$Evidence\p0-before.json" -Raw | ConvertFrom-Json
$outsideRoot=$items | Where-Object {
  $_.SourcePath -like 'C:*' -and
  -not $_.SourcePath.Equals($Root,[StringComparison]::OrdinalIgnoreCase) -and
  -not $_.SourcePath.StartsWith($Root+'\',[StringComparison]::OrdinalIgnoreCase)
}
foreach($item in $outsideRoot){
  New-Item -ItemType Directory -Force -Path $item.DestinationPath | Out-Null
  robocopy $item.SourcePath $item.DestinationPath /E /COPY:DAT /DCOPY:DAT /XJ /R:1 /W:1 /MT:8 /NFL /NDL /NP /LOG+:D:\QS3D-Workspace\migration.log
  if($LASTEXITCODE -ge 8){throw "External worktree copy failed: $($item.SourcePath), rc=$LASTEXITCODE"}
}
```

Expected: all external C: worktrees have D: copies; nested worktrees were copied by the canonical-root copy.

---

### Task 3: Repair worktree administration and compare exact state

**Files:**
- Update Git admin pointers under: `D:\QS3D-Workspace\QS3D-BricsCAD\.git\worktrees\...`
- Create: `D:\QS3D-Workspace\evidence\p0-after-repair.txt`

**Interfaces:**
- Consumes: complete D: copies.
- Produces: a valid D:-anchored worktree graph with exact identity/status equivalence.

- [ ] **Step 1: Repair every relocated C: worktree path**

```powershell
$relocated=@($items | Where-Object {$_.SourcePath -like 'C:*'} | ForEach-Object {$_.DestinationPath})
git -C $DMain worktree repair @relocated
if($LASTEXITCODE -ne 0){throw 'git worktree repair failed'}
```

Expected: exit 0.

- [ ] **Step 2: Compare HEAD, branch and porcelain state with baseline**

```powershell
foreach($item in $items){
  $path=if($item.SourcePath -like 'C:*'){$item.DestinationPath}else{$item.SourcePath}
  $head=(git -C $path rev-parse HEAD).Trim()
  $branch=(git -C $path branch --show-current).Trim()
  $status=((git -C $path status --porcelain=v1 --untracked-files=all) -join "`n")
  $status64=[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($status))
  if($head -ne $item.Head){throw "HEAD mismatch: $path"}
  if($branch -ne $item.Branch){throw "Branch mismatch: $path"}
  if($status64 -ne $item.StatusBase64){throw "Dirty/untracked state mismatch: $path"}
}
```

Expected: no mismatch. If another agent changed a source after Task 1, re-capture that source's current baseline and recopy it before continuing; never overwrite/discard the newer work.

- [ ] **Step 3: Verify worktree count and eliminate C: Git-admin paths**

```powershell
$after=git -C $DMain worktree list --porcelain
$afterCount=((($after -join "`n") -split "`n`n") | Where-Object {$_.Trim()}).Count
if($afterCount -ne $items.Count){throw "Worktree count mismatch: before=$($items.Count) after=$afterCount"}
if(($after -join "`n") -match '(?im)^worktree C:\\'){throw 'Git worktree administration still references C:'}
$after | Set-Content "$Evidence\p0-after-repair.txt" -Encoding utf8
```

Expected: count matches and no worktree admin entry starts with C:.

---

### Task 4: Create integrity and rollback evidence before cutover

**Files:**
- Create: `D:\QS3D-Workspace\refs-after.txt`
- Replace deliberate directory interlock with verified bundle file: `D:\QS3D-Workspace\all-refs-backup.bundle`
- Create: `D:\QS3D-Workspace\evidence\git-fsck.txt`

**Interfaces:**
- Consumes: repaired D: repo.
- Produces: full object/ref recovery proof.

- [ ] **Step 1: Run full object verification**

```powershell
git -C $DMain fsck --full --no-dangling 2>&1 | Tee-Object "$Evidence\git-fsck.txt"
if($LASTEXITCODE -ne 0){throw 'git fsck failed'}
```

Expected: exit 0.

- [ ] **Step 2: Persist refs and remove only the deliberate interlock directory**

```powershell
git -C $DMain show-ref | Set-Content 'D:\QS3D-Workspace\refs-after.txt' -Encoding ascii
$bundle='D:\QS3D-Workspace\all-refs-backup.bundle'
if(Test-Path $bundle -PathType Container){Remove-Item -LiteralPath $bundle -Recurse -Force}
if(Test-Path $bundle){throw 'Unexpected file occupies bundle path'}
```

Expected: refs file exists and bundle path is free.

- [ ] **Step 3: Create and verify all refs**

```powershell
git -C $DMain bundle create $bundle --all
if($LASTEXITCODE -ne 0){throw 'git bundle create failed'}
git -C $DMain bundle verify $bundle
if($LASTEXITCODE -ne 0){throw 'git bundle verify failed'}
```

Expected: both exit 0.

---

### Task 5: Cut over legacy C: paths safely

**Files:**
- Replace C: worktree directories outside root with junctions.
- Replace canonical C: root last with one junction to D:.
- Create: `D:\QS3D-Workspace\evidence\p0-processes-cutover.txt`.

**Interfaces:**
- Consumes: verified D: copies and recovery bundle.
- Produces: legacy path compatibility without duplicate C: storage.

- [ ] **Step 1: Capture processes immediately before cutover**

```powershell
Get-CimInstance Win32_Process |
  Where-Object {$_.CommandLine -match 'C:\\.*QS3D'} |
  Select-Object ProcessId,Name,CommandLine |
  Format-List | Out-File "$Evidence\p0-processes-cutover.txt" -Encoding utf8
```

Expected: evidence only. Defer a path if it is actively mutating.

- [ ] **Step 2: Cut over only external C: worktrees, deepest-first**

```powershell
$outsideRoot=$items | Where-Object {
  $_.SourcePath -like 'C:*' -and
  -not $_.SourcePath.Equals($Root,[StringComparison]::OrdinalIgnoreCase) -and
  -not $_.SourcePath.StartsWith($Root+'\',[StringComparison]::OrdinalIgnoreCase)
} | Sort-Object {$_.SourcePath.Length} -Descending
foreach($item in $outsideRoot){
  $src=$item.SourcePath; $dst=$item.DestinationPath; $quarantine=$src+'.__qs3d_migrated__'
  if(Test-Path $quarantine){throw "Quarantine path already exists: $quarantine"}
  Rename-Item -LiteralPath $src -NewName ([IO.Path]::GetFileName($quarantine)) -ErrorAction Stop
  New-Item -ItemType Junction -Path $src -Target $dst -ErrorAction Stop | Out-Null
  if((git -C $src rev-parse HEAD).Trim() -ne $item.Head){throw "Junction HEAD mismatch: $src"}
  Remove-Item -LiteralPath $quarantine -Recurse -Force -ErrorAction Stop
}
```

Expected: external legacy paths are valid junctions. Nested worktrees under `$Root` are deliberately not individually cut over.

- [ ] **Step 3: Cut over the canonical root last**

```powershell
$rootItem=$items | Where-Object {$_.SourcePath.Equals($Root,[StringComparison]::OrdinalIgnoreCase)}
$quarantine=$Root+'.__qs3d_migrated__'
if(Test-Path $quarantine){throw "Canonical quarantine path already exists: $quarantine"}
Rename-Item -LiteralPath $Root -NewName ([IO.Path]::GetFileName($quarantine)) -ErrorAction Stop
New-Item -ItemType Junction -Path $Root -Target $rootItem.DestinationPath -ErrorAction Stop | Out-Null
if((git -C $Root rev-parse HEAD).Trim() -ne $rootItem.Head){throw 'Canonical junction resolves to wrong HEAD'}
Remove-Item -LiteralPath $quarantine -Recurse -Force -ErrorAction Stop
```

Expected: canonical legacy root is a junction; its nested legacy paths now naturally resolve through the same root junction.

---

### Task 6: Final verification

**Files:**
- Create: `D:\QS3D-Workspace\evidence\p0-final.txt`
- Create: `D:\QS3D-Workspace\evidence\p0-disk-after.txt`

**Interfaces:**
- Consumes: completed cutover.
- Produces: final P0 evidence.

- [ ] **Step 1: Verify every D: identity and each applicable C: junction**

```powershell
$result=foreach($item in $items){
  $primary=if($item.SourcePath -like 'C:*'){$item.DestinationPath}else{$item.SourcePath}
  if((git -C $primary rev-parse HEAD).Trim() -ne $item.Head){throw "Final D: HEAD mismatch: $primary"}
  if((git -C $primary branch --show-current).Trim() -ne $item.Branch){throw "Final D: branch mismatch: $primary"}
  if($item.SourcePath -like 'C:*'){
    if((git -C $item.SourcePath rev-parse HEAD).Trim() -ne $item.Head){throw "Legacy C: path mismatch: $($item.SourcePath)"}
  }
  "PASS`t$($item.Head)`t$($item.Branch)`t$primary"
}
$result | Set-Content "$Evidence\p0-final.txt" -Encoding utf8
```

Expected: every worktree produces PASS.

- [ ] **Step 2: Re-run object and bundle verification**

```powershell
git -C $DMain fsck --full --no-dangling
if($LASTEXITCODE -ne 0){throw 'Post-cutover fsck failed'}
git -C $DMain bundle verify 'D:\QS3D-Workspace\all-refs-backup.bundle'
if($LASTEXITCODE -ne 0){throw 'Post-cutover bundle verify failed'}
```

Expected: both exit 0.

- [ ] **Step 3: Record disk state**

```powershell
Get-PSDrive C,D,E | Select-Object Name,@{N='FreeGB';E={[math]::Round($_.Free/1GB,2)}},@{N='UsedGB';E={[math]::Round($_.Used/1GB,2)}} |
  Format-Table -AutoSize | Out-File "$Evidence\p0-disk-after.txt" -Encoding utf8
```

Expected: C: normally recovers substantial space; exact Git/state verification, not a specific GB number, is the completion authority.

## P0 Completion Gate

P0 is complete only when every required file exists on D:, worktree count/HEAD/branch/dirty state match the live baseline, Git worktree administration contains no C: entries, `git fsck` and `git bundle verify` pass, all old C: source paths resolve through the intended junction topology, already-D dirty worktrees are unchanged, and no source data was discarded to obtain the result.