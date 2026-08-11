using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.BricsCAD.V25.Services
{
    internal sealed class StartCenterCommandItem
    {
        public StartCenterCommandItem(string command, string title, string description, string group, string keywords, int priority)
        {
            Command = Required(command, nameof(command)).ToUpperInvariant();
            if (!Command.StartsWith("QS3D", StringComparison.Ordinal))
                throw new ArgumentException("Start Center only accepts allowlisted QS3D commands.", nameof(command));
            Title = Required(title, nameof(title));
            Description = Required(description, nameof(description));
            Group = Required(group, nameof(group));
            Keywords = (keywords ?? string.Empty).Trim();
            Priority = priority;
        }

        public string Command { get; }
        public string Title { get; }
        public string Description { get; }
        public string Group { get; }
        public string Keywords { get; }
        public int Priority { get; }

        private static string Required(string value, string name)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length == 0) throw new ArgumentException("Value is required.", name);
            return text;
        }
    }

    internal static class StartCenterCommandCatalog
    {
        private static readonly IReadOnlyList<StartCenterCommandItem> Items = Build().AsReadOnly();
        private static readonly Dictionary<string, StartCenterCommandItem> ByCommand =
            Items.ToDictionary(x => x.Command, x => x, StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<StartCenterCommandItem> All => Items;

        public static IReadOnlyList<string> Groups =>
            Items.Select(x => x.Group)
                 .Distinct(StringComparer.CurrentCultureIgnoreCase)
                 .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                 .ToList()
                 .AsReadOnly();

        public static bool TryGet(string command, out StartCenterCommandItem item)
        {
            var key = (command ?? string.Empty).Trim();
            return ByCommand.TryGetValue(key, out item!);
        }

        public static IReadOnlyList<StartCenterCommandItem> Search(string query, string group)
        {
            var normalizedQuery = (query ?? string.Empty).Trim();
            var normalizedGroup = (group ?? string.Empty).Trim();
            var hasGroup = normalizedGroup.Length > 0 &&
                           !string.Equals(normalizedGroup, "Tất cả", StringComparison.CurrentCultureIgnoreCase);

            var ranked = Items
                .Where(x => !hasGroup || string.Equals(x.Group, normalizedGroup, StringComparison.CurrentCultureIgnoreCase))
                .Select(x => new { Item = x, Score = Score(x, normalizedQuery) })
                .Where(x => normalizedQuery.Length == 0 || x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Item.Priority)
                .ThenBy(x => x.Item.Title, StringComparer.CurrentCultureIgnoreCase)
                .Select(x => x.Item)
                .ToList();

            return ranked.AsReadOnly();
        }

        private static int Score(StartCenterCommandItem item, string query)
        {
            if (query.Length == 0) return 1;
            var score = 0;
            if (item.Command.StartsWith(query, StringComparison.OrdinalIgnoreCase)) score += 120;
            else if (item.Command.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) score += 70;

            if (item.Title.StartsWith(query, StringComparison.CurrentCultureIgnoreCase)) score += 110;
            else if (item.Title.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0) score += 80;

            if (item.Group.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0) score += 45;
            if (item.Description.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0) score += 35;
            if (item.Keywords.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0) score += 55;
            return score;
        }

        private static List<StartCenterCommandItem> Build()
        {
            var items = new List<StartCenterCommandItem>
            {
                New("QS3D", "Workspace QS3D", "Mở Workspace ba vùng làm việc.", "Khởi đầu", "workspace palette bảng điều khiển", 0),
                New("QS3DDOMAIN", "Full Domain Hub", "Mở toàn bộ nhóm công việc QS3D.", "Khởi đầu", "hub trung tâm chức năng", 1),
                New("QS3DPROJECTTOOLS", "Project Tools", "Mở công cụ dự án theo bản vẽ hiện hành.", "Khởi đầu", "project tools dự án", 2),
                New("QS3DSCHEDULES", "Schedule Hub", "Mở trung tâm schedule, BQ, BBS và xuất dữ liệu.", "Khởi đầu", "schedule bảng thống kê", 3),
                New("QS3DFAMILIES", "Family / Type", "Quản lý và kích hoạt Family / Type trước khi vẽ.", "Khởi đầu", "family type chủng loại cấu kiện", 4),
                New("QS3DLEVELS", "Tầng / Level", "Quản lý cao độ và tầng dự án.", "Khởi đầu", "floor level cao độ", 5),
                New("QS3DZONES", "Zone", "Quản lý Zone và gán đối tượng.", "Khởi đầu", "zone khu vực", 6),
                New("QS3DMATERIALS", "Vật liệu", "Mở Material Catalog.", "Khởi đầu", "material vật liệu", 7),

                New("QS3DDRAWWALL", "Vẽ Tường", "Direct Draw tường nhanh bằng Family đang active.", "Tạo mới", "wall tường kiến trúc quick", 10),
                New("QS3DDRAWGLASSWALL", "Vẽ Vách Kính", "Direct Draw GlassWall nhanh.", "Tạo mới", "glass wall curtain vách kính", 11),
                New("QS3DDRAWWALLPIER", "Vẽ Trụ Tường", "Direct Draw WallPier theo subset an toàn.", "Tạo mới", "wall pier trụ tường", 12),
                New("QS3DDRAWBEAM", "Vẽ Dầm", "Direct Draw Beam nhanh.", "Tạo mới", "beam dầm kết cấu", 13),
                New("QS3DDRAWSTRUCTWALL", "Vẽ Vách BTCT", "Direct Draw StructuralWall nhanh.", "Tạo mới", "structural wall vách btct", 14),
                New("QS3DDRAWCOLUMN", "Vẽ Cột", "Direct Draw Column nhanh.", "Tạo mới", "column cột kết cấu", 15),
                New("QS3DDRAWSLAB", "Vẽ Sàn", "Direct Draw Slab nhanh.", "Tạo mới", "slab sàn", 16),
                New("QS3DDRAWFOUNDATION", "Vẽ Móng", "Direct Draw Foundation nhanh.", "Tạo mới", "foundation móng", 17),
                New("QS3DDRAWDOOR", "Vẽ Cửa + Auto Host", "Tạo Door mới và chạy Auto Host có guard.", "Tạo mới", "door cửa auto host", 18),
                New("QS3DDRAWOPENING", "Vẽ Lỗ Mở + Auto Host", "Tạo WallOpening mới và chạy Auto Host có guard.", "Tạo mới", "opening lỗ mở auto host", 19),
                New("QS3DDRAWWINDOW", "Vẽ Cửa Sổ + Auto Host", "Tạo Window bằng WallOpening usage phù hợp.", "Tạo mới", "window cửa sổ opening", 20),
                New("QS3DCREATESIMILAR", "Vẽ Tương Tự", "Dùng Family của cấu kiện QS3D mẫu rồi Direct Draw nhanh.", "Tạo mới", "create similar tương tự copy family", 21),
                New("QS3DCUTSELECTEDOPENINGS", "Khoét Cửa/Lỗ đang chọn", "Khoét vật lý đúng tập Door/Opening đang chọn.", "Tạo mới", "cut opening boolean selected", 22),

                New("QS3DDRAWWALLADV", "Tường — Advanced", "Direct Draw tường với tham số nâng cao.", "Nâng cao", "wall advanced tùy chỉnh", 30),
                New("QS3DDRAWBEAMADV", "Dầm — Advanced", "Direct Draw Beam với tham số nâng cao.", "Nâng cao", "beam advanced", 31),
                New("QS3DDRAWCOLUMNADV", "Cột — Advanced", "Direct Draw Column với tham số nâng cao.", "Nâng cao", "column advanced", 32),
                New("QS3DDRAWSLABADV", "Sàn — Advanced", "Direct Draw Slab với tham số nâng cao.", "Nâng cao", "slab advanced", 33),
                New("QS3DDRAWGLASSWALLADV", "Vách Kính — Advanced", "Direct Draw GlassWall nâng cao.", "Nâng cao", "glasswall advanced", 34),
                New("QS3DDRAWWALLPIERADV", "Trụ Tường — Advanced", "Direct Draw WallPier nâng cao.", "Nâng cao", "wallpier advanced", 35),
                New("QS3DDRAWSTRUCTWALLADV", "Vách BTCT — Advanced", "Direct Draw StructuralWall nâng cao.", "Nâng cao", "structwall advanced", 36),
                New("QS3DDRAWFOUNDATIONADV", "Móng — Advanced", "Direct Draw Foundation nâng cao.", "Nâng cao", "foundation advanced", 37),
                New("QS3DDRAWDOORADV", "Cửa — Advanced", "Direct Draw Door nâng cao.", "Nâng cao", "door advanced", 38),
                New("QS3DDRAWOPENINGADV", "Lỗ Mở — Advanced", "Direct Draw Opening nâng cao.", "Nâng cao", "opening advanced", 39),
                New("QS3DDRAWWINDOWADV", "Cửa Sổ — Advanced", "Direct Draw Window nâng cao.", "Nâng cao", "window advanced", 40),
                New("QS3DCREATESIMILARADV", "Vẽ Tương Tự — Advanced", "Create Similar và chuyển sang Direct Draw nâng cao.", "Nâng cao", "create similar advanced", 41),

                New("QS3DROOMAUTO", "Phòng Auto", "Phát hiện Room từ mạng biên plan-view.", "Mô hình", "room phòng boundary auto", 50),
                New("QS3DBUILD3D", "Build / Update 3D", "Tạo hoặc cập nhật native 3D cho semantic selection.", "Mô hình", "build 3d regenerate solid", 51),
                New("QS3DWALLJUNCTIONS", "Giao Tường L/T/X", "Phân tích junction tường để review.", "Mô hình", "wall junction l t x", 52),
                New("QS3DWALLSNAPPREVIEW", "Preview Wall Snap", "Xem trước cleanup endpoint tim tường.", "Mô hình", "wall snap preview", 53),
                New("QS3DWALLSNAPAPPLY", "Apply Wall Snap", "Áp đúng preview fingerprint đã duyệt.", "Mô hình", "wall snap apply", 54),
                New("QS3DCURTAIN", "Curtain Hub", "Mở workflow Vách Kính / Curtain.", "Mô hình", "curtain glass wall", 55),
                New("QS3DCURTAIN3D", "Curtain 3D", "Build host, frame và panel theo contract hiện tại.", "Mô hình", "curtain frames panels 3d", 56),

                New("QS3DRECOGNIZE", "Nhận dạng + Review", "Nhận dạng deterministic và duyệt kết quả.", "Nhận dạng", "recognize recognition review", 60),
                New("QS3DRECOGNIZEAUTO", "Nhận dạng tự động", "Tự áp dụng kết quả đủ độ tin cậy.", "Nhận dạng", "recognize auto confidence", 61),
                New("QS3DB4D", "B4D Scan", "Quét Current Space theo boundary nhận dạng sạch.", "Nhận dạng", "b4d scan takeoff", 62),

                New("QS3DBQ", "Bóc khối lượng", "Mở quantity summary/filter/group/Locate/XLSX.", "Khối lượng", "bq quantity bóc khối lượng", 70),
                New("QS3DED2", "ED2 chi tiết", "Xuất chi tiết semantic theo Selection/Floor/Zone/All.", "Khối lượng", "ed2 excel chi tiết", 71),
                New("QS3DTAKEOFF", "Quick Takeoff", "Takeoff nhanh có quy đổi drawing unit.", "Khối lượng", "takeoff quantity nhanh", 72),
                New("QS3DBBSVIEW", "BBS Review", "Xem bảng cốt thép.", "Khối lượng", "bbs rebar schedule", 73),
                New("QS3DBBS", "Xuất BBS", "Xuất BBS Excel.", "Khối lượng", "bbs excel export", 74),

                New("QS3DREBARMESHSETUP", "Thiết lập lưới thép", "Cấu hình lưới thép trên semantic selection.", "Cốt thép", "rebar mesh setup", 80),
                New("QS3DREBAR3D", "Thép dọc Cột 3D", "Tạo/cập nhật longitudinal rebar cột.", "Cốt thép", "column rebar longitudinal", 81),
                New("QS3DREBARTIES3D", "Đai Cột 3D", "Tạo/cập nhật tie cột.", "Cốt thép", "column ties rebar", 82),
                New("QS3DBEAMREBAR3D", "Thép dọc Dầm 3D", "Tạo/cập nhật longitudinal rebar dầm.", "Cốt thép", "beam rebar longitudinal", 83),
                New("QS3DREBARSTIRRUP3D", "Đai Dầm 3D", "Tạo/cập nhật stirrup dầm.", "Cốt thép", "beam stirrup rebar", 84),
                New("QS3DSLABREBAR3D", "Lưới Sàn X/Y 3D", "Tạo/cập nhật slab mesh.", "Cốt thép", "slab rebar mesh", 85),
                New("QS3DWALLREBAR3D", "Lưới Vách 3D", "Tạo/cập nhật StructuralWall mesh.", "Cốt thép", "wall rebar mesh", 86),
                New("QS3DFOUNDATIONREBAR3D", "Lưới Móng X/Y 3D", "Tạo/cập nhật Foundation mesh.", "Cốt thép", "foundation rebar mesh", 87),
                New("QS3DREBARHEALTHALL", "Health cốt thép", "Kiểm tra aggregate generated rebar.", "Cốt thép", "rebar health kiểm tra", 88),

                New("QS3DINSPECT", "Đối tượng đang chọn", "Đọc selection hiện tại và đồng bộ Workspace.", "Review & Health", "inspect selection đối tượng", 90),
                New("QS3DRULEPREVIEW", "Quantity Rule Preview", "Dry-run rule trước mutation.", "Review & Health", "rule preview quantity", 91),
                New("QS3DREGENPREVIEW", "Regeneration Preview", "Xem trước regeneration delta và health diff.", "Review & Health", "regen preview regenerate", 92),
                New("QS3DHEALTH", "Model Health", "Kiểm tra health cơ bản.", "Review & Health", "health model", 93),
                New("QS3DHEALTHALL", "Model Health — All", "Kiểm tra aggregate semantic/source/generated state.", "Review & Health", "health all ownership", 94),
                New("QS3DOWNERSHIPHEALTH", "Ownership Health", "Kiểm tra ownership generated handle.", "Review & Health", "ownership generated health", 95),
                New("QS3DDIAGSUMMARY", "Diagnostic Summary", "Xuất diagnostic summary privacy-safe.", "Review & Health", "diagnostic support privacy", 96),
                New("QS3DRELEASECHECK", "Release Check", "Review readiness theo guard hiện có.", "Review & Health", "release check readiness", 97),

                New("QS3DSAVE", "Lưu QS3D Project", "Lưu sidecar project hiện hữu.", "Dự án", "save project qsdb", 100),
                New("QS3DRELOAD", "Reload QS3D Project", "Nạp lại project hiện hữu từ sidecar.", "Dự án", "reload project qsdb", 101),
                New("QS3DREFRESH", "Refresh QS3D", "Refresh UI/project theo lifecycle hiện tại.", "Dự án", "refresh project", 102),
                New("QS3DREGEN", "Regenerate", "Regenerate semantic dirty state theo engine hiện tại.", "Dự án", "regen regenerate dirty", 103),
                New("QS3DTEMPLATEIMPORT", "Nạp Template", "Import template theo contract hiện tại.", "Dự án", "template import", 104),
                New("QS3DTEMPLATEEXPORT", "Xuất Template", "Export template hiện tại.", "Dự án", "template export", 105),
                New("QS3DAUDIT", "Nhật ký thay đổi", "Mở audit/revision workflow.", "Dự án", "audit history revision", 106)
            };

            var duplicate = items.GroupBy(x => x.Command, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicate != null) throw new InvalidOperationException("Duplicate Start Center command: " + duplicate.Key);
            return items;
        }

        private static StartCenterCommandItem New(string command, string title, string description, string group, string keywords, int priority) =>
            new StartCenterCommandItem(command, title, description, group, keywords, priority);
    }
}