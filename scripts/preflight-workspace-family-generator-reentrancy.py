#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FAMILY_SUBTYPE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.FamilySubtype.cs"
WORKSPACE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml.cs"
WORKSPACE_XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.xaml"


def fail(message: str) -> None:
    print(f"FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


family_text = FAMILY_SUBTYPE.read_text(encoding="utf-8")
workspace_text = WORKSPACE.read_text(encoding="utf-8")
workspace_xaml = WORKSPACE_XAML.read_text(encoding="utf-8")

if "using System.Windows.Threading;" not in family_text:
    fail("Workspace Family reveal must use the WPF Dispatcher deferral path.")

refresh_marker = "private void RefreshSelectedFamilyHighlight()"
refresh_start = family_text.find(refresh_marker)
if refresh_start < 0:
    fail("RefreshSelectedFamilyHighlight() was not found.")
refresh_method = family_text[refresh_start:refresh_start + 1800]

pending = refresh_method.find("_familyHighlightRefreshPending")
deferred = refresh_method.find("Dispatcher.BeginInvoke(DispatcherPriority.Loaded")
reveal_call = refresh_method.find("RevealSelectedFamilyAndRefreshHighlight();")
if pending < 0:
    fail("Selected-family reveal requests must be coalesced before dispatcher deferral.")
if deferred < 0 or reveal_call < 0 or deferred > reveal_call:
    fail("Selected-family reveal must be deferred through DispatcherPriority.Loaded.")

reveal_marker = "private void RevealSelectedFamilyAndRefreshHighlight()"
reveal_start = family_text.find(reveal_marker)
if reveal_start < 0:
    fail("RevealSelectedFamilyAndRefreshHighlight() was not found.")
reveal_method = family_text[reveal_start:reveal_start + 1200]
guard = reveal_method.find("GeneratorStatus.GeneratingContainers")
scroll = reveal_method.find("FamilyList.ScrollIntoView(selected);")
if guard < 0:
    fail("Deferred FamilyList reveal must still guard active container generation.")
if scroll < 0 or not (guard < scroll):
    fail("Generator guard must run before the centralized FamilyList.ScrollIntoView call.")
if "DispatcherPriority.ContextIdle" in reveal_method:
    fail("Selected-family reveal must not schedule a second visual-styling dispatcher turn.")

if "FamilyList.UpdateLayout(" in family_text:
    fail("FamilyList reveal must not force synchronous UpdateLayout during generator-sensitive refresh.")
if family_text.count("FamilyList.ScrollIntoView(") != 1:
    fail("FamilyList scrolling must stay centralized in the deferred reveal helper.")
if "FamilyList.ScrollIntoView(" in workspace_text:
    fail("WorkspacePanel.xaml.cs must not bypass the generator-safe FamilyList reveal path.")

for forbidden in (
    "_lastHighlightedFamilyItem",
    "ApplySelectedFamilyHighlight(",
    "ClearValue(Control.BackgroundProperty)",
    "ClearValue(Control.ForegroundProperty)",
    "ClearValue(Control.FontWeightProperty)",
    "ClearValue(UIElement.OpacityProperty)",
    "container.Background =",
    "container.Foreground =",
    "container.FontWeight =",
    "container.Opacity =",
    "ContainerFromItem(selected) as ListBoxItem",
):
    if forbidden in family_text:
        fail("Selected-family visual state must be owned by WPF ItemContainerStyle, not C# mutation: " + forbidden)

style_start = workspace_xaml.find("<ListBox.ItemContainerStyle>")
style_end = workspace_xaml.find("</ListBox.ItemContainerStyle>", style_start)
if style_start < 0 or style_end < 0:
    fail("FamilyList must define an ItemContainerStyle for stable WPF-managed selection visuals.")
family_style = workspace_xaml[style_start:style_end]
if '<Trigger Property="IsSelected" Value="True">' not in family_style:
    fail("FamilyList ItemContainerStyle must render selection from ListBoxItem.IsSelected.")
if '<Setter Property="Background" Value="{StaticResource AccentBrush}"/>' not in family_style:
    fail("Selected Family background must be stable AccentBrush styling.")
if '<Setter Property="Foreground" Value="White"/>' not in family_style:
    fail("Selected Family foreground must be stable white styling.")
if "FontWeight" in family_style:
    fail("FamilyList ItemContainerStyle must not toggle FontWeight; descendant typography must remain stable.")

sync_marker = "private void SyncFamilyFromSelection()"
sync_start = workspace_text.find(sync_marker)
if sync_start < 0:
    fail("SyncFamilyFromSelection() was not found.")
sync_method = workspace_text[sync_start:sync_start + 3200]
selected = sync_method.find("FamilyList.SelectedItem = family;")
refresh = sync_method.find("RefreshSelectedFamilyHighlight();")
if selected < 0 or refresh < 0 or selected > refresh:
    fail("Selection sync must route selected-family reveal through RefreshSelectedFamilyHighlight().")

print("PASS: Workspace family reveal stays generator-safe and selection visuals are WPF-managed without typography churn.")
