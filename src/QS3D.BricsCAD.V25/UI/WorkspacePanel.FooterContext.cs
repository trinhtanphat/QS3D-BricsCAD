using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class WorkspacePanel
    {
        private TextBlock? _footerContextText;
        private bool _footerContextAttached;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Loaded += OnFooterContextLoaded;
        }

        private void OnFooterContextLoaded(object sender, RoutedEventArgs e)
        {
            AttachFooterContextPresentation();
            RefreshFooterContext();
        }

        private void AttachFooterContextPresentation()
        {
            if (_footerContextAttached)
                return;

            var liveSemantic = FindVisualChildren<TextBlock>(this)
                .FirstOrDefault(text => string.Equals(text.Text, "LIVE SEMANTIC", StringComparison.Ordinal));
            var statusPanel = liveSemantic == null
                ? null
                : VisualTreeHelper.GetParent(liveSemantic) as StackPanel;
            var footer = statusPanel == null
                ? null
                : VisualTreeHelper.GetParent(statusPanel) as DockPanel;
            if (footer == null)
                return;

            var context = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(12, 0, 12, 0),
                MinWidth = 0,
                ToolTip = "Project / Zone / Floor hiện hành"
            };
            if (TryFindResource("Caption") is Style captionStyle)
                context.Style = captionStyle;

            footer.LastChildFill = true;
            footer.Children.Add(context);
            _footerContextText = context;

            ZoneCombo.SelectionChanged += (_, __) => RefreshFooterContext();
            FloorCombo.SelectionChanged += (_, __) => RefreshFooterContext();
            DataContextChanged += (_, __) => RefreshFooterContext();
            IsVisibleChanged += (_, __) => RefreshFooterContext();

            _footerContextAttached = true;
        }

        private void RefreshFooterContext()
        {
            if (_footerContextText == null)
                return;

            var projectName = "—";
            var zoneName = "—";
            var floorName = "—";

            try
            {
                var document = Application.DocumentManager.MdiActiveDocument;
                if (document != null && ExistingProjectMutationContext.TryGet(document, out var project))
                {
                    projectName = NormalizeFooterName(project.Name);
                    zoneName = NormalizeFooterName(project.FindZone(project.ActiveZoneId)?.Name);
                    floorName = NormalizeFooterName(project.FindFloor(project.ActiveFloorId)?.Name);
                }
            }
            catch
            {
                // Footer context is presentation-only and must never break Workspace interaction.
            }

            _footerContextText.Text =
                "PROJECT  " + projectName +
                "   •   ZONE  " + zoneName +
                "   •   FLOOR  " + floorName;
        }

        private static string NormalizeFooterName(string? value)
        {
            var normalized = value ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalized) ? "—" : normalized.Trim();
        }
    }
}
