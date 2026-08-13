# QS3D Vẽ cơ bản — Active Family workflow

Updated: 2026-08-13 (UTC+7)

## Mục tiêu

Bổ sung đúng phần còn thiếu của tài liệu tham chiếu bản đầu: người dùng chọn Family / Type trong Workspace, Add/chỉnh thuộc tính như hiện có, sau đó có thể gọi ba lệnh vẽ CAD cơ bản trực tiếp trong BricsCAD. Family đang chọn là **ngữ cảnh thật của lệnh kế tiếp**, không chỉ là nhãn hiển thị.

QS3D vẫn là plugin chạy trong BricsCAD. BricsCAD tiếp tục sở hữu viewport, Editor, DWG database và native entity.

## Ba lệnh

- `QS3DDRAWLINE` — chọn điểm đầu và điểm cuối, tạo một native `LINE`.
- `QS3DDRAWRECT` — chọn hai góc đối diện trong UCS hiện tại, tạo một closed rectangular `POLYLINE`.
- `QS3DDRAWCIRCLE` — chọn tâm rồi chọn điểm trên đường tròn hoặc nhập bán kính, tạo một native `CIRCLE`.

Trong danh sách Family của Workspace, menu chuột phải hiển thị ba lệnh này. Phím tắt khi Workspace có focus là `Ctrl+1`, `Ctrl+2`, `Ctrl+3`. `Ctrl+D` / `Ctrl+Shift+D` vẫn giữ nguyên Direct Draw semantic hiện có.

## Contract Active Family

Mỗi lệnh Vẽ cơ bản:

1. chỉ chạy khi bản vẽ có QS3D project và có canonical Active Family;
2. chụp `ProjectId`, `ChangeVersion`, Family id/category, Floor, Zone và UCS trước khi người dùng bắt đầu chọn hình học;
3. ngay trước CAD commit, kiểm tra lại active DWG, Model Space, UCS, project version, Family, Floor và Zone;
4. nếu người dùng đổi Family, Zone, Floor, thuộc tính/project hoặc DWG trong lúc prompt đang mở, lệnh fail-closed và yêu cầu chạy lại;
5. sau commit, entity mới được chọn và Workspace status báo đúng Family/category đã dùng.

Vì vậy flow chuẩn là:

```text
Chọn cấu kiện / Family trong panel
→ Add hoặc chọn Family hiện có
→ chỉnh thuộc tính
→ QS3DDRAWLINE / QS3DDRAWRECT / QS3DDRAWCIRCLE
→ đổi Family
→ lệnh vẽ tiếp theo dùng Family mới
```

## Dấu ngữ cảnh trên DWG

Native entity do ba lệnh tạo được gắn XData application `QS3DBASICDRAW`, version `1`.

Marker lưu dạng token SHA-256 cho project/family/floor/zone cùng category và primitive kind. Không ghi raw ProjectId/FamilyId/FloorId/ZoneId vào XData. Marker này làm cho Active Family context tồn tại cùng entity thay vì biến mất sau khi command kết thúc.

Marker **không phải generated-geometry ownership** và không được dùng để giả lập semantic BIM ownership.

## Ranh giới với Direct Draw semantic

`QS3DDRAWLINE`, `QS3DDRAWRECT`, `QS3DDRAWCIRCLE` là công cụ drafting bản đầu theo tài liệu tham chiếu. Chúng không tự suy đoán rằng mọi LINE/Rectangle/Circle đều là Tường/Dầm/Sàn/Cột/... và không tự gọi `SemanticCaptureService`.

Muốn tạo semantic + native 3D theo đúng category, dùng workflow hiện có:

- chọn Family rồi `QS3DDRAWACTIVE` / `QS3DDRAWACTIVEADV`; hoặc
- dùng command Direct Draw cụ thể như `QS3DDRAWWALL`, `QS3DDRAWBEAM`, `QS3DDRAWCOLUMN`, `QS3DDRAWSLAB`, ...

Ranh giới này tránh việc một Circle bất kỳ bị gán sai thành một BIM element chỉ vì Family đang active.

## Cancel / lỗi

ESC/cancel trước commit không tạo native entity và không sửa project. Lệnh chỉ mở transaction tạo entity sau khi toàn bộ prompt cần thiết đã thành công và active context được xác minh lại.

Ba lệnh bản đầu hiện giới hạn ở Model Space. Exact editor/UCS/palette/runtime behavior phải được kiểm chứng trên BricsCAD V25 thật; source/static review không được báo thành `LOCAL_PASS`.
