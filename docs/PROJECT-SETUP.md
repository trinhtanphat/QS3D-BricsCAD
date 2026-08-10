# QS3D Project Setup

Tài liệu này mô tả lớp dữ liệu dự án mới dùng chung cho Workspace, Ribbon và các module chuyên ngành. Các manager dưới đây là clean-room QS3D và không sao chép tài sản/mã nguồn BLT3D.

## Command surface

- `QS3DPROJECTTOOLS` — hub Thiết lập dự án, khóa theo DWG đã mở.
- `QS3DLEVELS` — quản lý Tầng/Cao độ; create/update/delete, active floor, gán Floor cho semantic selection.
- `QS3DZONES` — quản lý Zone; create/update/delete, active zone, gán Zone cho semantic selection.
- `QS3DFAMILIES` — Family Manager; create/duplicate/rename/delete, chỉnh property, gán Family cùng Category cho semantic selection.
- `QS3DMATERIALS` — Material Catalog; built-in + custom, apply `Material` hoặc `CurtainFrameMaterial`.
- `QS3DMATERIALXLSX` — xuất bảng sử dụng vật liệu sang XLSX thật.

Tab `THIẾT LẬP DỰ ÁN` được bổ sung shortcut bằng `ProjectRibbonAugmenter`; augmenter chỉ append item ID chưa tồn tại và không rewrite base Ribbon spec.

## Modeless multi-document safety

`ProjectToolsWindow`, `FloorLevelWindow`, `ZoneManagerWindow`, `FamilyManagerWindow` và `MaterialCatalogWindow` đều được bind vào `Document` tại thời điểm mở. Các thao tác dựa trên CAD selection từ modeless window phải xác nhận DWG bind vẫn là `MdiActiveDocument`. Nếu user chuyển sang DWG khác, QS3D từ chối mutation thay vì âm thầm sửa project khác.

`SemanticSelectionResolver` chỉ đọc CAD handles đang được chọn rồi chuyển ownership resolution sang Core `SemanticHandleOwnershipResolver`. Ambiguity ở một selected handle bị nhiều semantic element claim là lỗi chặn. Ambiguity không liên quan đến selection không được phép chặn một selection sạch.

## Tầng / Floor

Core contract: `ProjectFloorService`.

- tên tầng unique case-insensitive;
- elevation phải hữu hạn;
- tối đa 2.000 tầng;
- không xóa tầng active;
- không xóa tầng còn semantic element tham chiếu;
- đổi cao độ đánh dirty `Geometry | Relations | Quantity` cho element trên tầng đó;
- gán tầng đánh dirty `Relations | Quantity`, từ đó generated outputs được stale qua `ProjectElement.MarkDirty`;
- đổi tầng semantic **không** Move/Translate source CAD. Người dùng phải chỉnh CAD source riêng rồi rebuild khi cần.

## Zone

Core contract: `ProjectZoneService`.

- tên Zone unique case-insensitive;
- tối đa 2.000 Zone;
- không xóa Zone active;
- không xóa Zone còn semantic element tham chiếu;
- gán Zone chỉ đổi semantic scope, đánh dirty `Relations | Quantity`, không dịch CAD geometry.

## Family

Core contract: `ProjectFamilyService`.

- family id unique;
- family name unique trong cùng `ElementCategory`;
- create / duplicate / rename / delete;
- delete bị chặn nếu Family còn instance tham chiếu hoặc đang là `ActiveFamilyId`;
- gán Family chỉ cho element cùng Category;
- khi đổi Family, các property chỉ là inherited-copy của Family cũ được bỏ để nhận default Family mới; instance override khác biệt được giữ;
- `SetProperty` chỉ propagate tới instance đang kế thừa giá trị cũ hoặc chưa có property; override thật không bị ghi đè;
- `RemoveProperty` loại bỏ inherited-copy tương ứng nhưng giữ override.

Family change ảnh hưởng hình học/quantity phải đi qua dirty/stale model trước khi rebuild native output.

## Material Catalog

Core contract: `ProjectMaterialCatalog`.

Custom catalog được persist trong `ProjectState.Metadata` bằng key versioned `QS3D.MaterialCatalog.v1`, không cần đổi schema `.qsdb`. Record custom được Base64-encode theo từng field và có validation/caps.

Built-in hiện gồm: Bê tông, Thép, Gạch, Kính, Nhôm, Chống thấm, Sơn, Gỗ và Đất.

- tối đa 500 custom materials;
- duplicate id/name bị reject;
- rename custom material migrate các Family/Instance reference đang thực sự kế thừa và giữ instance override;
- delete bị chặn khi Family/Instance còn tham chiếu;
- `CurtainFrameMaterial` được theo dõi riêng bên cạnh `Material`.

## Material Usage Schedule

Core contract: `MaterialUsageScheduleBuilder`.

Schedule group theo Floor + Material + Component + Category + Family và giữ `ElementIds` để audit. Material effective dùng instance override trước, Family value sau.

GlassWall phát sinh hai component độc lập:

- `Material` — kính, ưu tiên `CurtainNetGlassAreaM2`;
- `CurtainFrame` — khung, dùng `CurtainFrameLengthM` + `CurtainFrameFaceAreaM2`.

`PrimaryQuantity` dựa trên unit catalog:

- `m` → LengthM;
- `m²` / `m2` → AreaM2;
- `m³` / `m3` → VolumeM3;
- `kg` → MassKg.

Exporter `MaterialUsageXlsxExporter` tạo XLSX/OpenXML thật, ghi atomic temp → validate ZIP package → replace file đích, có freeze header và AutoFilter.

## Source gates

Các source preflight liên quan:

- `scripts/preflight-material-floor-pickers.py`
- `scripts/preflight-zones.py`
- `scripts/preflight-families.py`
- `scripts/preflight-project-tools.py`
- `scripts/preflight-material-usage.py`

Các preflight này là source/invariant guards, **không thay thế compile/NETLOAD/runtime Gate C trên BricsCAD V25 thật**.
