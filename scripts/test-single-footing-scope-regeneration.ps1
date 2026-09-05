# Execute production scope routing with host-free presentation doubles.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$taskRoot = Split-Path $PSScriptRoot -Parent
$vmSource = Get-Content (Join-Path $taskRoot 'src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs') -Raw -Encoding UTF8
function Read-ScopeMember([string]$Source, [string]$Marker) {
    $start = $Source.IndexOf($Marker, [StringComparison]::Ordinal)
    if ($start -lt 0) { throw "Missing production member: $Marker" }
    $brace = $Source.IndexOf('{', $start); $depth = 0
    for ($i = $brace; $i -lt $Source.Length; $i++) {
        if ($Source[$i] -eq '{') { $depth++ }
        elseif ($Source[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $Source.Substring($start, $i - $start + 1) }
        }
    }
    throw "Unterminated production member: $Marker"
}
$scope = Read-ScopeMember $vmSource 'public string SelectedPropertyScope'
$load = Read-ScopeMember $vmSource 'private void LoadCurrentProperties()'
$show = Read-ScopeMember $vmSource 'public void ShowFamilyProperties()'
$register = Read-ScopeMember $vmSource 'internal void SetFamilyPropertyPresenter('
$fixture = @'
#nullable enable
using System;
public sealed class ScopeRoutingFixture {
    const string FamilyScope = "Family / Type", InstanceScope = "Đối tượng / Instance";
    string _selectedPropertyScope = InstanceScope, Status = "", Rendered = "";
    ProjectFamily? _selectedFamily = new ProjectFamily();
    object? _selectedElement = new object();
    Func<ProjectFamily, bool>? _familyPropertyPresenter;
    int presentations;
    internal sealed class ProjectFamily { public bool IsFooting = true; }
    void OnChanged(string name = "") {}
    void LoadFamilyProperties(ProjectFamily? family) { Rendered = family == null ? "empty" : "generic"; }
    void LoadInstanceProperties(object element, ProjectFamily family) { Rendered = "instance"; }
    bool Present(ProjectFamily family) {
        presentations++;
        if (!family.IsFooting) return false;
        Rendered = "six-mm-native-regeneration"; return true;
    }
// SCOPE
// LOAD
// SHOW
// REGISTER
    public static void Run() {
        var vm = new ScopeRoutingFixture();
        vm.SetFamilyPropertyPresenter(vm.Present);
        if (vm._familyPropertyPresenter == null || vm.Status.Length != 0) throw new Exception("Invalid test setup");
        vm.SelectedPropertyScope = FamilyScope;
        if (vm.Rendered != "six-mm-native-regeneration" || vm.presentations != 1)
            throw new Exception("Family scope discarded the specialized SingleFooting presenter");
        vm.SetFamilyPropertyPresenter(vm.Present);
        if (vm.presentations != 1) throw new Exception("Repeated registration re-rendered or accumulated callbacks");
        vm.SelectedPropertyScope = InstanceScope;
        if (vm.Rendered != "instance" || vm.presentations != 1) throw new Exception("Instance path changed");
        vm.ShowFamilyProperties();
        if (vm.SelectedPropertyScope != FamilyScope || vm.Rendered != "six-mm-native-regeneration") throw new Exception("Explicit family selection failed");
        vm._selectedFamily!.IsFooting = false;
        vm.LoadCurrentProperties();
        if (vm.Rendered != "generic") throw new Exception("Generic family fallback changed");
        vm._selectedElement = null;
        vm.SelectedPropertyScope = InstanceScope;
        if (vm.SelectedPropertyScope != FamilyScope || vm.Rendered != "generic") throw new Exception("Missing-instance refusal changed");
        vm._selectedFamily = null;
        vm.LoadCurrentProperties();
        if (vm.Rendered != "empty") throw new Exception("Empty family fallback changed");
        vm._selectedFamily = new ProjectFamily(); vm._familyPropertyPresenter = null;
        vm.LoadCurrentProperties();
        if (vm.Rendered != "generic") throw new Exception("Unregistered VM fallback changed");
        var replacement = new ScopeRoutingFixture(); replacement.SetFamilyPropertyPresenter(replacement.Present);
        replacement.ShowFamilyProperties();
        if (replacement.Rendered != "six-mm-native-regeneration") throw new Exception("Replacement VM registration failed");
    }
}
'@
Add-Type -TypeDefinition ($fixture.Replace('// SCOPE',$scope).Replace('// LOAD',$load).Replace('// SHOW',$show).Replace('// REGISTER',$register))
[ScopeRoutingFixture]::Run()
Write-Output 'PASS: production scope routing retains specialized Family presentation, generic fallback and Instance/missing-instance behavior.'

# Compile the actual specialized renderer, edit callback, dimensional contract
# and row setter. Only document/project storage and native regeneration are doubles.
function Read-ScopeCompilationUnit([string]$Path) {
    $text = Get-Content (Join-Path $taskRoot $Path) -Raw -Encoding UTF8
    return [regex]::Replace($text, '(?m)^using [^\r\n]+\r?\n', '')
}
$editor = (Read-ScopeCompilationUnit 'src/QS3D.BricsCAD.V25/UI/WorkspacePanel.SingleFooting.Properties.cs').Replace('partial class WorkspacePanel','partial class ScopeEditorFixture')
$contract = Read-ScopeCompilationUnit 'src/QS3D.BricsCAD.V25/SingleFootingContract.cs'
$geometry = Read-ScopeCompilationUnit 'src/QS3D.Core/Geometry/SingleFootingGeometry.cs'
$row = Read-ScopeCompilationUnit 'src/QS3D.BricsCAD.V25/UI/ViewModels/PropertyRowViewModel.cs'
$editorFixture = @'
#nullable enable
using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.BricsCAD.V25.UI.ViewModels;
using Application = ScopeHostApplication;
public static class ScopeHostApplication { public static ScopeDocuments DocumentManager = new ScopeDocuments(); }
public sealed class ScopeDocuments { public ScopeDocument? MdiActiveDocument; }
public sealed class ScopeDocument { public ProjectState Project = new ProjectState(); public bool Suppressed; }
namespace QS3D.Core.Domain {
    public enum ElementCategory { Foundation, Generic }
    public sealed class ProjectFamily {
        public string Id = "family", Name = "Móng đơn";
        public ElementCategory Category = ElementCategory.Foundation;
        public Dictionary<string,string> Properties = new Dictionary<string,string>();
    }
    public sealed class ProjectElement {
        public ElementCategory Category = ElementCategory.Foundation;
        public Dictionary<string,string> Properties = new Dictionary<string,string>();
    }
    public sealed class ProjectState {
        public ProjectFamily? Family;
        public bool Duplicate;
        public ProjectFamily? FindFamily(string id) {
            if (Duplicate) throw new InvalidOperationException("duplicate family");
            return Family != null && Family.Id == id ? Family : null;
        }
    }
}
namespace QS3D.BricsCAD.V25 {
    internal static class ExistingProjectMutationContext {
        public static ProjectState Require(ScopeDocument doc, string operation) {
            if (doc.Suppressed) throw new InvalidOperationException("context suppressed"); return doc.Project;
        }
    }
    internal static class SingleFootingRegenerationService {
        public static int Calls;
        public static int ApplyFamilyDimensions(ScopeDocument doc, ProjectState project, ProjectFamily family, SingleFootingDimensions dimensions) {
            Calls++; SingleFootingContract.Apply(family, dimensions); return 2;
        }
    }
}
namespace QS3D.BricsCAD.V25.UI {
    public partial class ScopeEditorFixture {
        private sealed class ViewModel { public List<PropertyRowViewModel> Properties = new List<PropertyRowViewModel>(); }
        private readonly ViewModel _viewModel = new ViewModel();
        public string Status = "";
        private void SetStatus(string status) { Status = status; }
        private static ProjectFamily Family() {
            var family = new ProjectFamily(); SingleFootingContract.Apply(family, new SingleFootingDimensions(2,2,1,1,1,0)); return family;
        }
        private PropertyRowViewModel H2() => _viewModel.Properties.Single(row => row.Name == "H2");
        public static void Run() {
            var panel = new ScopeEditorFixture(); var family = Family();
            var doc = new ScopeDocument(); doc.Project.Family = family; Application.DocumentManager.MdiActiveDocument = doc;
            if (!panel.TryShowSingleFootingFamilyProperties(family)) throw new Exception("Specialized family declined");
            var dimensions = panel._viewModel.Properties.Where(row => row.IsEditable).ToList();
            if (!dimensions.Select(row => row.Name).SequenceEqual(new[]{"L1","W1","L2","W2","H1","H2"}) ||
                dimensions.Any(row => row.Unit != "mm" || row.EditorKind != PropertyRowViewModel.TextEditor))
                throw new Exception("Expected exactly six editable mm text fields");
            if (SingleFootingRegenerationService.Calls != 0) throw new Exception("Rendering triggered native mutation");
            panel.H2().Value = "1000";
            if (SingleFootingRegenerationService.Calls != 1 || SingleFootingContract.Read(family).H2M != 1 || panel.H2().Value != "1000")
                throw new Exception("Physical row callback lost validated mm-to-native path");
            foreach (var invalid in new[]{"-1","NaN","Infinity","invalid"}) {
                panel.H2().Value = invalid;
                if (SingleFootingRegenerationService.Calls != 1 || panel.H2().Value != "1000") throw new Exception("Invalid H2 reached native mutation");
            }
            panel._viewModel.Properties.Single(row => row.Name == "L2").Value = "3000";
            if (SingleFootingRegenerationService.Calls != 1) throw new Exception("Invalid taper reached native mutation");
            var staleRow = panel.H2();
            Application.DocumentManager.MdiActiveDocument = null; staleRow.Value = "2000";
            if (SingleFootingRegenerationService.Calls != 1) throw new Exception("Missing document accepted");
            Application.DocumentManager.MdiActiveDocument = new ScopeDocument { Project = new ProjectState { Family = Family() } };
            staleRow.Value = "2000";
            if (SingleFootingRegenerationService.Calls != 1) throw new Exception("Stale same-ID family accepted on other document");
            Application.DocumentManager.MdiActiveDocument = doc; doc.Suppressed = true; staleRow.Value = "2000"; doc.Suppressed = false;
            if (SingleFootingRegenerationService.Calls != 1) throw new Exception("Suppressed mutation context accepted");
            doc.Project.Duplicate = true; staleRow.Value = "2000"; doc.Project.Duplicate = false;
            if (SingleFootingRegenerationService.Calls != 1) throw new Exception("Duplicate family accepted");
            var generic = new ProjectFamily { Category = ElementCategory.Generic, Name = "Generic" };
            if (panel.TryShowSingleFootingFamilyProperties(generic)) throw new Exception("Generic family was hijacked");
            var corrupt = Family(); corrupt.Properties.Remove(SingleFootingContract.H2Key); corrupt.Properties.Remove("SingleFootingH2M");
            if (!panel.TryShowSingleFootingFamilyProperties(corrupt) || panel._viewModel.Properties.Any(row => row.IsEditable) || panel._viewModel.Properties.Count == 0)
                throw new Exception("Malformed footing fell through to editable generic fields");
            if (SingleFootingRegenerationService.Calls != 1) throw new Exception("Presentation/refusal mutated native state");
        }
    }
}
'@
Add-Type -TypeDefinition ($editorFixture + "`n" + $editor + "`n" + $contract + "`n" + $geometry + "`n" + $row)
[QS3D.BricsCAD.V25.UI.ScopeEditorFixture]::Run()
Write-Output 'PASS: actual six-mm renderer/row setter invokes native-regeneration boundary; invalid dimensions, missing/stale/duplicate/suppressed contexts refuse; malformed footing remains read-only.'

$panelSource = Get-Content (Join-Path $taskRoot 'src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs') -Raw -Encoding UTF8
$bind = Read-ScopeMember $panelSource 'private void BindViewModel()'
$clear = Read-ScopeMember $panelSource 'public void ClearProject(string status)'
$constructor = Read-ScopeMember $panelSource 'public WorkspacePanel()'
if ($bind.IndexOf('SetFamilyPropertyPresenter(TryShowSingleFootingFamilyProperties)') -lt 0 -or
    $bind.IndexOf('DataContext = _viewModel;') -lt 0 -or
    $bind.IndexOf('SetFamilyPropertyPresenter(') -gt $bind.IndexOf('DataContext = _viewModel;') -or
    $clear.IndexOf('_viewModel = new WorkspaceViewModel();') -lt 0 -or
    $clear.IndexOf('BindViewModel();') -lt 0 -or $constructor.IndexOf('BindViewModel();') -lt 0 -or
    $clear.IndexOf('_viewModel = new WorkspaceViewModel();') -gt $clear.IndexOf('BindViewModel();')) {
    throw 'Presenter must be bound before initial/replacement ViewModel publication.'
}
Write-Output 'PASS: initial and replacement ViewModels register presenter before DataContext publication. This is host-free evidence, not licensed native PASS.'
