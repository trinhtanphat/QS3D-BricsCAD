# Executes the production routing methods with real WPF controls, without CAD.
$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') { throw 'Run powershell -STA.' }
$taskRoot = Split-Path $PSScriptRoot -Parent
function Read-Method([string]$File, [string]$Name) {
    $source = Get-Content -LiteralPath (Join-Path $taskRoot ('src/QS3D.BricsCAD.V25/UI/' + $File)) -Raw -Encoding UTF8
    $match = [regex]::Match($source, '(?m)^        private (?:static )?(?:void|bool) ' + $Name + '\(')
    if (-not $match.Success) { throw "Missing production method $Name" }
    $brace = $source.IndexOf('{', $match.Index)
    $depth = 0
    for ($i = $brace; $i -lt $source.Length; $i++) {
        if ($source[$i] -eq '{') { $depth++ }
        elseif ($source[$i] -eq '}') { $depth--; if ($depth -eq 0) { return $source.Substring($match.Index, $i - $match.Index + 1) } }
    }
    throw "Unterminated method $Name"
}
$methods = @(
    foreach ($name in @('RewireFamilyAddActions','IsWorkspaceAddFamilyButton')) { Read-Method 'WorkspacePanel.FamilySubtype.cs' $name }
    foreach ($name in @('RewireGridAwareFamilyAddActions','OnGridAwareFamilyAddModeClick')) { Read-Method 'WorkspacePanel.GridFamilySubtype.cs' $name }
    foreach ($name in @('RewireBlt3dFamilyAddActions','IsBlt3dFamilyAddButton','OnBlt3dFamilyAddClick','IsVisualDescendant','RenameBlt3dButton')) { Read-Method 'WorkspacePanel.Blt3dFamilyWorkspace.cs' $name }
    foreach ($name in @('RewireBlt3dRoomAwareAddActions','OnBlt3dRoomAwareAddClick')) { Read-Method 'WorkspacePanel.RoomWorkspacePane.cs' $name }
) -join "`n"
$fixture = @'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
public class AddRouteFixture : UserControl {
    ListBox FamilyList = new ListBox();
    string _familySubtypeFilter;
    string mode;
    List<string> calls = new List<string>();
    static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T:DependencyObject {
        for (int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++) {
            var child=VisualTreeHelper.GetChild(root,i);
            if(child is T) yield return (T)child;
            foreach(var nested in FindVisualChildren<T>(child)) yield return nested;
        }
    }
    static IEnumerable<T> RoomPaneDescendants<T>(DependencyObject root) where T:DependencyObject { return FindVisualChildren<T>(root); }
    static T FindNearestAncestor<T>(DependencyObject child) where T:DependencyObject {
        for(var p=VisualTreeHelper.GetParent(child);p!=null;p=VisualTreeHelper.GetParent(p)) if(p is T) return (T)p;
        return null;
    }
    bool IsSingleFootingSelected(){return mode=="SingleFooting";}
    bool IsBlt3dRoomWorkspace(){return mode=="Room";}
    bool IsGridSubtype(string value){return value=="GridStraight" || value=="GridCurved";}
    void OnAddClick(object sender,RoutedEventArgs e){calls.Add("Legacy");}
    void OnFamilyAddModeClick(object sender,RoutedEventArgs e){e.Handled=true;calls.Add("Generic");}
    void HandleSingleFootingAdd(RoutedEventArgs e){calls.Add("SingleFooting");}
    void ShowBlt3dFamilyModeChooser(){calls.Add("Generic");}
    void HideBlt3dFamilyModeChooser(){}
    void CreateGridFamilyFromWorkspaceSubtype(bool solid){calls.Add(_familySubtypeFilter);}
    void CreateRoomFromWorkspace(){calls.Add("Room");}
    void OnAddFinishClick(object sender,RoutedEventArgs e){calls.Add("Finish");}
    void Check(Action click,string expected,string label){
        calls.Clear(); click();
        if(calls.Count!=1 || calls[0]!=expected) throw new Exception(label+": expected "+expected+", actual "+string.Join(",",calls));
    }
    public static void Run(){
        foreach(var label in new[]{"+ Th\u00eam","+ Add","\uff0b  Add","Add"}) {
            var p=new AddRouteFixture(); var root=new StackPanel(); p.Content=root;
            var familyPane=new DockPanel(); var finishPane=new DockPanel();root.Children.Add(familyPane);root.Children.Add(finishPane);
            var add=new Button{Content=label};var finish=new Button{Content=label};
            var toolbar=new WrapPanel();toolbar.Children.Add(add);familyPane.Children.Add(toolbar);
            var wrapper=new Grid();wrapper.Children.Add(p.FamilyList);familyPane.Children.Add(wrapper);finishPane.Children.Add(finish);
            var menu=new ContextMenu();var item=new MenuItem{Header="Nh\u00e2n b\u1ea3n Family"};menu.Items.Add(item);p.FamilyList.ContextMenu=menu;
            add.Click+=p.OnAddClick; item.Click+=p.OnAddClick; finish.Click+=p.OnAddFinishClick;
            p.Measure(new Size(800,600));p.Arrange(new Rect(0,0,800,600));p.UpdateLayout();
            p.RewireFamilyAddActions();p.RewireGridAwareFamilyAddActions();p.RenameBlt3dButton(label,"+ Add");p.RewireBlt3dFamilyAddActions();
            if((string)finish.Content!=label) throw new Exception("Finish label was changed");
            p.mode=p._familySubtypeFilter="GridStraight";
            p.Check(()=>add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)),"GridStraight","BLT Grid button "+label);
            p.Check(()=>item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)),"GridStraight","BLT Grid menu "+label);
            for(int round=0;round<3;round++) {
                p.RewireBlt3dRoomAwareAddActions();
                foreach(var mode in new[]{"SingleFooting","GridStraight","GridCurved","Room","Generic"}) {
                    p.mode=p._familySubtypeFilter=mode;p.RewireBlt3dRoomAwareAddActions();
                    p.Check(()=>add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)),mode,"Family button "+mode);
                    p.Check(()=>item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)),mode,"Family menu "+mode);
                    p.Check(()=>finish.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)),"Finish","Finish isolation "+mode);
                }
            }
        }
    }
// PRODUCTION_METHODS
}
'@
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase
Add-Type -TypeDefinition ($fixture.Replace('// PRODUCTION_METHODS',$methods)) -ReferencedAssemblies PresentationFramework,PresentationCore,WindowsBase,System.Xaml,System.Core
[AddRouteFixture]::Run()
Write-Output 'PASS: production WPF Add routing, Grid pre-Room, repeated Room wiring, button/menu and finish isolation.'
