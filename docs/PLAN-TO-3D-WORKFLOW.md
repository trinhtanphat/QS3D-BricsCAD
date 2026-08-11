# 2D Plan -> 3D Quick Workflow

Mục tiêu của workflow này là đưa luồng thao tác trong hình tham chiếu vào QS3D theo đúng product boundary **BricsCAD V25 native**: lấy mặt bằng 2D có sẵn, chọn một lần, nhập thông số tường một lần và tạo semantic + Solid3d ngay trong DWG hiện tại.

## Luồng 3 bước

### Bước 1 — Nhập / chuẩn bị mặt bằng 2D

- Dùng CAD 2D đang có trong Model Space, hoặc `QS3DPLANIMPORT` để nhập plan được QS3D hỗ trợ.
- Wall centerline đầu vào cho batch conversion là `LINE` hoặc **open `POLYLINE`**.
- Closed `POLYLINE` không được tự đoán là centerline tường; tách/BREAK thành LINE hoặc open POLYLINE trước để tránh tạo mô hình sai.

### Bước 2 — Chuyển mặt bằng sang tường 3D

Hai command tương đương:

- `QS3DCONVERT2D`
- `QS3DPLAN2WALLS`

Cách dùng:

1. Preselect hoặc chọn nhiều `LINE` / open `POLYLINE` của tường trên mặt bằng.
2. Nhập **bề dày tường** một lần cho toàn selection.
3. Nhập **chiều cao tường** một lần cho toàn selection. Fallback cho project mới là `3.0 m` (3000 mm), đúng quick-workflow tham chiếu; nếu project đã có ArchitecturalWall family thì command dùng family value làm default.
4. Nhập `BottomOffsetM` nếu cần.
5. QS3D capture từng source thành `ArchitecturalWall`, áp cùng bộ thông số, regenerate semantic và gọi native wall builder ngay.
6. Khi hoàn tất, QS3D chọn generated solids và chuyển sang `QS3DVIEW3D`.

Điểm quan trọng: **CAD 2D gốc không bị xóa hay thay thế**. Nó tiếp tục là semantic source; Solid3d do QS3D sinh có ownership marker riêng.

## Batch safety

`QS3DCONVERT2D` dành cho CAD 2D **chưa từng capture vào QS3D**. Nếu một source handle đã thuộc semantic element, command fail-closed và hướng người dùng sang `QS3DSETWALL` / `QS3DREFRESH` thay vì âm thầm tạo ownership trùng.

Trước mutation, toàn selection được kiểm tra:

- phải ở Model Space;
- chỉ nhận `LINE` hoặc open `POLYLINE`;
- LINE wall phải gần ngang, `|ΔZ| <= 0.005 m`;
- POLYLINE phải có ít nhất 2 đỉnh và mặt phẳng song song WCS XY;
- source phải chưa thuộc semantic/generated ownership khác.

Nếu batch mới bị lỗi giữa chừng, command tìm generated CAD bằng ownership metadata, xóa các Solid3d thuộc chính batch đó rồi restore `ProjectStateSnapshot`. Source 2D của người dùng không nằm trong rollback-delete set.

## Bước 3 — Hoàn thiện mô hình

Sau khi có tường 3D, tiếp tục bằng các công cụ QS3D hiện hữu:

- `QS3DDRAWDOOR` / door workflow để thêm cửa đi;
- `QS3DDRAWWINDOW` / window workflow để thêm cửa sổ;
- Family / Material Catalog để đổi family, vật liệu và thông số;
- `QS3DSETWALL` để chỉnh các tường đã capture;
- `QS3DREFRESH` để đồng bộ lại native geometry khi source/semantic thay đổi.

Như vậy luồng sử dụng trở thành:

`2D plan -> select walls -> QS3DCONVERT2D -> immediate 3D -> doors/windows/materials`

thay vì phải lặp `select -> capture -> set property -> build 3D` cho từng đối tượng.

## Product boundary

Hình tham chiếu có nhắc AutoCAD/BLT3D và copy sang BricsCAD. QS3D **không** thêm AutoCAD adapter hay phụ thuộc BLT3D. Tính năng này triển khai cùng ý tưởng UX trực tiếp trên BricsCAD V25 và tái sử dụng semantic capture + native builders + QS3D ownership hiện có.
