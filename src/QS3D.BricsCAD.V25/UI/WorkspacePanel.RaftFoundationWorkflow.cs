using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI.ViewModels;
using QS3D.Core.Domain;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private const string RaftColorModeKey = "ColorMode";
        private const string RaftTransparencyKey = "TransparencyPercent";
        private const string RaftMarkKey = "Mark";
        private const string RaftCommentKey = "Comment";
        private const string RaftWbsKey = "WBS";
        private const string RaftMaterialKey = "Material";

        private static readonly string[] RaftElevationChoices =
        {
            RaftFoundationPropertySet.BottomLevelMode,
            RaftFoundationPropertySet.TopLevelMode
        };

        private static readonly string[] RaftColorModeChoices = { "Theo loại (mặc định)", "Tùy chỉnh" };
        private static readonly string[] RaftTransparencyChoices = { "0", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100" };
        private static readonly bool _raftFoundationWorkspaceHandlersRegistered = RegisterRaftFoundationWorkspaceHandlers();

        private static bool RegisterRaftFoundationWorkspaceHandlers()
        {
            // Button class handling runs before the legacy per-button Click handlers. This is what
            // makes Móng Bè a direct Add/Draw workflow without briefly opening the old mode menu or
            // dispatching QS3DBUILD3D against an empty selection first.
            EventManager.RegisterClassHandler(
                typeof(Button),
                Button.ClickEvent,
                new RoutedEventHandler(OnRaftFoundationWorkspaceButtonClick),
                true);
            EventManager.RegisterClassHandler(
                typeof(WorkspacePanel),
                Selector.SelectionChangedEvent,
                new SelectionChangedEventHandler(OnRaftFoundationWorkspaceSelectionChanged),
                true);
            return true;
        }

        private static void OnRaftFoundationWorkspaceButtonClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var panel = button == null ? null : FindRaftWorkspacePanel(button);
            if (panel == null || button == null) return;

            if (IsWorkspaceAddFamilyButton(button) && panel.IsRaftSubtypeFilter())
            {
                e.Handled = true;
                panel.CreateFamilyFromWorkspaceSubtype(false);
                return;
            }

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
                panel._viewModel.SetActiveFamily(family);
                panel.SetStatus("Móng Bè: pick closed Polyline/Region để tạo bê tông 3D theo " +
                                RaftElevationMode(family) + ".");
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

        private static void OnRaftFoundationWorkspaceSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var panel = sender as WorkspacePanel;
            if (panel == null || panel._loadingContext) return;

            if (ReferenceEquals(e.Source, panel.FamilyList))
            {
                var family = panel.FamilyList.SelectedItem as ProjectFamily;
                if (family != null && RaftFoundationPropertySet.IsRaftFamily(family))
                    panel.ApplyRaftFoundationPropertyForm(family);
                return;
            }

            if (ReferenceEquals(e.Source, panel.FloorCombo) &&
                panel.FamilyList.SelectedItem is ProjectFamily selected &&
                RaftFoundationPropertySet.IsRaftFamily(selected))
            {
                panel.ApplyRaftFoundationPropertyForm(selected);
            }
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
                var fallbackName = new PropertyRowViewModel
                {
                    Group = "Information",
                    Name = "Tên Family",
                    IsReadOnly = true
                };
                fallbackName.Value = family.Name;
                _viewModel.Properties.Add(fallbackName);
            }

            var categoryRow = new PropertyRowViewModel
            {
                Group = "Information",
                Name = "Loại cấu kiện",
                IsReadOnly = true
            };
            categoryRow.Value = RaftFoundationPropertySet.SubtypeName;
            _viewModel.Properties.Add(categoryRow);

            var floorRow = new PropertyRowViewModel
            {
                Group = "Information",
                Name = "Tầng",
                IsReadOnly = true
            };
            floorRow.Value = FloorCombo?.SelectedItem as string ?? string.Empty;
            _viewModel.Properties.Add(floorRow);

            AddRaftFamilyPropertyRow(family, "Kích thước", "Dày", "ThicknessM", RaftThicknessUi(family), Array.Empty<string>(), "mm");
            AddRaftFamilyPropertyRow(
                family,
                "Cao độ",
                "Cao độ",
                RaftFoundationPropertySet.ElevationModeKey,
                RaftElevationMode(family),
                RaftElevationChoices);
            AddRaftFamilyPropertyRow(family, "Display", "Màu sắc", RaftColorModeKey, RaftValue(family, RaftColorModeKey, "Theo loại (mặc định)"), RaftColorModeChoices);
            AddRaftFamilyPropertyRow(family, "Display", "Độ trong suốt", RaftTransparencyKey, RaftValue(family, RaftTransparencyKey, "0"), RaftTransparencyChoices, "%");
            AddRaftFamilyPropertyRow(family, "Metadata", "Mark", RaftMarkKey, RaftValue(family, RaftMarkKey, string.Empty), Array.Empty<string>());
            AddRaftFamilyPropertyRow(family, "Metadata", "Comment", RaftCommentKey, RaftValue(family, RaftCommentKey, string.Empty), Array.Empty<string>());
            AddRaftFamilyPropertyRow(family, "Metadata", "WBS", RaftWbsKey, RaftValue(family, RaftWbsKey, string.Empty), Array.Empty<string>());
            AddRaftFamilyPropertyRow(family, "Metadata", "Vật liệu", RaftMaterialKey, RaftValue(family, RaftMaterialKey, "Bê tông"), new[] { "Bê tông" });
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
                var nextStored = NormalizeRaftPropertyForStorage(key, requested);
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null) throw new InvalidOperationException("Không có bản vẽ BricsCAD đang active.");
                var project = ExistingProjectMutationContext.Require(doc, "Cập nhật thuộc tính Family Móng Bè");
                var owned = project.FindFamily(family.Id);
                if (owned == null || !ReferenceEquals(owned, family) || !RaftFoundationPropertySet.IsRaftFamily(owned))
                    throw new InvalidOperationException("Family Móng Bè đang chọn đã stale hoặc không thuộc project hiện tại.");

                var result = ExecuteAtomic(project, () =>
                {
                    var aggregate = ProjectFamilyService.SetProperty(project, owned.Id, key, nextStored);
                    ProjectFamilyService.SetProperty(project, owned.Id, RaftFoundationPropertySet.WorkspaceSubtypeKey, RaftFoundationPropertySet.SubtypeName);
                    if (string.Equals(key, "ThicknessM", StringComparison.Ordinal) ||
                        string.Equals(key, RaftFoundationPropertySet.ElevationModeKey, StringComparison.Ordinal))
                    {
                        var thicknessM = RaftThicknessM(owned);
                        var mode = string.Equals(key, RaftFoundationPropertySet.ElevationModeKey, StringComparison.Ordinal)
                            ? RaftFoundationPropertySet.NormalizeElevationMode(nextStored)
                            : RaftElevationMode(owned);
                        var offsetM = RaftFoundationPropertySet.ResolveBottomOffsetM(mode, thicknessM);
                        var offsetResult = ProjectFamilyService.SetProperty(
                            project,
                            owned.Id,
                            "BottomOffsetM",
                            offsetM.ToString("R", CultureInfo.InvariantCulture));
                        aggregate.InheritedInstancesUpdated += offsetResult.InheritedInstancesUpdated;
                        aggregate.OverridesPreserved += offsetResult.OverridesPreserved;
                    }
                    return aggregate;
                }, "Cập nhật thuộc tính Family Móng Bè");

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

        private static string NormalizeRaftPropertyForStorage(string key, string requested)
        {
            var value = (requested ?? string.Empty).Trim();
            if (string.Equals(key, "ThicknessM", StringComparison.Ordinal))
            {
                var meters = ProjectFamilyQuickSchemaService.ParseUiMillimetersToMeters("Dày", value, CultureInfo.CurrentCulture, true);
                return meters.ToString("R", CultureInfo.InvariantCulture);
            }
            if (string.Equals(key, RaftFoundationPropertySet.ElevationModeKey, StringComparison.Ordinal))
                return RaftFoundationPropertySet.NormalizeElevationMode(value);
            if (string.Equals(key, RaftTransparencyKey, StringComparison.Ordinal))
            {
                if ((!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) &&
                     !double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out percent)) ||
                    double.IsNaN(percent) || double.IsInfinity(percent) || percent < 0d || percent > 100d)
                    throw new InvalidOperationException("Độ trong suốt chỉ nhận giá trị từ 0% đến 100%.");
                return percent.ToString("0.##", CultureInfo.InvariantCulture);
            }
            if ((string.Equals(key, RaftColorModeKey, StringComparison.Ordinal) ||
                 string.Equals(key, RaftMaterialKey, StringComparison.Ordinal)) && value.Length == 0)
                throw new InvalidOperationException(RaftPropertyLabel(key) + " không được để trống.");
            return value;
        }

        private static string RaftPropertyUiValue(ProjectFamily family, string key)
        {
            if (string.Equals(key, "ThicknessM", StringComparison.Ordinal)) return RaftThicknessUi(family);
            if (string.Equals(key, RaftFoundationPropertySet.ElevationModeKey, StringComparison.Ordinal)) return RaftElevationMode(family);
            return RaftValue(family, key, string.Empty);
        }

        private static string RaftThicknessUi(ProjectFamily family)
        {
            var stored = RaftValue(family, "ThicknessM", "0.5");
            return ProjectFamilyQuickSchemaService.FormatInternalMetersAsMillimeters("Dày", stored, CultureInfo.CurrentCulture);
        }

        private static double RaftThicknessM(ProjectFamily family)
        {
            var raw = RaftValue(family, "ThicknessM", "0.5");
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new InvalidOperationException("Dày Móng Bè không hợp lệ: '" + raw + "'.");
            return value;
        }

        private static string RaftElevationMode(ProjectFamily family)
        {
            var raw = RaftValue(family, RaftFoundationPropertySet.ElevationModeKey, RaftFoundationPropertySet.BottomLevelMode);
            return RaftFoundationPropertySet.NormalizeElevationMode(raw);
        }

        private static string RaftValue(ProjectFamily family, string key, string fallback) =>
            family.Properties.TryGetValue(key, out var stored) ? (stored ?? string.Empty).Trim() : fallback;

        private static string RaftPropertyLabel(string key)
        {
            switch (key)
            {
                case "ThicknessM": return "Dày";
                case RaftFoundationPropertySet.ElevationModeKey: return "Cao độ";
                case RaftColorModeKey: return "Màu sắc";
                case RaftTransparencyKey: return "Độ trong suốt";
                case RaftMarkKey: return "Mark";
                case RaftCommentKey: return "Comment";
                case RaftWbsKey: return "WBS";
                case RaftMaterialKey: return "Vật liệu";
                default: return key;
            }
        }
    }
}
