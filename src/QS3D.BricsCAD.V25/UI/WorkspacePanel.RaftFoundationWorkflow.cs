using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI.ViewModels;
using QS3D.Core.Domain;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private const string RaftLevelSelectionKey = "__RaftLevelSelection";
        private const string RaftColorModeKey = "ColorMode";
        private const string RaftTransparencyKey = "TransparencyPercent";
        private const string RaftMarkKey = "Mark";
        private const string RaftCommentKey = "Comment";
        private const string RaftWbsKey = "WBS";
        private const string RaftMaterialKey = "Material";
        private const string RaftMaterialTypeKey = "MaterialType";

        private static readonly string[] RaftElevationChoices =
        {
            RaftFoundationPropertySet.BottomLevelMode,
            RaftFoundationPropertySet.TopLevelMode
        };

        private static readonly string[] RaftColorModeChoices = { "ByLayer", "Theo loại (mặc định)", "Tùy chỉnh" };
        private static readonly string[] RaftTransparencyChoices = { "0", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100" };
        private static readonly string[] RaftMaterialChoices = { "Bê tông" };
        private static readonly string[] RaftMaterialTypeChoices = { "Bê tông" };
        private static readonly bool _raftFoundationWorkspaceHandlersRegistered = RegisterRaftFoundationWorkspaceHandlers();

        private static bool RegisterRaftFoundationWorkspaceHandlers()
        {
            // Visible + Add routing is owned exclusively by WorkspacePanel.RaftFoundationVisibleAddRoute.cs.
            // This legacy class handler now owns only the Móng Bè Draw command. Family property rendering is
            // owned by OnFamilySubtypeFamilySelectionChanged in the primary Workspace render path.
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(OnRaftFoundationWorkspaceButtonClick),
                true);
            return true;
        }

        private static void OnRaftFoundationWorkspaceButtonClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var panel = button == null ? null : FindRaftWorkspacePanel(button);
            if (panel == null || button == null) return;

            if (!string.Equals(button.Content as string, "Vẽ 3D", StringComparison.Ordinal) ||
                !panel.IsRaftWorkspaceContext()) return;

            e.Handled = true;
            var family = panel.FamilyList.SelectedItem as ProjectFamily;
            if (family == null || !RaftFoundationPropertySet.IsRaftFamily(family))
            {
                panel.SetStatus("Chọn Family Móng Bè trước khi Vẽ.");
                return;
            }

            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active.");
                var project = ExistingProjectMutationContext.Require(doc, "Vẽ Móng Bè");
                var owned = project.FindFamily(family.Id);
                if (owned == null || !RaftFoundationPropertySet.IsRaftFamily(owned))
                    throw new InvalidOperationException("Family Móng Bè đang chọn đã stale.");
                var placement = RaftFoundationLevelPlacement.Resolve(project, owned);
                panel._viewModel.SetActiveFamily(owned);
                panel.SetStatus(
                    "Móng Bè: " + RaftElevationMode(owned) + " • Z đáy=" +
                    placement.BottomElevationM.ToString("0.###", CultureInfo.InvariantCulture) + " m • pick closed Polyline/Region.");
                panel.Send("QS3DDRAWRAFTFOUNDATION");
            }
            catch (Exception ex)
            {
                panel.SetStatus("Không thể bắt đầu Vẽ Móng Bè: " + ex.Message);
            }
        }

        private static WorkspacePanel? FindRaftWorkspacePanel(DependencyObject source)
        {
            DependencyObject? current = source;
            while (current != null)
            {
                if (current is WorkspacePanel panel) return panel;
                current = ParentOf(current);
            }
            return null;
        }

        private bool IsRaftSubtypeFilter() =>
            string.Equals(_familySubtypeFilter, RaftFoundationPropertySet.SubtypeName, StringComparison.OrdinalIgnoreCase);

        private bool IsRaftWorkspaceContext()
        {
            if (IsRaftSubtypeFilter()) return true;
            return FamilyList.SelectedItem is ProjectFamily family && RaftFoundationPropertySet.IsRaftFamily(family);
        }

        private void ApplyRaftFoundationPropertyForm(ProjectFamily family)
        {
            if (family == null || !RaftFoundationPropertySet.IsRaftFamily(family) ||
                !string.Equals(_viewModel.SelectedPropertyScope, WorkspaceViewModel.FamilyScope, StringComparison.Ordinal)) return;

            var familyNameRow = _viewModel.Properties.FirstOrDefault(x =>
                string.Equals(x.Name, "Tên Family", StringComparison.CurrentCultureIgnoreCase));

            _viewModel.Properties.Clear();
            if (familyNameRow != null)
            {
                familyNameRow.Group = "Information";
                _viewModel.Properties.Add(familyNameRow);
            }
            else
            {
                var fallbackName = new PropertyRowViewModel { Group = "Information", Name = "Tên Family", IsReadOnly = true };
                fallbackName.Value = family.Name;
                _viewModel.Properties.Add(fallbackName);
            }

            var categoryRow = new PropertyRowViewModel { Group = "Information", Name = "Loại cấu kiện", IsReadOnly = true };
            categoryRow.Value = RaftFoundationPropertySet.SubtypeName;
            _viewModel.Properties.Add(categoryRow);

            var floorRow = new PropertyRowViewModel { Group = "Information", Name = "Tầng", IsReadOnly = true };
            floorRow.Value = FloorCombo?.SelectedItem as string ?? string.Empty;
            _viewModel.Properties.Add(floorRow);

            var project = TryRaftProject();
            var levelChoices = project == null
                ? Array.Empty<string>()
                : project.Floors.OrderBy(x => x.ElevationM).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).Select(x => x.Name).ToArray();

            AddRaftFamilyPropertyRow(family, "Kích thước", "Dày", RaftFoundationPropertySet.ThicknessKey, RaftThicknessUi(family), Array.Empty<string>(), "mm");
            AddRaftFamilyPropertyRow(family, "Cao độ", "Cách đặt", RaftFoundationPropertySet.ElevationModeKey, RaftElevationMode(family), RaftElevationChoices);
            AddRaftFamilyPropertyRow(family, "Cao độ", "Cao độ đầu", RaftLevelSelectionKey, RaftLevelUi(family), levelChoices);
            AddRaftFamilyPropertyRow(family, "Display", "Màu sắc", RaftColorModeKey, RaftValue(family, RaftColorModeKey, "ByLayer"), RaftColorModeChoices);
            AddRaftFamilyPropertyRow(family, "Display", "Độ trong suốt", RaftTransparencyKey, RaftValue(family, RaftTransparencyKey, "0"), RaftTransparencyChoices, "%");
            AddRaftFamilyPropertyRow(family, "Metadata", "Mark", RaftMarkKey, RaftValue(family, RaftMarkKey, string.Empty), Array.Empty<string>());
            AddRaftFamilyPropertyRow(family, "Metadata", "Comment", RaftCommentKey, RaftValue(family, RaftCommentKey, string.Empty), Array.Empty<string>());
            AddRaftFamilyPropertyRow(family, "Metadata", "WBS", RaftWbsKey, RaftValue(family, RaftWbsKey, string.Empty), Array.Empty<string>());
            AddRaftFamilyPropertyRow(family, "Vật liệu", "Vật liệu", RaftMaterialKey, RaftValue(family, RaftMaterialKey, "Bê tông"), RaftMaterialChoices);
            AddRaftFamilyPropertyRow(family, "Vật liệu", "Loại vật liệu", RaftMaterialTypeKey, RaftValue(family, RaftMaterialTypeKey, "Bê tông"), RaftMaterialTypeChoices);
        }

        private void AddRaftFamilyPropertyRow(
            ProjectFamily family,
            string group,
            string label,
            string key,
            string current,
            IReadOnlyList<string> choices,
            string unit = "")
        {
            var rowChoices = choices
                .Concat(new[] { current })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            var row = new PropertyRowViewModel
            {
                Group = group,
                Name = label,
                Unit = unit,
                EditorKind = rowChoices.Length > 0 ? PropertyRowViewModel.ChoiceEditor : PropertyRowViewModel.TextEditor,
                Choices = rowChoices
            };
            row.Value = current;
            row.Apply = value => ApplyRaftFamilyProperty(family, key, value);
            _viewModel.Properties.Add(row);
        }

        private string ApplyRaftFamilyProperty(ProjectFamily family, string key, string requested)
        {
            var previous = RaftPropertyUiValue(family, key);
            try
            {
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active.");
                var project = ExistingProjectMutationContext.Require(doc, "Cập nhật thuộc tính Family Móng Bè");
                var owned = project.FindFamily(family.Id);
                if (owned == null || !ReferenceEquals(owned, family) || !RaftFoundationPropertySet.IsRaftFamily(owned))
                    throw new InvalidOperationException("Family Móng Bè đang chọn đã stale hoặc không thuộc project hiện tại.");

                var result = ExecuteAtomic(project, () =>
                {
                    var aggregate = ProjectFamilyService.SetProperty(
                        project, owned.Id, RaftFoundationPropertySet.WorkspaceSubtypeKey, RaftFoundationPropertySet.SubtypeName);

                    if (string.Equals(key, RaftLevelSelectionKey, StringComparison.Ordinal))
                    {
                        var floorName = (requested ?? string.Empty).Trim();
                        var matches = project.Floors.Where(x => string.Equals(x.Name, floorName, StringComparison.CurrentCultureIgnoreCase)).ToList();
                        if (matches.Count != 1)
                            throw new InvalidOperationException("Cao độ đầu phải chọn đúng một Level hiện có trong project.");
                        var mode = RaftElevationMode(owned);
                        MergeRaftMutation(aggregate, ProjectFamilyService.SetProperty(project, owned.Id, RaftFoundationPropertySet.ActiveLevelKey(mode), matches[0].Id));
                        MergeRaftMutation(aggregate, ProjectFamilyService.SetProperty(project, owned.Id, RaftFoundationPropertySet.OppositeLevelKey(mode), string.Empty));
                    }
                    else if (string.Equals(key, RaftFoundationPropertySet.ElevationModeKey, StringComparison.Ordinal))
                    {
                        var nextMode = RaftFoundationPropertySet.NormalizeElevationMode(requested);
                        var oldMode = RaftElevationMode(owned);
                        var levelId = RaftValue(owned, RaftFoundationPropertySet.ActiveLevelKey(oldMode), string.Empty);
                        if (levelId.Length == 0) levelId = RaftValue(owned, RaftFoundationPropertySet.OppositeLevelKey(oldMode), string.Empty);
                        if (levelId.Length == 0) levelId = (project.ActiveFloorId ?? string.Empty).Trim();
                        if (levelId.Length == 0 || project.FindFloor(levelId) == null)
                            throw new InvalidOperationException("Cách đặt cần một Cao độ đầu hợp lệ.");
                        MergeRaftMutation(aggregate, ProjectFamilyService.SetProperty(project, owned.Id, key, nextMode));
                        MergeRaftMutation(aggregate, ProjectFamilyService.SetProperty(project, owned.Id, RaftFoundationPropertySet.ActiveLevelKey(nextMode), levelId));
                        MergeRaftMutation(aggregate, ProjectFamilyService.SetProperty(project, owned.Id, RaftFoundationPropertySet.OppositeLevelKey(nextMode), string.Empty));
                    }
                    else
                    {
                        var nextStored = NormalizeRaftPropertyForStorage(key, requested);
                        MergeRaftMutation(aggregate, ProjectFamilyService.SetProperty(project, owned.Id, key, nextStored));
                    }

                    RaftFoundationLevelPlacement.EnsureDefaults(project, owned);
                    RaftFoundationLevelPlacement.Resolve(project, owned);
                    return aggregate;
                }, "Cập nhật thuộc tính Family Móng Bè");

                ApplyRaftFoundationPropertyForm(owned);
                var live = RaftPropertyUiValue(owned, key);
                SetStatus("Đã cập nhật " + RaftPropertyLabel(key) + " • kế thừa " + result.InheritedInstancesUpdated + " cấu kiện" +
                          (result.OverridesPreserved > 0 ? " • giữ " + result.OverridesPreserved + " instance override" : string.Empty));
                return live;
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException || ex is OverflowException)
            {
                SetStatus("Không thể cập nhật " + RaftPropertyLabel(key) + ": " + ex.Message);
                return previous;
            }
        }

        private static void MergeRaftMutation(ProjectFamilyPropertyMutationResult aggregate, ProjectFamilyPropertyMutationResult next)
        {
            aggregate.InheritedInstancesUpdated += next.InheritedInstancesUpdated;
            aggregate.OverridesPreserved += next.OverridesPreserved;
        }

        private static string NormalizeRaftPropertyForStorage(string key, string requested)
        {
            var value = (requested ?? string.Empty).Trim();
            if (string.Equals(key, RaftFoundationPropertySet.ThicknessKey, StringComparison.Ordinal))
            {
                var meters = ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters("Dày", value, CultureInfo.CurrentCulture, true);
                return meters.ToString("R", CultureInfo.InvariantCulture);
            }
            if (string.Equals(key, RaftTransparencyKey, StringComparison.Ordinal))
            {
                if ((!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) &&
                     !double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out percent)) ||
                    double.IsNaN(percent) || double.IsInfinity(percent) || percent < 0d || percent > 100d)
                    throw new InvalidOperationException("Độ trong suốt chỉ nhận giá trị từ 0% đến 100%.");
                return percent.ToString("0.##", CultureInfo.InvariantCulture);
            }
            if ((string.Equals(key, RaftColorModeKey, StringComparison.Ordinal) ||
                 string.Equals(key, RaftMaterialKey, StringComparison.Ordinal) ||
                 string.Equals(key, RaftMaterialTypeKey, StringComparison.Ordinal)) && value.Length == 0)
                throw new InvalidOperationException(RaftPropertyLabel(key) + " không được để trống.");
            return value;
        }

        private string RaftPropertyUiValue(ProjectFamily family, string key)
        {
            if (string.Equals(key, RaftFoundationPropertySet.ThicknessKey, StringComparison.Ordinal)) return RaftThicknessUi(family);
            if (string.Equals(key, RaftFoundationPropertySet.ElevationModeKey, StringComparison.Ordinal)) return RaftElevationMode(family);
            if (string.Equals(key, RaftLevelSelectionKey, StringComparison.Ordinal)) return RaftLevelUi(family);
            return RaftValue(family, key, string.Empty);
        }

        private static string RaftThicknessUi(ProjectFamily family)
        {
            var stored = RaftValue(family, RaftFoundationPropertySet.ThicknessKey, "0.5");
            return ProjectFamilyQuickSchemaService.FormatInternalMetersAsMillimeters("Dày", stored, CultureInfo.CurrentCulture);
        }

        private static string RaftElevationMode(ProjectFamily family)
        {
            var raw = RaftValue(family, RaftFoundationPropertySet.ElevationModeKey, RaftFoundationPropertySet.BottomLevelMode);
            return RaftFoundationPropertySet.NormalizeElevationMode(raw);
        }

        private string RaftLevelUi(ProjectFamily family)
        {
            var project = TryRaftProject();
            if (project == null) return string.Empty;
            var levelId = RaftValue(family, RaftFoundationPropertySet.ActiveLevelKey(RaftElevationMode(family)), string.Empty);
            return project.FindFloor(levelId)?.Name ?? string.Empty;
        }

        private static string RaftValue(ProjectFamily family, string key, string fallback) =>
            family.Properties.TryGetValue(key, out var stored) ? (stored ?? string.Empty).Trim() : fallback;

        private ProjectState? TryRaftProject()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return null;
            return ProjectContextCoordinator.TryGetReadOnly(doc, out var project) ? project : null;
        }

        private static string RaftPropertyLabel(string key)
        {
            switch (key)
            {
                case RaftFoundationPropertySet.ThicknessKey: return "Dày";
                case RaftFoundationPropertySet.ElevationModeKey: return "Cách đặt";
                case RaftLevelSelectionKey: return "Cao độ đầu";
                case RaftColorModeKey: return "Màu sắc";
                case RaftTransparencyKey: return "Độ trong suốt";
                case RaftMarkKey: return "Mark";
                case RaftCommentKey: return "Comment";
                case RaftWbsKey: return "WBS";
                case RaftMaterialKey: return "Vật liệu";
                case RaftMaterialTypeKey: return "Loại vật liệu";
                default: return key;
            }
        }
    }
}
