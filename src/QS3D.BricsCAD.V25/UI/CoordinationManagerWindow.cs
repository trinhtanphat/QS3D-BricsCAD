using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using QS3D.Platform.Parity;

namespace QS3D.BricsCAD.V25.UI
{
    internal sealed class CoordinationManagerWindow : Window
    {
        private readonly string _projectId;
        private readonly string _drawingFingerprint;
        private readonly DataGrid _grid;
        private readonly ComboBox _statusFilter;
        private readonly ComboBox _severityFilter;
        private readonly ComboBox _kindFilter;
        private readonly TextBox _floorFilter;
        private readonly TextBox _categoryFilter;
        private readonly TextBox _ruleFilter;
        private readonly CheckBox _actionableOnly;
        private readonly ComboBox _editStatus;
        private readonly TextBox _assignee;
        private readonly TextBox _comment;
        private readonly TextBlock _message;
        private readonly Button _locate;
        private readonly Button _save;

        public CoordinationManagerWindow(Document document, string projectId, string drawingFingerprint)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            _projectId = RequireToken(projectId, nameof(projectId));
            _drawingFingerprint = RequireToken(drawingFingerprint, nameof(drawingFingerprint));

            Title = "QS3D • Coordination Manager";
            Width = 1120;
            Height = 700;
            MinWidth = 900;
            MinHeight = 520;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var root = new DockPanel { Margin = new Thickness(12) };
            Content = root;

            var filters = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            DockPanel.SetDock(filters, Dock.Top);
            root.Children.Add(filters);

            filters.Children.Add(Label("Trạng thái"));
            _statusFilter = new ComboBox { Width = 130, Margin = new Thickness(4, 0, 12, 0) };
            _statusFilter.Items.Add("Tất cả");
            foreach (CoordinationFindingStatus status in Enum.GetValues(typeof(CoordinationFindingStatus)))
                _statusFilter.Items.Add(status.ToString());
            _statusFilter.SelectedIndex = 0;
            filters.Children.Add(_statusFilter);

            filters.Children.Add(Label("Mức độ ≥"));
            _severityFilter = new ComboBox { Width = 130, Margin = new Thickness(4, 0, 12, 0) };
            _severityFilter.Items.Add("Tất cả");
            foreach (CoordinationFindingSeverity severity in Enum.GetValues(typeof(CoordinationFindingSeverity)))
                _severityFilter.Items.Add(severity.ToString());
            _severityFilter.SelectedIndex = 0;
            filters.Children.Add(_severityFilter);

            filters.Children.Add(Label("Loại"));
            _kindFilter = new ComboBox { Width = 120, Margin = new Thickness(4, 0, 12, 0) };
            _kindFilter.Items.Add("Tất cả");
            foreach (CoordinationFindingKind kind in Enum.GetValues(typeof(CoordinationFindingKind)))
                _kindFilter.Items.Add(kind.ToString());
            _kindFilter.SelectedIndex = 0;
            filters.Children.Add(_kindFilter);

            filters.Children.Add(Label("Tầng"));
            _floorFilter = new TextBox { Width = 105, Margin = new Thickness(4, 0, 12, 0), MaxLength = 256 };
            filters.Children.Add(_floorFilter);

            filters.Children.Add(Label("Category"));
            _categoryFilter = new TextBox { Width = 115, Margin = new Thickness(4, 0, 12, 0), MaxLength = 256 };
            filters.Children.Add(_categoryFilter);

            filters.Children.Add(Label("Rule"));
            _ruleFilter = new TextBox { Width = 115, Margin = new Thickness(4, 0, 12, 0), MaxLength = 256 };
            filters.Children.Add(_ruleFilter);

            _actionableOnly = new CheckBox
            {
                Content = "Chỉ dòng có thể định vị",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            filters.Children.Add(_actionableOnly);

            var refresh = new Button { Content = "Làm mới", MinWidth = 90, Margin = new Thickness(0, 0, 8, 0) };
            refresh.Click += (_, __) => SafeRefresh();
            filters.Children.Add(refresh);

            _locate = new Button { Content = "Định vị CAD", MinWidth = 105 };
            _locate.Click += (_, __) => SafeLocate();
            filters.Children.Add(_locate);

            _message = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 8)
            };
            DockPanel.SetDock(_message, Dock.Bottom);
            root.Children.Add(_message);

            var editor = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            editor.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            editor.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            editor.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            editor.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            editor.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            editor.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            editor.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            DockPanel.SetDock(editor, Dock.Bottom);
            root.Children.Add(editor);

            AddEditorLabel(editor, "Trạng thái", 0);
            _editStatus = new ComboBox { Margin = new Thickness(4, 0, 12, 0) };
            foreach (CoordinationIssueStatus status in Enum.GetValues(typeof(CoordinationIssueStatus)))
                _editStatus.Items.Add(status);
            Grid.SetColumn(_editStatus, 1);
            editor.Children.Add(_editStatus);

            AddEditorLabel(editor, "Phụ trách", 2);
            _assignee = new TextBox { Margin = new Thickness(4, 0, 12, 0), MaxLength = 256 };
            Grid.SetColumn(_assignee, 3);
            editor.Children.Add(_assignee);

            AddEditorLabel(editor, "Ghi chú", 4);
            _comment = new TextBox { Margin = new Thickness(4, 0, 12, 0), MaxLength = 2000 };
            Grid.SetColumn(_comment, 5);
            editor.Children.Add(_comment);

            _save = new Button { Content = "Lưu thay đổi", MinWidth = 110 };
            _save.Click += (_, __) => SafeSaveMutation();
            Grid.SetColumn(_save, 6);
            editor.Children.Add(_save);

            _grid = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                CanUserReorderColumns = false
            };
            _grid.Columns.Add(TextColumn("Issue", nameof(CoordinationManagerRow.IssueId), 180));
            _grid.Columns.Add(TextColumn("Loại", nameof(CoordinationManagerRow.Kind), 105));
            _grid.Columns.Add(TextColumn("Mức độ", nameof(CoordinationManagerRow.Severity), 90));
            _grid.Columns.Add(TextColumn("Trạng thái", nameof(CoordinationManagerRow.Status), 95));
            _grid.Columns.Add(TextColumn("Nội dung", nameof(CoordinationManagerRow.Title), 230));
            _grid.Columns.Add(TextColumn("Phụ trách", nameof(CoordinationManagerRow.Assignee), 120));
            _grid.Columns.Add(TextColumn("Relink", nameof(CoordinationManagerRow.RelinkStatus), 150));
            _grid.Columns.Add(TextColumn("A", nameof(CoordinationManagerRow.LeftSemanticId), 130));
            _grid.Columns.Add(TextColumn("B", nameof(CoordinationManagerRow.RightSemanticId), 130));
            _grid.SelectionChanged += (_, __) => PopulateEditor();
            root.Children.Add(_grid);

            _statusFilter.SelectionChanged += (_, __) => SafeRefresh();
            _severityFilter.SelectionChanged += (_, __) => SafeRefresh();
            _kindFilter.SelectionChanged += (_, __) => SafeRefresh();
            _floorFilter.TextChanged += (_, __) => SafeRefresh();
            _categoryFilter.TextChanged += (_, __) => SafeRefresh();
            _ruleFilter.TextChanged += (_, __) => SafeRefresh();
            _actionableOnly.Checked += (_, __) => SafeRefresh();
            _actionableOnly.Unchecked += (_, __) => SafeRefresh();

            Loaded += (_, __) => SafeRefresh();
        }

        private void SafeRefresh()
        {
            try
            {
                var project = RequireCurrentProject(false, out _);
                var snapshot = CoordinationIssuePersistence.Load(project);
                if (snapshot == null)
                {
                    _grid.ItemsSource = Array.Empty<CoordinationManagerRow>();
                    SetMessage("Chưa có persisted coordination issue trong project hiện hành.");
                    UpdateActionState();
                    return;
                }

                var issuesById = snapshot.Issues.ToDictionary(x => x.IssueId, StringComparer.OrdinalIgnoreCase);
                var findingById = new Dictionary<string, CoordinationManagerFinding>(StringComparer.OrdinalIgnoreCase);
                foreach (var issue in snapshot.Issues)
                    findingById.Add(issue.IssueId, ToFinding(project, snapshot, issue));

                var projected = CoordinationManagerProjection.Build(findingById.Values, BuildFilter());
                var rows = projected.Select(finding =>
                {
                    var issue = issuesById[finding.Id];
                    var relink = snapshot.EvaluateRelink(project, issue.IssueId);
                    return new CoordinationManagerRow(issue, finding, relink.Status);
                }).ToList();

                _grid.ItemsSource = rows;
                var groups = rows
                    .GroupBy(x => x.Kind)
                    .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.Key + "=" + x.Count());
                SetMessage("Coordination Manager • " + rows.Count + " dòng • revision " + snapshot.Revision +
                           (rows.Count == 0 ? string.Empty : " • " + string.Join(" • ", groups)) +
                           ". Dòng unresolved/foreign vẫn hiển thị nhưng không được định vị.");
                UpdateActionState();
            }
            catch (Exception)
            {
                _grid.ItemsSource = Array.Empty<CoordinationManagerRow>();
                SetMessage("Không thể làm mới Coordination Manager. Vui lòng thử lại.");
                UpdateActionState();
            }
        }

        private void SafeLocate()
        {
            try
            {
                var selected = SelectedRow();
                var project = RequireCurrentProject(false, out var document);
                var snapshot = CoordinationIssuePersistence.Load(project)
                    ?? throw new InvalidOperationException("Coordination persistence không còn tồn tại.");
                var issue = RequireFreshIssue(snapshot, selected);
                var relink = snapshot.EvaluateRelink(project, issue.IssueId);
                if (relink.Status != CoordinationRelinkStatus.ReadyForHostValidation &&
                    relink.Status != CoordinationRelinkStatus.Relinked)
                    throw new InvalidOperationException("Issue không thể định vị: " + relink.Status + ".");

                var leftHandles = CanonicalHandles(SourceHandleResolver.Resolve(project, new[] { issue.LeftSemanticId }));
                var rightHandles = CanonicalHandles(SourceHandleResolver.Resolve(project, new[] { issue.RightSemanticId }));
                if (leftHandles.Count == 0 || rightHandles.Count == 0)
                    throw new InvalidOperationException("Issue thiếu source Handle hiện hành ở một hoặc cả hai phía; selection không đổi.");

                var handles = leftHandles.Concat(rightHandles)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var resolved = CadHandleService.Resolve(document, handles);
                if (resolved.Count != handles.Count)
                    throw new InvalidOperationException("Không resolve đủ toàn bộ source Handle hiện hành; selection không đổi.");

                document.Editor.SetImpliedSelection(resolved.ToArray());
                var zoomed = global::QS3D.BricsCAD.V25.ViewportCommands.TryZoomSelection(document);
                SetMessage(zoomed
                    ? "Đã định vị issue " + issue.IssueId + " bằng " + resolved.Count + " CAD object sau khi revalidate toàn bộ provenance."
                    : "Đã chọn issue " + issue.IssueId + " bằng " + resolved.Count + " CAD object sau khi revalidate toàn bộ provenance nhưng chưa thể zoom vùng chọn hiện hành.");
            }
            catch (Exception)
            {
                SetMessage("Không thể định vị Coordination issue. Vui lòng thử lại.");
            }
        }

        private void SafeSaveMutation()
        {
            try
            {
                var selected = SelectedRow();
                var project = RequireCurrentProject(true, out var document);
                var snapshot = CoordinationIssuePersistence.Load(project)
                    ?? throw new InvalidOperationException("Coordination persistence không còn tồn tại.");
                var issue = RequireFreshIssue(snapshot, selected);
                var desiredStatus = _editStatus.SelectedItem is CoordinationIssueStatus status
                    ? status
                    : issue.Status;
                var desiredAssignee = (_assignee.Text ?? string.Empty).Trim();
                var comment = (_comment.Text ?? string.Empty).Trim();

                var changed = false;
                if (desiredStatus != issue.Status)
                {
                    if (!CoordinationIssue.CanTransition(issue.Status, desiredStatus))
                        throw new InvalidOperationException("Chuyển trạng thái không hợp lệ: " + issue.Status + " → " + desiredStatus + ".");
                    issue.TransitionTo(desiredStatus, NextMutationTime(issue));
                    changed = true;
                }

                var normalizedCurrentAssignee = (issue.Assignee ?? string.Empty).Trim();
                if (!string.Equals(normalizedCurrentAssignee, desiredAssignee, StringComparison.Ordinal))
                {
                    issue.Assign(desiredAssignee.Length == 0 ? null : desiredAssignee, NextMutationTime(issue));
                    changed = true;
                }

                if (comment.Length > 0)
                {
                    var author = string.IsNullOrWhiteSpace(Environment.UserName) ? "QS3D" : Environment.UserName.Trim();
                    issue.AddComment(new CoordinationIssueComment(
                        "coord-ui-" + Guid.NewGuid().ToString("N"),
                        author,
                        comment,
                        NextMutationTime(issue)));
                    changed = true;
                }

                if (!changed)
                {
                    SetMessage("Không có thay đổi để lưu cho issue " + issue.IssueId + ".");
                    return;
                }

                var metadataBefore = project.Metadata.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
                try
                {
                    var nextRevision = checked(snapshot.Revision + 1L);
                    CoordinationIssuePersistence.Save(project, snapshot.Issues, nextRevision);
                    ProjectContextCoordinator.Save(document);
                }
                catch
                {
                    project.Metadata.Clear();
                    foreach (var pair in metadataBefore) project.Metadata[pair.Key] = pair.Value;
                    throw;
                }

                _comment.Text = string.Empty;
                SetMessage("Đã lưu lifecycle của issue " + issue.IssueId + " qua canonical QSDB persistence.");
                SafeRefresh();
            }
            catch (Exception)
            {
                SetMessage("Không thể lưu Coordination issue. Vui lòng thử lại.");
            }
        }

        private ProjectState RequireCurrentProject(bool mutation, out Document document)
        {
            document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument
                ?? throw new InvalidOperationException("Không có DWG đang active; Coordination Manager không được phép thao tác.");

            ProjectState project;
            if (mutation)
                project = ExistingProjectMutationContext.Require(document, "Coordination Manager");
            else if (!ProjectContextCoordinator.TryGetReadOnly(document, out project))
                throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng.");

            if (!string.Equals(project.ProjectId, _projectId, StringComparison.Ordinal) ||
                !string.Equals(project.DrawingFingerprint, _drawingFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("DWG hoặc Project/Drawing Fingerprint đã đổi; Coordination Manager này không được phép tác động lên document khác.");
            return project;
        }

        private CoordinationManagerFilter BuildFilter()
        {
            return CoordinationManagerFilterBuilder.Build(
                _statusFilter.SelectedItem?.ToString() ?? string.Empty,
                _severityFilter.SelectedItem?.ToString() ?? string.Empty,
                _actionableOnly.IsChecked == true,
                _kindFilter.SelectedItem?.ToString() ?? string.Empty,
                _floorFilter.Text,
                _categoryFilter.Text,
                _ruleFilter.Text);
        }

        private static CoordinationManagerFinding ToFinding(
            ProjectState project,
            CoordinationIssuePersistenceSnapshot snapshot,
            CoordinationIssue issue)
        {
            var relink = snapshot.EvaluateRelink(project, issue.IssueId);
            var leftResolved = true;
            var rightResolved = true;
            var stale = false;
            switch (relink.Status)
            {
                case CoordinationRelinkStatus.ReadyForHostValidation:
                case CoordinationRelinkStatus.Relinked:
                    break;
                case CoordinationRelinkStatus.MissingLeftSemantic:
                case CoordinationRelinkStatus.MissingLeftCadReference:
                case CoordinationRelinkStatus.StaleLeftCadReference:
                    leftResolved = false;
                    stale = relink.Status == CoordinationRelinkStatus.StaleLeftCadReference;
                    break;
                case CoordinationRelinkStatus.MissingRightSemantic:
                case CoordinationRelinkStatus.MissingRightCadReference:
                case CoordinationRelinkStatus.StaleRightCadReference:
                    rightResolved = false;
                    stale = relink.Status == CoordinationRelinkStatus.StaleRightCadReference;
                    break;
                default:
                    leftResolved = false;
                    rightResolved = false;
                    stale = relink.Status == CoordinationRelinkStatus.ProjectMismatch ||
                            relink.Status == CoordinationRelinkStatus.DrawingMismatch ||
                            relink.Status == CoordinationRelinkStatus.StaleBothCadReferences;
                    break;
            }

            return new CoordinationManagerFinding(
                issue.IssueId,
                MapKind(issue.Kind),
                MapStatus(issue.Status),
                (CoordinationFindingSeverity)(int)issue.Severity,
                null,
                issue.CategoryContext,
                issue.CategoryContext,
                issue.SystemContext,
                leftResolved,
                rightResolved,
                stale,
                leftResolved && rightResolved && !stale ? null : relink.Status.ToString());
        }

        private static CoordinationFindingKind MapKind(CoordinationIssueKind kind)
        {
            return kind switch
            {
                CoordinationIssueKind.HardClash => CoordinationFindingKind.HardClash,
                CoordinationIssueKind.ExactHardClash => CoordinationFindingKind.HardClash,
                CoordinationIssueKind.ClearanceClash => CoordinationFindingKind.Clearance,
                CoordinationIssueKind.Review => CoordinationFindingKind.Duplicate,
                _ => throw new InvalidOperationException("Unsupported coordination issue kind: " + kind + ".")
            };
        }

        private static CoordinationFindingStatus MapStatus(CoordinationIssueStatus status)
        {
            return status switch
            {
                CoordinationIssueStatus.Open => CoordinationFindingStatus.Open,
                CoordinationIssueStatus.InReview => CoordinationFindingStatus.Reviewed,
                CoordinationIssueStatus.Resolved => CoordinationFindingStatus.Resolved,
                CoordinationIssueStatus.Closed => CoordinationFindingStatus.Ignored,
                _ => throw new InvalidOperationException("Unsupported coordination issue status: " + status + ".")
            };
        }

        private static CoordinationIssue RequireFreshIssue(
            CoordinationIssuePersistenceSnapshot snapshot,
            CoordinationManagerRow selected)
        {
            var issue = snapshot.Find(selected.IssueId)
                ?? throw new InvalidOperationException("Issue đã bị xóa hoặc thay thế từ lúc hiển thị.");
            if (issue.UpdatedAtUtc != selected.UpdatedAtUtc)
                throw new InvalidOperationException("Issue đã thay đổi từ lúc hiển thị; làm mới trước khi thao tác.");
            return issue;
        }

        private static DateTime NextMutationTime(CoordinationIssue issue)
        {
            var now = DateTime.UtcNow;
            return now > issue.UpdatedAtUtc ? now : issue.UpdatedAtUtc.AddTicks(1);
        }

        private CoordinationManagerRow SelectedRow()
        {
            return _grid.SelectedItem as CoordinationManagerRow
                ?? throw new InvalidOperationException("Hãy chọn một coordination issue trước.");
        }

        private void PopulateEditor()
        {
            if (!(_grid.SelectedItem is CoordinationManagerRow row))
            {
                _editStatus.SelectedItem = null;
                _assignee.Text = string.Empty;
                UpdateActionState();
                return;
            }
            _editStatus.SelectedItem = row.PlatformStatus;
            _assignee.Text = row.Assignee;
            _comment.Text = string.Empty;
            UpdateActionState();
        }

        private void UpdateActionState()
        {
            var row = _grid.SelectedItem as CoordinationManagerRow;
            _locate.IsEnabled = row != null && row.CanLocate;
            _save.IsEnabled = row != null;
        }

        private void SetMessage(string message)
        {
            _message.Text = message ?? string.Empty;
            try { PaletteCoordinator.SetStatus(_message.Text); } catch { }
        }

        private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> handles)
        {
            return (handles ?? throw new ArgumentNullException(nameof(handles)))
                .Select(value => CadHandleService.NormalizeHexHandle(value)
                    ?? throw new InvalidOperationException("Project contains an invalid source CAD Handle."))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        private static string RequireToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", parameterName);
            return value.Trim();
        }

        private static TextBlock Label(string text) =>
            new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };

        private static void AddEditorLabel(Grid grid, string text, int column)
        {
            var label = Label(text);
            Grid.SetColumn(label, column);
            grid.Children.Add(label);
        }

        private static DataGridTextColumn TextColumn(string header, string path, double width)
        {
            return new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding(path),
                Width = new DataGridLength(width)
            };
        }
    }

    internal sealed class CoordinationManagerRow
    {
        public CoordinationManagerRow(
            CoordinationIssue issue,
            CoordinationManagerFinding finding,
            CoordinationRelinkStatus relinkStatus)
        {
            if (issue == null) throw new ArgumentNullException(nameof(issue));
            if (finding == null) throw new ArgumentNullException(nameof(finding));
            IssueId = issue.IssueId;
            Kind = finding.Kind.ToString();
            Severity = finding.Severity.ToString();
            Status = finding.Status.ToString();
            PlatformStatus = issue.Status;
            Title = issue.Title;
            Assignee = issue.Assignee ?? string.Empty;
            RelinkStatus = relinkStatus.ToString();
            LeftSemanticId = issue.LeftSemanticId;
            RightSemanticId = issue.RightSemanticId;
            UpdatedAtUtc = issue.UpdatedAtUtc;
            CanLocate = finding.IsActionable &&
                        (relinkStatus == CoordinationRelinkStatus.ReadyForHostValidation ||
                         relinkStatus == CoordinationRelinkStatus.Relinked);
        }

        public string IssueId { get; }
        public string Kind { get; }
        public string Severity { get; }
        public string Status { get; }
        public CoordinationIssueStatus PlatformStatus { get; }
        public string Title { get; }
        public string Assignee { get; }
        public string RelinkStatus { get; }
        public string LeftSemanticId { get; }
        public string RightSemanticId { get; }
        public DateTime UpdatedAtUtc { get; }
        public bool CanLocate { get; }
    }
}
