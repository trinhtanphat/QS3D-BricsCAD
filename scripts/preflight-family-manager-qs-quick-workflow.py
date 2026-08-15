#!/usr/bin/env python3
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FamilyManagerWindow.xaml"
CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FamilyManagerWindow.QuickWorkflow.cs"
PREFIX = "[preflight-family-manager-qs-quick-workflow]"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(f"{PREFIX} ERROR: {message}")


def main() -> None:
    xaml = XAML.read_text(encoding="utf-8")
    code = CODE.read_text(encoding="utf-8")

    try:
        ET.fromstring(xaml)
    except ET.ParseError as exc:
        raise SystemExit(f"{PREFIX} ERROR: FamilyManagerWindow.xaml is not valid XML: {exc}")

    xaml_contracts = {
        "quick workflow card": 'x:Name="QsQuickWorkflowCard"',
        "post-load race guard hook": 'ContentRendered="OnQuickWorkflowContentRendered"',
        "WidthM editor": 'x:Name="QuickWidthBox"',
        "DepthM editor": 'x:Name="QuickDepthBox"',
        "HeightM editor": 'x:Name="QuickHeightBox"',
        "ThicknessM editor": 'x:Name="QuickThicknessBox"',
        "BottomOffsetM editor": 'x:Name="QuickBottomOffsetBox"',
        "Auto Family action": 'Content="Auto Family" Click="OnAutoFamilyClick"',
        "create-and-use action": 'Click="OnCreateAndUseClick"',
        "save-and-draw action": 'Click="OnSaveAndDrawClick"',
        "existing raw property editor": 'x:Name="PropertyList"',
        "existing assignment action": 'Click="OnAssignClick"',
    }
    for label, token in xaml_contracts.items():
        require(token in xaml, f"missing {label}: {token}")

    code_contracts = {
        "canonical CAD project-context namespace": 'using QS3D.BricsCAD.V25.Cad;',
        "late selection event hookup": 'FamilyList.SelectionChanged += OnQuickFamilySelectionChanged;',
        "selection handler loading guard": 'if (_loading) return;',
        "selected Family exits draft mode": 'if (FamilyList.SelectedItem != null) _creatingNew = false;',
        "selection refresh": 'RefreshQuickWorkflow();',
        "category-aware schema": 'ProjectFamilyQuickSchemaService.GetSchema(category)',
        "Direct Draw width key": '"WidthM"',
        "Direct Draw depth key": '"DepthM"',
        "Direct Draw height key": '"HeightM"',
        "Direct Draw thickness key": '"ThicknessM"',
        "Direct Draw offset key": '"BottomOffsetM"',
        "atomic Family mutation": 'var family = ExecuteAtomic(project, () =>',
        "current audited Family property helper": 'SetQuickPropertyWithAudit(project, target, pair.Key, pair.Value, auditSource);',
        "canonical Family property service": 'var update = ProjectFamilyService.SetProperty(project, target.Id, key, value);',
        "no-op aware property audit": 'if (project.ChangeVersion == beforeVersion) return;',
        "canonical active Family service": 'ProjectFamilyActivationService.SetActive(project, target.Id);',
        "canonical draw support predicate": 'ActiveFamilyQuickDrawCommands.SupportsFamily(routeProbe)',
        "post-modal canonical draw dispatch": '_document.SendStringToExecute("QS3DDRAWACTIVE ", true, false, false);',
        "close before draw dispatch": 'Close();',
        "positive dimension validation": 'if (positive && value <= 0d)',
        "finite value validation": 'double.IsNaN(value) || double.IsInfinity(value)',
    }
    for label, token in code_contracts.items():
        require(token in code, f"missing {label}: {token}")

    require('_creatingNew = FamilyList.SelectedItem == null;' not in code,
            "quick selection handler must not infer draft mode from a transient null selection")

    defaults = {
        "Wall thickness": 'ThicknessM = 0.2d, HeightM = 3.6d',
        "Beam": 'WidthM = 0.3d, HeightM = 0.5d',
        "Column": 'WidthM = 0.4d, DepthM = 0.4d, HeightM = 3.6d',
        "Slab": 'ThicknessM = 0.12d, BottomOffsetM = 0d',
        "GlassWall": 'ThicknessM = 0.012d, HeightM = 3.6d',
        "Foundation": 'ThicknessM = 0.5d, BottomOffsetM = 0d',
    }
    for label, token in defaults.items():
        require(token in code, f"quick default diverged from Direct Draw for {label}: {token}")

    close_index = code.find('Close();')
    dispatch_index = code.find('_document.SendStringToExecute("QS3DDRAWACTIVE ", true, false, false);')
    require(0 <= close_index < dispatch_index, "Family Manager must close before queuing QS3DDRAWACTIVE")

    for forbidden in ("StructuralSolidBuilder", "BuildSelected(", "new Solid3d", "QsdbProjectStore"):
        require(forbidden not in code, f"quick Family workflow must not introduce parallel CAD/persistence machinery: {forbidden}")

    require(code.count('ProjectFamilyActivationService.SetActive(project, target.Id);') == 1,
            "quick Family commit must have one canonical active-Family mutation point")
    require(code.count('_document.SendStringToExecute("QS3DDRAWACTIVE ", true, false, false);') == 1,
            "Lưu & Vẽ must queue exactly one canonical QS3DDRAWACTIVE command")

    print(f"{PREFIX} PASS: Family Manager QS quick workflow uses intentional draft state, audited canonical property mutation, one active-Family mutation point, and the existing Direct Draw route.")


if __name__ == "__main__":
    main()
