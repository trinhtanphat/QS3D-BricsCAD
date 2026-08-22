# 2D Plan -> 3D Quick Workflow

Mục tiêu của workflow này là đưa luồng thao tác trong hình tham chiếu vào QS3D theo đúng product boundary **BricsCAD V25 native**: lấy mặt bằng 2D có sẵn, chọn một lần và tạo semantic + Solid3d ngay trong DWG hiện tại với số thao tác tối thiểu.

## Luồng 3 bước

### Bước 1 — Nhập / chuẩn bị mặt bằng 2D

- Dùng CAD 2D đang có trong Model Space, hoặc `QS3DPLANIMPORT` để nhập plan được QS3D hỗ trợ.
- Wall centerline đầu vào cho batch conversion là `LINE` hoặc **open `POLYLINE`**.
- Closed `POLYLINE` không được tự đoán là centerline tường; tách/BREAK thành LINE hoặc open POLYLINE trước để tránh tạo mô hình sai.
- QS3D chạy trực tiếp trong BricsCAD V25; không yêu cầu AutoCAD/BLT3D làm trung gian.

### Bước 2 — Chuyển mặt bằng sang tường 3D

Hai command quick tương đương:

- `QS3DCONVERT2D`
- `QS3DPLAN2WALLS`

Cách dùng quick path:

1. Preselect hoặc chọn nhiều `LINE` / open `POLYLINE` của tường trên mặt bằng.
2. QS3D đọc `ThicknessM`, `HeightM`, `BottomOffsetM` từ active/preferred `ArchitecturalWall` Family hiện có mà **không mở ba numeric prompt**.
3. Nếu drawing chưa có project/Family, fallback quick hiện là `ThicknessM=0.2 m`, `HeightM=3.0 m`, `BottomOffsetM=0 m`.
4. QS3D capture từng source thành `ArchitecturalWall`, áp cùng bộ thông số, regenerate đúng wall vừa capture và gọi native wall builder ngay.
5. Khi hoàn tất, QS3D chọn generated solids và chuyển sang `QS3DVIEW3D`.

Khi cần override bộ thông số cho riêng batch hiện tại, dùng:

- `QS3DCONVERT2DADV`

Advanced path giữ ba prompt cũ cho Thickness / Height / BottomOffset, với Family values làm default. Cancel ở bất kỳ prompt nào vẫn kết thúc trước project mutation/bootstrap.

Điểm quan trọng: **CAD 2D gốc không bị xóa hay thay thế**. Nó tiếp tục là semantic source; Solid3d do QS3D sinh có ownership marker riêng.

## Batch safety

`QS3DCONVERT2D` dành cho CAD 2D **chưa từng capture vào QS3D**. Nếu một source handle đã thuộc semantic element, command fail-closed và hướng người dùng sang `QS3DSETWALL` / `QS3DREFRESH` thay vì âm thầm tạo ownership trùng.

Trước mutation, toàn selection được kiểm tra:

- phải ở Model Space;
- chỉ nhận `LINE` hoặc open `POLYLINE`;
- LINE wall phải gần ngang, `|ΔZ| <= 0.005 m`;
- POLYLINE phải có ít nhất 2 đỉnh và mặt phẳng song song WCS XY;
- source phải chưa thuộc semantic/generated ownership khác.

### Preview-to-commit freshness

Family defaults được đọc trước commit, vì vậy command giữ một **preview-to-commit** boundary rõ ràng thay vì tin rằng project/drawing/source vẫn giống lúc bắt đầu. Plan-to-3D dùng cùng `DirectDrawProjectPreviewContext` với Direct Draw để snapshot canonical project state và CAD context trước khi có prompt hoặc mutation.

Trước `ProjectStateSnapshot` hoặc semantic/native mutation, command hiện:

- xác nhận đúng DWG ban đầu vẫn active;
- kiểm tra lại Model Space và planar UCS;
- **re-preflight** đúng các `ObjectId` đã chọn và yêu cầu count/ObjectId/handle/source kind/canonical geometry fingerprint vẫn khớp selection ban đầu;
- resolve lại guarded preview và yêu cầu drawing unit policy + exact UCS vẫn đúng snapshot ban đầu;
- nếu preview đã có project, bind canonical existing project và yêu cầu **same `ProjectId`** cùng **same `ProjectState.ChangeVersion`**;
- nếu project đã bị chỉnh sửa trong lúc người dùng xác nhận Advanced prompts, fail-closed để tránh áp Family defaults/parameters đọc từ state cũ;
- nếu preview bắt đầu projectless nhưng một project appears hoặc sidecar/backing store xuất hiện trước commit, fail-closed và yêu cầu chạy lại thay vì áp projectless defaults vào project vừa xuất hiện;
- sau khi project được resolve mới kiểm lại semantic/generated ownership của source;
- chỉ sau toàn bộ freshness checks mới capture snapshot và bắt đầu batch mutation.

Quick path dùng Family/fallback values đã đọc trong preview. Advanced path dùng values người dùng xác nhận ở prompt. Cả hai đều dùng một mutation bridge chung, nên identity/version/unit/UCS/backing-store race không có đường bootstrap riêng trong `PlanTo3DCommands`.

Mỗi wall mới chỉ được semantic-regenerate bằng `RegenerateDirtySubset(project, new[] { element.Id })`; conversion không được regenerate hoặc mark-clean các element cũ đang dirty ngoài selection. Điều này vừa giới hạn side effect vừa tránh chi phí whole-project regeneration trong batch lớn.

Nếu batch mới bị lỗi giữa chừng, command tìm generated CAD bằng ownership metadata, xóa các Solid3d thuộc chính batch đó rồi restore `ProjectStateSnapshot`. Source 2D của người dùng không nằm trong rollback-delete set. Compensation này vẫn là whole-batch safety boundary hiện hữu; freshness hardening không tạo transaction engine thứ hai.

Static lifecycle contract được khóa bởi `scripts/preflight-plan-to-3d-project-lifecycle.py` và `scripts/preflight-plan-to-3d-preview-context.py`; same-ObjectId geometry freshness được khóa bởi `scripts/preflight-plan-to-3d-source-geometry-freshness.py`; scoped regeneration được khóa bởi `scripts/preflight-plan-to-3d-scoped-regeneration.py`; quick-vs-advanced interaction contract được khóa bởi `scripts/preflight-plan-to-3d-quick-authoring.py`.

Exact BricsCAD V25 proof cho toàn bộ Plan-to-3D command contract — quick no-prompt defaults của `QS3DCONVERT2D` / `QS3DPLAN2WALLS`, advanced prompt cancellation của `QS3DCONVERT2DADV`, preview-to-commit freshness, scoped regeneration và ownership-scoped compensation — nằm trong **LOCAL-014** của `docs/LOCAL-AGENT-INBOX.md`. Runtime proof cho `QS3DDRAWWINDOW`, các quick-workflow Ribbon entry, Auto Host và explicit `QS3DCUTSELECTEDOPENINGS` handoff nằm trong **LOCAL-008**. Cả hai vẫn là `PENDING_LOCAL`; source review không được coi là `LOCAL_PASS` và không xác nhận các source gap chưa được sửa.

## Bước 3 — Hoàn thiện mô hình

Sau khi có tường 3D, tiếp tục trực tiếp trong BricsCAD bằng các công cụ QS3D:

- `QS3DDRAWDOOR` — vẽ Cửa Đi và Auto Host vào tường semantic duy nhất;
- `QS3DDRAWWINDOW` — vẽ Cửa Sổ bằng `WallOpening` canonical, mặc định gợi ý cao `1.2 m` và bậu `0.9 m`, gắn `OpeningUsage=Window`, rồi Auto Host;
- `QS3DDRAWOPENING` — tạo lỗ mở/vách tổng quát;
- `QS3DMATERIALS` — mở Material Catalog để đổi vật liệu và thông số;
- `QS3DCUTSELECTEDOPENINGS` — khoét vật lý các Cửa/Lỗ/Cửa Sổ đã chọn khi người dùng sẵn sàng commit boolean;
- `QS3DSETWALL` — chỉnh các tường đã capture;
- `QS3DREFRESH` — đồng bộ lại native geometry khi source/semantic thay đổi.

`QS3DDRAWWINDOW` không tạo thêm một `ElementCategory.Window` song song. Nó dùng `WallOpening` hiện hữu để giữ nguyên host/boolean/health/quantity contract và chỉ thêm semantic usage `Window` cho UI/schedule. Door/Opening schedule vì vậy vẫn đi qua cùng pipeline nhưng có thể hiển thị Cửa Sổ thành nhóm riêng.

Window authoring cũng fail-closed: source LINE do command tạo sẽ bị xóa và semantic snapshot được restore nếu không tìm được host duy nhất hoặc Auto Host làm state không còn hợp lệ. Việc khoét vật lý vẫn là một bước explicit qua `QS3DCUTSELECTEDOPENINGS`, không âm thầm boolean trong lúc authoring.

## Ribbon nhanh

Tab **TẠO MỚI** được augment thêm các entry point theo đúng workflow tham chiếu:

- **2D → Tường 3D** → `QS3DCONVERT2D` quick/no-prompt path;
- **Vẽ Cửa Sổ** → `QS3DDRAWWINDOW`;
- **Vật liệu** → `QS3DMATERIALS`.

Các nút Direct Draw hiện hữu như Vẽ Tường, Vẽ Dầm, Vẽ Cột, Vẽ Sàn, Vẽ Cửa và Vẽ Lỗ Mở vẫn giữ nguyên. Augmenter chỉ bổ sung discoverability vào tab hiện hữu và dùng ID ổn định để không tạo nút/tab trùng khi plugin được khởi tạo lại.

Như vậy luồng sử dụng mặc định trở thành:

`2D plan -> select walls -> QS3DCONVERT2D -> immediate 3D -> QS3DDRAWDOOR / QS3DDRAWWINDOW -> QS3DMATERIALS -> optional targeted cut`

thay vì phải lặp `select -> capture -> set property -> build 3D` cho từng đối tượng hoặc nhập lại cùng ba thông số cho mỗi batch.

## Product boundary

Hình tham chiếu có nhắc AutoCAD/BLT3D và copy sang BricsCAD. QS3D **không** thêm AutoCAD adapter hay phụ thuộc BLT3D. Tính năng này triển khai cùng ý tưởng UX trực tiếp trên BricsCAD V25 và tái sử dụng semantic capture + native builders + QS3D ownership hiện có.

Exact-current-sha compile/NETLOAD/runtime validation vẫn cần môi trường BricsCAD V25 có license; source-side implementation không được mô tả là runtime-certified nếu chưa chạy trên môi trường đó.
