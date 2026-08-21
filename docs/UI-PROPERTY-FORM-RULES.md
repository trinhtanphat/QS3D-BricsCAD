# Quy tắc Property Form theo từng Menu Item

## Mục tiêu

Panel thuộc tính của QS3D **không phải một form chung dùng cho tất cả loại đối tượng**. Mỗi item trong menu đại diện cho một loại đối tượng/nghiệp vụ riêng và phải có bộ thuộc tính, schema và form tương ứng với loại đó.

> **Nguyên tắc chốt: chung component, không chung schema/form.**

## Quy tắc bắt buộc

- Mỗi `itemType` phải có `PropertySchema` / `PropertyForm` riêng phù hợp nghiệp vụ của item đó.
- Không dùng một universal/generic form cố định làm form thuộc tính cho toàn bộ menu.
- Có thể tái sử dụng các component/field nền tảng như Tên, Layer, Màu, Ghi chú, trạng thái hiển thị hoặc các control UI chung; tuy nhiên schema, section, field đặc thù, validation, default value và logic ẩn/hiện/enable/disable phải được xác định theo từng `itemType`.
- Khi người dùng đổi item đang chọn, Properties Panel phải đổi schema/form theo item tương ứng ngay; không giữ nguyên một bộ field chung chung.
- Item con có nghiệp vụ khác nhau cũng được xem là type riêng. Ví dụ `Lưới Thẳng` và `Lưới Cong` không được render cùng một schema chỉ vì cùng nằm trong nhóm `Lưới Trục`.
- Không gom toàn bộ thuộc tính của mọi loại vào một form khổng lồ rồi rải `if/else` thủ công để ẩn/hiện field. Mapping type -> schema/form phải rõ ràng, có thể mở rộng và kiểm thử được.

## Các menu item hiện tại cần được coi là các loại thuộc tính riêng

Theo cây menu hiện tại, tối thiểu cần phân biệt các nhóm/type sau:

- `Lưới Thẳng`
- `Lưới Cong`
- `HT Phòng`
- `Dầm`
- `Sàn`
- `Cột`
- `Vách`
- `Tường KT`
- `Cửa`
- `Cầu Thang`
- `Móng`
- `Đào đắp`
- `Kết cấu thép`
- `Cấu kiện khác`

Nếu sau này menu có thêm item mới thì item mới cũng phải khai báo schema/form riêng hoặc khai báo rõ ràng việc kế thừa một schema đã có vì cùng một nghiệp vụ; không được tự động fallback vào một form tổng quát chỉ vì chưa có cấu hình.

## Ví dụ thuộc tính đặc thù

| Menu item | Thuộc tính đặc thù minh họa |
|---|---|
| `Lưới Thẳng` | hướng, vị trí/khoảng cách, phạm vi, nhãn trục... |
| `Lưới Cong` | tâm, bán kính, góc/cung, phạm vi, nhãn trục... |
| `HT Phòng` | mã/tên phòng, loại phòng, cao độ, diện tích hoặc dữ liệu nghiệp vụ phòng... |
| `Dầm` | tiết diện, kích thước, cao độ, vật liệu, liên kết... |
| `Sàn` | chiều dày, cao độ, vật liệu, biên/phạm vi... |
| `Cột` | tiết diện, kích thước, chiều cao/cao độ, vật liệu... |
| `Vách` | chiều dày, chiều cao/cao độ, vật liệu, biên... |
| `Tường KT` | chiều dày, cao độ, lớp/cấu tạo, vật liệu... |
| `Cửa` | kích thước, cao độ, kiểu mở, host/liên kết tường... |
| `Cầu Thang` | số bậc, kích thước bậc, chiều cao tầng, vế thang, chiếu nghỉ... |
| `Móng` | loại móng, kích thước, cao độ, vật liệu... |
| `Đào đắp` | loại công tác, cao độ, phạm vi, khối lượng/tham số hình học liên quan... |
| `Kết cấu thép` | loại cấu kiện, profile/section, vật liệu, liên kết... |
| `Cấu kiện khác` | schema riêng theo subtype/cấu kiện thực tế, không mặc định dùng form của loại khác. |

Các field trong bảng chỉ là ví dụ định hướng; source/domain model thực tế quyết định danh sách field cuối cùng. Quy tắc quan trọng là **mỗi loại có schema phù hợp với chính nó**.

## Gợi ý kiến trúc

```text
itemType
  -> propertySchema
  -> propertyForm / renderer
  -> validation + defaults + conditional rules
```

Ví dụ registry:

```text
STRAIGHT_GRID -> StraightGridPropertyForm
CURVED_GRID   -> CurvedGridPropertyForm
ROOM_SYSTEM   -> RoomSystemPropertyForm
BEAM          -> BeamPropertyForm
SLAB          -> SlabPropertyForm
COLUMN        -> ColumnPropertyForm
WALL          -> WallPropertyForm
TECH_WALL     -> TechnicalWallPropertyForm
DOOR          -> DoorPropertyForm
STAIR         -> StairPropertyForm
FOUNDATION    -> FoundationPropertyForm
EARTHWORK     -> EarthworkPropertyForm
STEEL         -> SteelPropertyForm
OTHER         -> OtherComponentPropertyForm
```

Phần component UI có thể dùng chung, nhưng registry/schema phải giữ ranh giới theo type. Nếu nhiều item dùng chung một field thì tái sử dụng field/component đó; không vì vậy mà hợp nhất toàn bộ form thành một form chung.

## Hành vi khi chọn item

1. Xác định `itemType` của item/menu node đang được chọn.
2. Lấy đúng schema/form đã đăng ký cho `itemType` đó.
3. Render các section/field tương ứng.
4. Áp dụng default, validation và rule ẩn/hiện/enable/disable của chính type đó.
5. Khi selection đổi sang type khác, thay schema/form tương ứng và không để sót field/state của type trước.

## Acceptance criteria

- Chọn hai item khác loại phải thấy bộ field/section phù hợp và có thể khác nhau theo nghiệp vụ.
- `Lưới Thẳng` và `Lưới Cong` không render cùng một schema thuộc tính nếu hình học/nghiệp vụ của chúng khác nhau.
- `Dầm`, `Sàn`, `Cột`, `Vách`, `Cửa`, `Cầu Thang`, `Móng`, v.v. có schema/form riêng thay vì cùng dùng một generic form.
- Field/component dùng chung được tái sử dụng ở mức component/schema fragment, không copy-paste UI và không biến thành một universal form.
- Validation, default value, visibility và enabled state có thể được khai báo theo từng `itemType`.
- Chuyển selection giữa các type không làm rò field/value/state không thuộc type mới.
- Item mới phải đăng ký schema/form rõ ràng hoặc khai báo kế thừa có chủ đích; thiếu schema phải fail rõ ràng thay vì silently dùng một form tổng quát không đúng nghiệp vụ.

## Phạm vi của tài liệu này

Tài liệu này là contract UI/kiến trúc cho Properties Panel trong plugin QS3D-BricsCAD. Nó không khẳng định rằng toàn bộ các schema/form trên đã được implement; khi triển khai source phải đối chiếu domain model và command/workflow thực tế của từng loại đối tượng.
