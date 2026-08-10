# QS3D Schedules

Các schedule chuyên dụng này bổ sung cho `QS3DBQ`; chúng không thay thế BQ tổng hợp mà cung cấp bảng nghiệp vụ dễ kiểm/audit theo domain.

## Schedule Hub

- `QS3DSCHEDULES` — cửa sổ modeless document-bound gom BQ, vật liệu, Vách Kính, Cửa/Lỗ và BBS/Rebar workflows.
- Nếu user chuyển sang DWG khác, Schedule Hub không gửi command sang drawing mới; phải kích hoạt lại đúng DWG đã mở Hub.

## Material Usage

- `QS3DMATERIALS` — Material Catalog.
- `QS3DMATERIALXLSX` — XLSX sử dụng vật liệu.
- Core: `MaterialUsageScheduleBuilder`.
- Exporter: `MaterialUsageXlsxExporter`.
- Group: Floor + Material + Component + Category + Family.
- Instance material override thắng Family material; GlassWall tách `Material` và `CurtainFrame`.
- Primary quantity đi theo unit catalog: `m`, `m²`, `m³`, `kg`.

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

## HT_Phòng

- `QS3DFINISHSCHEDULE` — modeless review/filter schedule.
- `QS3DFINISHXLSX` — XLSX.
- Core: `RoomFinishScheduleBuilder`.
- Exporter: `RoomFinishXlsxExporter`.
- Domain:
  - `FloorFinish`
  - `Waterproofing`
  - `Skirting`
  - `WallFinish`
  - `CeilingFinish`
- Room link fallback: `ParentRoomId` → `SourceRoomId` → `GeneratedFromRoomId` → `RoomId`.
- Finish chưa link Room vẫn được schedule dưới nhãn `(chưa liên kết phòng)` để không mất dữ liệu.
- Skirting ưu tiên chiều dài; các finish diện tích ưu tiên quantity domain tương ứng (`NetFinishAreaM2`, `TopAreaM2`, `BottomAreaM2`, `AreaM2`).
- Material effective dùng Instance override trước, Family sau; unit catalog quyết định primary quantity khi có.

## Curtain Wall

- `QS3DCURTAIN` — Curtain Hub.
- `QS3DCURTAINXLSX` — curtain schedule XLSX.
- Quantities gồm panel/frame/glass/opening deductions theo semantic layout.

## Validation

Source guards liên quan:

- `scripts/preflight-material-usage.py`
- `scripts/preflight-door-opening-schedule.py`
- `scripts/preflight-schedule-hub.py`
- `scripts/preflight-room-finish-schedule.py`
- `scripts/preflight-room-finish-ui.py`

Core deterministic tests bao gồm schedule grouping/inheritance/provenance và XLSX package validation.

Các gate trên **không thay thế BricsCAD V25 Gate C**. Adapter command/modeless behavior, native CAD interaction và UI runtime vẫn phải được compile/NETLOAD/smoke-test trên Windows x64 có BricsCAD V25 thật trước release.
