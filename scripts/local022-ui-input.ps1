# Test-only, opt-in physical input. No MCP surface or general command execution.
function Assert-Local022UiPhase($Marker, [string]$RunId, [string]$Phase) {
    $keys = @($Marker.PSObject.Properties.Name)
    $expected = @('schema','run_id','phase','status','stage','error_code','checks')
    if ($keys.Count -ne $expected.Count) { throw 'UI marker field count mismatch.' }
    foreach ($key in $expected) { if ($keys -cnotcontains $key) { throw 'UI marker schema mismatch.' } }
    foreach ($key in @('schema','run_id','phase','status','stage','error_code')) {
        if ($Marker.$key -isnot [string]) { throw 'UI marker field type mismatch.' }
    }
    if ($Marker.schema -cne 'QS3D_LOCAL022_NATIVE_UI_V1' -or $Marker.run_id -cne $RunId -or
        $Marker.phase -cne $Phase) { throw 'UI marker identity mismatch.' }
    if ($Marker.stage -cnotmatch '^[a-z0-9_]{1,80}$' -or $Marker.error_code -cnotmatch '^[A-Z0-9_]{1,80}$') {
        throw 'Unsanitized UI marker diagnostic.'
    }
    if ($Marker.status -cne 'PASS') { throw ('Native UI failed: ' + $Marker.stage + '/' + $Marker.error_code) }
    if ($Marker.stage -cne $Phase -or $Marker.error_code -cne 'NONE') { throw 'UI PASS marker contains failure metadata.' }
    $requiredByPhase = @{
        ui = @('active_disposable_drawing','mcp_mutation_boundary_paused','workspace_visible','single_footing_tree_clicked','cancel_nonmutation','six_field_dialog_layout','six_field_physical_input','active_family_h2_zero','two_physical_centres','enter_command_termination','family_h2_physical_edit','existing_geometry_regenerated','former_generated_handles_erased','repeat_physical_centre','escape_command_termination','geometry_ownership_extents','exact_semantic_native_cardinality','physical_receipts_complete','saved_exact_artifact_digest')
        uisaved = @('active_disposable_drawing','mcp_mutation_boundary_paused','same_process_ui_state','sidecar_exists_after_qs3dsave','qsave_command_completed','saved_semantic_native_state','saved_exact_artifact_digest','saved_exact_cardinality')
        uireopen = @('active_disposable_drawing','mcp_mutation_boundary_paused','cold_project_bind','reopened_family_identity','reopened_semantic_identity','reopened_generated_solids_live','reopened_dimensions_volume_extents','reopened_exact_artifact_digest','reopened_exact_cardinality')
    }
    if (-not $requiredByPhase.ContainsKey($Phase)) { throw 'Unknown UI phase.' }
    $required = @($requiredByPhase[$Phase] | Sort-Object)
    $actual = @($Marker.checks.PSObject.Properties.Name | Sort-Object)
    if ($required.Count -ne $actual.Count -or [string]::Join([char]0,$required) -cne [string]::Join([char]0,$actual)) {
        throw 'UI marker assertion coverage mismatch.'
    }
    foreach ($check in $Marker.checks.PSObject.Properties) {
        if ($check.Value -isnot [bool] -or -not $check.Value) { throw 'UI assertion is not a true Boolean.' }
    }
    return $Marker
}

function Assert-Local022UiAction($Request, [string]$RunId, [int]$Sequence, [int]$OwnedProcessId) {
    $keys = @($Request.PSObject.Properties.Name)
    $expected = @('schema','run_id','sequence','action','x','y','text','target_pid')
    if ($keys.Count -ne $expected.Count) { throw 'UI action field count mismatch.' }
    foreach ($key in $expected) { if ($keys -cnotcontains $key) { throw 'UI action field mismatch.' } }
    foreach ($key in @('schema','run_id','action','text')) {
        if ($Request.$key -isnot [string]) { throw 'UI action string type mismatch.' }
    }
    if ($RunId -cnotmatch '^[0-9a-f]{32}$' -or $Request.run_id -cne $RunId -or
        $Request.schema -cne 'QS3D_LOCAL022_UI_ACTION_V1') { throw 'UI action allocation mismatch.' }
    foreach ($name in @('sequence','target_pid','x','y')) {
        if ($Request.$name -isnot [int] -and $Request.$name -isnot [long]) { throw 'UI action integer type mismatch.' }
    }
    if ($Sequence -lt 1 -or $Sequence -gt 100 -or $Request.sequence -ne $Sequence -or
        $OwnedProcessId -le 0 -or $Request.target_pid -ne $OwnedProcessId) { throw 'UI action process/sequence mismatch.' }
    if ($Request.x -lt -32768 -or $Request.x -gt 32767 -or $Request.y -lt -32768 -or $Request.y -gt 32767) {
        throw 'UI action coordinates out of bounds.'
    }
    if ($Request.action -isnot [string] -or $Request.text -isnot [string]) { throw 'UI action text type mismatch.' }
    switch -CaseSensitive ($Request.action) {
        'move' { if ($Request.text -cne '') { throw 'Move cannot carry text.' } }
        'click' { if ($Request.text -cne '') { throw 'Click cannot carry text.' } }
        'text' { if ($Request.text -cnotmatch '^\-?[0-9]{1,7}(\.[0-9]{1,4})?$') { throw 'Only bounded numeric dimension input is allowed.' } }
        'key' { if ($Request.text -cnotin @('ENTER','ESC')) { throw 'Only Enter/Esc termination is allowed.' } }
        default { throw 'Unsupported UI action.' }
    }
    return $Request
}

function Initialize-Local022UiInput {
    if ('Qs3dLocal022Input' -as [type]) { return }
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class Qs3dLocal022Input {
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] struct INPUT { public uint Type; public UNION Data; }
    [StructLayout(LayoutKind.Explicit)] struct UNION {
        [FieldOffset(0)] public MOUSE Mouse;
        [FieldOffset(0)] public KEY Keyboard;
    }
    [StructLayout(LayoutKind.Sequential)] struct MOUSE {
        public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra;
    }
    [StructLayout(LayoutKind.Sequential)] struct KEY {
        public ushort VirtualKey, Scan; public uint Flags, Time; public UIntPtr Extra;
    }
    [DllImport("user32.dll")] static extern uint SendInput(uint count, INPUT[] input, int size);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll")] public static extern IntPtr GetLastActivePopup(IntPtr window);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint thread, uint other, bool attach);
    [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr window);
    public static bool ActivateOwned(IntPtr window, int process) {
        uint actual;
        uint targetThread=GetWindowThreadProcessId(window,out actual);
        if (targetThread == 0 || actual != process) throw new InvalidOperationException("Activation target is not owned.");
        uint foregroundProcess;
        uint foregroundThread=GetWindowThreadProcessId(GetForegroundWindow(),out foregroundProcess);
        uint thread=GetCurrentThreadId();
        bool attachedForeground=false, attachedTarget=false;
        try {
            if (foregroundThread != 0 && foregroundThread != thread)
                attachedForeground=AttachThreadInput(thread,foregroundThread,true);
            if (targetThread != thread && targetThread != foregroundThread)
                attachedTarget=AttachThreadInput(thread,targetThread,true);
            BringWindowToTop(window);
            SetForegroundWindow(window);
        } finally {
            if (attachedTarget) AttachThreadInput(thread,targetThread,false);
            if (attachedForeground) AttachThreadInput(thread,foregroundThread,false);
        }
        GetWindowThreadProcessId(GetForegroundWindow(),out actual);
        return actual == process;
    }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window, out RECT rectangle);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);
    public static void RequireForeground(int process) {
        uint actual;
        if (GetWindowThreadProcessId(GetForegroundWindow(), out actual) == 0 || actual != process)
            throw new InvalidOperationException("UI foreground is not the owned host.");
    }
    public static void RequirePoint(int process, int x, int y) {
        uint actual;
        if (GetWindowThreadProcessId(WindowFromPoint(new POINT { X=x, Y=y }), out actual) == 0 || actual != process)
            throw new InvalidOperationException("UI point is occluded or outside the owned host.");
    }
    static void Send(INPUT[] values) {
        if (SendInput((uint)values.Length, values, Marshal.SizeOf(typeof(INPUT))) != values.Length)
            throw new InvalidOperationException("Native input was not fully delivered.");
    }
    static INPUT Key(ushort key, uint flags) {
        return new INPUT { Type=1, Data=new UNION { Keyboard=new KEY { VirtualKey=key, Flags=flags } } };
    }
    public static void Click(int process, int x, int y) {
        RequireForeground(process); RequirePoint(process,x,y);
        if (!SetCursorPos(x,y)) throw new InvalidOperationException("Cannot position test cursor.");
        RequireForeground(process); RequirePoint(process,x,y);
        Send(new[] {
            new INPUT { Data=new UNION { Mouse=new MOUSE { Flags=2 } } },
            new INPUT { Data=new UNION { Mouse=new MOUSE { Flags=4 } } }
        });
    }
    public static void Move(int process, int x, int y) {
        RequireForeground(process); RequirePoint(process,x,y);
        if (!SetCursorPos(x,y)) throw new InvalidOperationException("Cannot position test cursor.");
    }
    public static void Terminate(int process, bool escape) {
        RequireForeground(process);
        ushort key = escape ? (ushort)27 : (ushort)13;
        Send(new[] { Key(key,0), Key(key,2) });
    }
    public static void SelectAll(int process) {
        RequireForeground(process);
        Send(new[] { Key(17,0), Key(65,0), Key(65,2), Key(17,2) });
    }
    public static void NumericText(int process, string text) {
        foreach(char value in text) {
            if (!(value >= '0' && value <= '9') && value != '.' && value != '-')
                throw new InvalidOperationException("Non-numeric test input refused.");
            RequireForeground(process);
            Send(new[] {
                new INPUT { Type=1, Data=new UNION { Keyboard=new KEY { Scan=value, Flags=4 } } },
                new INPUT { Type=1, Data=new UNION { Keyboard=new KEY { Scan=value, Flags=6 } } }
            });
        }
    }
}
'@
}

function Save-Local022OwnedWindow([Diagnostics.Process]$Process, [string]$Path) {
    # Exact-HWND capture only. Never fall back to copying the whole desktop.
    [Qs3dLocal022Input]::RequireForeground($Process.Id)
    $window = [Qs3dLocal022Input]::GetForegroundWindow()
    $rect = [Qs3dLocal022Input+RECT]::new()
    if (-not [Qs3dLocal022Input]::GetWindowRect($window,[ref]$rect)) { throw 'Cannot read owned screenshot bounds.' }
    $width=$rect.Right-$rect.Left; $height=$rect.Bottom-$rect.Top
    if ($width -lt 1 -or $height -lt 1 -or $width -gt 10000 -or $height -gt 10000) { throw 'Owned screenshot bounds invalid.' }
    if (Test-Path -LiteralPath $Path) { throw 'UI screenshot already exists.' }
    Add-Type -AssemblyName System.Drawing
    $bitmap=[Drawing.Bitmap]::new($width,$height)
    try {
        $graphics=[Drawing.Graphics]::FromImage($bitmap)
        try {
            $device=$graphics.GetHdc()
            try {
                [Qs3dLocal022Input]::RequireForeground($Process.Id)
                if (-not [Qs3dLocal022Input]::PrintWindow($window,$device,2)) { throw 'Owned HWND capture failed; no desktop fallback.' }
            } finally { $graphics.ReleaseHdc($device) }
        } finally { $graphics.Dispose() }
        $bitmap.Save($Path,[Drawing.Imaging.ImageFormat]::Png)
    } finally { $bitmap.Dispose() }
}

function Invoke-Local022UiPhysicalAction($Request, [Diagnostics.Process]$Process, [string]$ExpectedExecutable) {
    $Process.Refresh()
    if ($Process.HasExited -or $Process.ProcessName -ine 'bricscad' -or
        [IO.Path]::GetFullPath($Process.Path) -ine [IO.Path]::GetFullPath($ExpectedExecutable) -or
        $Request.target_pid -ne $Process.Id) { throw 'UI input target is not the exact owned BricsCAD process.' }
    Initialize-Local022UiInput
    $window = $Process.MainWindowHandle
    if ($window -eq [IntPtr]::Zero) { throw 'Owned BricsCAD has no UI window.' }
    # Activate only the owned window or its owned modal dialog. Never inject into
    # the desktop when activation fails or a user/other process takes focus.
    # Do not restore/resize a maximized window after the probe has measured its
    # controls; that would invalidate every screen coordinate in this request.
    $popup = [Qs3dLocal022Input]::GetLastActivePopup($window)
    if ($popup -ne [IntPtr]::Zero) { $window = $popup }
    $activationDeadline = [DateTime]::UtcNow.AddSeconds(5)
    $activationShell = New-Object -ComObject WScript.Shell
    do {
        [void]$activationShell.AppActivate($Process.Id)
        $activated = [Qs3dLocal022Input]::ActivateOwned($window,$Process.Id)
        if ($activated) { break }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $activationDeadline)
    [Qs3dLocal022Input]::RequireForeground($Process.Id)
    switch -CaseSensitive ($Request.action) {
        'move' { [Qs3dLocal022Input]::Move($Process.Id, $Request.x, $Request.y) }
        'click' { [Qs3dLocal022Input]::Click($Process.Id, $Request.x, $Request.y) }
        'text' {
            [Qs3dLocal022Input]::Click($Process.Id, $Request.x, $Request.y)
            Start-Sleep -Milliseconds 50
            [Qs3dLocal022Input]::SelectAll($Process.Id)
            [Qs3dLocal022Input]::NumericText($Process.Id, $Request.text)
        }
        'key' { [Qs3dLocal022Input]::Terminate($Process.Id, ($Request.text -ceq 'ESC')) }
        default { throw 'Unsupported native action.' }
    }
}

function Invoke-Local022UiPendingAction([string]$Root, [string]$RunId, [int]$Sequence, [Diagnostics.Process]$Process, [string]$ExpectedExecutable) {
    $requestPath = Join-Path $Root ('ui-action-{0:D4}.private.json' -f $Sequence)
    if (-not (Test-Path -LiteralPath $requestPath -PathType Leaf)) { return $false }
    $file = Get-Item -LiteralPath $requestPath
    if ($file.Length -gt 4096 -or ($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Unsafe UI request file.' }
    $raw = [IO.File]::ReadAllText($requestPath)
    if ([regex]::Matches($raw, '"(?:schema|run_id|sequence|action|x|y|text|target_pid)"\s*:').Count -ne 8) {
        throw 'Duplicate or missing UI request key.'
    }
    $request = Assert-Local022UiAction ($raw | ConvertFrom-Json) $RunId $Sequence $Process.Id
    $ack = Join-Path $Root ('ui-ack-{0:D4}.private.json' -f $Sequence)
    if (Test-Path -LiteralPath $ack) { throw 'UI action already acknowledged; refusing replay.' }
    try { Invoke-Local022UiPhysicalAction $request $Process $ExpectedExecutable }
    catch {
        # Preserve the exact owned window for diagnosis of rejected coordinates.
        # No input retries or alternate/unverified coordinates are attempted.
        try { Save-Local022OwnedWindow $Process (Join-Path $Root ('ui-rejected-{0:D4}.private.png' -f $Sequence)) } catch { }
        throw
    }
    # The matching private frame supports human visual QA; it never establishes
    # the semantic/native PASS by itself. The probe independently checks state.
    Save-Local022OwnedWindow $Process (Join-Path $Root ('ui-window-{0:D4}.private.png' -f $Sequence))
    $receipt = [ordered]@{ schema='QS3D_LOCAL022_UI_ACK_V1'; run_id=$RunId; sequence=$Sequence; status='SENT' }
    $bytes = [Text.UTF8Encoding]::new($false).GetBytes(($receipt | ConvertTo-Json -Compress))
    $temporary = $ack + '.tmp'
    $stream = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $stream.Write($bytes,0,$bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
    [IO.File]::Move($temporary,$ack)
    Write-Host ('LOCAL-022 physical UI action sent: {0} ({1})' -f $Sequence,$request.action)
    return $true
}
