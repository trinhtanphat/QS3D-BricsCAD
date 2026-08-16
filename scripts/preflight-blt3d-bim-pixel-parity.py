#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(relative):
    path = ROOT / relative
    if not path.is_file():
        fail(f"missing required source: {relative}")
    return path.read_text(encoding="utf-8")


def require(text, needle, label):
    if needle not in text:
        fail(f"{label}: expected source contract not found: {needle}")


def fail(message):
    print("ERROR:", message)
    raise SystemExit(1)


def main():
    icon = read("src/QS3D.BricsCAD.V25/UI/Blt3dVectorIcon.cs")
    workspace = read("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dPixelParity.cs")
    right = read("src/QS3D.BricsCAD.V25/UI/RightPanel.Blt3dPixelParity.cs")
    right_code = read("src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs")
    catalog = read("src/QS3D.BricsCAD.V25/Cad/DrawingCatalogReader.cs")

    for token in (
        "internal static class Blt3dVectorIcon",
        "internal const string Add",
        "internal const string Delete",
        "internal const string Bolt",
        "button.ContentTemplate = CreateTemplate",
        "item.HeaderTemplate = CreateTemplate",
        "Geometry.Parse(geometryData)",
    ):
        require(icon, token, "BLT3D vector icon contract")

    for token in (
        "CollapseBlt3dWorkspaceHeader();",
        "root.RowDefinitions[0].Height = new GridLength(0);",
        'add.Content = "+ Add";',
        'delete.Content = "Delete";',
        'import.Content = "Nhập tự động";',
        "Blt3dVectorIcon.Apply(import, Blt3dVectorIcon.Bolt);",
        "TuneBlt3dModelTreeIcons();",
        'var health = FindButton("Kiểm tra");',
        'context.Text = "Tầng "',
        'text.Text.IndexOf("VIEWPORT BRICSCAD"',
    ):
        require(workspace, token, "BLT3D workspace pixel parity")

    for token in (
        "root.RowDefinitions[0].Height = new GridLength(282);",
        "root.RowDefinitions[3].Height = new GridLength(0);",
        'string.Equals(label, "Khóa", StringComparison.Ordinal)',
        'string.Equals(label, "Mở khóa", StringComparison.Ordinal)',
        "Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Reload);",
        "Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Move);",
        "Blt3dVectorIcon.Apply(button, Blt3dVectorIcon.Zoom);",
        'gridView.Columns[2].Width = 0d;',
        'gridView.Columns[3].Header = "Tỉ lệ";',
        'button.Content = "Đảo";',
    ):
        require(right, token, "BLT3D right-manager pixel parity")

    # Button/row behavior from the supplied reference must be real, not cosmetic.
    for token in (
        'private void OnAttachXrefClick(object sender, RoutedEventArgs e) => Send("_XATTACH");',
        "XrefService.Reload(doc, item.Name);",
        'if (TrySend(doc, "_MOVE"))',
        "XrefService.Detach(doc, item.Name);",
        'private void OnZoomWindowClick(object sender, RoutedEventArgs e) => Send("_ZOOM _W");',
        "ScaleText = item.ScaleText,",
        "var applyToSelection = selected.Length > 1",
        "LayerVisibilityService.SetVisible(doc, names, visible);",
        '" layer trong cụm đang chọn."',
    ):
        require(right_code, token, "BLT3D button behavior")

    for token in (
        "snapshot.ScaleText = snapshot.InstanceCount == 0 || !snapshot.HasScale",
        'if (SameScale(x, 1d)) return "1:1";',
        'return "X/Y/Z " + FormatScaleNumber(x)',
    ):
        require(catalog, token, "Xref scale reader")

    # The new parity helpers stay presentation-only. Production mutations remain in their existing
    # handlers/services and are separately guarded above.
    forbidden = (
        "ProjectStateService.Save",
        "ProjectStateService.Load",
        "TransactionManager.StartTransaction",
        "SendStringToExecute",
        "File.WriteAll",
    )
    for source_name, source in (("workspace", workspace), ("right", right), ("icon", icon)):
        for token in forbidden:
            if token in source:
                fail(f"{source_name}: presentation-only parity layer must not mutate CAD/project via {token}")

    print("PASS: BLT3D BIM pixel parity, icons, Xref actions, scale display and grouped layer toggles are source-guarded.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
