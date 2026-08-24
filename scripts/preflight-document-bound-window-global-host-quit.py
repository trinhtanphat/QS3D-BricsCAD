#!/usr/bin/env python3
"""Guard the single-owner BricsCAD host-quiescence contract for modeless QS3D windows."""

# Lane-Key: issue-3621 — post-p05 N-reactor teardown regression.
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LIFETIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ModelessHostQuiescenceCoordinator.cs"
PLUGIN = ROOT / "src" / "QS3D.BricsCAD.V25" / "PluginEntry.cs"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    require(start >= 0, f"{signature} is missing.")
    brace = source.find("{", start)
    require(brace >= 0, f"{signature} body is missing.")
    depth = 0
    for index in range(brace, len(source)):
        char = source[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[start : index + 1]
    raise AssertionError(f"{signature} body is unterminated.")


require(COORDINATOR.exists(), "ModelessHostQuiescenceCoordinator.cs must own the single BricsCAD application-quit subscription.")
lifetime = LIFETIME.read_text(encoding="utf-8")
coordinator = COORDINATOR.read_text(encoding="utf-8")
plugin = PLUGIN.read_text(encoding="utf-8")

# Native application lifecycle ownership must not scale with the number of WPF windows.
for marker in (
    "BcadApplication.QuitWillStart +=",
    "BcadApplication.QuitWillStart -=",
    "BcadApplication.BeginQuit +=",
    "BcadApplication.BeginQuit -=",
    "BcadApplication.QuitAborted +=",
    "BcadApplication.QuitAborted -=",
):
    require(marker not in lifetime, f"DocumentBoundWindowLifetime must not own per-window BricsCAD application lifecycle subscriptions: {marker}")

require("private int _hostQuitStarted;" not in lifetime, "Host quit state must be plugin-global, not copied into every Registration.")
require(
    "ModelessHostQuiescenceCoordinator.QuiescenceAborted += OnHostQuiescenceAborted;" in lifetime,
    "Registrations may subscribe only to the coordinator's managed abort notification.",
)
require(
    "ModelessHostQuiescenceCoordinator.QuiescenceAborted -= OnHostQuiescenceAborted;" in lifetime,
    "Normal Registration detach must release the managed abort notification.",
)
require(
    lifetime.count("ModelessHostQuiescenceCoordinator.IsQuiescing") >= 6,
    "All input/document/dispatcher/Closed/detach host barriers must consume the single global quiescence state.",
)

# Exactly one plugin-global native application subscription pair.
require(coordinator.count("BcadApplication.QuitWillStart += OnQuitWillStart;") == 1,
        "The global coordinator must subscribe QuitWillStart exactly once.")
require(coordinator.count("BcadApplication.QuitAborted += OnQuitAborted;") == 1,
        "The global coordinator must subscribe QuitAborted exactly once.")
require(coordinator.count("BcadApplication.QuitWillStart -= OnQuitWillStart;") == 2,
        "The global coordinator may release QuitWillStart only in partial-start rollback and normal Stop.")
require(coordinator.count("BcadApplication.QuitAborted -= OnQuitAborted;") == 1,
        "The global coordinator must release QuitAborted only from normal Stop.")
require("BcadApplication.BeginQuit" not in coordinator,
        "The global barrier must use the earlier QuitWillStart boundary, not BeginQuit.")

quit_start = method_block(coordinator, "private static void OnQuitWillStart(object? sender, EventArgs e)")
require("Volatile.Write(ref _isQuiescing, 1);" in quit_start,
        "QuitWillStart must atomically arm global quiescence.")
for forbidden in (
    "Window.Close",
    ".Close()",
    "-=",
    "+=",
    "DocumentManager",
    "Dispatcher",
    "QuiescenceAborted",
):
    require(forbidden not in quit_start,
            f"QuitWillStart must be state-only and non-reentrant: {forbidden}")

quit_aborted = method_block(coordinator, "private static void OnQuitAborted(object? sender, EventArgs e)")
require("Interlocked.Exchange(ref _isQuiescing, 0)" in quit_aborted,
        "QuitAborted must clear global quiescence before managed recovery is notified.")
require("QuiescenceAborted?.Invoke(null, EventArgs.Empty);" in quit_aborted,
        "QuitAborted recovery must be centralized through one managed notification owner.")
for forbidden in ("Window.Close", ".Close()", "DocumentManager"):
    require(forbidden not in quit_aborted,
            f"Native QuitAborted must not perform WPF/document teardown directly: {forbidden}")

stop = method_block(coordinator, "internal static void Stop()")
require("if (IsQuiescing) return;" in stop,
        "Plugin Terminate during host shutdown must not unsubscribe BricsCAD native lifecycle reactors.")
require(stop.index("if (IsQuiescing) return;") < stop.index("BcadApplication.QuitWillStart -= OnQuitWillStart;"),
        "The host-quiescence guard must precede every native application unsubscription in Stop.")

initialize = method_block(plugin, "public void Initialize()")
require("ModelessHostQuiescenceCoordinator.EnsureInitialized();" in initialize,
        "Plugin initialization must establish the single host-quiescence owner before modeless lifetime services start.")
require(initialize.index("ModelessHostQuiescenceCoordinator.EnsureInitialized();") < initialize.index("DocumentLifecycleCoordinator.Start();"),
        "Host quit ownership must be armed before document/modeless lifecycle services.")

teardown = method_block(plugin, "private static void TeardownHostServices()")
require("TryCleanup(ModelessHostQuiescenceCoordinator.Stop);" in teardown,
        "Normal plugin teardown must release the global coordinator when host shutdown is not active.")

print("[OK] V25 modeless host quit has one plugin-global native lifecycle owner; per-window registrations consume managed quiescence only, QuitWillStart is state-only, and host Terminate never unsubscribes native reactors after quiescence begins.")
