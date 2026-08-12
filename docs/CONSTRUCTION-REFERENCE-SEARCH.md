# Construction Reference Search

`QS3DREFSEARCH` mở cửa sổ **Tham khảo thi công** dạng modeless, gắn với đúng BricsCAD Document đã khởi tạo cửa sổ.

## Mục đích

Tính năng này hỗ trợ tra cứu nhanh các hình ảnh và tài liệu tham khảo phục vụ dựng hình/triển khai thi công mà không nhúng trang kết quả vào plugin. Người dùng nhập từ khóa rồi chọn một trong các nhóm:

- Hình ảnh
- Web
- Video
- Mua sắm
- Video ngắn
- Tin tức

Các từ khóa nhanh mặc định gồm `Ván khuôn móng`, `Cốt thép móng`, `Chi tiết dầm`, `Chi tiết sàn`, `Cấu tạo tường` và `Mặt cắt móng`.

## Cách dùng

1. Trong BricsCAD V25 chạy lệnh `QS3DREFSEARCH`.
2. Nhập từ khóa cần tìm hoặc chọn một từ khóa nhanh.
3. Có thể bật **Ưu tiên ngữ cảnh kỹ thuật xây dựng / chi tiết thi công** để thêm ngữ cảnh kỹ thuật vào truy vấn.
4. Chọn loại kết quả. Nhấn Enter trong ô từ khóa tương đương mở **Hình ảnh**.
5. Kết quả được mở bằng **trình duyệt mặc định** của Windows.

Cửa sổ được khóa theo Document. Nếu người dùng chuyển sang DWG khác, thao tác mở kết quả sẽ fail closed cho đến khi kích hoạt lại đúng bản vẽ đã mở cửa sổ.

## Biên an toàn

QS3D chỉ ghép URL HTTPS cố định của nhà cung cấp tìm kiếm, mã hóa toàn bộ từ khóa bằng `Uri.EscapeDataString` và bật SafeSearch trong tham số truy vấn khi nhà cung cấp hỗ trợ. Từ khóa được giới hạn độ dài trước khi ghép URL.

Tính năng **không scrape** HTML, không dùng `HttpClient`, `WebClient`, `WebRequest`, WPF `WebBrowser`, API key bên thứ ba hoặc dependency NuGet mới. Kết quả không được tải vào process BricsCAD; plugin chỉ yêu cầu hệ điều hành mở URL bằng trình duyệt mặc định.

Tính năng cũng không tạo hoặc mutate `ProjectState`, không ghi CAD database, không tạo transaction và không đụng persistence `.qsdb`.

## Kiểm tra source-safe

Chạy:

```powershell
python scripts/preflight-construction-reference-search.py
```

Preflight kiểm tra:

- command `QS3DREFSEARCH` và modeless launcher;
- đủ 6 nhóm kết quả và các từ khóa nhanh;
- XAML parse được;
- query luôn được URL-encode;
- chỉ dùng URL HTTPS cố định + SafeSearch;
- shell launch qua trình duyệt mặc định;
- document affinity/fail-closed khi đổi DWG;
- không xuất hiện API scrape/network client, WebBrowser, project mutation hoặc CAD write transaction.

## Local validation

Source-safe preflight không thay thế runtime test với BricsCAD V25 có license. Trên máy local cần xác nhận thêm: cửa sổ modeless hiển thị đúng theme, Enter/các nút mở đúng loại kết quả, trình duyệt mặc định nhận URL đúng, và chuyển active DWG khiến cửa sổ cũ từ chối mở kết quả.
