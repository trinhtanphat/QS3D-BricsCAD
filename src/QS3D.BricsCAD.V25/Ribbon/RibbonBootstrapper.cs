using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.Ribbon
{
    internal static class RibbonBootstrapper
    {
        private const string AssemblyName = "BrxMgd";
        private static bool _initialized;

        private sealed class RibbonButtonSpec
        {
            public RibbonButtonSpec(string text, string command)
            {
                Text = text;
                Command = command;
            }

            public string Text { get; }
            public string Command { get; }
        }

        private sealed class RibbonPanelSpec
        {
            public RibbonPanelSpec(string id, string title, params RibbonButtonSpec[] buttons)
            {
                Id = id;
                Title = title;
                Buttons = buttons;
            }

            public string Id { get; }
            public string Title { get; }
            public IReadOnlyList<RibbonButtonSpec> Buttons { get; }
        }

        private sealed class RibbonTabSpec
        {
            public RibbonTabSpec(string id, string title, params RibbonPanelSpec[] panels)
            {
                Id = id;
                Title = title;
                Panels = panels;
            }

            public string Id { get; }
            public string Title { get; }
            public IReadOnlyList<RibbonPanelSpec> Panels { get; }
        }

        public static bool TryInitialize()
        {
            if (_initialized)
                return true;

            try
            {
                var control = FindRibbonControl();
                if (control == null)
                    return false;

                var tabs = GetProperty(control, "Tabs");
                if (tabs == null)
                    return false;

                foreach (var tabSpec in CreateSpecs())
                    ReconcileTab(tabs, tabSpec);

                _initialized = true;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Reset() => _initialized = false;

        private static void ReconcileTab(object tabs, RibbonTabSpec tabSpec)
        {
            var tab = FindById(tabs, tabSpec.Id);
            var created = tab == null;
            if (created)
            {
                tab = Create("Bricscad.Windows.RibbonTab");
                SetProperty(tab, "Id", tabSpec.Id);
            }

            SetProperty(tab!, "Name", tabSpec.Id);
            SetProperty(tab!, "Title", tabSpec.Title);

            var panels = GetProperty(tab!, "Panels")
                         ?? throw new InvalidOperationException("RibbonTab.Panels was not available.");

            foreach (var panelSpec in tabSpec.Panels)
                EnsurePanel(tabSpec, panelSpec, panels);

            // Older QS3D versions used exactly one <TAB>_PANEL_SOURCE per tab. Retire that
            // working fallback only after every grouped panel/button has reconciled successfully;
            // dedicated/unknown augmenter panels are intentionally preserved.
            RemoveLegacyFlatPanel(panels, tabSpec.Id + "_PANEL_SOURCE");

            if (created)
                Add(tabs, tab!);
        }

        private static void EnsurePanel(RibbonTabSpec tabSpec, RibbonPanelSpec panelSpec, object panels)
        {
            var sourceId = PanelSourceId(tabSpec, panelSpec);
            var source = FindPanelSource(panels, sourceId);
            if (source == null)
            {
                AddPanel(tabSpec, panelSpec, panels);
                return;
            }

            SetProperty(source, "Name", panelSpec.Title);
            SetProperty(source, "Title", panelSpec.Title);
            EnsurePanelButtons(tabSpec, panelSpec, source);
        }

        private static void AddPanel(RibbonTabSpec tabSpec, RibbonPanelSpec panelSpec, object panels)
        {
            var source = Create("Bricscad.Windows.RibbonPanelSource");
            SetProperty(source, "Id", PanelSourceId(tabSpec, panelSpec));
            SetProperty(source, "Name", panelSpec.Title);
            SetProperty(source, "Title", panelSpec.Title);
            EnsurePanelButtons(tabSpec, panelSpec, source);

            var panel = Create("Bricscad.Windows.RibbonPanel");
            SetProperty(panel, "Source", source);
            Add(panels, panel);
        }

        private static void EnsurePanelButtons(RibbonTabSpec tabSpec, RibbonPanelSpec panelSpec, object source)
        {
            var items = GetProperty(source, "Items")
                        ?? throw new InvalidOperationException("RibbonPanelSource.Items was not available.");

            foreach (var buttonSpec in panelSpec.Buttons)
            {
                var buttonId = ButtonId(tabSpec, panelSpec, buttonSpec);
                var button = FindById(items, buttonId);
                if (button == null)
                {
                    button = Create("Bricscad.Windows.RibbonButton");
                    SetProperty(button, "Id", buttonId);
                    Add(items, button);
                }

                SetProperty(button, "Name", buttonSpec.Text);
                SetProperty(button, "Text", buttonSpec.Text);
                SetProperty(button, "ShowText", true);
                SetProperty(button, "ShowImage", false);
                SetProperty(button, "CommandParameter", buttonSpec.Command);
                SetProperty(button, "CommandHandler", new RibbonCommandHandler());
            }
        }

        private static string PanelSourceId(RibbonTabSpec tabSpec, RibbonPanelSpec panelSpec) =>
            tabSpec.Id + "_" + panelSpec.Id + "_PANEL_SOURCE";

        private static string ButtonId(RibbonTabSpec tabSpec, RibbonPanelSpec panelSpec, RibbonButtonSpec buttonSpec) =>
            tabSpec.Id + "_" + panelSpec.Id + "_" + Normalize(buttonSpec.Text);

        private static object? FindPanelSource(object panels, string sourceId)
        {
            if (!(panels is IEnumerable enumerable))
                return null;

            foreach (var panel in enumerable)
            {
                if (panel == null)
                    continue;
                var source = GetProperty(panel, "Source");
                if (source == null)
                    continue;
                if (string.Equals(GetProperty(source, "Id") as string, sourceId, StringComparison.OrdinalIgnoreCase))
                    return source;
            }
            return null;
        }

        private static void RemoveLegacyFlatPanel(object panels, string legacySourceId)
        {
            if (!(panels is IEnumerable enumerable))
                return;

            object? legacyPanel = null;
            foreach (var panel in enumerable)
            {
                if (panel == null)
                    continue;
                var source = GetProperty(panel, "Source");
                if (source == null)
                    continue;
                if (!string.Equals(GetProperty(source, "Id") as string, legacySourceId, StringComparison.OrdinalIgnoreCase))
                    continue;
                legacyPanel = panel;
                break;
            }

            if (legacyPanel != null)
                Remove(panels, legacyPanel);
        }

        private static object? FindById(object collection, string id)
        {
            if (!(collection is IEnumerable enumerable))
                return null;

            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;
                if (string.Equals(GetProperty(item, "Id") as string, id, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
            return null;
        }

        private static RibbonButtonSpec Button(string text, string command) => new RibbonButtonSpec(text, command);
        private static RibbonPanelSpec Panel(string id, string title, params RibbonButtonSpec[] buttons) => new RibbonPanelSpec(id, title, buttons);

        private static IEnumerable<RibbonTabSpec> CreateSpecs()
        {
            yield return new RibbonTabSpec(
                "QS3D_HOME",
                "KHỞI ĐẦU",
                Panel("PROJECT", "Dự án",
                    Button("Workspace", "QS3D"),
                    Button("Start Center", "QS3DSTART"),
                    Button("Lưu", "QS3DSAVE")),
                Panel("COORDINATION", "Điều phối",
                    Button("Regenerate", "QS3DREGEN"),
                    Button("BQ", "QS3DBQ"),
                    Button("BBS", "QS3DBBSVIEW")),
                Panel("QUALITY", "Chất lượng",
                    Button("Health All", "QS3DHEALTHALL"),
                    Button("Release Check", "QS3DRELEASECHECK")));

            yield return new RibbonTabSpec(
                "QS3D_PROJECT",
                "THIẾT LẬP DỰ ÁN",
                Panel("STATE", "Trạng thái",
                    Button("Làm mới", "QS3DREFRESH"),
                    Button("Nạp dự án", "QS3DRELOAD")),
                Panel("TEMPLATE", "Template",
                    Button("Xuất Template", "QS3DTEMPLATEEXPORT"),
                    Button("Nạp Template", "QS3DTEMPLATEIMPORT")),
                Panel("WORKSPACE", "Phạm vi",
                    Button("Layer / Xref", "QS3D")));

            yield return new RibbonTabSpec(
                "QS3D_AUTHOR",
                "TẠO MỚI",
                Panel("SETUP", "Thiết lập",
                    Button("Family / Type", "QS3DFAMILIES"),
                    Button("Capture / Bóc chọn", "QS3D")),
                Panel("ARCHITECTURE", "Kiến trúc",
                    Button("Vẽ Tường", "QS3DDRAWWALL"),
                    Button("Vẽ Vách Kính", "QS3DDRAWGLASSWALL"),
                    Button("Vẽ Trụ Tường", "QS3DDRAWWALLPIER"),
                    Button("Vẽ Cửa", "QS3DDRAWDOOR"),
                    Button("Vẽ Lỗ Mở", "QS3DDRAWOPENING")),
                Panel("STRUCTURE", "Kết cấu",
                    Button("Vẽ Dầm", "QS3DDRAWBEAM"),
                    Button("Vẽ Vách BTCT", "QS3DDRAWSTRUCTWALL"),
                    Button("Vẽ Cột", "QS3DDRAWCOLUMN"),
                    Button("Vẽ Sàn", "QS3DDRAWSLAB"),
                    Button("Vẽ Móng", "QS3DDRAWFOUNDATION")),
                Panel("OUTPUT", "Hoàn thiện 3D",
                    Button("Khoét Cửa/Lỗ chọn", "QS3DCUTSELECTEDOPENINGS"),
                    Button("Tạo 3D từ selection", "QS3DBUILD3D")));

            yield return new RibbonTabSpec(
                "QS3D_BIM",
                "MÔ HÌNH BIM",
                Panel("ROOMS", "Phòng & hoàn thiện",
                    Button("Phòng", "QS3DROOM"),
                    Button("Phòng Auto", "QS3DROOMAUTO"),
                    Button("HT_Phòng", "QS3DFINISH")),
                Panel("ENVELOPE", "Tường & vách",
                    Button("Tường Gạch", "QS3DWALL"),
                    Button("Vách Kính", "QS3DGLASSWALL"),
                    Button("Vách Kính Hub", "QS3DCURTAIN"),
                    Button("Curtain 3D", "QS3DCURTAIN3D"),
                    Button("Health khung kính", "QS3DCURTAINFRAMEHEALTH"),
                    Button("Trụ Tường", "QS3DWALLPIER"),
                    Button("Giao tường", "QS3DWALLJUNCTIONS"),
                    Button("Snap Preview", "QS3DWALLSNAPPREVIEW"),
                    Button("Snap Apply", "QS3DWALLSNAPAPPLY")),
                Panel("STRUCTURE", "Kết cấu",
                    Button("Dầm", "QS3DBEAM"),
                    Button("Sàn", "QS3DSLAB"),
                    Button("Cột", "QS3DCOLUMN"),
                    Button("Vách BTCT", "QS3DSTRUCTWALL"),
                    Button("Móng", "QS3DFOUNDATION"),
                    Button("Cầu thang", "QS3DSTAIR"),
                    Button("Lan can", "QS3DRAILING"),
                    Button("Đào đất", "QS3DEARTHWORK")),
                Panel("OPENINGS", "Cửa & lỗ mở",
                    Button("Lỗ mở", "QS3DOPENING"),
                    Button("Cửa", "QS3DDOOR"),
                    Button("Schedule Cửa/Lỗ", "QS3DDOORSCHEDULE"),
                    Button("Auto Host", "QS3DAUTOLINKHOSTS"),
                    Button("Link Host", "QS3DLINKHOST"),
                    Button("Khoét Cửa/Lỗ chọn", "QS3DCUTSELECTEDOPENINGS"),
                    Button("Khoét Cửa/Lỗ", "QS3DCUTOPENINGS"),
                    Button("Khoét cong", "QS3DCUTOPENINGSCURVED")),
                Panel("GENERATE", "Sinh mô hình",
                    Button("Vẽ 3D", "QS3DBUILD3D")));

            yield return new RibbonTabSpec(
                "QS3D_RECOGNIZE",
                "NHẬN DẠNG",
                Panel("RECOGNIZE", "Nhận dạng",
                    Button("Nhận dạng", "QS3DRECOGNIZE"),
                    Button("Auto chắc chắn", "QS3DRECOGNIZEAUTO")),
                Panel("REVIEW", "Kiểm tra",
                    Button("Quick Takeoff", "QS3DTAKEOFF"),
                    Button("Inspect", "QS3DINSPECT")));

            yield return new RibbonTabSpec(
                "QS3D_DRAW",
                "VẼ",
                Panel("PRIMITIVES", "Hình học",
                    Button("Điểm", "_POINT"),
                    Button("Đường thẳng", "_LINE"),
                    Button("Cung", "_ARC"),
                    Button("Chữ nhật", "_RECTANG")),
                Panel("TRANSFORM", "Biến đổi",
                    Button("Di chuyển", "_MOVE"),
                    Button("Xoay", "_ROTATE"),
                    Button("Đối xứng", "_MIRROR"),
                    Button("Sao chép", "_COPY")),
                Panel("EDIT", "Kết nối & đo",
                    Button("Chia cấu kiện", "_BREAK"),
                    Button("Nối liền", "_JOIN"),
                    Button("Đo khoảng cách", "_DIST"),
                    Button("Section", "QS3DSECTIONPLANE")));

            yield return new RibbonTabSpec(
                "QS3D_TOOL",
                "TOOL",
                Panel("INSPECT", "Kiểm tra",
                    Button("Inspect", "QS3DINSPECT"),
                    Button("Locate", "QS3DLOCATE"),
                    Button("Highlight", "QS3DHIGHLIGHT"),
                    Button("Bỏ highlight", "QS3DUNHIGHLIGHT")),
                Panel("FOCUS", "Tập trung",
                    Button("Focus", "QS3DFOCUS"),
                    Button("Cô lập", "QS3DISOLATE"),
                    Button("Khôi phục", "QS3DUNISOLATE")),
                Panel("VIEW", "Cắt & zoom",
                    Button("Section Box", "QS3DSECTIONBOX"),
                    Button("Clip", "QS3DCLIPDISPLAY"),
                    Button("Zoom chọn", "QS3DZOOMSELECTED")),
                Panel("QUALITY", "Bảo trì",
                    Button("Regenerate", "QS3DREGEN"),
                    Button("Health All", "QS3DHEALTHALL"),
                    Button("Release Check", "QS3DRELEASECHECK")));

            yield return new RibbonTabSpec(
                "QS3D_MODELING",
                "MODELING",
                Panel("GENERATE", "Sinh 3D",
                    Button("Vẽ 3D", "QS3DBUILD3D")),
                Panel("WALLS", "Tường & vách",
                    Button("Tường Gạch", "QS3DWALL"),
                    Button("Vách Kính", "QS3DGLASSWALL"),
                    Button("Curtain 3D", "QS3DCURTAIN3D"),
                    Button("Trụ Tường", "QS3DWALLPIER"),
                    Button("Giao tường", "QS3DWALLJUNCTIONS"),
                    Button("Snap Preview", "QS3DWALLSNAPPREVIEW"),
                    Button("Snap Apply", "QS3DWALLSNAPAPPLY")),
                Panel("STRUCTURE", "Kết cấu",
                    Button("Dầm", "QS3DBEAM"),
                    Button("Sàn", "QS3DSLAB"),
                    Button("Cột", "QS3DCOLUMN"),
                    Button("Vách", "QS3DSTRUCTWALL"),
                    Button("Móng", "QS3DFOUNDATION")),
                Panel("OPENINGS", "Cửa & host",
                    Button("Auto Host", "QS3DAUTOLINKHOSTS"),
                    Button("Khoét Cửa/Lỗ chọn", "QS3DCUTSELECTEDOPENINGS"),
                    Button("Khoét Cửa/Lỗ", "QS3DCUTOPENINGS"),
                    Button("Khoét cong", "QS3DCUTOPENINGSCURVED"),
                    Button("Cửa", "QS3DDOOR")),
                Panel("ROOMS", "Phòng",
                    Button("Phòng", "QS3DROOM"),
                    Button("Phòng Auto", "QS3DROOMAUTO")));

            yield return new RibbonTabSpec(
                "QS3D_VIEW",
                "XEM",
                Panel("ORIENTATION", "Góc nhìn",
                    Button("3D", "QS3DVIEW3D"),
                    Button("Top", "QS3DVIEWTOP"),
                    Button("Orbit", "QS3DORBIT")),
                Panel("FOCUS", "Tập trung",
                    Button("Focus", "QS3DFOCUS"),
                    Button("Cô lập", "QS3DISOLATE"),
                    Button("Khôi phục", "QS3DUNISOLATE")),
                Panel("SECTION", "Mặt cắt",
                    Button("Section Box", "QS3DSECTIONBOX"),
                    Button("Section Plane", "QS3DSECTIONPLANE"),
                    Button("Clip Display", "QS3DCLIPDISPLAY")),
                Panel("ZOOM", "Điều hướng",
                    Button("Zoom chọn", "QS3DZOOMSELECTED"),
                    Button("Zoom all", "QS3DZOOMALL")),
                Panel("WORKSPACE", "Workspace",
                    Button("Workspace", "QS3D"),
                    Button("BQ", "QS3DBQ"),
                    Button("Refresh", "QS3DREFRESH")));

            yield return new RibbonTabSpec(
                "QS3D_QTY",
                "ĐỊNH LƯỢNG",
                Panel("QUANTITY", "Khối lượng",
                    Button("Regenerate", "QS3DREGEN"),
                    Button("BQ", "QS3DBQ"),
                    Button("Takeoff", "QS3DTAKEOFF")),
                Panel("EXCEL", "Excel ↔ CAD",
                    Button("ED2 • Excel ↔ CAD", "QS3DED2"),
                    Button("Excel → CAD", "QS3DEXCELLOCATE")),
                Panel("OPENINGS", "Cửa & lỗ mở",
                    Button("Cửa/Lỗ Schedule", "QS3DDOORSCHEDULE"),
                    Button("Cửa/Lỗ XLSX", "QS3DDOORXLSX")),
                Panel("REBAR_SCHEDULE", "BBS",
                    Button("BBS Review", "QS3DBBSVIEW"),
                    Button("BBS XLSX", "QS3DBBS"),
                    Button("Mesh Setup", "QS3DREBARMESHSETUP")),
                Panel("REBAR_3D", "Cốt thép 3D",
                    Button("Cốt thép cột 3D", "QS3DREBAR3D"),
                    Button("Đai cột 3D", "QS3DREBARTIES3D"),
                    Button("Thép dọc dầm 3D", "QS3DBEAMREBAR3D"),
                    Button("Đai dầm 3D", "QS3DREBARSTIRRUP3D"),
                    Button("Lưới sàn 3D", "QS3DSLABREBAR3D"),
                    Button("Lưới vách 3D", "QS3DWALLREBAR3D"),
                    Button("Lưới móng 3D", "QS3DFOUNDATIONREBAR3D"),
                    Button("Cốt thép shape 3D", "QS3DREBAR3DSHAPE")),
                Panel("REBAR_HEALTH", "Health cốt thép",
                    Button("Health đai cột", "QS3DREBARTIEHEALTH"),
                    Button("Health đai dầm", "QS3DREBARSTIRRUPHEALTH"),
                    Button("Health lưới sàn", "QS3DSLABREBARHEALTH"),
                    Button("Health lưới vách", "QS3DWALLREBARHEALTH"),
                    Button("Health lưới móng", "QS3DFOUNDATIONREBARHEALTH"),
                    Button("Health cốt thép", "QS3DREBARHEALTH"),
                    Button("Health shape", "QS3DREBARSHAPEHEALTH"),
                    Button("Health All", "QS3DREBARHEALTHALL")));

            yield return new RibbonTabSpec(
                "QS3D_REV",
                "BẢN SỬA ĐỔI",
                Panel("REVISION", "Bản sửa đổi",
                    Button("Tạo baseline", "QS3DREVBASE"),
                    Button("So sánh", "QS3DREVDIFF")),
                Panel("QUALITY", "Kiểm tra",
                    Button("Health All", "QS3DHEALTHALL"),
                    Button("Release Check", "QS3DRELEASECHECK")),
                Panel("PROJECT", "Dự án",
                    Button("Lưu", "QS3DSAVE")));
        }

        private static object? FindRibbonControl()
        {
            var servicesType = Type.GetType("Bricscad.Ribbon.RibbonServices, " + AssemblyName, false);
            if (servicesType == null)
                return null;

            var paletteProperty = servicesType.GetProperty("RibbonPaletteSet", BindingFlags.Public | BindingFlags.Static);
            var palette = paletteProperty?.GetValue(null, null);
            if (palette == null)
            {
                servicesType.GetMethod("CreateRibbonPaletteSet", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
                palette = paletteProperty?.GetValue(null, null);
            }

            if (palette == null)
                return null;
            if (palette.GetType().Name == "RibbonControl")
                return palette;

            var direct = GetProperty(palette, "RibbonControl");
            if (direct != null)
                return direct;

            foreach (var property in palette.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.PropertyType.Name != "RibbonControl" || property.GetIndexParameters().Length != 0)
                    continue;

                var value = property.GetValue(palette, null);
                if (value != null)
                    return value;
            }

            return null;
        }

        private static object Create(string fullName) =>
            Activator.CreateInstance(Type.GetType(fullName + ", " + AssemblyName, true)!)
            ?? throw new InvalidOperationException("Cannot create " + fullName);

        private static object? GetProperty(object target, string name) =>
            target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(target, null);

        private static void SetProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.CanWrite)
                return;

            if (value == null || property.PropertyType.IsInstanceOfType(value) || property.PropertyType == value.GetType())
                property.SetValue(target, value, null);
        }

        private static void Add(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x =>
                    x.Name == "Add"
                    && x.GetParameters().Length == 1
                    && x.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));

            if (method == null)
                throw new InvalidOperationException("Collection does not expose a compatible Add method.");

            method.Invoke(collection, new[] { item });
        }

        private static void Remove(object collection, object item)
        {
            var method = collection.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x =>
                    x.Name == "Remove"
                    && x.GetParameters().Length == 1
                    && x.GetParameters()[0].ParameterType.IsAssignableFrom(item.GetType()));

            if (method == null)
                throw new InvalidOperationException("Collection does not expose a compatible Remove method.");

            method.Invoke(collection, new[] { item });
        }

        private static string Normalize(string text) =>
            new string((text ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        private sealed class RibbonCommandHandler : ICommand
        {
            public bool CanExecute(object? parameter) =>
                parameter is string text && !string.IsNullOrWhiteSpace(text);

            public void Execute(object? parameter)
            {
                if (!(parameter is string command) || string.IsNullOrWhiteSpace(command))
                    return;

                Application.DocumentManager.MdiActiveDocument?.SendStringToExecute(command + " ", true, false, false);
            }

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }
        }
    }
}