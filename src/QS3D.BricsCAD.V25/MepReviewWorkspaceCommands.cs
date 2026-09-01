using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Bricscad.ApplicationServices;
using QS3D.Core.Mep;
using Teigha.Runtime;
using BricsApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    public sealed class MepReviewWorkspaceCommands
    {
        private static MepReviewWorkspaceWindow? _published;
        private static MepReviewWorkspaceWindow? _pending;

        [CommandMethod("QS3DMEPREVIEW")]
        public void ShowReviewWorkspace()
        {
            MepReviewWorkspaceWindow? candidate = null;
            try
            {
                var pending = _pending;
                if (pending != null && !TryClosePendingWindow(pending))
                {
                    var blockedDocument = BricsApplication.DocumentManager.MdiActiveDocument;
                    blockedDocument?.Editor.WriteMessage("\nQS3DMEPREVIEW chưa thể mở lại vì cửa sổ lỗi trước đó chưa đóng hoàn toàn.");
                    return;
                }

                var published = _published;
                if (published != null)
                {
                    if (published.IsLoaded)
                    {
                        try { published.Activate(); } catch (System.Exception) { }
                        return;
                    }

                    ReleasePublishedWindow(published);
                }

                candidate = new MepReviewWorkspaceWindow();
                var window = candidate;
                _pending = window;
                window.Closed += (_, __) => ReleaseWindow(window);

                BricsApplication.ShowModelessWindow(window);
                if (!window.IsLoaded)
                    throw new InvalidOperationException("MEP Review host publication did not remain loaded.");

                _published = window;
                ReleasePendingWindow(window);
                candidate = null;
            }
            catch (System.Exception ex)
            {
                var document = BricsApplication.DocumentManager.MdiActiveDocument;
                document?.Editor.WriteMessage("\nQS3DMEPREVIEW failed (" + ex.GetType().Name + ").");
            }
            finally
            {
                if (candidate != null)
                    TryClosePendingWindow(candidate);
            }
        }

        private static void ReleaseWindow(MepReviewWorkspaceWindow window)
        {
            ReleasePublishedWindow(window);
            ReleasePendingWindow(window);
        }

        private static void ReleasePublishedWindow(MepReviewWorkspaceWindow window)
        {
            if (!ReferenceEquals(_published, window)) return;
            _published = null;
        }

        private static void ReleasePendingWindow(MepReviewWorkspaceWindow window)
        {
            if (!ReferenceEquals(_pending, window)) return;
            _pending = null;
        }

        private static bool TryClosePendingWindow(MepReviewWorkspaceWindow window)
        {
            if (!ReferenceEquals(_pending, window)) return true;
            if (ReferenceEquals(_published, window))
            {
                ReleasePendingWindow(window);
                return true;
            }

            if (window.IsLoaded)
            {
                try { window.Close(); } catch (System.Exception) { }
            }

            if (window.IsLoaded) return false;
            ReleasePendingWindow(window);
            return true;
        }
    }

    internal sealed class MepReviewWorkspaceWindow : Window
    {
        private readonly DataGrid _profileGrid;
        private readonly TextBlock _profileStatus;
        private readonly TextBlock _hostStatus;
        private readonly TextBox _profilePath;
        private List<MepProfileRuleRow> _rows = new List<MepProfileRuleRow>();

        internal MepReviewWorkspaceWindow()
        {
            Title = "QS3D MEP Review";
            Width = 1060;
            Height = 720;
            MinWidth = 820;
            MinHeight = 560;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var root = new DockPanel { LastChildFill = true, Margin = new Thickness(12) };
            Content = root;

            var header = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 10) };
            DockPanel.SetDock(header, Dock.Top);
            root.Children.Add(header);
            header.Children.Add(new TextBlock
            {
                Text = "QS3D • MEP Takeoff / Clash Review",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 4)
            });
            _hostStatus = new TextBlock { TextWrapping = TextWrapping.Wrap };
            header.Children.Add(_hostStatus);

            var commandPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            DockPanel.SetDock(commandPanel, Dock.Top);
            root.Children.Add(commandPanel);
            commandPanel.Children.Add(CommandButton("Takeoff", "QS3DMEPTAKEOFF"));
            commandPanel.Children.Add(CommandButton("Broad Clash", "QS3DMEPCLASH"));
            commandPanel.Children.Add(CommandButton("Clash Locate", "QS3DMEPCLASHLOCATE"));
            commandPanel.Children.Add(CommandButton("Exact Clash", "QS3DMEPEXACTCLASH"));
            commandPanel.Children.Add(CommandButton("Exact Highlight", "QS3DMEPEXACTCLASHHIGHLIGHT"));
            commandPanel.Children.Add(CommandButton("Zoom Selection", "QS3DMEPZOOMSELECTION"));
            var refreshHost = SimpleButton("Refresh Host", (_, __) => RefreshHostStatus());
            commandPanel.Children.Add(refreshHost);

            var profileHeader = new Grid { Margin = new Thickness(0, 2, 0, 8) };
            profileHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            profileHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            DockPanel.SetDock(profileHeader, Dock.Top);
            root.Children.Add(profileHeader);
            profileHeader.Children.Add(new TextBlock
            {
                Text = "Recognition profile:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                FontWeight = FontWeights.SemiBold
            });
            _profilePath = new TextBox
            {
                IsReadOnly = true,
                Text = MepRecognitionProfileProvider.ProfilePath,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_profilePath, 1);
            profileHeader.Children.Add(_profilePath);

            var profileActions = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            DockPanel.SetDock(profileActions, Dock.Top);
            root.Children.Add(profileActions);
            profileActions.Children.Add(SimpleButton("Reload", (_, __) => ReloadProfile()));
            profileActions.Children.Add(SimpleButton("Reset Default", (_, __) => ResetDefaultProfile()));
            profileActions.Children.Add(SimpleButton("Add Rule", (_, __) => AddRule()));
            profileActions.Children.Add(SimpleButton("Remove Selected", (_, __) => RemoveSelectedRule()));
            profileActions.Children.Add(SimpleButton("Save Profile", (_, __) => SaveProfile()));
            _profileStatus = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap
            };
            profileActions.Children.Add(_profileStatus);

            _profileGrid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.All,
                IsReadOnly = false
            };
            AddTextColumn("Id", "Id", 150);
            AddTextColumn("Priority", "Priority", 75);
            AddTextColumn("Discipline", "Discipline", 100);
            AddTextColumn("Category", "Category", 110);
            AddTextColumn("Source", "Source", 125);
            AddTextColumn("MEP Kind", "MepKind", 105);
            AddTextColumn("Tokens (comma-separated)", "Tokens", 1, true);
            root.Children.Add(_profileGrid);

            ReloadProfile();
            RefreshHostStatus();
        }

        private Button CommandButton(string label, string command)
        {
            return SimpleButton(label, (_, __) => ExecuteCadCommand(command));
        }

        private static Button SimpleButton(string label, RoutedEventHandler handler)
        {
            var button = new Button
            {
                Content = label,
                MinWidth = 100,
                Margin = new Thickness(0, 0, 8, 6),
                Padding = new Thickness(10, 5, 10, 5)
            };
            button.Click += handler;
            return button;
        }

        private void AddTextColumn(string header, string property, double width, bool star = false)
        {
            _profileGrid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding(property) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus },
                Width = star ? new DataGridLength(width, DataGridLengthUnitType.Star) : new DataGridLength(width)
            });
        }

        private void ExecuteCadCommand(string command)
        {
            var document = BricsApplication.DocumentManager.MdiActiveDocument;
            if (document == null)
            {
                _hostStatus.Text = "Không có active BricsCAD document.";
                return;
            }

            try
            {
                document.SendStringToExecute(command + " ", true, false, false);
                _hostStatus.Text = "Queued " + command + " trên active document: " + SafeDocumentName(document) + ".";
            }
            catch (System.Exception ex)
            {
                _hostStatus.Text = "Không queue được " + command + " (" + ex.GetType().Name + ").";
            }
        }

        private void RefreshHostStatus()
        {
            var document = BricsApplication.DocumentManager.MdiActiveDocument;
            _hostStatus.Text = document == null
                ? "Không có active BricsCAD document. Workspace không giữ Document/ObjectId giữa các lần click."
                : "Active document: " + SafeDocumentName(document) + ". Mỗi action sẽ resolve lại document hiện hành.";
        }

        private static string SafeDocumentName(Document document)
        {
            try
            {
                var name = document.Name;
                return string.IsNullOrWhiteSpace(name) ? "<unnamed>" : name;
            }
            catch (System.Exception)
            {
                return "<unavailable>";
            }
        }

        private void ReloadProfile()
        {
            var ok = MepRecognitionProfileProvider.Reload();
            LoadRows(MepRecognitionProfileProvider.Current);
            var error = MepRecognitionProfileProvider.LastError;
            _profileStatus.Text = ok
                ? (MepRecognitionProfileProvider.IsCustom ? "Loaded custom profile." : "Using built-in default profile.")
                : "Invalid profile; default active. " + (error ?? string.Empty);
        }

        private void ResetDefaultProfile()
        {
            try
            {
                var profile = MepRecognitionProfiles.CreateDefault();
                MepRecognitionProfileProvider.Save(profile);
                LoadRows(profile);
                _profileStatus.Text = "Default profile saved atomically and activated.";
            }
            catch (System.Exception ex)
            {
                _profileStatus.Text = "Reset default failed: " + ex.Message;
            }
        }

        private void AddRule()
        {
            CommitGridEdits();
            _rows.Add(new MepProfileRuleRow
            {
                Id = "rule." + (_rows.Count + 1).ToString(CultureInfo.InvariantCulture),
                Priority = "100",
                Discipline = MepRecognitionDiscipline.Mep.ToString(),
                Category = "Custom",
                Source = MepRecognitionSource.LayerOrBlockName.ToString(),
                MepKind = MepElementKind.Equipment.ToString(),
                Tokens = "TOKEN"
            });
            RefreshRows();
            _profileGrid.SelectedItem = _rows[_rows.Count - 1];
        }

        private void RemoveSelectedRule()
        {
            CommitGridEdits();
            var row = _profileGrid.SelectedItem as MepProfileRuleRow;
            if (row == null)
            {
                _profileStatus.Text = "Select a rule first.";
                return;
            }
            if (_rows.Count <= 1)
            {
                _profileStatus.Text = "Profile must retain at least one rule.";
                return;
            }
            _rows.Remove(row);
            RefreshRows();
        }

        private void SaveProfile()
        {
            try
            {
                CommitGridEdits();
                var profile = BuildProfileFromRows();
                MepRecognitionProfileProvider.Save(profile);
                LoadRows(profile);
                _profileStatus.Text = "Saved + activated " + profile.Rules.Count.ToString(CultureInfo.InvariantCulture) + " rules.";
            }
            catch (System.Exception ex)
            {
                _profileStatus.Text = "Save refused: " + ex.Message;
            }
        }

        private MepRecognitionProfile BuildProfileFromRows()
        {
            if (_rows.Count == 0) throw new InvalidOperationException("Profile must contain at least one rule.");
            var rules = new List<MepRecognitionRule>(_rows.Count);
            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var id = Required(row.Id, "Id", i);
                if (!int.TryParse(row.Priority, NumberStyles.Integer, CultureInfo.InvariantCulture, out var priority))
                    throw new InvalidOperationException("Row " + (i + 1) + ": Priority is invalid.");
                if (!Enum.TryParse(row.Discipline, true, out MepRecognitionDiscipline discipline) ||
                    !Enum.IsDefined(typeof(MepRecognitionDiscipline), discipline))
                    throw new InvalidOperationException("Row " + (i + 1) + ": Discipline is invalid.");
                if (!Enum.TryParse(row.Source, true, out MepRecognitionSource source) ||
                    source == MepRecognitionSource.None ||
                    (source & ~MepRecognitionSource.LayerOrBlockName) != MepRecognitionSource.None)
                    throw new InvalidOperationException("Row " + (i + 1) + ": Source is invalid.");

                MepElementKind? kind = null;
                if (discipline == MepRecognitionDiscipline.Mep)
                {
                    if (!Enum.TryParse(row.MepKind, true, out MepElementKind parsedKind) || !Enum.IsDefined(typeof(MepElementKind), parsedKind))
                        throw new InvalidOperationException("Row " + (i + 1) + ": MEP Kind is required/invalid.");
                    kind = parsedKind;
                }
                else if (!string.IsNullOrWhiteSpace(row.MepKind))
                {
                    throw new InvalidOperationException("Row " + (i + 1) + ": non-MEP rules must leave MEP Kind blank.");
                }

                var tokens = (row.Tokens ?? string.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(static token => token.Trim())
                    .Where(static token => token.Length > 0)
                    .ToArray();
                if (tokens.Length == 0) throw new InvalidOperationException("Row " + (i + 1) + ": at least one token is required.");
                if (tokens.Length > 100) throw new InvalidOperationException("Row " + (i + 1) + ": max 100 tokens.");

                rules.Add(new MepRecognitionRule(
                    id,
                    priority,
                    discipline,
                    Required(row.Category, "Category", i),
                    tokens,
                    source,
                    kind));
            }
            return new MepRecognitionProfile(rules);
        }

        private static string Required(string? value, string label, int rowIndex)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length == 0) throw new InvalidOperationException("Row " + (rowIndex + 1) + ": " + label + " is required.");
            return text;
        }

        private void LoadRows(MepRecognitionProfile profile)
        {
            _rows = new List<MepProfileRuleRow>(profile.Rules.Count);
            for (var i = 0; i < profile.Rules.Count; i++)
            {
                var rule = profile.Rules[i];
                _rows.Add(new MepProfileRuleRow
                {
                    Id = rule.Id,
                    Priority = rule.Priority.ToString(CultureInfo.InvariantCulture),
                    Discipline = rule.Discipline.ToString(),
                    Category = rule.Category,
                    Source = rule.Source.ToString(),
                    MepKind = rule.MepKind.HasValue ? rule.MepKind.Value.ToString() : string.Empty,
                    Tokens = string.Join(", ", rule.Tokens)
                });
            }
            RefreshRows();
        }

        private void RefreshRows()
        {
            _profileGrid.ItemsSource = null;
            _profileGrid.ItemsSource = _rows;
        }

        private void CommitGridEdits()
        {
            _profileGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            _profileGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }
    }

    internal sealed class MepProfileRuleRow
    {
        public string Id { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Discipline { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string MepKind { get; set; } = string.Empty;
        public string Tokens { get; set; } = string.Empty;
    }
}
