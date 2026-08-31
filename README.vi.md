# QS3D cho BricsCAD V25 + V26

[English](README.md) | [Tiếng Việt](README.vi.md) | [Русский](README.ru.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

QS3D là plugin **BIM, mô hình 3D ngữ nghĩa, phối hợp và bóc tách khối lượng cho BricsCAD V25 và V26 x64**, được phát triển theo hướng clean-room. QS3D chạy bên trong BricsCAD dưới dạng managed plugin; đây không phải là một phần mềm CAD độc lập.

> **Mốc review — 31/08/2026:** README này được cập nhật dựa trên baseline `main` `74a6aee92fc7066857e429b37fa2ff80e045ed9e`. Repo đang được phát triển đồng thời với tốc độ cao, vì vậy khi cần kết luận về release hãy đối chiếu `main` hiện tại, [`docs/README.md`](docs/README.md), [`docs/COMMANDS.md`](docs/COMMANDS.md) và bằng chứng CI/runtime gắn với đúng SHA.

> **Họ sản phẩm QS3D:** repo này là sản phẩm QS3D chạy trong BricsCAD. Mã dùng chung không phụ thuộc nhà cung cấp CAD được phát triển ở repo anh em `trinhtanphat/QS3D-Platform`; sản phẩm desktop độc lập là `trinhtanphat/QS3D-CAD`. Xem [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) và [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md).

## Thành phần chính của repo

| Lớp | Target | Vai trò |
| --- | --- | --- |
| `QS3D.Core` | `netstandard2.0` | Domain model độc lập CAD, persistence, hình học/khối lượng, diagnostics, reporting và application services |
| `QS3D.BricsCAD.V25` | .NET Framework 4.8 / x64 | Host adapter BricsCAD V25, command, WPF UI và tích hợp CAD |
| `QS3D.BricsCAD.V26` | `net8.0-windows` / x64 | Host build BricsCAD V26 với biên host/update riêng, tái sử dụng phần source tương thích |
| `external/QS3D-Platform` | submodule được pin | Contract và platform code dùng chung, không phụ thuộc nhà cung cấp |
| `tests/` | nhiều test project/executable | Core regression, architecture, host/runtime harness và các contract test tập trung |
| `scripts/` + `.github/workflows/` | Python/PowerShell/YAML | Preflight, đóng gói, cài đặt/update, CI, release và runtime-proof |

Muốn build/đánh giá runtime host phải có bản BricsCAD tương ứng và license hợp lệ. Repo chủ động không commit các binary SDK BricsCAD độc quyền, bản vẽ khách hàng, dữ liệu dự án riêng tư hoặc source của sản phẩm bên thứ ba.

## Bản đồ công năng

Repo đã vượt xa mức prototype, nhưng mức trưởng thành của từng chức năng không giống nhau. Muốn biết chính xác maturity của từng command, hãy dùng [`docs/COMMANDS.md`](docs/COMMANDS.md) thay vì coi README này là danh sách chứng nhận.

### BIM ngữ nghĩa và mô hình dự án

- Project, Zone, Floor/Level, Family/Type và trạng thái Element ngữ nghĩa.
- Vòng đời project gắn với bản vẽ, ownership của CAD handle nguồn/generated và metadata dự án.
- Dependency, dirty/freshness, regeneration, persistence và recovery contract.
- Đồng bộ Project Browser / Workspace / Project Tools.
- Model Health, preflight và các bề mặt kiểm tra release readiness.

### Kết cấu và sinh 3D

- Direct Draw và workflow ngữ nghĩa cho cột, dầm, sàn, tường, lỗ mở và các family kiến trúc/kết cấu liên quan.
- Workflow móng, bao gồm phần source/proof hardening hiện tại cho móng đơn.
- Plan-to-3D và sinh native `Solid3d` có kiểm soát ownership/rollback.
- Rebar 3D cho dầm, cột, sàn, tường kết cấu và móng.
- Steel detailing, weld/BOM và các bề mặt CSV/reporting kết cấu.

### Khối lượng, schedule và hồ sơ đầu ra

- Review Quantity/BQ, filter, tính lại, locate/reveal và model-evidence.
- Quick Takeoff và các luồng nhận dạng/review có hỗ trợ.
- Schedule Hub cùng schedule theo domain cho khối lượng, hoàn thiện, vật liệu, cửa/lỗ mở, curtain và cốt thép/BBS.
- Xuất XLSX/CSV kèm provenance tới element/source ở các workflow hỗ trợ.
- Cost, reporting, design report và project-information surfaces.

### MEP và Coordination

- Authoring thiết bị điện/đèn/dây, tag, template, schema/readiness và host-export.
- Coordination/clash, zone, dashboard và lưu trạng thái issue.
- BCF import/export và các bề mặt trao đổi external clash.
- Kiến trúc rộng hơn có code cho HTTP CAD worker, PostgreSQL/Supabase/RLS, RabbitMQ và object storage; việc có source không đồng nghĩa dịch vụ ngoài đang được cấu hình/chạy thật.

### BIM interchange, planning và review

- IFC và JSON import/export; maturity được ghi theo từng command trong `docs/COMMANDS.md`.
- Task link, task list/export, 4D, animation và planning/reporting.
- Ribbon, Workspace palette, Project Tools, Domain/Schedule/Rebar hubs và modeless WPF.
- Highlight/focus/isolate/section-style review và các guard bảo đảm đúng drawing affinity.

### Web/integration thử nghiệm

Repo còn có các surface kiểm thử web/integration như health/settings/project/document/quantity/cost API và viewer/bridge validation. Đây là lớp tích hợp xung quanh họ sản phẩm QS3D; **không** biến repo plugin BricsCAD này thành một CAD độc lập thay thế BricsCAD.

## Mô hình bằng chứng và qualification

Nguyên tắc quan trọng nhất:

> **Có code trong source không đồng nghĩa đã production-qualified trên BricsCAD có license.**

Cần tách rõ các lớp bằng chứng:

| Bằng chứng | Có thể chứng minh | Không chứng minh được |
| --- | --- | --- |
| Static/source preflight | cấu trúc source, policy, security/package contract, regression xác định | hành vi runtime native trong BricsCAD |
| Core test xác định | domain, persistence, geometry, quantity, dependency, interchange không phụ thuộc CAD | `NETLOAD`, WPF/Ribbon hoặc native CAD API |
| Host build | khả năng compile với SDK/major BricsCAD đã chọn | plugin chạy thành công trong host có license |
| Licensed host proof | hành vi runtime của đúng SHA, đúng host major và đúng scenario đã chạy | host major khác, bản vẽ khác hoặc môi trường chưa test |

Lịch sử dự án hiện có lượng bằng chứng source/preflight/Core/build lớn, nhưng một số lane licensed-host chính xác vẫn có thể `BLOCKED` vì license, COM, UI hoặc môi trường máy. Không được đổi các cell bị chặn đó thành runtime PASS.

Xem [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md), [`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md), runbook runtime và artifact gắn đúng SHA khi đánh giá sản phẩm.

## Kiến trúc và mô hình dùng chung source

```text
src/
  QS3D.Core/                 domain/application logic độc lập CAD
  QS3D.BricsCAD.V25/         host BricsCAD + WPF V25 net48/x64
  QS3D.BricsCAD.V26/         host V26 net8.0-windows/x64

external/QS3D-Platform/      submodule platform dùng chung đã pin
tests/                       test xác định và test hướng host
scripts/                     preflight, build, package, install/update, proof
docs/                        kiến trúc, workflow, policy, qualification
.github/workflows/            validation tự động và release/runtime có kiểm soát
```

V25 là adapter .NET Framework đã được thiết lập lâu hơn. V26 là host build .NET 8 thật, không phải đổi tên binary V25. Project V26 tái sử dụng phần application/UI source V25 tương thích nhưng giữ riêng các biên entry/update đặc thù host. Vì vậy **bằng chứng V25 không tự động là bằng chứng V26**, và ngược lại.

`QS3D.Core` cần duy trì tính độc lập CAD. Logic trung lập nhà cung cấp mới nên ưu tiên Core/Platform thay vì kéo dependency BricsCAD proprietary API vào domain layer.

## Persistence và nguồn dữ liệu chuẩn

Sidecar `.qsdb` được coi là dữ liệu sản phẩm, không phải cache có thể bỏ. Codebase có bounded input handling, kiểm tra identity/reference, validation trước khi save, atomic publication, backup/recovery, locking/revision và contract dirty/freshness.

Mô hình source-of-truth thực tế kết hợp **hình học nguồn trong DWG** với **metadata ngữ nghĩa/project trong `.qsdb`**. Xem [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md).

## Bắt đầu nhanh cho contributor

### 1. Clone kèm submodule đã pin

```bash
git clone --recurse-submodules https://github.com/trinhtanphat/QS3D-BricsCAD.git
cd QS3D-BricsCAD
```

Nếu đã clone mà chưa lấy submodule:

```bash
git submodule sync --recursive
git submodule update --init --recursive
```

Trước khi sửa đáng kể, đọc:

- [`AGENTS.md`](AGENTS.md)
- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md)
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md)
- [`CI_POLICY.md`](CI_POLICY.md)

### 2. Chạy preflight repo

```bash
python scripts/preflight.py
python scripts/preflight-all.py
```

### 3. Build và chạy Core smoke test không phụ thuộc CAD

```bash
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Các lệnh trên không cần binary SDK BricsCAD.

### 4. Build host adapter

Không commit `BrxMgd.dll`, `TD_Mgd.dll` hoặc binary BricsCAD độc quyền khác.

Ví dụ V25:

```powershell
$env:BRICSCAD_V25_DIR = '<thư mục cài BricsCAD V25>'
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64
```

Ví dụ V26:

```powershell
$env:BRICSCAD_V26_DIR = '<thư mục cài BricsCAD V26>'
dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release -p:Platform=x64
```

Không trỏ project của host major này sang assembly SDK của host major khác.

## Cài đặt và load plugin

Người dùng cuối nên ưu tiên release bundle cùng hướng dẫn installer/checksum đi kèm thay vì tự copy build output bất kỳ. Xem trang **Releases** và tài liệu release theo từng host major.

Với package V25 tải từ trình duyệt, Windows Mark-of-the-Web có thể chặn managed dependency trước khi code startup QS3D chạy. Ưu tiên `INSTALL-QS3D.cmd` trong package đã giải nén. Khi cần troubleshoot bằng `NETLOAD` trực tiếp, chỉ dùng `UNBLOCK-QS3D.cmd` đi kèm đúng package sau khi kiểm tra provenance/integrity.

Không hạ BricsCAD trusted-path/security chỉ để né lỗi provenance hoặc package integrity.

## Tra cứu command

QS3D có nhiều command vận hành, authoring, kết cấu, MEP, coordination, quantity, schedule và interchange. Nguồn duy trì chính thức là:

- [`docs/COMMANDS.md`](docs/COMMANDS.md) — tên command, mục đích và maturity;
- [`docs/README.md`](docs/README.md) — điểm vào hệ thống tài liệu;
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — bản đồ kiến trúc.

README không lặp toàn bộ command vì danh sách này thay đổi thường xuyên.

## CI, PR và merge

Mô hình CI hiện tại tự động cho task branch và protected PR:

- push lên `agent/**` và `integration/**` đủ điều kiện chạy `.github/workflows/ci.yml`;
- PR có stable required contexts `preflight` và `core`;
- thay đổi chỉ docs/repository metadata dùng tier nhẹ;
- thay đổi ảnh hưởng source/build dùng source/Core/V25 validation mạnh hơn theo changed-path classifier;
- release/runtime publishing là các lane có kiểm soát riêng.

Green check chỉ chứng minh đúng candidate đã test. Hosted CI không tự sinh bằng chứng licensed-BricsCAD runtime.

Workflow task chuẩn:

```text
Issue / Reservation v2
  -> agent/<globally-distinct-session-token>/issue-<N>-<scope>
  -> implement + validate
  -> canonical PR
  -> required checks fresh
  -> merge đúng task PR khi current + green + mergeable + collision-clean
  -> verify main + đóng/release task state
```

**Không có ngoại lệ ghi trực tiếp `main` cho docs.** Xem [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) và [`CI_POLICY.md`](CI_POLICY.md).

## Các vùng rủi ro chéo cao

Review toàn repo cho thấy các vùng sau cần regression evidence tập trung khi thay đổi:

- Source dùng chung V25/V26 và tương thích framework/runtime.
- Drawing ownership, multi-DWG và vòng đời modeless WPF.
- Native geometry/boolean và ownership đối tượng source/generated.
- Identity `.qsdb`, dirty/freshness, atomic save và recovery.
- Provenance quantity/export và tính toàn vẹn XLSX/CSV.
- Installer/update/package origin và cách ly host major.
- External integrations cùng credential/connectivity phụ thuộc môi trường.

Đây là các constraint thiết kế có ảnh hưởng rộng, không phải tự động là blocker.

## Bản đồ tài liệu

Bắt đầu tại [`docs/README.md`](docs/README.md). Các tài liệu quan trọng:

- [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) — ranh giới sản phẩm/host.
- [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md) — ranh giới Platform/CAD và migration.
- [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md) — quy tắc DWG/semantic source of truth.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — kiến trúc và dependency.
- [`docs/COMMANDS.md`](docs/COMMANDS.md) — catalog command chuẩn.
- [`docs/HEALTH-AND-PREFLIGHT.md`](docs/HEALTH-AND-PREFLIGHT.md) — health/preflight.
- [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md) — qualification runtime V25.
- [`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md) — qualification runtime V26.
- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) — quyền merge protected main.
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md) — Reservation v2/canonical carrier.
- [`CI_POLICY.md`](CI_POLICY.md) — semantics CI hiện hành.

## Release và ranh giới hỗ trợ

Dùng GitHub **Releases** để lấy package candidate và release note gắn với đúng phiên bản. Published package, source build thành công và licensed-runtime qualification là ba lớp bằng chứng khác nhau; hãy đọc release note/artifact proof của đúng host major dự định sử dụng.

Repo không phân phối binary SDK/runtime BricsCAD độc quyền. Người dùng và agent CI/runtime phải tự cung cấp cài đặt BricsCAD cùng license hợp lệ khi cần chạy host.

## License

Xem [`LICENSE`](LICENSE) để biết điều khoản license của repo. Component bên thứ ba và proprietary component vẫn tuân theo license riêng của chúng.
