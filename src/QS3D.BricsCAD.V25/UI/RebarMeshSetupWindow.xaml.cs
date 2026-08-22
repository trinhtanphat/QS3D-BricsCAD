using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RebarMeshSetupWindow : Window
    {
        private readonly ProjectState _project;
        private readonly ProjectElement _element;
        private readonly Action _saved;
        private readonly bool _slab;

        public RebarMeshSetupWindow(ProjectState project, ProjectElement element, Action saved)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _saved = saved ?? throw new ArgumentNullException(nameof(saved));
            if (element.Category != ElementCategory.Slab && element.Category != ElementCategory.StructuralWall)
                throw new ArgumentException("Rebar Mesh Setup only supports Slab or StructuralWall.", nameof(element));
            _slab = element.Category == ElementCategory.Slab;
            InitializeComponent();
            Configure();
        }

        private void Configure()
        {
            ContextText.Text = (_slab ? "Sàn" : "Vách BTCT") + " • " + _element.Id;
            Direction1Label.Text = _slab ? "Phương X notation" : "Phương ngang notation";
            Direction2Label.Text = _slab ? "Phương Y notation" : "Phương đứng notation";
            Direction1Text.Text = Effective(_slab ? "RebarSlabXNotation" : "RebarWallHorizontalNotation");
            Direction2Text.Text = Effective(_slab ? "RebarSlabYNotation" : "RebarWallVerticalNotation");
            CoverText.Text = Effective(_slab ? "RebarSlabCoverM" : "RebarWallCoverM");
            if (string.IsNullOrWhiteSpace(CoverText.Text)) CoverText.Text = Effective("RebarCoverM");

            FacesCombo.Items.Clear();
            foreach (var value in (_slab ? new[] { "Bottom", "Top", "Both" } : new[] { "Near", "Far", "Both" })) FacesCombo.Items.Add(value);
            var currentFaces = Effective(_slab ? "RebarSlabFaces" : "RebarWallFaces");
            if (!string.IsNullOrWhiteSpace(currentFaces))
            {
                for (var index = 0; index < FacesCombo.Items.Count; index++)
                {
                    if (!string.Equals(Convert.ToString(FacesCombo.Items[index], CultureInfo.InvariantCulture), currentFaces, StringComparison.OrdinalIgnoreCase)) continue;
                    FacesCombo.SelectedIndex = index;
                    break;
                }
            }

            var closest = Effective(_slab ? "RebarSlabXClosestToFace" : "RebarWallHorizontalClosestToFace");
            if (bool.TryParse(closest, out var boolValue)) ClosestToFaceCheck.IsChecked = boolValue;
            else if (closest == "1") ClosestToFaceCheck.IsChecked = true;
            else if (closest == "0") ClosestToFaceCheck.IsChecked = false;
            else ClosestToFaceCheck.IsChecked = null;
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            try
            {
                var first = ParseSingleDistribution(Direction1Text.Text, _slab ? "RebarSlabXNotation" : "RebarWallHorizontalNotation");
                var second = ParseSingleDistribution(Direction2Text.Text, _slab ? "RebarSlabYNotation" : "RebarWallVerticalNotation");
                if (Math.Abs(first.DiameterMm - second.DiameterMm) > 1e-9d)
                    throw new InvalidOperationException("Native mesh hiện yêu cầu hai phương cùng đường kính. Không tự đổi diameter để tránh thay đổi thiết kế của người dùng.");
                if (!double.TryParse((CoverText.Text ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var cover) || double.IsNaN(cover) || double.IsInfinity(cover) || cover < 0d)
                    throw new InvalidOperationException("Cover phải là số hữu hạn >= 0, dùng dấu chấm thập phân theo dữ liệu QS3D.");
                if (!(FacesCombo.SelectedItem is string faces) || string.IsNullOrWhiteSpace(faces))
                    throw new InvalidOperationException("Chọn face(s) cần dựng mesh.");
                if (!ClosestToFaceCheck.IsChecked.HasValue)
                    throw new InvalidOperationException("Chọn rõ phương nào nằm gần mặt bê tông hơn.");

                if (_slab)
                {
                    _element.SetProperty("RebarSlabXNotation", Direction1Text.Text.Trim());
                    _element.SetProperty("RebarSlabYNotation", Direction2Text.Text.Trim());
                    _element.SetProperty("RebarSlabCoverM", cover.ToString("R", CultureInfo.InvariantCulture));
                    _element.SetProperty("RebarSlabFaces", faces.Trim());
                    _element.SetProperty("RebarSlabXClosestToFace", ClosestToFaceCheck.IsChecked.Value ? "true" : "false");
                }
                else
                {
                    _element.SetProperty("RebarWallHorizontalNotation", Direction1Text.Text.Trim());
                    _element.SetProperty("RebarWallVerticalNotation", Direction2Text.Text.Trim());
                    _element.SetProperty("RebarWallCoverM", cover.ToString("R", CultureInfo.InvariantCulture));
                    _element.SetProperty("RebarWallFaces", faces.Trim());
                    _element.SetProperty("RebarWallHorizontalClosestToFace", ClosestToFaceCheck.IsChecked.Value ? "true" : "false");
                }
                _project.Touch();
                _saved();
                ValidationText.Text = "Đã lưu thông số explicit. Generated rebar cũ (nếu có) đã được đánh dấu stale bởi semantic mutation và cần rebuild.";
                Close();
            }
            catch (Exception ex)
            {
                ValidationText.Text = ex.Message;
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

        private string Effective(string key)
        {
            if (_element.Properties.TryGetValue(key, out var own) && !string.IsNullOrWhiteSpace(own)) return own.Trim();
            var family = _project.FindFamily(_element.FamilyId);
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited)) return inherited.Trim();
            return string.Empty;
        }
    }
}
