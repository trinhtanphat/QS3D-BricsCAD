using System;
using System.Globalization;
using System.Windows;
using Bricscad.ApplicationServices;
using Application = Bricscad.ApplicationServices.Application;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RebarMeshSetupWindow : Window
    {
        private readonly Document _document;
        private readonly ProjectState _project;
        private readonly ProjectElement _element;
        private readonly Action _saved;
        private readonly MeshKeys _keys;

        private sealed class MeshKeys
        {
            public string ContextLabel { get; set; } = string.Empty;
            public string Direction1Label { get; set; } = string.Empty;
            public string Direction2Label { get; set; } = string.Empty;
            public string Direction1Key { get; set; } = string.Empty;
            public string Direction2Key { get; set; } = string.Empty;
            public string CoverKey { get; set; } = string.Empty;
            public string FacesKey { get; set; } = string.Empty;
            public string ClosestKey { get; set; } = string.Empty;
            public string[] Faces { get; set; } = Array.Empty<string>();
            public string DefaultDirection1 { get; set; } = string.Empty;
            public string DefaultDirection2 { get; set; } = string.Empty;
            public string DefaultCover { get; set; } = string.Empty;
        }

        public RebarMeshSetupWindow(Document document, ProjectState project, ProjectElement element, Action saved)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _saved = saved ?? throw new ArgumentNullException(nameof(saved));
            _keys = KeysFor(element.Category) ?? throw new ArgumentException("Rebar Mesh Setup only supports Slab, StructuralWall or Foundation.", nameof(element));
            InitializeComponent();
            DocumentBoundWindowLifetime.Attach(this, _document);
            Configure();
            Title = "QS3D • Rebar Mesh Setup • " + DrawingLabel(_document);
        }

        private void Configure()
        {
            ContextText.Text = _keys.ContextLabel + " • " + _element.Id;
            Direction1Label.Text = _keys.Direction1Label;
            Direction2Label.Text = _keys.Direction2Label;
            Direction1Text.Text = Effective(_keys.Direction1Key, _keys.DefaultDirection1);
            Direction2Text.Text = Effective(_keys.Direction2Key, _keys.DefaultDirection2);
            CoverText.Text = Effective(_keys.CoverKey, string.Empty);
            if (string.IsNullOrWhiteSpace(CoverText.Text)) CoverText.Text = Effective("RebarCoverM", _keys.DefaultCover);

            FacesCombo.Items.Clear();
            foreach (var value in _keys.Faces) FacesCombo.Items.Add(value);
            FacesCombo.SelectedIndex = 0;
            var currentFaces = Effective(_keys.FacesKey, _keys.Faces[0]);
            for (var index = 0; index < FacesCombo.Items.Count; index++)
            {
                if (!string.Equals(Convert.ToString(FacesCombo.Items[index], CultureInfo.InvariantCulture), currentFaces, StringComparison.OrdinalIgnoreCase)) continue;
                FacesCombo.SelectedIndex = index;
                break;
            }

            var closest = Effective(_keys.ClosestKey, "true");
            if (bool.TryParse(closest, out var boolValue)) ClosestToFaceCheck.IsChecked = boolValue;
            else if (closest == "1") ClosestToFaceCheck.IsChecked = true;
            else if (closest == "0") ClosestToFaceCheck.IsChecked = false;
            else ClosestToFaceCheck.IsChecked = null;
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureActive("lưu Rebar Mesh Setup");
                ParseSingleDistribution(Direction1Text.Text, _keys.Direction1Key);
                ParseSingleDistribution(Direction2Text.Text, _keys.Direction2Key);
                if (!double.TryParse((CoverText.Text ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var cover) || double.IsNaN(cover) || double.IsInfinity(cover) || cover < 0d)
                    throw new InvalidOperationException("Cover phải là số hữu hạn >= 0, dùng dấu chấm thập phân theo dữ liệu QS3D.");
                if (!(FacesCombo.SelectedItem is string faces) || string.IsNullOrWhiteSpace(faces))
                    throw new InvalidOperationException("Chọn face(s) cần dựng mesh.");
                if (!ClosestToFaceCheck.IsChecked.HasValue)
                    throw new InvalidOperationException("Chọn rõ phương nào nằm gần mặt bê tông hơn.");

                var project = ProjectContextCoordinator.GetOrCreate(_document);
                if (!ReferenceEquals(project, _project))
                    throw new InvalidOperationException("Project của DWG này đã được reload/thay thế trong khi Rebar Mesh Setup đang mở. Đóng và mở lại cửa sổ trước khi lưu.");
                var element = project.FindElement(_element.Id) ?? throw new InvalidOperationException("Semantic element " + _element.Id + " không còn tồn tại trong project hiện tại. Đóng và mở lại Rebar Mesh Setup.");
                if (element.Category != _element.Category || KeysFor(element.Category) == null)
                    throw new InvalidOperationException("Semantic element " + _element.Id + " đã đổi category. Đóng và mở lại Rebar Mesh Setup.");

                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    element.SetProperty(_keys.Direction1Key, Direction1Text.Text.Trim());
                    element.SetProperty(_keys.Direction2Key, Direction2Text.Text.Trim());
                    element.SetProperty(_keys.CoverKey, cover.ToString("R", CultureInfo.InvariantCulture));
                    element.SetProperty(_keys.FacesKey, faces.Trim());
                    element.SetProperty(_keys.ClosestKey, ClosestToFaceCheck.IsChecked.Value ? "true" : "false");
                    project.Touch();
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError);
                    throw;
                }

                NotifySavedAfterCommit();
                ValidationText.Text = "Đã lưu thông số explicit. Hai phương có thể dùng diameter/count/spacing độc lập; generated mesh cũ (nếu có) đã stale và cần rebuild.";
                Close();
            }
            catch (Exception ex)
            {
                ValidationText.Text = ex.Message;
            }
        }

        private void NotifySavedAfterCommit()
        {
            try
            {
                _saved();
            }
            catch (Exception callbackError)
            {
                try
                {
                    _document.Editor.WriteMessage("\nQS3D Rebar Mesh Setup đã commit; UI sync warning: " + callbackError.Message);
                }
                catch { }
            }
        }

        private static void RestoreOrThrow(ProjectState project, ProjectStateSnapshot rollback, Exception operationError)
        {
            try
            {
                rollback.Restore(project);
            }
            catch (Exception restoreError)
            {
                throw new InvalidOperationException(
                    "Lưu Rebar Mesh Setup thất bại và rollback project cũng không hoàn tất.",
                    new AggregateException(operationError, restoreError));
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e) => Close();

        private static RebarGroup ParseSingleDistribution(string raw, string label)
        {
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException(label + " là bắt buộc.");
            var groups = RebarNotationParser.Parse(raw.Trim());
            if (groups.Count != 1) throw new InvalidOperationException(label + " chỉ hỗ trợ một group trong native mesh hiện tại.");
            var group = groups[0];
            if (!group.Quantity.HasValue && !group.SpacingMm.HasValue) throw new InvalidOperationException(label + " phải có count hoặc spacing.");
            if (group.Quantity.HasValue && group.SpacingMm.HasValue) throw new InvalidOperationException(label + " không được đồng thời có count và spacing.");
            return group;
        }

        private string Effective(string key, string fallback)
        {
            if (_element.Properties.TryGetValue(key, out var own) && !string.IsNullOrWhiteSpace(own)) return own.Trim();
            var family = _project.FindFamily(_element.FamilyId);
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) return inherited.Trim();
            return fallback;
        }

        private void EnsureActive(string operation)
        {
            if (!ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document))
                throw new InvalidOperationException("Hãy kích hoạt lại đúng bản vẽ đã mở Rebar Mesh Setup trước khi " + operation + ".");
        }

        private static string DrawingLabel(Document document)
        {
            var name = document.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name)) return "Bản vẽ chưa lưu";
            try { return System.IO.Path.GetFileName(name); }
            catch { return name; }
        }

        private static MeshKeys? KeysFor(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Slab:
                    return new MeshKeys
                    {
                        ContextLabel = "Sàn",
                        Direction1Label = "Phương X notation",
                        Direction2Label = "Phương Y notation",
                        Direction1Key = "RebarSlabXNotation",
                        Direction2Key = "RebarSlabYNotation",
                        CoverKey = "RebarSlabCoverM",
                        FacesKey = "RebarSlabFaces",
                        ClosestKey = "RebarSlabXClosestToFace",
                        Faces = new[] { "Bottom", "Top", "Both" },
                        DefaultDirection1 = "D12@200",
                        DefaultDirection2 = "D12@200",
                        DefaultCover = "0.025"
                    };
                case ElementCategory.StructuralWall:
                    return new MeshKeys
                    {
                        ContextLabel = "Vách BTCT",
                        Direction1Label = "Phương ngang notation",
                        Direction2Label = "Phương đứng notation",
                        Direction1Key = "RebarWallHorizontalNotation",
                        Direction2Key = "RebarWallVerticalNotation",
                        CoverKey = "RebarWallCoverM",
                        FacesKey = "RebarWallFaces",
                        ClosestKey = "RebarWallHorizontalClosestToFace",
                        Faces = new[] { "Near", "Far", "Both" },
                        DefaultDirection1 = "D12@200",
                        DefaultDirection2 = "D12@200",
                        DefaultCover = "0.025"
                    };
                case ElementCategory.Foundation:
                    return new MeshKeys
                    {
                        ContextLabel = "Móng",
                        Direction1Label = "Phương X notation",
                        Direction2Label = "Phương Y notation",
                        Direction1Key = "RebarFoundationXNotation",
                        Direction2Key = "RebarFoundationYNotation",
                        CoverKey = "RebarFoundationCoverM",
                        FacesKey = "RebarFoundationFaces",
                        ClosestKey = "RebarFoundationXClosestToFace",
                        Faces = new[] { "Bottom", "Top", "Both" },
                        DefaultDirection1 = "D16@200",
                        DefaultDirection2 = "D16@200",
                        DefaultCover = "0.05"
                    };
                default:
                    return null;
            }
        }
    }
}
