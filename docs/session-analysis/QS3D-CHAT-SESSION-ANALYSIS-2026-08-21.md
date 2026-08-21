# QS3D-BricsCAD — Tổng hợp và phân tích đầy đủ chat session 2026-08-20 → 2026-08-21

**Loại tài liệu:** historical session analysis / handoff context  
**Issue / Lane-Key:** #3408 / `issue-3408`  
**Canonical carrier:** `agent/chatgpt-gpt56sol/issue-3408-session-analysis`  
**Authored from main baseline:** `e207a05d77d8619094f72e63064866da0b596506`  
**Repository:** `trinhtanphat/QS3D-BricsCAD`  
**Mục tiêu:** lưu một bản tổng hợp đủ sâu để session/agent sau có thể hiểu yêu cầu, bằng chứng, quyết định, phần đã làm, phần còn thiếu và ranh giới không được suy diễn lại từ đầu.

> **Quy tắc đọc tài liệu này:** đây là bản ghi lịch sử/phân tích của session, **không phải** live backlog hay bằng chứng rằng mọi trạng thái lịch sử vẫn còn đúng ở thời điểm đọc. `current main`, source hiện tại, Issue/PR hiện tại, `AGENTS.md`, `docs/AGENT-RUNTIME-CONTRACT.md`, `docs/MAIN-WRITE-AUTHORIZATION.md`, `CI_POLICY.md`, `docs/PRODUCT-BOUNDARY.md`, `docs/REMOTE-AGENT-SCOPE.md` và `docs/LOCAL-AGENT-INBOX.md` luôn có ưu tiên cao hơn nếu có khác biệt.

---

## 1. Tóm tắt điều hành

Session này xoay quanh một mục tiêu sản phẩm nhất quán: biến `QS3D-BricsCAD` thành workflow BIM/QS thực dụng, quen thuộc kiểu BLT/BLT3D nhưng được triển khai clean-room trên kiến trúc QS3D hiện tại, trong đó kỹ sư QS có thể **dựng/chọn cấu kiện → xem 3D → tính BT/VK → review → locate/highlight → xuất Excel → từ Excel truy ngược lại model** với số thao tác thấp.

North-star nghiệp vụ đã hình thành trong trao đổi:

> **Vẽ nhanh → sửa nhanh → tính đúng BT/VK → giải thích được → click định vị → xuất đúng mẫu Excel.**

Các kết luận quan trọng nhất của session:

1. **QS3D-BricsCAD vẫn là plugin BricsCAD V25/V26**, không phải standalone CAD executable. BricsCAD sở hữu native DWG database/editor/viewport; QS3D cung cấp semantic BIM/QS, command, Ribbon/palette/modeless UI, quantity/reporting, recognition và generated geometry workflow.
2. **BLT/BLT3D chỉ là clean-room workflow/UX benchmark.** Không copy source, thuật toán proprietary, binary, license, asset, hoặc bundle BricsCAD SDK DLL của BLT.
3. Source QS3D đã có nền tảng khá rộng: semantic identity, project/floor/zone/family/type, native/semantic geometry ownership, QTO/BQ, locate/highlight, XLSX/CSV provenance, ED2 Excel→CAD locate, direct draw, openings, rebar, schedules, health/release checks, Ribbon/WPF surfaces.
4. Khoảng trống quan trọng được ưu tiên trong session là **customer-facing Excel round trip**, đặc biệt workbook dễ đọc + aggregate/detail reverse locate. Lane #3296 / PR #3299 đã land source-safe implementation lên `main` trong session.
5. **Formwork vẫn là nghiệp vụ cần đầu tư sâu hơn** để đạt BLT-like parity: deduction theo tiếp xúc bê tông, opening reveal/soffit, beam/slab/wall/foundation rules, giải thích công thức, và validation với real DWG.
6. Các yêu cầu UI kiểu BLT cần tập trung vào **workflow familiarity**, không pixel-clone. Ribbon-first, palette theo floor/category, command gần với thao tác QS, highlight/locate/recalculate là trọng tâm.
7. Repository governance đã được làm rõ mạnh trong session: **one Lane-Key → one owner → one canonical branch → one PR**, không direct write `main`, đỏ CI thì tự sửa trên carrier hiện tại, merge chỉ khi protected current candidate `preflight + core` xanh/current/mergeable, và licensed BricsCAD runtime evidence là `LOCAL_ONLY` khi chưa chạy thật.

---

## 2. Từ vựng trạng thái dùng trong phân tích

Để tránh trộn fact và suy luận, session dùng các marker sau:

- **[ĐÃ XÁC NHẬN]** — đã có bằng chứng từ source/repo/Issue/PR/CI hoặc artifact người dùng cung cấp.
- **[SUY LUẬN]** — kết luận hợp lý từ bằng chứng nhưng chưa phải chứng cứ runtime/behavior trực tiếp.
- **[CHƯA RÕ]** — chưa đủ evidence để kết luận.
- **[ĐỀ XUẤT]** — hướng kiến trúc/nghiệp vụ được khuyến nghị, chưa tự động trở thành product contract.
- **[MÂU THUẪN]** — hai nguồn/trạng thái không đồng nhất và cần source/current GitHub quyết định.
- **[RỦI RO]** — có khả năng tạo sai quantity, sai provenance, duplicate authority, runtime regression hoặc governance violation.

Một nguyên tắc xuyên suốt: **implemented in source ≠ production-qualified in BricsCAD**. Static/Core/CI build có thể chứng minh source-safe behavior, nhưng không được tự biến thành `LOCAL_PASS` cho NETLOAD, Ribbon/WPF rendering, native geometry, real-DWG, save/reopen, multi-DWG, installer/signing hoặc interactive behavior nếu chưa chạy đúng môi trường.

---

## 3. Product boundary và kiến trúc sản phẩm

### 3.1 [ĐÃ XÁC NHẬN] Shipping form của repo này

`QS3D-BricsCAD` là **Windows x64 BricsCAD-hosted plugin**:

- V25: `QS3D.BricsCAD.V25.dll`, .NET Framework 4.8 / `net48`;
- V26: `QS3D.BricsCAD.V26.dll`, .NET 8 / `net8.0-windows`;
- shared Core hiện tại: `QS3D.Core`, `netstandard2.0`;
- BricsCAD V25/V26 là native host, không được thay thế bởi một QS3D-owned viewport/database trong repository này.

Các sibling product có ranh giới riêng:

- `QS3D-Platform`: vendor-neutral shared domain/contracts;
- `QS3D-BricsCAD`: hosted BricsCAD plugin;
- `QS3D-CAD`: standalone CAD/BIM/QS product riêng.

Do đó các cụm từ “giống BLT”, “BLT-style”, “BLT-like app” trong session phải được hiểu là **familiar workflow/UX**, không phải yêu cầu repo này build `QS3D.exe`.

### 3.2 [ĐỀ XUẤT] Kiến trúc dài hạn hợp lý

Giữ boundary theo hướng:

```text
QS3D-Platform / QS3D.Core
  ├─ semantic BIM/QS model
  ├─ quantity/formwork/rebar/reporting
  ├─ persistence/provenance
  ├─ geometry/value objects
  └─ vendor-neutral application contracts
          |
          +--> QS3D-BricsCAD V25 adapter
          +--> QS3D-BricsCAD V26 adapter
          +--> sibling QS3D-CAD adapter/host
```

Host adapter nên sở hữu transaction/editor/selection/native `Solid3d`/WPF/Ribbon/NETLOAD concerns. Core không nên bị ô nhiễm bởi proprietary BricsCAD runtime types.

---

## 4. Requirement master hình thành trong session

### 4.1 [ĐÃ XÁC NHẬN] Mục tiêu sản phẩm cấp cao

Người dùng đã lặp lại các mục tiêu sau trong nhiều lượt trao đổi:

- dựng/load model BIM/QS;
- show/live 3D trong BricsCAD;
- xác định vị trí object;
- giao cắt/clash và overlap/trùng;
- highlight/isolate/select/zoom;
- quantity takeoff;
- xuất Excel;
- Excel/quantity → model trace-back;
- ưu tiên Model ↔ Excel bidirectional traceability;
- kiểm tra duplicate/missing quantity;
- thao tác nhanh cho một kỹ sư QS, hướng đến các review task chính khoảng 1–2 click;
- giao diện/workflow quen thuộc với BLT/BLT3D nhưng không copy proprietary implementation.

### 4.2 [ĐÃ XÁC NHẬN] Quy trình phân tích yêu cầu được mong muốn

Một workflow quản lý yêu cầu được ghi nhận từ session trước:

```text
Problem P-xxx
→ Requirement R-xxx
→ Solution S-xxx
→ User Approval
→ Gap Analysis
→ Architecture
→ Plan
→ Task
→ Code
→ Test
```

Tài liệu session cũ còn ghi nguyên tắc rằng user là người duyệt requirement/solution cuối cùng, Change Request mới nên được tách `CR-xxx`, và mỗi requirement cần được map vào `Existing / Partial / Missing / Need Modify / Need New / Refactor` khi đối chiếu source.

### 4.3 [ĐỀ XUẤT] Golden path nghiệp vụ

Workflow phù hợp nhất với toàn bộ trao đổi:

```text
Create / Capture / Recognize
→ semantic object + native geometry
→ Calculate BT/VK/Rebar
→ Review quantities + explanation
→ Highlight / Locate / Isolate / Zoom
→ Recalculate after edit
→ Export customer Excel
→ Excel row/group → CAD object(s)
→ save/reopen with provenance intact
```

Đây là workflow “BLT-familiar” nhưng authority vẫn là semantic/project model của QS3D.

---

## 5. Phân tích clean-room BLT/BLT3D từ artifact người dùng cung cấp

### 5.1 [ĐÃ XÁC NHẬN] Bản executable được phân tích là package/SFX, không phải bằng chứng source

Static analysis của file BLT3D V25.6 cho thấy nó là **PE32 WinRAR SFX** chứa archive RAR5, không phải source tree. Archive có hàng trăm entries và nhiều module DLL/resources.

Các module quan sát được gồm những tên như:

- `blt3D.dll`;
- `blt_library.dll`;
- `BLT_QS_V2021.dll`;
- `BLT_QSR.dll`;
- `PHC_QS.dll`;
- `BLT_Fuzor.dll`;
- `model_from_cad.dll`;
- `BRC/BLT-BIM.dll`;
- các template Excel, DWG/LSP/resources và dependency liên quan.

### 5.2 [SUY LUẬN] Năng lực nghiệp vụ gợi ý từ metadata/module naming

Static metadata và naming cho thấy BLT-family có xu hướng bao phủ:

- BIM/QTO/formwork;
- beam/wall/slab/column/foundation workflows;
- rebar/shopdrawing;
- recognition / 2D→3D;
- level/project/WBS;
- earthwork/model quantities;
- Excel integration;
- một số interoperability bridge.

Đây **không phải** bằng chứng về thuật toán nội bộ, API contract hoặc quyền sao chép implementation. Nó chỉ giúp xác định feature/workflow benchmark.

### 5.3 [ĐÃ XÁC NHẬN] Một số cấu hình BLT quantity/formwork quan sát được

File setup được quan sát trong bundle có các giá trị:

```json
{
  "coppha_đáy_dầm_tính_vào_đáy_sàn": false,
  "custom1": false,
  "custom2": false,
  "custom3": false,
  "custom4": false,
  "dien_tich_ck_lay_khi": 0.001,
  "goc_min_lay_coppha_mat_tren": 45,
  "offset_coppha": 6,
  "the_tich_ck_lay_khi": 0.0001,
  "tinhTongChieuDai": false
}
```

Ý nghĩa phù hợp với domain formwork/QTO: có threshold diện tích/thể tích, lựa chọn ownership đáy dầm/đáy sàn, offset coppha, rule góc mặt trên, và một số toggle. Đây là **compatibility/behavior clue**, không phải source contract cần copy nguyên xi.

### 5.4 [ĐÃ XÁC NHẬN] UI/workflow tham chiếu từ screenshot

Ribbon BLT/BIM screenshot trong session cho thấy các nhóm hành vi điển hình:

- **BẢN VẼ**: Level;
- **VẼ**: điểm/line/rectangle/circle/tạo cửa/tạo lanh tô/chọn đối tượng;
- **TỰ ĐỘNG NHẬN DẠNG**: nhận dạng dầm/tường, tạo bê tông lót, cập nhật vị trí, chuyển đổi solid, copy tầng;
- **HỆ THỐNG**: cài đặt, xóa highlight, sửa thuộc tính hàng loạt;
- **KHỐI LƯỢNG**: khấu trừ thể tích, khấu trừ diện tích, tính toán, xem khối lượng;
- **HIỂN THỊ**: hiện tất cả mô hình, hiện coppha;
- **HỆ THỐNG/BẢN QUYỀN**: login/register/info.

Palette trái được tổ chức theo floor/category, với nhóm quan sát được như:

- Room/finish-related;
- Dầm;
- Sàn + Lỗ Mở;
- Cột;
- Vách + Lỗ Mở Vách;
- TườngKT + Cửa_đi;
- MóngCọc / Đài_Cọc / Dầm_Móng / Móng_Băng / Móng_Bè / Bê_Tông_Lót.

### 5.5 [ĐỀ XUẤT] Cách dùng BLT benchmark đúng

Nên học **trình tự thao tác và vocabulary nghiệp vụ**:

```text
Select/Create
→ Calculate
→ Review
→ Locate/Highlight
→ Recalculate
→ Report/Excel
```

Không nên clone pixel-perfect UI hoặc phụ thuộc vào binary/package cũ. Exact pixel parity còn phụ thuộc theme/DPI/host runtime và không tạo thêm giá trị nếu workflow QS đã nhanh, rõ, traceable.

---

## 6. Current-system inventory được xác nhận trong session

### 6.1 [ĐÃ XÁC NHẬN] Semantic/project foundation

Source review trong session xác nhận QS3D đã có nền tảng domain cho:

- Project / Zone / Floor / Level;
- Family / Type / semantic Element;
- Element ID / CAD Handle / Drawing Fingerprint provenance;
- source/generated CAD handle ownership;
- dirty/regeneration/persistence lifecycle;
- project browser/workspace/health/release-readiness surfaces.

### 6.2 [ĐÃ XÁC NHẬN] Authoring/geometry capabilities

Các capability đã tồn tại ở mức source gồm nhiều nhóm như:

- semantic capture architecture/structure/rooms/openings;
- direct draw wall/beam/slab/column và extended families;
- plan-to-3D/native solid workflows;
- door/opening physical cuts;
- room/finish;
- curtain wall;
- generated geometry ownership/invalidation;
- một số native/source reconciliation paths.

### 6.3 [ĐÃ XÁC NHẬN] Quantity/review capabilities

Repo đã có:

- quantity/BQ review;
- filter/group/recalculate;
- locate/reveal/highlight/focus/isolate/section review;
- measurement/evidence semantics;
- schedule/reporting paths;
- Quick Takeoff/B4D recognition;
- Schedule Hub và nhiều domain smoke/preflight.

### 6.4 [ĐÃ XÁC NHẬN] Rebar/BBS và related domain

Source có các lane 3D rebar/BBS cho nhiều category, layout/stirrup/tie/shape/catalog/fabrication-related logic. Session không coi đây là feature gap số 1; ưu tiên vẫn là Excel round-trip, quantity/formwork parity, editability và end-user workflow.

### 6.5 [ĐÃ XÁC NHẬN] Existing Excel capabilities trước customer workbook lane

Các command/source path đã được review:

- `QS3DBQ` — quantity summary/filter/group/Locate/XLSX;
- `QS3DSETUPBLT` — BLT3D compatibility quantity settings preset;
- `QS3DED2` — scoped regeneration/export cho `CHI_TIET` + `TONG_HOP` với provenance;
- `QS3DEXCELLOCATE` — locate từ ED2/QS3D workbook theo Element ID + Handle + Drawing Fingerprint, với legacy path được kiểm soát.

Core classes quan trọng đã tồn tại:

- `XlsxQuantityExporter`;
- `XlsxHandleReader`;
- BricsCAD `ExcelLocateResolutionService`.

Điểm mấu chốt: **Excel→model foundation đã có trước khi session này bắt đầu lane customer workbook**; không cần tạo quantity engine thứ hai.

---

## 7. Excel round-trip — phân tích requirement và kết quả đã land

### 7.1 [ĐÃ XÁC NHẬN] Requirement customer-facing

User muốn một nút **Xuất Excel** trực tiếp trên QS3D menu/Ribbon và workflow **Excel → CAD** dễ dùng. Workbook hướng nghiệp vụ phải có dữ liệu kiểu:

```text
STT | Cấu kiện | Loại | Tầng | Dài | Rộng | Cao | BT m³ | VK m²
```

Nhưng để trace an toàn, cần giữ provenance kỹ thuật không phụ thuộc text hiển thị.

### 7.2 [ĐỀ XUẤT ban đầu] Provenance contract

Trong phân tích ban đầu, hướng đề xuất là giữ các field kỹ thuật như:

- `ElementId`;
- `CadHandle`;
- `DrawingFingerprint`;
- project/export revision khi cần;
- sheet/row identity hoặc deterministic trace key.

Mục tiêu là:

- không locate nhầm DWG;
- không chọn partial stale handle set;
- không dựa vào tên cấu kiện dễ đổi;
- aggregate row có thể map về nhiều element;
- detail row map về đúng một element.

### 7.3 [ĐÃ XÁC NHẬN] Lane #3296 / PR #3299 đã land

Trong chính session này, lane **#3296** đã hoàn thành source-safe và PR **#3299** đã merge.

Final session evidence của lane:

- Issue: `#3296` — `[BIM3D-QS][P0][Excel] customer workbook + aggregate/detail reverse locate`;
- canonical branch: `agent/chatgpt-gpt56sol/customer-excel-trace-3296`;
- final task head: `9053af77d7dc3b6d6d76dd4bc6eeb46ee18439e4`;
- protected CI: run `32434789970`, `preflight SUCCESS + core SUCCESS`;
- PR: `#3299`;
- landed main commit: `99dc024faafa4becc1a89fa61a894f69fba8aa49`.

### 7.4 [ĐÃ XÁC NHẬN] Customer workbook contract đã land

Workbook customer-facing có bốn sheet:

- `DGKL` — grouped quantity;
- `COP_PHA` — grouped formwork-oriented output;
- `CHI_TIET` — one semantic element per row;
- `TRACE_MODEL` — explicit row→model provenance map.

Landed implementation gồm:

- `QsCustomerWorkbookExporter`;
- `QsCustomerWorkbookTraceReader`;
- `QS3DEXCEL`;
- `QS3DEXCELTRACE`;
- multi-element aggregate reverse trace;
- complete live Handle resolution trước khi thay PICKFIRST;
- Drawing Fingerprint + Element ID + Handle revalidation;
- deterministic smoke và focused source preflight;
- visible quantity Ribbon `QS3D`, `Xuất Excel`, `Excel → CAD` trong khi giữ internal identity `QS3D_QTY`.

### 7.5 [ĐÃ XÁC NHẬN] Evidence semantics

Session nhấn mạnh rule:

- metric **unsupported** phải để blank;
- measured value bằng **0 thật** vẫn là numeric zero;
- không fabricate `0` chỉ để Excel đẹp.

Đây là requirement quan trọng vì QS cần biết “không có khối lượng” khác với “chưa có evidence để tính”.

### 7.6 [RỦI RO] Source-safe ≠ interactive qualification

Mặc dù #3299 land với protected CI xanh, interactive BricsCAD V25 UI/NETLOAD/selection/zoom/save-reopen qualification vẫn phải lấy từ local exact-SHA evidence theo lane/runtime policy. Không được dùng CI xanh để tuyên bố customer runtime acceptance hoàn tất.

---

## 8. Formwork / coppha — requirement và khoảng trống nghiệp vụ

### 8.1 [ĐÃ XÁC NHẬN] User yêu cầu quantity ván khuôn phải trừ đúng mặt tiếp xúc

Trong session, user hỏi cụ thể cách chỉnh rule để **trừ diện tích mặt tiếp xúc bê tông lân cận**, đặc biệt cho cấu kiện vách và các giao cắt.

Business requirement rút ra:

- coppha phải tính **mặt thực sự cần ván khuôn**;
- mặt áp vào bê tông lân cận/host/neighbor không được double-count;
- opening/intersection phải tạo/loại surface đúng theo exposed-contact semantics;
- result phải giải thích được vì sao một mặt được tính hoặc bị trừ.

### 8.2 [ĐÃ XÁC NHẬN] Wall opening / reveal / soffit concern

User cung cấp screenshot và hỏi vì sao sau khi vẽ vách có lỗ mở, highlight ván khuôn **không có ván khuôn đáy ở phía trên lỗ mở** tại vùng được đánh dấu.

Requirement nghiệp vụ nên hiểu là:

- opening trong wall/vách không chỉ trừ volume/face lớn;
- opening tạo các **reveal surfaces** quanh biên;
- cạnh trên của opening tạo một **soffit/bottom formwork surface** của phần bê tông phía trên lỗ nếu mặt đó thực sự exposed và cần coppha;
- các mặt đứng hai bên opening và các mặt còn lại phải được classification theo exposure/contact;
- nếu opening sát slab/beam/neighbor thì deduction/contact rule phải tránh double-count.

### 8.3 [CHƯA RÕ] Exact legacy BLT command cho “Lỗ Mở Vách”

Screenshot cho thấy category `Vách → Lỗ Mở Vách` và `TườngKT → Cửa_đi`, nhưng static screenshot/binary evidence không đủ chứng minh exact command name, internal execution path hoặc rule precedence của BLT.

Do đó QS3D nên triển khai theo requirement nghiệp vụ rõ ràng, không cố đoán/copy command internals của legacy tool.

### 8.4 [ĐỀ XUẤT] Dedicated formwork subsystem

Một subsystem đủ mạnh nên có:

1. **surface generation/classification** từ host geometry;
2. **contact detection** với neighbor/host/slab/beam/wall/foundation;
3. **opening reveal generation**;
4. **deduction rules** theo contact/intersection;
5. **ownership rules** như đáy dầm tính vào dầm hay sàn;
6. **thresholds** diện tích/thể tích;
7. **top-face angle rule** nếu business yêu cầu;
8. **explain quantity**: surface nào tính, surface nào trừ, lý do và công thức;
9. deterministic Core tests + real BricsCAD/local DWG acceptance.

### 8.5 [ĐỀ XUẤT] Formwork acceptance matrix tối thiểu

Các family cần test độc lập:

- cột;
- dầm;
- sàn;
- vách/tường;
- móng;
- opening trong wall/slab;
- beam-wall, beam-column, wall-slab, foundation-soil/blinding/contact boundaries;
- repeated/intersecting openings;
- neighbor touching/overlap edge cases.

Output test nên so sánh **surface-level decomposition**, không chỉ một tổng `VK m²`, để debug được sai rule.

---

## 9. Direct Draw / editing / repeated authoring

### 9.1 [ĐÃ XÁC NHẬN] User mong workflow vẽ nhanh

BLT-like value không chỉ nằm ở Ribbon mà ở thao tác liên tục: chọn family/category rồi vẽ nhiều cấu kiện nhanh, có preview và ít phải đóng/mở dialog.

### 9.2 [ĐỀ XUẤT] Direct Draw V2

Các capability nên tiếp tục hoàn thiện:

- transient preview/jig;
- repeated authoring;
- click-to-place theo active floor/level/family;
- cancel/undo rõ ràng;
- create semantic + native geometry trong một authority transaction;
- editing invalidates generated quantity/rebar/formwork đúng phạm vi;
- native grip/STRETCH/modify reconciliation an toàn.

### 9.3 [ĐỀ XUẤT] Native modify/property panel

Một product gap quan trọng được nhắc lại là **native modify/edit**: sửa kích thước/thuộc tính cấu kiện phải cập nhật semantic model, quantity và dependent generated outputs thay vì để native geometry và semantic authority lệch nhau.

---

## 10. UI/UX parity với BLT — nên hiểu thế nào

### 10.1 [ĐÃ XÁC NHẬN] Ribbon-first

User muốn QS3D hiển thị như một công cụ làm việc trực tiếp trong BricsCAD, với menu `QS3D` và action rõ ràng, đặc biệt `Xuất Excel`.

### 10.2 [ĐỀ XUẤT] Navigation model

Nên tổ chức theo mental model của kỹ sư QS:

- active Floor/Level;
- category/family;
- Create/Recognize;
- Quantity;
- Review/Locate;
- Formwork;
- Excel/Report;
- Health/Settings.

### 10.3 [RỦI RO] Không theo đuổi pixel clone

Pixel clone BLT sẽ tạo chi phí bảo trì và phụ thuộc theme/DPI/BricsCAD version. Goal nên là:

- command discoverability;
- số click thấp;
- consistent selection/highlight;
- familiar category naming;
- persistent active floor/family context;
- predictable calculation/review/report loop.

---

## 11. Clash / duplicate / coordination

### 11.1 [ĐÃ XÁC NHẬN] User yêu cầu giao cắt và trùng nhau

Goal ban đầu có clash/intersection/overlap/highlight. Source đã có broad/exact clash-related foundations và model review pieces, nhưng session trước phân loại vẫn còn gap ở mức **persistent issue manager / unified workflow**.

### 11.2 [ĐỀ XUẤT] Unified coordination object

Nếu tiếp tục lane này, một `CoordinationIssue`/equivalent nên lưu:

- issue type: clash / duplicate / missing quantity / provenance drift;
- involved Element IDs + Handles/fingerprint;
- severity/status;
- rule/source;
- model location/view focus;
- resolution/waiver metadata.

Không nên có một duplicate engine authority riêng tách khỏi semantic provenance.

---

## 12. Báo cáo/Excel ngoài customer workbook hiện tại

### 12.1 [ĐỀ XUẤT] Template fidelity

User mong “xuất đúng mẫu Excel”. Sau khi customer workbook/trace foundation đã land, bước nâng cấp có giá trị là template engine:

- giữ formatting/border/merge/formula;
- mapping field theo template customer;
- hidden technical provenance khi cần;
- không phá user-editable presentation cells;
- version/revision metadata;
- deterministic re-export behavior.

### 12.2 [ĐỀ XUẤT] Report family

Các report nên converged về cùng canonical quantity data:

- detail;
- summary;
- floor;
- zone;
- family/category;
- material;
- concrete;
- formwork;
- rebar/BBS;
- Excel trước, PDF/Word nếu business cần.

Không tạo calculator/report authority thứ hai chỉ để phục vụ một format.

---

## 13. Interoperability roadmap

### 13.1 [ĐỀ XUẤT] Thứ tự ưu tiên

Session thống nhất về mặt chiến lược rằng interoperability nên đi theo:

1. IFC / vendor-neutral exchange trước;
2. Revit bridge sau;
3. Tekla/broader vendor workflows sau nữa.

### 13.2 [RỦI RO] Không dùng binary legacy làm shortcut

Không copy/ship `model_from_cad.dll`, `BLT_Fuzor.dll` hoặc dependency legacy để “có nhanh Revit/Fuzor”. QS3D cần contract clean-room, legal/licensed adapter riêng và test provenance rõ ràng.

---

## 14. NETLOAD/logging responsiveness concern

### 14.1 [ĐÃ XÁC NHẬN] User đã báo log bị lag/noisy khi NETLOAD

Đây là một requirement riêng trong session: startup/NETLOAD không nên bị kéo chậm bởi logging/reconcile/palette refresh hoặc spam lỗi lặp lại.

### 14.2 [ĐỀ XUẤT] Acceptance đúng

- không deserialize/reconcile lặp lại cùng corrupt/stable sidecar generation;
- automatic lifecycle errors nên fail closed nhưng không spam command line;
- deferred reconcile phải chạy theo host-idle scheduling hợp lý;
- explicit user commands vẫn được phép báo lỗi chi tiết;
- runtime responsiveness phải được chứng minh local, không suy từ static source alone.

Tài liệu này không tuyên bố current active NETLOAD lane đã hoàn tất; live Issue/PR/current main phải được kiểm tra khi tiếp tục.

---

## 15. GitHub/CI milestones đã xảy ra trong session

Đây là **historical session evidence**, không phải current queue.

### 15.1 [ĐÃ XÁC NHẬN] Integration batch #3294 / PR #3295

Một owner-authorized integration batch đã được assemble và merge qua protected PR:

- batch issue: #3294;
- PR: #3295 `integration: land reconciled open-PR batch to main`;
- final integration head: `122cc799a87555e7b4117cba8f90e9c297d4d809`;
- merge commit: `db7cc6f15a828d166731cee8011dd5289e948422`.

Batch này minh họa một số nguyên tắc quan trọng:

- fix stale smoke fixtures khi stricter canonicality contract land;
- không dùng polluted carrier nếu exact intended scope phải được phục hồi cẩn thận;
- held task #2842/#2857 ở snapshot đó được loại khỏi batch theo `STOP BEFORE MERGE`;
- exact-current protected checks là merge gate.

### 15.2 [ĐÃ XÁC NHẬN] Customer Excel lane #3296 / PR #3299

Như mục 7 đã ghi, lane này đã đạt `MERGED_MAIN / SOURCE_SAFE` với protected `preflight + core` success và landed commit `99dc024faafa4becc1a89fa61a894f69fba8aa49`.

CI remediation trong lane này cũng chứng minh repository rule: nullable analysis, workbook parser strictness, CAD Handle edge case, Excel cell-length bound và smoke fixture issues đều phải sửa trên cùng canonical carrier, không report rồi bỏ dở.

### 15.3 [ĐÃ XÁC NHẬN] Current-main snapshot khi tạo tài liệu

Khi lane #3408 được đăng ký, `main` được refresh ở:

`e207a05d77d8619094f72e63064866da0b596506`

với recent merge #3396. Repo có nhiều agent hoạt động song song, do đó tài liệu này cố ý không biến danh sách PR/Issue tại thời điểm viết thành một “to-do list” cố định.

---

## 16. Governance và cách làm việc rút ra trong session

### 16.1 [ĐÃ XÁC NHẬN] `main` là PR-only cho normal task

Không có docs-only exception. Các câu như:

- `commit push git`;
- `update docs`;
- `update md`;
- `continue all`;
- `merge main`;

không cấp quyền direct contents write/ref update/force-push vào `main`.

Đường đúng:

```text
latest main
→ Issue/Lane-Key
→ one canonical agent branch
→ commit/push
→ branch CI
→ PR
→ protected current-candidate preflight + core
→ freshness/mergeable/expected-head
→ protected PR merge
→ refresh landed main SHA
```

### 16.2 [ĐÃ XÁC NHẬN] One Lane-Key / one carrier

Mỗi task concrete chỉ có tối đa:

- one ACTIVE owner/session;
- one canonical branch;
- one open canonical PR.

Stale/red/behind/draft/slow không làm lane tự do. Không được tạo PR/branch replacement chỉ vì CI timing bất tiện.

### 16.3 [ĐÃ XÁC NHẬN] Red CI là action trigger

Nếu current owned carrier đỏ và lỗi source-safe/fixable:

```text
observe exact failing SHA/job/step
→ root cause
→ fix same branch
→ regression/guard nếu cần
→ commit/push
→ observe new exact candidate
→ repeat until green
```

Không giao owner việc “hãy check CI hộ”, không reuse stale green evidence, không no-op commit để đánh lừa gate, không manual rerun/dispatch nếu policy không cho phép.

### 16.4 [ĐÃ XÁC NHẬN] Protected merge gate

Merge candidate cần:

- canonical Lane-Key/carrier;
- no duplicate active carrier;
- current/fresh candidate;
- `preflight SUCCESS`;
- `core SUCCESS`;
- mergeable;
- expected-head SHA đúng;
- không có unresolved blocker invalidating candidate.

### 16.5 [ĐÃ XÁC NHẬN] LOCAL_ONLY boundary

Remote agent không được manufacture `LOCAL_PASS` cho:

- licensed BricsCAD NETLOAD/DemandLoad;
- native Solid3d/Boolean/DrawJig/editor runtime;
- real Windows Ribbon/Palette/WPF/HiDPI;
- private/customer DWG;
- save/reopen/multi-DWG runtime behavior;
- clean-machine installer/update;
- Authenticode/signing secret;
- real performance profiling.

Remote phải hoàn thiện source-safe implementation/test/guard/handoff trước, commit/push exact candidate rồi mới park phần runtime vào `docs/LOCAL-AGENT-INBOX.md` nếu task đó cần.

### 16.6 [ĐÃ XÁC NHẬN] External ChatGPT scheduler không phải repo ownership

Các nhãn scheduler kiểu worker/controller/C0/W1-W4 là orchestration ngoài repo. Chúng không tự tạo Lane-Key, merge authority hay branch ownership. Mỗi invocation vẫn phải resolve current GitHub state.

---

## 17. Những lỗi cách làm việc đã được sửa trong chính session

### 17.1 [ĐÃ XÁC NHẬN] Không dừng ở report khi user yêu cầu fix

Nhiều lượt user nhấn mạnh “fix đi chứ không phải report”. Repository policy sau đó cũng làm rõ terminal-first/action-first contract: khi còn safe authorized action, agent phải tiếp tục lifecycle thay vì đưa status dump như output cuối.

### 17.2 [ĐÃ XÁC NHẬN] Không claim merge khi chưa verify

Session có những thời điểm chỉ mới review repo hoặc CI đang running. Kết luận đúng là không được nói “xong/merged” cho đến khi GitHub xác nhận protected PR merged và refreshed `main` chứa work.

### 17.3 [ĐÃ XÁC NHẬN] Không tự nhập nhằng source-safe và runtime-safe

Các feature có BricsCAD build PASS/CI PASS vẫn phải giữ label `PENDING_LOCAL` nếu acceptance cần UI/native/runtime evidence thật.

---

## 18. Gap analysis tổng hợp

### 18.1 Existing / mạnh

- semantic project model + identity/provenance;
- many authoring/capture workflows;
- quantity/reporting foundation;
- locate/highlight/review;
- ED2 + customer Excel trace foundation;
- rebar/BBS substantial coverage;
- persistence/health/guard infrastructure;
- V25/V26 product boundary rõ;
- strong CI/source-guard discipline.

### 18.2 Partial / cần hoàn thiện

- BLT-familiar repeated authoring/preview;
- native edit/reconcile ergonomics;
- formwork contact/intersection/opening reveal correctness;
- template-level customer Excel fidelity;
- unified clash/duplicate issue workflow;
- large-model incremental/live update UX;
- exact runtime qualification của customer golden path;
- some cross-host/interchange workflows.

### 18.3 Missing / chưa nên overclaim

- full pixel/behavior parity với BLT3D;
- full customer/private-DWG qualification mọi feature;
- universal Revit/Tekla parity;
- standalone QS3D CAD engine trong repo này;
- automatic proof rằng mọi source feature đã production-qualified trên cả V25/V26.

---

## 19. Priority roadmap đề xuất sau session

### P0 — trực tiếp ảnh hưởng QS golden path

1. **Formwork correctness + explainability**
   - surface-level rules;
   - wall openings/reveals/soffits;
   - contact deduction;
   - family matrix;
   - deterministic + local acceptance.

2. **Direct Draw / Modify V2**
   - transient preview;
   - repeated authoring;
   - native modify reconciliation;
   - undo/failure rollback;
   - dependent invalidation.

3. **Excel template fidelity**
   - customer templates;
   - formatting/formula preservation;
   - hidden provenance;
   - stable re-export.

4. **Exact-SHA customer runtime acceptance**
   - `QS3DEXCEL`;
   - `QS3DEXCELTRACE`;
   - selection/zoom;
   - save/reopen;
   - multi-DWG;
   - wrong-DWG/stale-handle fail-closed.

### P1 — mở rộng productivity

- visual rebar shape/catalog/shopdrawing UX;
- report suite;
- clash/duplicate persistent review issues;
- earthwork;
- grid/level ergonomics;
- performance/incremental update.

### P2 — interoperability/product-family expansion

- IFC maturity;
- Revit bridge;
- Tekla/other integrations;
- deliberate migration slices to `QS3D-Platform`;
- sibling `QS3D-CAD` standalone concerns ngoài repo này.

---

## 20. Acceptance criteria đề xuất cho “BLT-like QS golden path”

Một release slice chỉ nên được gọi là gần đạt mục tiêu nếu một kỹ sư QS có thể chạy end-to-end:

1. mở/create project và chọn Floor/Level;
2. vẽ hoặc capture Beam/Slab/Column/Wall/Foundation/Opening;
3. sửa kích thước/source geometry mà semantic state vẫn reconcile đúng;
4. tính BT/VK/Rebar không double-count contact;
5. mở quantity explanation cho selected object;
6. highlight/isolate/zoom chính xác;
7. export customer workbook;
8. chọn `CHI_TIET` và aggregate `DGKL/COP_PHA` row để locate đúng object set;
9. wrong-DWG/stale provenance phải fail closed;
10. save/cold reopen không mất identity/provenance;
11. Undo/cancel/failure không để partial semantic/native mutation;
12. all source-safe checks green;
13. các bước native/customer được chạy trên exact licensed BricsCAD SHA nếu release acceptance yêu cầu.

---

## 21. Appendix A — customer Excel trace contract đã land

### Business sheets

```text
DGKL
COP_PHA
CHI_TIET
TRACE_MODEL
```

### Identity principles

```text
visible business row
  -> deterministic TRACE_KEY
  -> exactly matching TRACE_MODEL entry
  -> QS3D Element ID(s)
  -> canonical CAD Handle(s)
  -> Drawing Fingerprint
  -> current project revalidation
  -> complete live handle resolution
  -> only then replace PICKFIRST + zoom
```

### Fail-closed cases

- unsupported sheet/workbook state;
- malformed/duplicate critical header;
- formula-backed identity cell where literal identity is required;
- malformed Handle;
- TRACE_KEY mismatch;
- wrong Drawing Fingerprint;
- missing/stale Element ID;
- provenance Handle set drift;
- partial native handle resolution.

---

## 22. Appendix B — commands/workflows đáng nhớ từ session

Existing/compatibility paths reviewed:

```text
QS3DBQ
QS3DSETUPBLT
QS3DED2
QS3DEXCELLOCATE
```

Customer Excel lane landed:

```text
QS3DEXCEL
QS3DEXCELTRACE
```

UI wording landed in quantity Ribbon lane:

```text
QS3D
Xuất Excel
Excel → CAD
```

Không dùng danh sách này như complete command reference; `docs/COMMANDS.md` và current source là canonical reference.

---

## 23. Appendix C — câu hỏi formwork cần giữ cho future implementation

Khi xây formwork rule, future agent phải trả lời được bằng code/test, không chỉ bằng tổng số:

- Mặt này exposed hay contact với bê tông lân cận?
- Nếu contact, owner của diện tích là cấu kiện nào?
- Opening tạo bao nhiêu reveal faces?
- Soffit phía trên opening có được tính không, và vì sao?
- Beam bottom có tính riêng hay nhập vào slab bottom?
- Top face có tính theo góc/setting không?
- threshold area/volume có làm mất tiny surface không?
- intersection deduction có deterministic theo order không?
- repeated recalculate có idempotent không?
- edit/resize opening có invalidate/rebuild đúng dependent formwork không?
- explain UI có thể chỉ ra từng surface và deduction source không?

---

## 24. Appendix D — “do not infer” checklist cho session sau

Future session **không được suy ra** các điều sau chỉ từ tài liệu này:

- rằng current `main` vẫn là SHA ghi ở đầu file;
- rằng Issue/PR từng open/held vẫn còn open/held;
- rằng source-safe PASS là licensed runtime PASS;
- rằng BLT metadata nói lên thuật toán nội bộ;
- rằng exact legacy BLT command name đã được reverse-engineer;
- rằng `QS3D-BricsCAD` phải thành standalone EXE;
- rằng scheduler worker label cho phép takeover GitHub lane;
- rằng docs-only cho phép direct write `main`;
- rằng green CI của SHA cũ cho phép merge SHA mới;
- rằng một quantity/report view được phép tạo authority engine thứ hai.

---

## 25. Handoff ngắn cho session mới

Nếu cần tiếp tục nhanh, hãy đọc theo thứ tự:

1. current `main` SHA;
2. `AGENTS.md`;
3. `docs/AGENT-RUNTIME-CONTRACT.md`;
4. `docs/MAIN-WRITE-AUTHORIZATION.md`;
5. `docs/PRODUCT-BOUNDARY.md`;
6. `CI_POLICY.md`;
7. current Issues/PRs cho đúng lane;
8. source hiện tại;
9. tài liệu này chỉ để lấy lịch sử requirement/rationale.

Sau đó giữ mục tiêu sản phẩm:

> **BricsCAD-hosted QS3D plugin, clean-room BLT-familiar workflow, one semantic/provenance authority, correct/explainable quantity, robust Model↔Excel trace, and real runtime evidence only when actually executed.**

---

## 26. Kết luận

Session đã chuyển dự án từ một yêu cầu rộng “giống BLT3D, có 3D/QS/Excel/trace” sang một tập requirement và boundary rõ hơn. Thành tựu source-safe nổi bật nhất của session là **customer Excel workbook + aggregate/detail reverse CAD trace đã land qua #3296/#3299**. Khoảng trống có giá trị nghiệp vụ cao nhất còn lại là **formwork correctness/explainability, opening reveal/soffit behavior, direct edit/repeated authoring ergonomics, customer template fidelity và exact licensed runtime qualification của golden path**.

Bất kỳ implementation tiếp theo nào cũng nên tiếp tục từ current source, reuse canonical quantity/provenance authority, không clone proprietary BLT implementation, không tạo duplicate carrier, và không dừng ở report khi còn bug/CI remediation an toàn có thể thực hiện trên lane hiện tại.
