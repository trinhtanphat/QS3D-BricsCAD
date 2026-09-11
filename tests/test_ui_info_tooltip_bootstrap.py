"""Compile and exercise the real WPF bootstrap without BricsCAD or visible UI.

Windows-only behavior test; a skip elsewhere is not a licensed-host PASS.
The only substitute is localization, which is unrelated to tooltip registration.
"""
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
from xml.sax.saxutils import quoteattr


ROOT = Path(__file__).resolve().parents[1]


@unittest.skipUnless(sys.platform == "win32" and shutil.which("dotnet"),
                     "Requires Windows WPF and the .NET SDK")
class TooltipBootstrapTests(unittest.TestCase):
    def test_host_initialization_enables_tooltips_without_module_load_side_effects(self):
        with tempfile.TemporaryDirectory(prefix="qs3d-tooltip-6279-") as directory:
            root = Path(directory)
            library = root / "Library"
            library.mkdir()
            linked = [ROOT / "src/QS3D.BricsCAD.V25/UiInfoTooltipBootstrap.cs",
                      ROOT / "src/QS3D.BricsCAD.V25/UI/ProductionUiPolish.cs"]
            includes = "".join(f'<Compile Include={quoteattr(str(p))} />'
                               for p in linked)
            (library / "Probe.csproj").write_text(f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework><UseWPF>true</UseWPF>
    <OutputType>Library</OutputType><Nullable>annotations</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <DefineConstants>BRICSCAD_V26</DefineConstants>
  </PropertyGroup>
  <ItemGroup>{includes}</ItemGroup>
</Project>""", encoding="utf-8")
            (library / "Probe.cs").write_text(PROBE, encoding="utf-8")
            (root / "Runner.csproj").write_text("""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF><OutputType>Exe</OutputType>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup><Compile Include="Program.cs" />
    <ProjectReference Include="Library/Probe.csproj" /></ItemGroup>
</Project>""", encoding="utf-8")
            (root / "Program.cs").write_text("""using System;
internal static class Program {
  [STAThread] private static int Main() {
    try { Probe.Run(); return 0; }
    catch (Exception error) { Console.Error.WriteLine(error); return 1; }
  }
}
""", encoding="utf-8")
            result = subprocess.run(
                ["dotnet", "run", "--project", str(root / "Runner.csproj"), "-c", "Release"],
                capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=120)
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("PASS: explicit bootstrap, repeated initialization, target scoping", result.stdout)


PROBE = r'''
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QS3D.BricsCAD.V25.UI;

// Localization's independent dictionary work is outside this registration test.
namespace QS3D.BricsCAD.V25.UI {
    internal static class UiLocalization {
        internal static int RootLoads;
        internal static void Apply(FrameworkElement root) { }
        internal static void RegisterAndApply(FrameworkElement root) { RootLoads++; }
    }
}
// An unshown Window normally has no visual tree. Supply a real WPF visual child
// explicitly so Loaded traversal is testable without creating an on-screen HWND.
internal class TestWindow : Window {
    private StackPanel _panel;
    internal void SetPanel(StackPanel panel) { _panel = panel; AddVisualChild(panel); }
    protected override int VisualChildrenCount { get { return _panel == null ? 0 : 1; } }
    protected override Visual GetVisualChild(int index) {
        if (index != 0 || _panel == null) throw new ArgumentOutOfRangeException(nameof(index));
        return _panel;
    }
}
internal sealed class McpAgentControlCenterWindow : TestWindow { }
internal sealed class OtherWindow : TestWindow { }
public static class Probe {
    private static void Check(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static StackPanel Populate(TestWindow window) {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = "Chọn một transport. Test explanation" });
        window.SetPanel(panel);
        return panel;
    }
    private static void Loaded(Window window) {
        window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
    }
    public static void Run() {
        // Never call Show: this test creates no visible window or host session.
        var target = new McpAgentControlCenterWindow();
        var panel = Populate(target);
        Loaded(target);
        Check(panel.Children.Count == 1, "Assembly loading must not install tooltip handlers");
        ProductionUiPolish.EnsureRegistered();
        ProductionUiPolish.EnsureRegistered();
        Loaded(target);
        Console.WriteLine("Probe state: visualChildren=" + VisualTreeHelper.GetChildrenCount(target)
            + " panelChildren=" + panel.Children.Count + " polishLoads=" + UiLocalization.RootLoads);
        Check(panel.Children.Count == 2, "Explicit host bootstrap must enable target tooltips");
        Check(((TextBlock)panel.Children[0]).Visibility == Visibility.Collapsed,
              "Original explanation must remain available but compacted");
        var replacement = (StackPanel)panel.Children[1];
        Check(replacement.Children[replacement.Children.Count - 1] is Button,
              "Target must receive a focusable tooltip button");
        Check(UiLocalization.RootLoads == 1, "Repeated bootstrap must not duplicate root handlers");
        Loaded(target);
        Check(panel.Children.Count == 2, "Repeated Loaded must not duplicate tooltip controls");
        var other = new OtherWindow();
        var otherPanel = Populate(other);
        Loaded(other);
        Check(otherPanel.Children.Count == 1, "Non-target windows must not receive tooltips");
        target.Close();
        other.Close();
        Console.WriteLine("PASS: explicit bootstrap, repeated initialization, target scoping");
    }
}
'''


if __name__ == "__main__":
    unittest.main()
