$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Replay the actual cleanup/context-guard method bodies with in-memory host
# doubles. This must never load BricsCAD or send commands to any application.
$uiSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Local022NativeFootingProbeCommands.Ui.cs') -Raw
$baseSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Local022NativeFootingProbeCommands.cs') -Raw
function Read-UiMethod([string]$Name) {
    $method = [regex]::Match($uiSource, ('(?ms)^        private static void ' + [regex]::Escape($Name) + '\([^\r\n]*\)\r?\n        \{.*?^        \}'))
    if (-not $method.Success) { throw "Missing cleanup method $Name." }
    $method.Value
}
$quitMethod = Read-UiMethod 'QueueOwnedQuit'
$contextMethod = Read-UiMethod 'RequireUiContextStable'
$cleanupMethod = Read-UiMethod 'RequireUiCleanupContext'
$pathMethods = [regex]::Matches($baseSource, '(?m)^        private static bool (?:SamePath|IsChildPath)\([^\r\n]+$')
if ($pathMethods.Count -ne 2) { throw 'Missing exact path guard helpers.' }
$pathSource = ($pathMethods | ForEach-Object Value) -join "`n"
$typeName = 'Local022QuitReplay_' + [Guid]::NewGuid().ToString('N')
$replay = @"
#nullable enable
using System;
using System.IO;
public static class $typeName {
    private sealed class Document {
        public string Name = @"C:\private\owned.dwg";
        public int Commands;
        public string LastCommand = string.Empty;
        public void SendStringToExecute(string command, bool activate, bool wrap, bool echo) {
            Commands++; LastCommand = command;
        }
    }
    private sealed class Manager {
        public Document? MdiActiveDocument;
        public int Count = 1;
    }
    private static class Application { public static Manager DocumentManager = new Manager(); }
    private sealed class Context {
        public Document Document;
        public string Drawing = @"C:\private\owned.dwg";
        public string Root = @"C:\private";
        public object Product = new object();
        public Context(Document document) { Document = document; }
    }
    private sealed class ProbeException : Exception { public ProbeException(string code) : base(code) {} }
    private static bool Paused = true;
    private static void RequireMcpMutationBoundaryPaused(object product) {
        if (!Paused) throw new ProbeException("boundary_not_paused");
    }
    private static void Require(bool condition, string message) {
        if (!condition) throw new Exception("FAIL: " + message);
    }
$pathSource
$contextMethod
$cleanupMethod
$quitMethod
    public static void Run() {
        var owned = new Document();
        var foreign = new Document();
        var context = new Context(owned);
        Application.DocumentManager.MdiActiveDocument = foreign;
        QueueOwnedQuit(null, true);
        QueueOwnedQuit(null, false);
        Require(foreign.Commands == 0, "unbound cleanup queued QUIT on an active drawing");
        QueueOwnedQuit(context, true);
        Require(owned.Commands == 0 && foreign.Commands == 0, "document drift queued QUIT");
        Application.DocumentManager.MdiActiveDocument = null;
        QueueOwnedQuit(context, true);
        Require(owned.Commands == 0, "missing active document queued QUIT");
        Application.DocumentManager.MdiActiveDocument = owned;
        owned.Name = @"C:\private\renamed.dwg";
        QueueOwnedQuit(context, true);
        Require(owned.Commands == 0, "renamed drawing queued QUIT");
        owned.Name = context.Drawing;
        context.Root = @"C:\different-allocation";
        QueueOwnedQuit(context, true);
        Require(owned.Commands == 0, "outside-allocation drawing queued QUIT");
        context.Root = @"C:\private";
        Application.DocumentManager.Count = 2;
        QueueOwnedQuit(context, true);
        Require(owned.Commands == 0, "additional drawing allowed application-wide QUIT");
        Application.DocumentManager.Count = 1;
        Paused = false;
        QueueOwnedQuit(context, true);
        Require(owned.Commands == 0, "unpaused boundary queued QUIT");
        Paused = true;
        QueueOwnedQuit(context, false);
        Require(owned.Commands == 1 && owned.LastCommand == "_.QUIT _N ", "qualified owned quit changed");
        QueueOwnedQuit(context, true);
        Require(owned.Commands == 2 && owned.LastCommand == "\u001b\u001b_.QUIT _N ", "qualified owned cancellation changed");
    }
}
"@
$type = Add-Type -TypeDefinition $replay -PassThru | Where-Object Name -CEQ $typeName
$type::Run()
Write-Output 'PASS: actual UI quit method refuses unbound/drift/renamed/foreign/additional/unpaused contexts; valid owned cleanup retained. No CAD loaded.'
