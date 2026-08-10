# HT_Phòng identity, provenance và quantity integrity

Tài liệu này khóa contract clean-room cho `Room -> HT_Phòng -> Schedule/BQ/Material/Release`.

## 1. Stable identity

Mỗi Room và mỗi loại finish chỉ được có tối đa một semantic finish hợp lệ:

- `FloorFinish`
- `Waterproofing`
- `Skirting`
- `WallFinish`
- `CeilingFinish`

Canonical id mới dùng `RoomFinishIdentityService.CanonicalId(roomId, category)`, nhưng dữ liệu legacy không bị ép đổi id nếu provenance hiện hữu vẫn xác định duy nhất cùng Room + Category.

`RoomFinishIdentityService.FindExisting(...)` là nguồn sự thật dùng bởi generation/synchronization. Nó:

1. ưu tiên/reuse canonical finish nếu hợp lệ;
2. reuse finish legacy liên kết qua `RoomSourceId`, alias provenance hoặc `DependsOn`;
3. reject canonical id collision với category khác;
4. reject canonical finish đang trỏ sang Room khác;
5. reject nhiều finish cùng Room + Category thay vì tạo/giữ duplicate.

`SemanticCaptureService.GenerateRoomFinishes` và `SyncExistingRoomFinishes` không tự viết lại identity rule; cả hai dùng Core identity/synchronization services.

## 2. Room provenance

`AutoRoomLifecycle.ResolveRoomReferenceId(...)` gom Room provenance từ:

- `RoomSourceId` — canonical key;
- `ParentRoomId`;
- `SourceRoomId`;
- `GeneratedFromRoomId`;
- `RoomId`;
- dependency `DependsOn` trỏ tới semantic Room.

Nhiều Room id khác nhau trên cùng finish là conflict và fail closed.

Property-only legacy finish vẫn truy vết CAD được: `SourceHandleResolver` đi từ finish -> resolved Room -> Room boundary/source/generated handle graph.

## 3. Room -> finish synchronization

`RoomFinishSynchronizationService` là nguồn sự thật cho lifecycle update/re-activate của HT_Phòng.

- Chỉ nhận đúng `ProjectElement` Room/finish thuộc chính `ProjectState`; same-id object từ project khác bị từ chối.
- Stale AutoRoom không được dùng để refresh finish.
- Existing legacy finish được repair `RoomSourceId` canonical và bổ sung `DependsOn(room.Id)` nếu thiếu.
- `FloorId`, `ZoneId` và `DrawingFingerprint` của finish được đồng bộ theo Room.
- Các source metric đồng bộ gồm `AreaM2`, `PerimeterM`, `HeightM`, `OpeningAreaM2`, `DoorWidthM`.
- Metric được chuẩn hóa thành invariant finite non-negative number; dữ liệu invalid fail closed.
- Quan trọng: nếu Room không còn metric đó ở cả Properties lẫn Quantities, key tương ứng **bị xóa khỏi finish** thay vì giữ giá trị cũ. Điều này ngăn `WallFinish` tiếp tục trừ `OpeningAreaM2` cũ hoặc `Skirting` tiếp tục trừ `DoorWidthM` cũ sau topology/door update.
- `GenerateRoomFinishes` và `SyncExistingRoomFinishes` đều đi qua service chung rồi regenerate finish.
- `QS3DROOMAUTO` re-use/re-activate đúng Room sẽ sync finish hiện hữu trước project regeneration. Nếu topology split/merge sinh Room mới và Room cũ thành stale, QS3D không tự đoán chuyển finish sang Room mới; finish cũ tiếp tục bị exclusion/Health bắt cho tới khi người dùng xác nhận/generate theo Room mới.

## 4. Quantity exclusion

Room-linked finish bị loại khỏi quantity nếu:

- Room không còn tồn tại;
- referenced element không phải `Room`;
- AutoRoom đã stale;
- finish và Room không cùng Floor/Zone.

Finish thật sự chưa có Room provenance vẫn được giữ trong HT_Phòng schedule dưới nhãn `(chưa liên kết phòng)`, nhưng Health tạo warning để repair trước release.

## 5. Duplicate fail-closed

`RoomFinishIdentityService.ValidateProject(project)` chạy trước:

- `ProjectQuantityReportBuilder.Group(project)`;
- `RoomFinishScheduleBuilder.Build(project)`;
- `MaterialUsageScheduleBuilder.Build(project)`.

Vì vậy canonical + legacy duplicate hoặc hai legacy finish cùng Room + Category không được cộng đôi. BQ, Material Usage và HT_Phòng Schedule đều từ chối build cho đến khi duplicate được repair.

## 6. Health / repair diagnostics

`RoomFinishHealthService` surfacing các code:

- `UNLINKED_ROOM_FINISH`
- `ORPHAN_ROOM_FINISH`
- `INVALID_ROOM_FINISH_PARENT`
- `ROOM_FINISH_SCOPE_MISMATCH`
- `STALE_ROOM_FINISH`
- `ROOM_PROVENANCE_CONFLICT`
- `DUPLICATE_ROOM_FINISH`

Command `QS3DROOMFINISHHEALTH` mở review modeless và Locate qua dependency-aware source handle resolution. `QS3DHEALTHALL` cũng aggregate cùng diagnostics.

## 7. Release integrity

`BomReleaseGuardService` nhận Room Finish Health trước khi dựng release set.

- provenance/identity issue vẫn xuất hiện thành structured blocker;
- lỗi khi quyết định exclusion trở thành `BOM_EXCLUSION_FAILED`, không làm release guard crash;
- lỗi traceability trở thành `BOM_TRACEABILITY_FAILED`;
- lỗi dựng grouped BQ trở thành `BOM_REPORT_FAILED`.

Runtime/private-DWG Gate C/D vẫn là bước riêng; static/Core integrity không thay cho compile/NETLOAD/Boolean/Undo/DPI validation trên BricsCAD V25 thật.

## 8. Validation guards

Các source guards liên quan:

- `scripts/preflight-room-lifecycle.py`
- `scripts/preflight-room-finish-schedule.py`
- `scripts/preflight-room-finish-health.py`
- `scripts/preflight-room-finish-identity.py`
- `scripts/preflight-material-usage.py`
- `scripts/preflight-schedule-hub.py`
- `scripts/preflight-release-readiness.py`

Core smoke liên quan:

- `AutoRoomLifecycleSmoke`
- `RoomFinishIdentitySmoke`
- `RoomFinishSynchronizationSmoke`
- `RoomFinishHealthSmoke`
- `RoomFinishScheduleSmoke`
- `MaterialUsageScheduleSmoke`
- `BomReleaseGuardSmoke`
