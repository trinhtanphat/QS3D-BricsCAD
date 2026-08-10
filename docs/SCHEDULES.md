# QS3D Schedules

Các schedule chuyên dụng này bổ sung cho `QS3DBQ`; chúng không thay thế BQ tổng hợp mà cung cấp bảng nghiệp vụ dễ kiểm/audit theo domain.

## Shared schedule contracts

- Modeless review/export UI phải bound vào đúng `Document` đã mở cửa sổ. Chuyển sang DWG khác thì thao tác CAD/project/export bị từ chối cho tới khi kích hoạt lại đúng bản vẽ.
- Các tổng count/length/area/weight hiển thị hoặc ghi trong status dùng checked finite aggregation (`QuantityReportMath`) thay vì unchecked LINQ `Sum`.
- Builder/export phải fail closed với `NaN`, `Infinity`, overflow hoặc semantic quantity không hợp lệ thay vì clamp/biến thành `0` im lặng.
- Preferred/fallback quantity được đọc lazy theo thứ tự ưu tiên; một legacy fallback lỗi không được làm hỏng primary quantity đang hợp lệ.
- Export có đường regenerate/recalculate trước khi lấy dữ liệu khi workflow có callback cập nhật hiện hành; BQ modeless hiện recalculate trước XLSX.

## Schedule Hub

- `QS3DSCHEDULES` — cửa sổ modeless document-bound gom BQ, HT_Phòng, vật liệu, Vách Kính, Cửa/Lỗ và BBS/Rebar workflows.
- Snapshot badges không còn đếm raw semantic rows: Hub regenerate dirty data rồi dùng chính `ProjectQuantityReportBuilder`, `RoomFinishScheduleBuilder`, `DoorOpeningScheduleBuilder`, `CurtainWallScheduleBuilder` và `MaterialUsageScheduleBuilder`. Vì vậy stale/orphan/invalid data bị xử lý giống schedule/export thay vì badge báo một số còn XLSX báo số khác.
- `Cấu kiện BQ`, `HT_Phòng`, `Door / Opening` và `GlassWall` dùng checked counts; `Vật liệu dùng` đếm material name distinct từ Material Usage schedule.
- Nếu user chuyển sang DWG khác, Hub giữ snapshot đang hiển thị, không regenerate project nền và không gửi command sang drawing mới; phải kích hoạt lại đúng DWG đã mở Hub.

## BQ tổng hợp

- `QS3DBQ` — BQ modeless theo Floor / Category / search, có Locate và XLSX.
- `ProjectQuantityReportBuilder` loại stale AutoRoom và các room-linked finish/dependent bị lifecycle rule loại khỏi quantity.
- Gross/net/perimeter/finish quantity fallback là lazy, không đánh giá legacy fallback không dùng.
- `QuantitySummaryWindow` tính lại project hiện hành trước khi XLSX và từ chối export nếu cửa sổ thuộc DWG khác.

## Material Usage

- `QS3DMATERIALS` — Material Catalog.
- `QS3DMATERIALXLSX` — XLSX sử dụng vật liệu.
- Core: `MaterialUsageScheduleBuilder`.
- Exporter: `MaterialUsageXlsxExporter`.
- Group: Floor + Material + Component + Category + Family.
- Instance material override thắng Family material; GlassWall tách `Material` và `CurtainFrame`.
- Primary quantity đi theo unit catalog: `m`, `m²`, `m³`, `kg`.
- Material usage dùng cùng `AutoRoomLifecycle.IsExcludedFromQuantity` với BQ/HT_Phòng nên stale/orphan room-linked finish không tạo vật liệu “ma”.

## Cửa / Lỗ mở

- `QS3DDOORSCHEDULE` — modeless review/filter schedule.
- `QS3DDOORXLSX` — XLSX.
- Core: `DoorOpeningScheduleBuilder`.
- Exporter: `DoorOpeningXlsxExporter`.
- Domain: `Door` + `WallOpening`.
- Group: Floor + Category + Family + Width + Height + Sill + Thickness + Material.
- `OpeningAreaM2` semantic quantity được ưu tiên; nếu chưa có thì dùng Width × Height.
- Giữ provenance `ElementIds` và distinct `HostIds` từ `HostWallId`.
- Invalid/non-finite/negative semantic dimensions bị reject thay vì xuất sai.
- Modeless Refresh/Export khóa theo source DWG và summary dùng checked count/area aggregation.

## HT_Phòng

- `QS3DFINISHSCHEDULE` — modeless review/filter schedule.
- `QS3DFINISHXLSX` — XLSX.
- Core: `RoomFinishScheduleBuilder`.
- Exporter: `RoomFinishXlsxExporter`.
- Domain: `FloorFinish`, `Waterproofing`, `Skirting`, `WallFinish`, `CeilingFinish`.
- Room provenance được resolve tập trung bởi `AutoRoomLifecycle.ResolveRoomReferenceId`: canonical `RoomSourceId`, các alias `ParentRoomId` / `SourceRoomId` / `GeneratedFromRoomId` / `RoomId`, và room dependency trong `DependsOn`.
- Nhiều provenance ID mâu thuẫn fail closed. Finish có room link nhưng Room đã mất/sai category hoặc AutoRoom stale bị loại khỏi BQ / Material / HT_Phòng schedule.
- Finish thật sự chưa có room provenance vẫn được schedule dưới nhãn `(chưa liên kết phòng)` để không làm mất dữ liệu thủ công.
- Group dùng stable Room ID; hai phòng khác nhau có cùng display name không bị gộp.
- Skirting ưu tiên chiều dài; finish diện tích ưu tiên quantity domain tương ứng (`NetFinishAreaM2`, `TopAreaM2`, `BottomAreaM2`, `AreaM2`) theo lazy fallback.
- Material effective dùng Instance override trước, Family sau; unit catalog quyết định primary quantity khi có.

## Curtain Wall

- `QS3DCURTAIN` — Curtain Hub modeless source-DWG-bound.
- `QS3DCURTAINXLSX` — curtain schedule XLSX.
- Quantities gồm panel/frame/glass/opening deductions theo semantic layout.
- Hub summary reject quantity invalid/non-integer thay vì clamp về `0` / `Int32.MaxValue`; panel, net glass area và frame length dùng checked aggregation.

## BBS / Rebar

- BBS modeless Locate và XLSX đều khóa theo source DWG.
- UI totals quantity/length/weight và BBS CSV status dùng checked finite aggregation.
- Core `RebarScheduleBuilder` tiếp tục validate aggregate trước khi trả rows.

## Validation

Source guards liên quan gồm `preflight-material-usage.py`, `preflight-door-opening-schedule.py`, `preflight-schedule-hub.py`, `preflight-room-finish-schedule.py`, `preflight-room-finish-ui.py`, `preflight-room-lifecycle.py`, `preflight-schedule-arithmetic.py`, `preflight-modeless-review-windows.py` và `preflight-curtain-wall-ui-export.py`.

Core deterministic tests bao gồm schedule grouping/inheritance/provenance, stale/orphan exclusion, lazy fallback và XLSX package validation. Các gate trên **không thay thế BricsCAD V25 Gate C**. Adapter command/modeless behavior, native CAD interaction và UI runtime vẫn phải được compile/NETLOAD/smoke-test trên Windows x64 có BricsCAD V25 thật trước release.
