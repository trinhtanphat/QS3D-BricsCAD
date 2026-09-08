using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Bricscad.ApplicationServices;
using QS3D.Core.Commercial;

namespace QS3D.BricsCAD.V25.UI
{
    public enum CommercialQsSurface
    {
        Variations = 0,
        Ipc = 1,
        FinalAccount = 2
    }

    public sealed class CommercialQsWindow : Window
    {
        private const string MetadataKey = "QS3D.Commercial.Workspace.v1";
        private static readonly Brush WindowBrush = new SolidColorBrush(Color.FromRgb(24, 24, 24));
        private static readonly Brush PanelBrush = new SolidColorBrush(Color.FromRgb(34, 34, 34));
        private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(232, 232, 232));
        private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(170, 170, 170));
        private static readonly Brush BorderBrush = new SolidColorBrush(Color.FromRgb(76, 76, 76));

        private readonly Document _document;
        private readonly IntPtr _databaseIdentity;
        private CommercialQsWorkspaceState _state;
        private readonly TabControl _tabs;
        private readonly TextBlock _status;
        private readonly TextBlock _variationSummary;
        private readonly TextBlock _ipcSummary;
        private readonly TextBlock _finalSummary;
        private DataGrid _variationGrid = null!;
        private DataGrid _progressGrid = null!;
        private DataGrid _variationCertificationGrid = null!;

        public CommercialQsWindow(Document document, CommercialQsSurface initialSurface)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            if (document.Database == null || document.Database.UnmanagedObject == IntPtr.Zero)
                throw new InvalidOperationException("Commercial QS workspace requires a live BricsCAD document database.");
            _databaseIdentity = document.Database.UnmanagedObject;
            _state = LoadWorkspace(document);

            Title = "QS3D — Commercial QS";
            Width = 1180;
            Height = 760;
            MinWidth = 880;
            MinHeight = 560;
            Background = WindowBrush;
            Foreground = TextBrush;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var root = new DockPanel { LastChildFill = true, Background = WindowBrush };
            root.Children.Add(BuildHeader());
            _status = new TextBlock
            {
                Margin = new Thickness(14, 6, 14, 10),
                Foreground = MutedBrush,
                TextWrapping = TextWrapping.Wrap,
                Text = BuildProjectStatus()
            };
            DockPanel.SetDock(_status, Dock.Bottom);
            root.Children.Add(_status);

            _tabs = new TabControl
            {
                Margin = new Thickness(12, 0, 12, 0),
                Background = WindowBrush,
                Foreground = TextBrush
            };

            _variationSummary = SummaryText();
            _ipcSummary = SummaryText();
            _finalSummary = SummaryText();
            _tabs.Items.Add(new TabItem { Header = "Variations", Content = BuildVariationsSurface() });
            _tabs.Items.Add(new TabItem { Header = "IPC", Content = BuildIpcSurface() });
            _tabs.Items.Add(new TabItem { Header = "Final Account", Content = BuildFinalAccountSurface() });
            _tabs.SelectedIndex = (int)initialSurface;
            root.Children.Add(_tabs);
            Content = root;

            Loaded += (_, __) => RefreshAllSummaries(false);
        }

        public void SelectSurface(CommercialQsSurface surface)
        {
            _tabs.SelectedIndex = (int)surface;
        }

        private UIElement BuildHeader()
        {
            var header = new Grid
            {
                Margin = new Thickness(12, 12, 12, 8),
                Background = PanelBrush
            };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel { Margin = new Thickness(14, 10, 14, 10) };
            left.Children.Add(new TextBlock
            {
                Text = "COMMERCIAL QS",
                Foreground = TextBrush,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold
            });
            left.Children.Add(new TextBlock
            {
                Text = "Variation register • Interim Payment Certificate • Final Account",
                Foreground = MutedBrush,
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            });
            Grid.SetColumn(left, 0);
            header.Children.Add(left);

            var currencyPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8)
            };
            currencyPanel.Children.Add(new TextBlock
            {
                Text = "Currency",
                Foreground = MutedBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            });
            var currencyBox = InputBox(86);
            Bind(currencyBox, "Currency", _state);
            currencyPanel.Children.Add(currencyBox);
            Grid.SetColumn(currencyPanel, 1);
            header.Children.Add(currencyPanel);

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8)
            };
            actions.Children.Add(ActionButton("Reload", (_, __) => ReloadFromProject(), false));
            actions.Children.Add(ActionButton("Save Project", (_, __) => SaveToProject(), true));
            Grid.SetColumn(actions, 2);
            header.Children.Add(actions);
            return header;
        }

        private UIElement BuildVariationsSurface()
        {
            var root = SurfaceGrid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            toolbar.Children.Add(ActionButton("Add Variation", (_, __) => AddVariation(), true));
            toolbar.Children.Add(ActionButton("Remove", (_, __) => RemoveSelectedVariation(), false));
            toolbar.Children.Add(ActionButton("Validate Register", (_, __) => RefreshVariationSummary(true), false));
            Grid.SetRow(toolbar, 0);
            root.Children.Add(toolbar);

            _variationGrid = DataGridBase();
            _variationGrid.Columns.Add(TextColumn("Variation ID", "VariationId", 120));
            _variationGrid.Columns.Add(TextColumn("Description", "Description", 260));
            _variationGrid.Columns.Add(TextColumn("Proposed", "ProposedAmount", 120));
            _variationGrid.Columns.Add(TextColumn("Approved", "ApprovedAmount", 120));
            _variationGrid.Columns.Add(new DataGridComboBoxColumn
            {
                Header = "Status",
                SelectedItemBinding = new Binding("Status") { Mode = BindingMode.TwoWay },
                ItemsSource = Enum.GetValues(typeof(CommercialVariationStatus)),
                Width = 125
            });
            _variationGrid.Columns.Add(TextColumn("Revision", "RevisionId", 100));
            _variationGrid.ItemsSource = _state.Variations;
            Grid.SetRow(_variationGrid, 1);
            root.Children.Add(_variationGrid);

            Grid.SetRow(_variationSummary, 2);
            root.Children.Add(_variationSummary);
            return root;
        }

        private UIElement BuildIpcSurface()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var root = new StackPanel { Margin = new Thickness(6) };
            scroll.Content = root;

            root.Children.Add(SectionTitle("Certificate inputs"));
            var inputs = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
            inputs.Children.Add(Field("Certificate ID", "CertificateId", _state.Ipc, 125));
            inputs.Children.Add(Field("Retention %", "RetentionPercent", _state.Ipc, 100));
            inputs.Children.Add(Field("VO retention", "VariationRetentionThisPeriod", _state.Ipc, 110));
            inputs.Children.Add(Field("Retention release", "RetentionRelease", _state.Ipc, 110));
            inputs.Children.Add(Field("Advance recovery", "AdvanceRecovery", _state.Ipc, 110));
            inputs.Children.Add(Field("Other deductions", "OtherDeductions", _state.Ipc, 110));
            inputs.Children.Add(Field("Previous net", "PreviousNetCertified", _state.Ipc, 120));
            root.Children.Add(inputs);

            var progressHeader = new DockPanel { Margin = new Thickness(0, 4, 0, 5) };
            progressHeader.Children.Add(SectionTitle("Progress claim / contract items"));
            var progressActions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            progressActions.Children.Add(ActionButton("Add Item", (_, __) => AddProgressItem(), true));
            progressActions.Children.Add(ActionButton("Remove", (_, __) => RemoveSelectedProgressItem(), false));
            DockPanel.SetDock(progressActions, Dock.Right);
            progressHeader.Children.Add(progressActions);
            root.Children.Add(progressHeader);

            _progressGrid = DataGridBase();
            _progressGrid.Height = 205;
            _progressGrid.Columns.Add(TextColumn("Item code", "ItemCode", 115));
            _progressGrid.Columns.Add(TextColumn("Unit", "Unit", 80));
            _progressGrid.Columns.Add(TextColumn("Contract qty", "ContractQuantity", 110));
            _progressGrid.Columns.Add(TextColumn("Unit rate", "UnitRate", 110));
            _progressGrid.Columns.Add(TextColumn("Previous qty", "PreviousCertifiedQuantity", 110));
            _progressGrid.Columns.Add(TextColumn("This period qty", "ClaimedThisPeriodQuantity", 120));
            _progressGrid.ItemsSource = _state.ProgressItems;
            root.Children.Add(_progressGrid);

            var variationHeader = new DockPanel { Margin = new Thickness(0, 12, 0, 5) };
            variationHeader.Children.Add(SectionTitle("Approved variation certification"));
            var variationActions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            variationActions.Children.Add(ActionButton("Sync Approved VOs", (_, __) => SyncApprovedVariationCertifications(), false));
            DockPanel.SetDock(variationActions, Dock.Right);
            variationHeader.Children.Add(variationActions);
            root.Children.Add(variationHeader);

            _variationCertificationGrid = DataGridBase();
            _variationCertificationGrid.Height = 170;
            _variationCertificationGrid.Columns.Add(TextColumn("Variation ID", "VariationId", 145));
            _variationCertificationGrid.Columns.Add(TextColumn("Previous certified", "PreviousCertified", 145));
            _variationCertificationGrid.Columns.Add(TextColumn("This period", "CertifiedThisPeriod", 145));
            _variationCertificationGrid.ItemsSource = _state.VariationCertifications;
            root.Children.Add(_variationCertificationGrid);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            actions.Children.Add(ActionButton("Calculate IPC", (_, __) => RefreshIpcSummary(true), true));
            root.Children.Add(actions);
            root.Children.Add(_ipcSummary);
            return scroll;
        }

        private UIElement BuildFinalAccountSurface()
        {
            var root = new StackPanel { Margin = new Thickness(8) };
            root.Children.Add(SectionTitle("Final Account reconciliation"));
            root.Children.Add(new TextBlock
            {
                Text = "Approved Variation register is used automatically. Rejected, pending and superseded VOs are excluded by Core.",
                Foreground = MutedBrush,
                Margin = new Thickness(0, 2, 0, 12),
                TextWrapping = TextWrapping.Wrap
            });

            var inputs = new WrapPanel();
            inputs.Children.Add(Field("Final Account ID", "FinalAccountId", _state.FinalAccount, 140));
            inputs.Children.Add(Field("Original contract", "OriginalContractValue", _state.FinalAccount, 130));
            inputs.Children.Add(Field("Final adjustment", "FinalAdjustment", _state.FinalAccount, 125));
            inputs.Children.Add(Field("Previous gross", "PreviousGrossCertified", _state.FinalAccount, 125));
            inputs.Children.Add(Field("Retention held", "RetentionHeld", _state.FinalAccount, 120));
            inputs.Children.Add(Field("Retention release", "RetentionRelease", _state.FinalAccount, 120));
            inputs.Children.Add(Field("Final deductions", "FinalDeductions", _state.FinalAccount, 120));
            root.Children.Add(inputs);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 0) };
            actions.Children.Add(ActionButton("Reconcile Final Account", (_, __) => RefreshFinalSummary(true), true));
            root.Children.Add(actions);
            root.Children.Add(_finalSummary);
            return root;
        }

        private void AddVariation()
        {
            CommitEditors();
            var index = _state.Variations.Count + 1;
            _state.Variations.Add(new CommercialVariationWorkspaceRow
            {
                VariationId = "VO-" + index.ToString("000", CultureInfo.InvariantCulture),
                Description = "New variation",
                RevisionId = "R1"
            });
            ResetGrid(_variationGrid, _state.Variations);
            RefreshVariationSummary(false);
        }

        private void RemoveSelectedVariation()
        {
            CommitEditors();
            var row = _variationGrid.SelectedItem as CommercialVariationWorkspaceRow;
            if (row == null) return;
            _state.Variations.Remove(row);
            _state.VariationCertifications.RemoveAll(x => string.Equals(x.VariationId, row.VariationId, StringComparison.Ordinal));
            ResetGrid(_variationGrid, _state.Variations);
            ResetGrid(_variationCertificationGrid, _state.VariationCertifications);
            RefreshAllSummaries(false);
        }

        private void AddProgressItem()
        {
            CommitEditors();
            var index = _state.ProgressItems.Count + 1;
            _state.ProgressItems.Add(new CommercialProgressWorkspaceRow
            {
                ItemCode = "ITEM-" + index.ToString("000", CultureInfo.InvariantCulture),
                Unit = "m"
            });
            ResetGrid(_progressGrid, _state.ProgressItems);
        }

        private void RemoveSelectedProgressItem()
        {
            CommitEditors();
            var row = _progressGrid.SelectedItem as CommercialProgressWorkspaceRow;
            if (row == null) return;
            _state.ProgressItems.Remove(row);
            ResetGrid(_progressGrid, _state.ProgressItems);
            RefreshIpcSummary(false);
        }

        private void SyncApprovedVariationCertifications()
        {
            CommitEditors();
            var approved = new HashSet<string>(
                _state.Variations
                    .Where(x => x != null && x.Status == CommercialVariationStatus.Approved && !string.IsNullOrWhiteSpace(x.VariationId))
                    .Select(x => x.VariationId),
                StringComparer.Ordinal);

            _state.VariationCertifications.RemoveAll(x => x == null || !approved.Contains(x.VariationId));
            foreach (var variationId in approved.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (_state.VariationCertifications.Any(x => string.Equals(x.VariationId, variationId, StringComparison.Ordinal)))
                    continue;
                _state.VariationCertifications.Add(new CommercialVariationCertificationWorkspaceRow { VariationId = variationId });
            }
            ResetGrid(_variationCertificationGrid, _state.VariationCertifications);
            SetStatus("Approved variations synchronized into IPC certification lines.");
        }

        private void ReloadFromProject()
        {
            try
            {
                EnsureDocumentAffinity();
                _state = LoadWorkspace(_document);
                Close();
                SetStatus("Commercial QS workspace reloaded. Re-open the command to continue.");
            }
            catch (Exception ex)
            {
                ShowError("Reload failed", ex);
            }
        }

        private void SaveToProject()
        {
            try
            {
                EnsureDocumentAffinity();
                CommitEditors();
                RefreshAllSummaries(true);
                var serialized = CommercialQsWorkspaceCodec.Serialize(_state);
                var project = ExistingProjectMutationContext.Require(_document, "Save Commercial QS workspace");
                project.Metadata[MetadataKey] = serialized;
                project.Touch();
                var path = ProjectContextCoordinator.Save(_document);
                try { PaletteCoordinator.RefreshProject(); } catch { }
                SetStatus("Saved Commercial QS workspace to " + path + ".");
            }
            catch (Exception ex)
            {
                ShowError("Save failed", ex);
            }
        }

        private void RefreshAllSummaries(bool showErrors)
        {
            RefreshVariationSummary(showErrors);
            RefreshIpcSummary(false);
            RefreshFinalSummary(false);
        }

        private void RefreshVariationSummary(bool showErrors)
        {
            try
            {
                CommitEditors();
                var register = CommercialQsWorkspaceCalculator.BuildVariationRegister(_state);
                _variationSummary.Text = FormatResult("Variation Register", register);
                if (showErrors) SetStatus("Variation Register validated against QS3D Core.");
            }
            catch (Exception ex)
            {
                _variationSummary.Text = "Variation Register: " + ex.Message;
                if (showErrors) ShowError("Variation validation failed", ex);
            }
        }

        private void RefreshIpcSummary(bool showErrors)
        {
            try
            {
                CommitEditors();
                var result = CommercialQsWorkspaceCalculator.CreateIpc(_state);
                _ipcSummary.Text = FormatResult("Interim Payment Certificate", result);
                if (showErrors) SetStatus("IPC calculated with QS3D Core progress + variation certification services.");
            }
            catch (Exception ex)
            {
                _ipcSummary.Text = "IPC: " + ex.Message;
                if (showErrors) ShowError("IPC calculation failed", ex);
            }
        }

        private void RefreshFinalSummary(bool showErrors)
        {
            try
            {
                CommitEditors();
                var result = CommercialQsWorkspaceCalculator.ReconcileFinalAccount(_state);
                _finalSummary.Text = FormatResult("Final Account", result);
                if (showErrors) SetStatus("Final Account reconciled with QS3D Core.");
            }
            catch (Exception ex)
            {
                _finalSummary.Text = "Final Account: " + ex.Message;
                if (showErrors) ShowError("Final Account reconciliation failed", ex);
            }
        }

        private static CommercialQsWorkspaceState LoadWorkspace(Document document)
        {
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                return new CommercialQsWorkspaceState();
            if (!project.Metadata.TryGetValue(MetadataKey, out var serialized) || string.IsNullOrWhiteSpace(serialized))
                return new CommercialQsWorkspaceState();
            return CommercialQsWorkspaceCodec.Deserialize(serialized);
        }

        private string BuildProjectStatus()
        {
            try
            {
                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                    return "No existing QS3D project sidecar is bound. You can review inputs, but Save Project will refuse to create a project implicitly.";
                return "Project: " + project.ProjectId + " • " + ProjectContextCoordinator.GetProjectPath(_document);
            }
            catch (Exception ex)
            {
                return "Project status unavailable: " + ex.Message;
            }
        }

        private void EnsureDocumentAffinity()
        {
            var active = Application.DocumentManager.MdiActiveDocument;
            if (!ReferenceEquals(active, _document))
                throw new InvalidOperationException("Commercial QS workspace is bound to another DWG. Return to the original drawing or close this window.");
            if (_document.Database == null || _document.Database.UnmanagedObject == IntPtr.Zero || _document.Database.UnmanagedObject != _databaseIdentity)
                throw new InvalidOperationException("Commercial QS workspace document identity changed. Close and reopen the workspace.");
        }

        private void CommitEditors()
        {
            CommitGrid(_variationGrid);
            CommitGrid(_progressGrid);
            CommitGrid(_variationCertificationGrid);
        }

        private static void CommitGrid(DataGrid grid)
        {
            if (grid == null) return;
            try
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }
            catch { }
        }

        private static void ResetGrid(DataGrid grid, object source)
        {
            if (grid == null) return;
            grid.ItemsSource = null;
            grid.ItemsSource = source as System.Collections.IEnumerable;
        }

        private void SetStatus(string message)
        {
            _status.Text = message;
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }

        private void ShowError(string title, Exception ex)
        {
            var message = title + ": " + ex.Message;
            SetStatus(message);
            MessageBox.Show(this, message, "QS3D Commercial QS", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private static string FormatResult(string title, object result)
        {
            var properties = result.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && IsSummaryValue(x.PropertyType))
                .OrderBy(x => x.Name, StringComparer.Ordinal)
                .Select(x => x.Name + ": " + FormatValue(x.GetValue(result, null)))
                .ToArray();
            return properties.Length == 0 ? title + ": calculated successfully." : title + "\n" + string.Join("  •  ", properties);
        }

        private static bool IsSummaryValue(Type type)
        {
            var actual = Nullable.GetUnderlyingType(type) ?? type;
            return actual == typeof(decimal) || actual == typeof(int) || actual == typeof(long) || actual == typeof(string) || actual.IsEnum;
        }

        private static string FormatValue(object value)
        {
            if (value == null) return string.Empty;
            if (value is decimal amount) return amount.ToString("0.##", CultureInfo.CurrentCulture);
            return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
        }

        private static Grid SurfaceGrid()
        {
            return new Grid { Margin = new Thickness(8), Background = WindowBrush };
        }

        private static TextBlock SummaryText()
        {
            return new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 4),
                Padding = new Thickness(10),
                Background = PanelBrush,
                Foreground = TextBrush,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 48
            };
        }

        private static TextBlock SectionTitle(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = TextBrush,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static DataGrid DataGridBase()
        {
            return new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                Background = PanelBrush,
                Foreground = TextBrush,
                BorderBrush = BorderBrush,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                RowHeaderWidth = 0,
                HeadersVisibility = DataGridHeadersVisibility.Column
            };
        }

        private static DataGridTextColumn TextColumn(string header, string path, double width)
        {
            return new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding(path)
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                },
                Width = width
            };
        }

        private static FrameworkElement Field(string label, string path, object source, double width)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 12, 8), Width = Math.Max(width, 100) };
            panel.Children.Add(new TextBlock { Text = label, Foreground = MutedBrush, Margin = new Thickness(0, 0, 0, 3) });
            var box = InputBox(width);
            Bind(box, path, source);
            panel.Children.Add(box);
            return panel;
        }

        private static TextBox InputBox(double width)
        {
            return new TextBox
            {
                Width = width,
                MinHeight = 25,
                Padding = new Thickness(5, 3, 5, 3),
                Background = new SolidColorBrush(Color.FromRgb(48, 48, 48)),
                Foreground = TextBrush,
                BorderBrush = BorderBrush
            };
        }

        private static void Bind(TextBox box, string path, object source)
        {
            box.SetBinding(TextBox.TextProperty, new Binding(path)
            {
                Source = source,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                ValidatesOnExceptions = true,
                NotifyOnValidationError = true
            });
        }

        private static Button ActionButton(string text, RoutedEventHandler click, bool primary)
        {
            var button = new Button
            {
                Content = text,
                MinWidth = 88,
                MinHeight = 28,
                Padding = new Thickness(9, 4, 9, 4),
                Margin = new Thickness(0, 0, 7, 0),
                Background = new SolidColorBrush(primary ? Color.FromRgb(62, 92, 128) : Color.FromRgb(54, 54, 54)),
                Foreground = TextBrush,
                BorderBrush = BorderBrush
            };
            button.Click += click;
            return button;
        }
    }
}
