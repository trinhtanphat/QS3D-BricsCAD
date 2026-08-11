# Workspace compact shell — 2026-08-11

## Mục tiêu

Hoàn thiện Workspace theo tinh thần ảnh tham chiếu BLT3D: giao diện tối, dày thông tin nhưng dễ quét mắt, ưu tiên thao tác mô hình trực tiếp trong BricsCAD và giữ các tác vụ thường dùng ở đúng vùng làm việc. Thay đổi này là **presentation-only**: nó không tạo command mới, không đổi semantic model, không tạo viewport giả và không thay thế các handler đang chạy thật.

## Mapping với ảnh tham chiếu

- **Zone / Tầng**: giữ `ZoneCombo` và `FloorCombo`, giảm chiều cao chrome và tăng độ rõ của vùng phạm vi làm việc.
- **Cây mô hình**: giữ `ModelTree` cùng toàn bộ category/tag đang dùng; thu gọn chiều cao từng node để xem được nhiều cấu kiện hơn trong cùng chiều cao màn hình.
- **Family / Type**: giữ các thao tác thật `+ Thêm`, `Xóa`, `Bóc chọn`, `Vẽ 3D`, cùng nhóm `Giao tường`, `Snap xem`, `Snap áp`, `Auto Host`; tăng không gian cho danh sách Family và property inspector.
- **Thuộc tính**: giữ editor theo kiểu dữ liệu, scope hiện hành, search, state badge và reset override; vùng property được ưu tiên thêm chiều cao khi pane thấp.
- **HT_PHÒNG**: giữ luồng hoàn thiện phòng và các nút có handler thật.
- **Đối tượng đang chọn**: giữ bảng CAD inspection cùng `Focus`, `Cô lập`, `Khôi phục`, `Định vị`, `Mặt bằng` để thao tác lựa chọn giống workflow trong ảnh.
- **BricsCAD viewport**: viewport trung tâm vẫn là viewport native của BricsCAD. Workspace chỉ điều khiển/inspect nó; không nhúng `Viewport3D` hoặc dựng một canvas CAD song song.
- Phần `DIỄN GIẢI KHỐI LƯỢNG`, quản lý bản vẽ/Xref/layer và BQ chuyên sâu tiếp tục nằm ở các surface/right-panel đang được phát triển riêng; không nhân đôi chúng vào Workspace.

## Mật độ và khả năng sử dụng

Lớp `WorkspacePanel.CompactShell.cs` chạy idempotent khi `WorkspacePanel` được load và chỉ chỉnh presentation của các control đã tồn tại. Top bar/footer được thu gọn; cột cây mô hình và Family/Type được phân bổ ổn định hơn; vùng Family và HT_Phòng bớt chiếm chiều cao để property/selection inspector có thêm không gian. Mục tiêu nguồn là bố cục làm việc tốt ở nhóm màn hình **1366×768** trở lên, đồng thời giữ horizontal fallback của XAML hiện hữu khi palette hẹp.

Grid splitter chuyển sang preview resize để kéo pane mượt và ít giật nội dung hơn. Zone/Floor, Family search, cây mô hình, danh sách Family, property và inspection được đặt minimum density có chủ đích nhưng không khóa resize của người dùng.

## Discoverability

Các phím tắt đã tồn tại trong Workspace được đưa vào tooltip, không tạo behavior mới:

- `Ctrl+S` — Lưu project QS3D.
- `Ctrl+F` — focus ô tìm Family / Type.
- `Ctrl+B` — mở BQ.
- `F5` — làm mới Workspace/CAD.
- `Delete` — xóa Family khi Family list đang focus.

Các section quan trọng `PHẠM VI LÀM VIỆC`, `MÔ HÌNH`, `FAMILY / TYPE`, `THUỘC TÍNH`, `HT_PHÒNG`, `ĐỐI TƯỢNG ĐANG CHỌN` được giữ nguyên và tăng hierarchy typography ở runtime để khớp cách chia vùng rõ ràng trong ảnh tham chiếu.

## Biên chức năng giữ nguyên

Presentation partial không được gọi `SendStringToExecute`, không dùng project mutation/context service, không tự capture semantic và không khai báo lại handler nút. Mọi hành động trên UI tiếp tục đi qua handler hiện hữu trong `WorkspacePanel` và command thật của plugin. Điều này giúp đổi mật độ/UX mà không tạo đường ghi state thứ hai.

`WorkspacePanel.xaml` vẫn là contract nguồn cho `ZoneCombo`, `FloorCombo`, `ModelTree`, `FamilyList`, `PropertyList`, `InspectionList`, các action handler và footer `Mô hình / BQ / Kiểm tra`. Focused preflight khóa các binding/handler/tag quan trọng, kiểm tra XAML well-formed và từ chối embedded fake viewport.

## Validation

Source-side gate: `python scripts/preflight-workspace-compact-shell.py`.

Gate này được auto-discover bởi `scripts/preflight-all.py`. Nó xác minh contract XAML, presentation-only partial, shortcut discoverability và boundary native BricsCAD. Không dispatch GitHub Actions từ agent remote.

Việc xác nhận pixel-level, contrast thực tế, resize, dock/undock và 100/125/150/200% DPI phải thực hiện trong BricsCAD V25/Windows thật theo **LOCAL-012**. Source-side PASS không được xem là native WPF/BricsCAD runtime PASS.
