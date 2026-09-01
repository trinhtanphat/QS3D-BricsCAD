# QS3D for BricsCAD V25 + V26

[English](README.md) | [Tiếng Việt](README.vi.md) | [Русский](README.ru.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

QS3D 是面向 **BricsCAD V25 和 V26 x64** 的 clean-room **BIM、语义化 3D、协同与工程量计算插件**。它以托管插件的形式运行在 BricsCAD 内部，并不是一套独立 CAD 软件。

> **审查快照 — 2026-08-31：** 本 README 按 `main` 基线 `74a6aee92fc7066857e429b37fa2ff80e045ed9e` 重新整理。仓库处于高频并行开发状态，因此涉及发布、可用性或运行时结论时，请以当前 `main`、[`docs/README.md`](docs/README.md)、[`docs/COMMANDS.md`](docs/COMMANDS.md) 以及绑定到精确 SHA 的 CI/runtime 证据为准。

> **QS3D 产品族：** 本仓库是运行在 BricsCAD 内的 QS3D 产品。与 CAD 厂商无关的共享代码在兄弟仓库 `trinhtanphat/QS3D-Platform` 中开发；独立桌面产品位于 `trinhtanphat/QS3D-CAD`。详见 [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) 和 [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md)。

## 仓库主要组成

| 层 | Target | 职责 |
| --- | --- | --- |
| `QS3D.Core` | `netstandard2.0` | 与 CAD 无关的领域模型、持久化、几何/工程量逻辑、诊断、报告和应用服务 |
| `QS3D.BricsCAD.V25` | .NET Framework 4.8 / x64 | BricsCAD V25 host adapter、命令、WPF UI 和 CAD 集成 |
| `QS3D.BricsCAD.V26` | `net8.0-windows` / x64 | BricsCAD V26 host build，具有独立的 V26 host/update 边界，同时复用兼容应用层源码 |
| `external/QS3D-Platform` | 固定版本 submodule | 与厂商无关的共享 contract 与 platform code |
| `tests/` | 多个测试项目/可执行程序 | 确定性 Core 回归、架构测试、host/runtime harness 和聚焦 contract tests |
| `scripts/` + `.github/workflows/` | Python/PowerShell/YAML | Preflight、构建/打包、安装/update、CI、release 与 runtime-proof 工具 |

Host 构建和运行时资格验证需要安装与目标 major 匹配的 BricsCAD，并具备有效许可。仓库不会提交 BricsCAD 专有 SDK binary、客户 DWG、私有项目数据或第三方产品源码。

## 功能地图

该项目已经远超原型阶段，但各功能成熟度并不一致。具体命令的 maturity 请以 [`docs/COMMANDS.md`](docs/COMMANDS.md) 为准，不应把本 README 当作每项能力的认证清单。

### 语义 BIM 与项目模型

- Project、Zone、Floor/Level、Family/Type 与语义 Element 状态。
- 与图纸绑定的项目生命周期、源/生成 CAD handle ownership 和项目元数据。
- Dependency、dirty/freshness、regeneration、persistence 与 recovery contract。
- Project Browser / Workspace / Project Tools 同步。
- Model Health、preflight 与 release-readiness 检查面。

### 结构建模与 3D

- 面向柱、梁、板、墙、洞口以及相关建筑/结构 family 的 Direct Draw 与语义工作流。
- 基础工作流，包括当前对单独基础（single footing）的 source/proof 强化。
- Plan-to-3D 与受保护的 native `Solid3d` 生成，并带 ownership/rollback 检查。
- 梁、柱、板、结构墙和基础的 Rebar 3D 工作流。
- Steel detailing、weld/BOM 与结构 CSV/reporting 功能面。

### 工程量、表格与交付物

- Quantity/BQ review、过滤、重新计算、locate/reveal 与 model-evidence 流程。
- Quick Takeoff 及辅助识别/review 路径。
- Schedule Hub，以及工程量、装修、材料、门/洞口、幕墙和钢筋/BBS 等领域 schedule。
- 在支持的工作流中输出带 element/source provenance 的 XLSX/CSV。
- Cost、reporting、design report 与 project-information 功能。

### MEP 与协同

- 电气设备/灯具/导线 authoring、tag、template、schema/readiness 和 host-export 工作流。
- Coordination/clash、zone、dashboard 与 issue persistence。
- BCF import/export 与 external-clash 交换功能。
- 更广泛架构中包含 HTTP CAD worker、PostgreSQL/Supabase/RLS、RabbitMQ 和 object storage 集成代码；**存在源码不等于外部服务已在当前环境中实际配置或在线。**

### BIM 交换、计划与审查

- IFC 与 JSON import/export；各命令成熟度记录在 `docs/COMMANDS.md`。
- Task link、task list/export、4D、animation 与 planning/reporting 功能。
- Ribbon、Workspace palette、Project Tools、Domain/Schedule/Rebar hubs 和 modeless WPF 工具。
- Highlight/focus/isolate/section-style review 与 drawing-affinity 安全保护。

### 实验性 Web/Integration 面

仓库还包含 web/integration 测试面，例如 health/settings/project/document/quantity/cost API、viewer 与 bridge validation。这些是围绕 QS3D 产品族的集成能力，**不会**把本 BricsCAD 插件仓库变成独立 CAD 替代品。

## 证据与资格验证模型

仓库最重要的原则是：

> **源码中已经实现，不等于已经在有许可的 BricsCAD 中完成生产级资格验证。**

必须区分以下证据层级：

| 证据 | 能证明什么 | 不能证明什么 |
| --- | --- | --- |
| Static/source preflight | 源码形态、policy、security/package contract、确定性源码回归 | BricsCAD native runtime 行为 |
| 确定性 Core tests | 不依赖 CAD 的 domain、persistence、geometry、quantity、dependency、interchange 行为 | `NETLOAD`、WPF/Ribbon、native CAD API |
| Host build | 与所选 BricsCAD SDK/major 的编译兼容性 | 在许可 host 中成功执行 |
| Licensed host proof | 已测试的精确 SHA、host major 和 scenario 的 runtime 行为 | 其他 major、其他 DWG 或未测试环境 |

项目历史已经积累大量 source/preflight/Core/build 证据，但某些精确 licensed-host lane 仍可能因 license、COM、UI 或机器环境限制而处于 `BLOCKED`。这些受阻项不能被描述为 runtime PASS。

产品资格验证请使用 [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md)、[`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md)、runtime runbook 和绑定精确 SHA 的 artifact。

## 架构与源码共享模型

```text
src/
  QS3D.Core/                 与 CAD 无关的 domain/application logic
  QS3D.BricsCAD.V25/         V25 net48/x64 BricsCAD + WPF host
  QS3D.BricsCAD.V26/         V26 net8.0-windows/x64 host project

external/QS3D-Platform/      固定版本的共享 platform submodule
tests/                       deterministic 与 host-oriented tests
scripts/                     preflight、build、package、install/update、proof
docs/                        architecture、workflow、policy、qualification
.github/workflows/            自动 validation 与受控 release/runtime workflow
```

V25 是已经建立的 .NET Framework adapter。V26 是真实的 .NET 8 host build，而不是重命名的 V25 binary。V26 会复用兼容的 V25 application/UI source，同时保留独立的 host-specific entry/update 边界。因此 **V25 证据绝不能自动当作 V26 证据**，反之亦然。

`QS3D.Core` 应保持与 CAD 无关。新的 vendor-neutral 逻辑应优先放在 Core/Platform 边界，而不是把 BricsCAD proprietary API dependency 泄漏到 domain layer。

## Persistence 与 Source of Truth

`.qsdb` sidecar 被视为产品数据，而不是可随意丢弃的 cache。代码包括 bounded input handling、identity/reference validation、save-time validation、atomic publication、backup/recovery、locking/revision 与 dirty/freshness contract。

实际 source-of-truth 模型结合 **DWG 源几何** 与 **`.qsdb` 中的语义/项目元数据**。详见 [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md)。

## Contributor 快速开始

### 1. 连同固定 submodule 一起克隆

```bash
git clone --recurse-submodules https://github.com/trinhtanphat/QS3D-BricsCAD.git
cd QS3D-BricsCAD
```

如果此前没有拉取 submodule：

```bash
git submodule sync --recursive
git submodule update --init --recursive
```

进行重要修改前请阅读：

- [`AGENTS.md`](AGENTS.md)
- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md)
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md)
- [`CI_POLICY.md`](CI_POLICY.md)

### 2. 运行仓库 preflight

```bash
python scripts/preflight.py
python scripts/preflight-all.py
```

### 3. 构建并运行与 CAD 无关的 Core smoke tests

```bash
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

以上命令不需要 BricsCAD SDK binaries。

### 4. 构建 host adapter

不要提交 `BrxMgd.dll`、`TD_Mgd.dll` 或其他 BricsCAD proprietary binaries。

V25 示例：

```powershell
$env:BRICSCAD_V25_DIR = '<BricsCAD V25 安装目录>'
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64
```

V26 示例：

```powershell
$env:BRICSCAD_V26_DIR = '<BricsCAD V26 安装目录>'
dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release -p:Platform=x64
```

不要让一个 host major 的项目引用另一个 major 的 SDK assembly。

## 安装与加载

最终用户应优先使用 GitHub **Releases** 中的 release bundle 以及其中的 installer/checksum 说明，不要随意复制本地 build output。

浏览器下载的 V25 package 可能被 Windows Mark-of-the-Web 阻止加载 managed dependency，甚至在 QS3D startup code 运行前就失败。优先使用解压包中的 `INSTALL-QS3D.cmd`。只有在明确排障、且确认工具来自同一个已校验 release package 时，才使用 `UNBLOCK-QS3D.cmd` 配合直接 `NETLOAD`。

不要通过降低 BricsCAD trusted-path/security 设置来掩盖 package provenance 或 integrity 问题。

## 命令查询

QS3D 包含大量 operational、authoring、structural、MEP、coordination、quantity、schedule 与 interchange 命令。维护中的权威入口是：

- [`docs/COMMANDS.md`](docs/COMMANDS.md) — 命令名、用途与 maturity；
- [`docs/README.md`](docs/README.md) — 文档入口；
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 架构地图。

由于命令列表变化频繁，本 README 不重复完整清单。

## CI、PR 与 Merge

当前 CI 模型会自动验证 task branch 与 protected PR：

- push 到 `agent/**` 和 `integration/**` 可触发共享 `.github/workflows/ci.yml`；
- PR 具有稳定的 required contexts：`preflight` 与 `core`；
- 仅 docs/repository metadata 的变更使用轻量 tier；
- source/build-relevant 变更根据 changed-path classifier 使用更强的 source/Core/V25 validation；
- release/runtime publishing 保持为独立的受控 lane。

Green check 只证明其实际测试的精确 candidate。Hosted CI 本身不会产生 licensed-BricsCAD runtime proof。

标准任务流程：

```text
Issue / Reservation v2
  -> agent/<globally-distinct-session-token>/issue-<N>-<scope>
  -> implement + validate
  -> canonical PR
  -> fresh required checks
  -> current + green + mergeable + collision-clean 时合并同一个 task PR
  -> verify main + 关闭/释放 task state
```

文档修改也**没有直接写入 `main` 的例外**。详见 [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) 与 [`CI_POLICY.md`](CI_POLICY.md)。

## 高交叉风险区域

全仓审查显示，下列区域变更时特别需要聚焦 regression evidence：

- V25/V26 共享 host source 与 framework/runtime 兼容性。
- Drawing ownership、multi-DWG 与 modeless WPF 生命周期。
- Native geometry/boolean 和 source/generated object ownership。
- `.qsdb` identity、dirty/freshness、atomic save 与 recovery。
- Quantity/export provenance 与 XLSX/CSV 完整性。
- Installer/update/package origin 与 host-major 隔离。
- 外部集成以及依赖环境的 credential/connectivity。

这些是高影响设计约束，并不意味着它们天然就是 blocker。

## 文档地图

从 [`docs/README.md`](docs/README.md) 开始。重要文档包括：

- [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) — 产品/host 边界。
- [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md) — Platform/CAD 边界与 migration。
- [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md) — DWG/semantic source of truth。
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — 架构与 dependency map。
- [`docs/COMMANDS.md`](docs/COMMANDS.md) — 权威命令目录。
- [`docs/HEALTH-AND-PREFLIGHT.md`](docs/HEALTH-AND-PREFLIGHT.md) — health/preflight 模型。
- [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md) — V25 runtime qualification。
- [`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md) — V26 runtime qualification。
- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) — protected-main merge 授权。
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md) — Reservation v2/canonical carrier。
- [`CI_POLICY.md`](CI_POLICY.md) — 当前 CI 语义。

## Release 与支持边界

通过 GitHub **Releases** 获取 packaged candidate 及对应 release notes。已发布 package、成功 source build 和 licensed-runtime qualification 是不同证据类别；请针对实际要运行的 host major 阅读对应 release note 与 proof artifact。

本仓库不分发 BricsCAD proprietary SDK/runtime binaries。需要 host execution 时，用户或 CI/runtime agent 必须自行提供有效的 BricsCAD 安装与许可。

## License

仓库许可条款见 [`LICENSE`](LICENSE)。第三方和 proprietary component 仍受各自许可证约束。
