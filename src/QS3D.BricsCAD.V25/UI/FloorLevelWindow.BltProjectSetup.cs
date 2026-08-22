using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow
    {
        private const string BltMetadataPrefix = "QS3D.BLT3D.ProjectSetup.Floor.";
        private readonly ObservableCollection<BltZoneRow> _bltZones = new ObservableCollection<BltZoneRow>();
        private readonly ObservableCollection<BltFloorRow> _bltFloors = new ObservableCollection<BltFloorRow>();
        private bool _bltLoading;

        private void OnBltSetupLoaded(object sender, RoutedEventArgs e)
        {
            // The legacy refresh binds the existing project instance and retains semantic
            // level safety checks. The BLT view is a second presentation over the same state.
            RefreshAll();
            BltZoneList.ItemsSource = _bltZones;
            BltFloorGrid.ItemsSource = _bltFloors;
            RefreshBltSetup();
        }

        private void OnBltProjectInfoClick(object sender, RoutedEventArgs e) => OpenProjectTools("Thông tin dự án");
        private void OnBltProjectPropertiesClick(object sender, RoutedEventArgs e) => OpenDedicatedBltProjectProperties();

        private void OnBltFloorSettingsClick(object sender, RoutedEventArgs e)
        {
            RefreshBltSetup();
            SetBltStatus("Cài đặt tầng: chỉnh dữ liệu trong bảng rồi bấm Áp dụng thay đổi.");
        }

        private void OpenProjectTools(string section)
        {
            try
            {
                var window = new ProjectToolsWindow(_document);
                Bricscad.ApplicationServices.Application.ShowModelessWindow(IntPtr.Zero, window, true);
                SetBltStatus(section + " đã mở trong Project Tools.");
            }
            catch (Exception ex)
            {
                SetBltStatus(section + " lỗi: " + ex.Message);
            }
        }

        private void RefreshBltSetup(string preferredFloorId = "", string preferredZoneId = "")
        {
            try
            {
                var selectedFloorId = !string.IsNullOrWhiteSpace(preferredFloorId)
                    ? preferredFloorId
                    : (BltFloorGrid.SelectedItem as BltFloorRow)?.Id ?? string.Empty;
                var selectedZoneId = !string.IsNullOrWhiteSpace(preferredZoneId)
                    ? preferredZoneId
                    : (BltZoneList.SelectedItem as BltZoneRow)?.Id ?? string.Empty;

                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                {
                    _bltLoading = true;
                    try
                    {
                        _bltZones.Clear();
                        _bltFloors.Clear();
                    }
                    finally { _bltLoading = false; }
                    SetBltStatus("Chưa có QS3D project cho bản vẽ này. Hãy nạp hoặc tạo project trước khi cấu hình tầng.");
                    return;
                }

                var orderedFloors = OrderFloors(project);
                _bltLoading = true;
                try
                {
                    _bltZones.Clear();
                    foreach (var zone in project.Zones)
                        _bltZones.Add(new BltZoneRow(zone.Id, zone.Name));

                    _bltFloors.Clear();
                    for (var i = 0; i < orderedFloors.Count; i++)
                    {
                        var floor = orderedFloors[i];
                        var inferredHeight = i == 0
                            ? 3.3d
                            : Math.Max(0d, orderedFloors[i - 1].ElevationM - floor.ElevationM);
                        var height = ReadMetadataDouble(project, floor.Id, "height", inferredHeight);
                        var code = ReadMetadata(project, floor.Id, "code", (orderedFloors.Count - i).ToString(CultureInfo.InvariantCulture));
                        var typical = ReadMetadataInt(project, floor.Id, "typical", 1);
                        var comment = ReadMetadata(project, floor.Id, "comment", string.Empty);

                        _bltFloors.Add(new BltFloorRow(
                            floor.Id,
                            code,
                            floor.Name,
                            height.ToString("0.000", CultureInfo.InvariantCulture),
                            floor.ElevationM.ToString("0.000", CultureInfo.InvariantCulture),
                            typical.ToString(CultureInfo.InvariantCulture),
                            comment,
                            string.Equals(project.ActiveFloorId, floor.Id, StringComparison.OrdinalIgnoreCase)));
                    }

                    BltZoneList.SelectedItem = _bltZones.FirstOrDefault(x => string.Equals(x.Id, selectedZoneId, StringComparison.OrdinalIgnoreCase))
                        ?? _bltZones.FirstOrDefault(x => string.Equals(x.Id, project.ActiveZoneId, StringComparison.OrdinalIgnoreCase))
                        ?? _bltZones.FirstOrDefault();
                    BltFloorGrid.SelectedItem = _bltFloors.FirstOrDefault(x => string.Equals(x.Id, selectedFloorId, StringComparison.OrdinalIgnoreCase))
                        ?? _bltFloors.FirstOrDefault(x => string.Equals(x.Id, project.ActiveFloorId, StringComparison.OrdinalIgnoreCase))
                        ?? _bltFloors.FirstOrDefault();
                }
                finally { _bltLoading = false; }

                Title = "QS3D — Thiết lập dự án — " + DrawingLabel(_document);
                SetBltStatus(_bltFloors.Count == 0
                    ? "Project chưa có tầng. Dùng Chèn sàn để tạo tầng đầu tiên."
                    : "Đã nạp " + _bltFloors.Count.ToString(CultureInfo.InvariantCulture) + " tầng và " + _bltZones.Count.ToString(CultureInfo.InvariantCulture) + " vùng.");
            }
            catch (Exception ex)
            {
                SetBltStatus("Đọc Thiết lập dự án lỗi: " + ex.Message);
            }
        }

        private void OnBltAddZoneClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var project = RequireBoundProjectForMutation("thêm vùng", "Thêm vùng Project Setup");
                var rollback = ProjectStateSnapshot.Capture(project);
                ZoneDefinition zone;
                try
                {
                    var name = NextZoneName(project);
                    zone = ProjectZoneService.Create(project, "zone-" + Guid.NewGuid().ToString("N"), name);
                    AuditTrail.ForProject(project).Record("zone.create", string.Empty, zone.Id + " • " + zone.Name + " • BLT project setup");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Thêm vùng Project Setup");
                    throw;
                }

                RefreshAfterCommit(
                    () => { RefreshAll(); RefreshBltSetup(string.Empty, zone.Id); },
                    "Đã thêm “" + zone.Name + "”.",
                    "BLT project setup zone create");
            }
            catch (Exception ex) { SetBltStatus("Thêm vùng lỗi: " + ex.Message); }
        }

        private void OnBltDeleteZoneClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(BltZoneList.SelectedItem is BltZoneRow selected))
                    throw new InvalidOperationException("Chọn một vùng trước khi xóa.");
                var project = RequireBoundProjectForMutation("xóa vùng", "Xóa vùng Project Setup");
                var zone = project.Zones.FirstOrDefault(x => string.Equals(x.Id, selected.Id, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Vùng đã chọn không còn tồn tại.");
                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    if (string.Equals(project.ActiveZoneId, zone.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        var replacement = project.Zones.FirstOrDefault(x => !string.Equals(x.Id, zone.Id, StringComparison.OrdinalIgnoreCase));
                        if (replacement != null) ProjectZoneService.SetActive(project, replacement.Id);
                    }
                    if (!ProjectZoneService.Delete(project, zone.Id)) return;
                    AuditTrail.ForProject(project).Record("zone.delete", string.Empty, zone.Id + " • " + zone.Name + " • BLT project setup");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Xóa vùng Project Setup");
                    throw;
                }

                RefreshAfterCommit(
                    () => { RefreshAll(); RefreshBltSetup(); },
                    "Đã xóa vùng “" + zone.Name + "”.",
                    "BLT project setup zone delete");
            }
            catch (Exception ex) { SetBltStatus("Xóa vùng lỗi: " + ex.Message); }
        }

        private void OnBltZoneSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_bltLoading || !(BltZoneList.SelectedItem is BltZoneRow selected)) return;
            try
            {
                var project = RequireBoundProjectForMutation("chọn vùng", "Đặt vùng hoạt động Project Setup");
                if (string.Equals(project.ActiveZoneId, selected.Id, StringComparison.OrdinalIgnoreCase)) return;
                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    ProjectZoneService.SetActive(project, selected.Id);
                    AuditTrail.ForProject(project).Record("zone.activate", string.Empty, selected.Id + " • " + selected.Name + " • BLT project setup");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Đặt vùng hoạt động Project Setup");
                    throw;
                }
                PaletteCoordinator.RefreshProject();
                SetBltStatus("Vùng hoạt động: " + selected.Name + ".");
            }
            catch (Exception ex) { SetBltStatus("Chọn vùng lỗi: " + ex.Message); }
        }

        private void OnBltInsertFloorClick(object sender, RoutedEventArgs e) => InsertBltFloor(false);
        private void OnBltInsertFloorBelowClick(object sender, RoutedEventArgs e) => InsertBltFloor(true);

        private void InsertBltFloor(bool below)
        {
            try
            {
                CommitBltGridEdits();
                var project = RequireBoundProjectForMutation(below ? "chèn sàn xuống dưới" : "chèn sàn", below ? "Chèn sàn dưới Project Setup" : "Chèn sàn Project Setup");
                var selected = BltFloorGrid.SelectedItem as BltFloorRow;
                var selectedElevation = selected == null ? (project.Floors.Count == 0 ? 0d : project.Floors.Max(x => x.ElevationM)) : ParseFinite(selected.ElevationText, "Độ cao đáy");
                var selectedHeight = selected == null ? 3.3d : ParseNonNegative(selected.HeightText, "Chiều cao sàn");
                var step = selectedHeight > 0.000001d ? selectedHeight : 3.3d;
                var elevation = project.Floors.Count == 0
                    ? 0d
                    : below ? selectedElevation - step : selectedElevation + step;

                var rollback = ProjectStateSnapshot.Capture(project);
                FloorDefinition floor;
                try
                {
                    var name = NextFloorName(project);
                    floor = ProjectFloorService.Create(project, "floor-" + Guid.NewGuid().ToString("N"), name, elevation);
                    var nextCode = NextFloorCode();
                    WriteMetadata(project, floor.Id, "code", nextCode);
                    WriteMetadata(project, floor.Id, "height", "3.300");
                    WriteMetadata(project, floor.Id, "typical", "1");
                    WriteMetadata(project, floor.Id, "comment", string.Empty);
                    AuditTrail.ForProject(project).Record("floor.create", string.Empty, floor.Id + " • " + floor.Name + " • " + elevation.ToString("R", CultureInfo.InvariantCulture) + "m • BLT project setup");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, below ? "Chèn sàn dưới Project Setup" : "Chèn sàn Project Setup");
                    throw;
                }

                RefreshAfterCommit(
                    () => { RefreshAll(floor.Id); RefreshBltSetup(floor.Id); },
                    "Đã " + (below ? "chèn sàn dưới" : "chèn sàn") + " “" + floor.Name + "”.",
                    "BLT project setup floor insert");
            }
            catch (Exception ex) { SetBltStatus((below ? "Chèn sàn dưới" : "Chèn sàn") + " lỗi: " + ex.Message); }
        }

        private void OnBltDeleteFloorClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(BltFloorGrid.SelectedItem is BltFloorRow selected))
                    throw new InvalidOperationException("Chọn một tầng trước khi xóa.");
                var project = RequireBoundProjectForMutation("xóa sàn", "Xóa sàn Project Setup");
                var floor = project.Floors.FirstOrDefault(x => string.Equals(x.Id, selected.Id, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("Tầng đã chọn không còn tồn tại.");
                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    if (string.Equals(project.ActiveFloorId, floor.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        var replacement = project.Floors.FirstOrDefault(x => !string.Equals(x.Id, floor.Id, StringComparison.OrdinalIgnoreCase));
                        if (replacement == null)
                            throw new InvalidOperationException("Không thể xóa tầng cuối cùng đang làm tầng tham chiếu.");
                        ProjectFloorService.SetActive(project, replacement.Id);
                    }
                    if (!ProjectFloorService.Delete(project, floor.Id)) return;
                    RemoveFloorMetadata(project, floor.Id);
                    AuditTrail.ForProject(project).Record("floor.delete", string.Empty, floor.Id + " • " + floor.Name + " • BLT project setup");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Xóa sàn Project Setup");
                    throw;
                }

                RefreshAfterCommit(
                    () => { RefreshAll(); RefreshBltSetup(); },
                    "Đã xóa sàn “" + floor.Name + "”.",
                    "BLT project setup floor delete");
            }
            catch (Exception ex) { SetBltStatus("Xóa sàn lỗi: " + ex.Message); }
        }

        private void OnBltReferenceClick(object sender, RoutedEventArgs e)
        {
            if (_bltLoading || !(sender is CheckBox checkBox) || !(checkBox.Tag is BltFloorRow row) || checkBox.IsChecked != true) return;
            _bltLoading = true;
            try
            {
                foreach (var item in _bltFloors)
                    item.IsReference = ReferenceEquals(item, row);
            }
            finally { _bltLoading = false; }
        }

        private void OnBltApplyChangesClick(object sender, RoutedEventArgs e)
        {
            try
            {
                CommitBltGridEdits();
                var project = RequireBoundProjectForMutation("áp dụng thay đổi", "Áp dụng thay đổi Project Setup");
                var rows = _bltFloors.ToList();
                if (rows.Select(x => (x.Name ?? string.Empty).Trim()).Any(string.IsNullOrWhiteSpace))
                    throw new InvalidOperationException("Tên tầng không được để trống.");
                var duplicateName = rows.GroupBy(x => (x.Name ?? string.Empty).Trim(), StringComparer.CurrentCultureIgnoreCase).FirstOrDefault(x => x.Count() > 1);
                if (duplicateName != null)
                    throw new InvalidOperationException("Tên tầng bị trùng: " + duplicateName.Key + ".");

                var parsed = rows.Select(row => new ParsedBltFloor(
                    row,
                    RequireCode(row.Code),
                    (row.Name ?? string.Empty).Trim(),
                    ParseNonNegative(row.HeightText, "Chiều cao sàn"),
                    ParseFinite(row.ElevationText, "Độ cao đáy"),
                    ParseTypicalCount(row.TypicalCountText),
                    row.Comment ?? string.Empty)).ToList();

                var rollback = ProjectStateSnapshot.Capture(project);
                try
                {
                    foreach (var item in parsed)
                    {
                        var floor = project.Floors.FirstOrDefault(x => string.Equals(x.Id, item.Row.Id, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException("Tầng “" + item.Name + "” đã thay đổi hoặc bị xóa. Hãy làm mới.");
                        ProjectFloorService.Update(project, floor.Id, item.Name, item.Elevation);
                        WriteMetadata(project, floor.Id, "code", item.Code);
                        WriteMetadata(project, floor.Id, "height", item.Height.ToString("0.000", CultureInfo.InvariantCulture));
                        WriteMetadata(project, floor.Id, "typical", item.TypicalCount.ToString(CultureInfo.InvariantCulture));
                        WriteMetadata(project, floor.Id, "comment", item.Comment);
                    }

                    var reference = parsed.FirstOrDefault(x => x.Row.IsReference);
                    if (reference != null)
                        ProjectFloorService.SetActive(project, reference.Row.Id);
                    AuditTrail.ForProject(project).Record("floor.setup.apply", string.Empty, parsed.Count.ToString(CultureInfo.InvariantCulture) + " floor(s) • BLT project setup");
                }
                catch (Exception operationError)
                {
                    RestoreOrThrow(project, rollback, operationError, "Áp dụng thay đổi Project Setup");
                    throw;
                }

                var preferred = (BltFloorGrid.SelectedItem as BltFloorRow)?.Id ?? string.Empty;
                RefreshAfterCommit(
                    () => { RefreshAll(preferred); RefreshBltSetup(preferred); },
                    "Đã áp dụng thay đổi cho " + parsed.Count.ToString(CultureInfo.InvariantCulture) + " tầng.",
                    "BLT project setup apply");
            }
            catch (Exception ex) { SetBltStatus("Áp dụng thay đổi lỗi: " + ex.Message); }
        }

        private void CommitBltGridEdits()
        {
            BltFloorGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            BltFloorGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }

        private static List<FloorDefinition> OrderFloors(ProjectState project) => project.Floors
            .Select((floor, index) => new { floor, index })
            .OrderByDescending(x => x.floor.ElevationM)
            .ThenBy(x => x.index)
            .Select(x => x.floor)
            .ToList();

        private static string NextZoneName(ProjectState project)
        {
            for (var i = 1; i < 10000; i++)
            {
                var candidate = "Vùng-" + i.ToString(CultureInfo.InvariantCulture);
                if (!project.Zones.Any(x => string.Equals(x.Name, candidate, StringComparison.CurrentCultureIgnoreCase))) return candidate;
            }
            return "Vùng-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        private static string NextFloorName(ProjectState project)
        {
            for (var i = 1; i < 10000; i++)
            {
                var candidate = "Tầng " + i.ToString(CultureInfo.InvariantCulture);
                if (!project.Floors.Any(x => string.Equals(x.Name, candidate, StringComparison.CurrentCultureIgnoreCase))) return candidate;
            }
            return "Tầng " + Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        private string NextFloorCode()
        {
            var values = _bltFloors
                .Select(x => int.TryParse((x.Code ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0)
                .ToList();
            return ((values.Count == 0 ? 0 : values.Max()) + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static string MetadataKey(string floorId, string suffix) => BltMetadataPrefix + floorId + "." + suffix;

        private static string ReadMetadata(ProjectState project, string floorId, string suffix, string fallback) =>
            project.Metadata.TryGetValue(MetadataKey(floorId, suffix), out var value) ? value : fallback;

        private static double ReadMetadataDouble(ProjectState project, string floorId, string suffix, double fallback)
        {
            var raw = ReadMetadata(project, floorId, suffix, string.Empty);
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && !double.IsNaN(value) && !double.IsInfinity(value)
                ? value
                : fallback;
        }

        private static int ReadMetadataInt(ProjectState project, string floorId, string suffix, int fallback)
        {
            var raw = ReadMetadata(project, floorId, suffix, string.Empty);
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0 ? value : fallback;
        }

        private static void WriteMetadata(ProjectState project, string floorId, string suffix, string value) =>
            project.Metadata[MetadataKey(floorId, suffix)] = value ?? string.Empty;

        private static void RemoveFloorMetadata(ProjectState project, string floorId)
        {
            foreach (var suffix in new[] { "code", "height", "typical", "comment" })
                project.Metadata.Remove(MetadataKey(floorId, suffix));
        }

        private static double ParseFinite(string raw, string label)
        {
            var text = (raw ?? string.Empty).Trim();
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                throw new InvalidOperationException(label + " không phải số hợp lệ.");
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(label + " phải là số hữu hạn.");
            return value == 0d ? 0d : value;
        }

        private static double ParseNonNegative(string raw, string label)
        {
            var value = ParseFinite(raw, label);
            if (value < 0d) throw new InvalidOperationException(label + " không được âm.");
            return value;
        }

        private static int ParseTypicalCount(string raw)
        {
            if (!int.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value < 1 || value > 9999)
                throw new InvalidOperationException("Số tầng điển hình phải là số nguyên từ 1 đến 9999.");
            return value;
        }

        private static string RequireCode(string raw)
        {
            var value = (raw ?? string.Empty).Trim();
            if (value.Length == 0) throw new InvalidOperationException("Mã hóa tầng không được để trống.");
            if (value.Length > 32) throw new InvalidOperationException("Mã hóa tầng tối đa 32 ký tự.");
            if (value.Any(char.IsControl)) throw new InvalidOperationException("Mã hóa tầng chứa ký tự điều khiển không hợp lệ.");
            return value;
        }

        private void SetBltStatus(string text)
        {
            try { BltStatusText.Text = text ?? string.Empty; } catch { }
            try { PaletteCoordinator.SetStatus(text ?? string.Empty); } catch { }
        }

        private sealed class BltZoneRow
        {
            public BltZoneRow(string id, string name) { Id = id; Name = name; }
            public string Id { get; }
            public string Name { get; }
            public override string ToString() => Name;
        }

        private sealed class BltFloorRow : INotifyPropertyChanged
        {
            private string _code;
            private string _name;
            private string _heightText;
            private string _elevationText;
            private string _typicalCountText;
            private string _comment;
            private bool _isReference;

            public BltFloorRow(string id, string code, string name, string heightText, string elevationText, string typicalCountText, string comment, bool isReference)
            {
                Id = id;
                _code = code;
                _name = name;
                _heightText = heightText;
                _elevationText = elevationText;
                _typicalCountText = typicalCountText;
                _comment = comment;
                _isReference = isReference;
            }

            public string Id { get; }
            public string Code { get => _code; set => Set(ref _code, value ?? string.Empty); }
            public string Name { get => _name; set => Set(ref _name, value ?? string.Empty); }
            public string HeightText { get => _heightText; set => Set(ref _heightText, value ?? string.Empty); }
            public string ElevationText { get => _elevationText; set => Set(ref _elevationText, value ?? string.Empty); }
            public string TypicalCountText { get => _typicalCountText; set => Set(ref _typicalCountText, value ?? string.Empty); }
            public string Comment { get => _comment; set => Set(ref _comment, value ?? string.Empty); }
            public bool IsReference { get => _isReference; set => Set(ref _isReference, value); }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void Set<T>(ref T field, T value, [CallerMemberName] string name = "")
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return;
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        private sealed class ParsedBltFloor
        {
            public ParsedBltFloor(BltFloorRow row, string code, string name, double height, double elevation, int typicalCount, string comment)
            {
                Row = row;
                Code = code;
                Name = name;
                Height = height;
                Elevation = elevation;
                TypicalCount = typicalCount;
                Comment = comment;
            }

            public BltFloorRow Row { get; }
            public string Code { get; }
            public string Name { get; }
            public double Height { get; }
            public double Elevation { get; }
            public int TypicalCount { get; }
            public string Comment { get; }
        }
    }
}
