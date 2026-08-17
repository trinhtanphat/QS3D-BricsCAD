using System;
using System.IO;

namespace QS3D.Core.SmokeTests
{
    internal static class Blt3dRuntimeLayoutSourceContractSmoke
    {
        internal static void Run()
        {
            var root = FindRepositoryRoot();
            var pluginRoot = Path.Combine(root, "src", "QS3D.BricsCAD.V25");

            var paletteCoordinator = Read(Path.Combine(pluginRoot, "PaletteCoordinator.cs"));
            var ribbonCoordinator = Read(Path.Combine(pluginRoot, "Ribbon", "BltBimWorkspaceActivationCoordinator.cs"));
            var runtimeLayout = Read(Path.Combine(pluginRoot, "UI", "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"));
            var runtimeRepair = Read(Path.Combine(pluginRoot, "UI", "WorkspacePanel.Blt3dRuntimeLayoutRepair.cs"));
            var compactShell = Read(Path.Combine(pluginRoot, "UI", "WorkspacePanel.CompactShell.cs"));

            RequireContains(
                paletteCoordinator,
                "public static void Show() => ShowBimWorkspace();",
                "The owner-facing QS3D activation must restore the coordinated BIM workspace.");
            RequireContains(
                paletteCoordinator,
                "SetVisibility(workspace: true, right: true, quantityInsight: true);",
                "The BIM workspace must expose all three coordinated QS3D palettes around the native viewport.");

            RequireContains(
                ribbonCoordinator,
                "private const int BimSettleTicks = 2;",
                "Ribbon activation must remain bounded to two settle retries.");
            RequireContains(
                ribbonCoordinator,
                "StartCenterPaletteCoordinator.Hide();",
                "BIM activation must release the Start Center before restoring the model workspace.");
            RequireContains(
                ribbonCoordinator,
                "PaletteCoordinator.ShowBimWorkspace();",
                "BIM ribbon activation must route through the coordinated workspace path.");

            RequireContains(
                runtimeLayout,
                "DispatcherPriority.SystemIdle",
                "The final five-zone layout must run after earlier Loaded compatibility passes.");
            RequireContains(
                runtimeLayout,
                "IsVisualDescendant(child, FamilyList)",
                "Repeated settle passes must rediscover the moved Family/Properties pane by ownership, not stale column position.");
            RequireContains(
                runtimeLayout,
                "IsVisualDescendant(child, PropertyList)",
                "The dedicated QS3D Properties region must remain part of the runtime five-zone layout.");
            RequireNotContains(
                runtimeLayout,
                "static WorkspacePanel()",
                "The runtime layout partial must not define a second WorkspacePanel static constructor.");
            RequireContains(
                compactShell,
                "static WorkspacePanel()",
                "WorkspacePanel must keep one explicit type initializer so partial registration fields initialize deterministically.");

            RequireContains(
                runtimeRepair,
                "private const int Blt3dRuntimeSettlePasses = 2;",
                "Host docking repair must stay bounded to two settle passes.");
            RequireContains(
                runtimeRepair,
                "FrameworkElement.LoadedEvent",
                "Workspace runtime repair must restart when BricsCAD loads/reparents the panel.");
            RequireContains(
                runtimeRepair,
                "FrameworkElement.UnloadedEvent",
                "Workspace runtime repair must stop and reset when the panel unloads.");
            RequireContains(
                runtimeRepair,
                "ApplyBlt3dFiveZoneRuntimeLayout();",
                "Runtime repair must reassert the owner-approved five-zone layout.");
        }

        private static string FindRepositoryRoot()
        {
            foreach (var seed in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var directory = new DirectoryInfo(seed);
                for (var depth = 0; directory != null && depth < 16; depth++, directory = directory.Parent)
                {
                    if (File.Exists(Path.Combine(
                        directory.FullName,
                        "src",
                        "QS3D.BricsCAD.V25",
                        "PaletteCoordinator.cs")))
                    {
                        return directory.FullName;
                    }
                }
            }

            throw new InvalidOperationException(
                "Unable to locate repository root for the BLT3D runtime layout source-contract smoke test.");
        }

        private static string Read(string path)
        {
            if (!File.Exists(path))
                throw new InvalidOperationException("Required BLT3D runtime source file is missing: " + path);
            return File.ReadAllText(path);
        }

        private static void RequireContains(string source, string required, string message)
        {
            if (source.IndexOf(required, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Missing contract: " + required);
        }

        private static void RequireNotContains(string source, string forbidden, string message)
        {
            if (source.IndexOf(forbidden, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException(message + " Forbidden contract: " + forbidden);
        }
    }
}
