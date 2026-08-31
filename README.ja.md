# QS3D for BricsCAD V25 + V26

[English](README.md) | [Tiếng Việt](README.vi.md) | [Русский](README.ru.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

QS3D は **BricsCAD V25 / V26 x64 向けの BIM、セマンティック 3D、コーディネーション、数量拾いプラグイン**です。clean-room 方針で開発され、BricsCAD 内で managed plugin として動作します。単独で動作する CAD アプリケーションではありません。

> **レビュー時点 — 2026-08-31:** この README は `main` の基準 SHA `74a6aee92fc7066857e429b37fa2ff80e045ed9e` を基に更新しました。リポジトリは複数レーンで高速に更新されているため、リリースや実行可否を判断するときは、現在の `main`、[`docs/README.md`](docs/README.md)、[`docs/COMMANDS.md`](docs/COMMANDS.md)、および exact SHA に紐づく CI/runtime 証跡を確認してください。

> **QS3D 製品ファミリー:** このリポジトリは BricsCAD ホスト版 QS3D です。CAD ベンダー非依存の共有コードは兄弟リポジトリ `trinhtanphat/QS3D-Platform`、独立デスクトップ製品は `trinhtanphat/QS3D-CAD` で開発されています。詳しくは [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) と [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md) を参照してください。

## リポジトリの主要構成

| レイヤー | Target | 役割 |
| --- | --- | --- |
| `QS3D.Core` | `netstandard2.0` | CAD 非依存のドメインモデル、永続化、幾何/数量ロジック、診断、レポート、application services |
| `QS3D.BricsCAD.V25` | .NET Framework 4.8 / x64 | BricsCAD V25 host adapter、コマンド、WPF UI、CAD 統合 |
| `QS3D.BricsCAD.V26` | `net8.0-windows` / x64 | V26 固有の host/update 境界を持つ BricsCAD V26 host build。互換なアプリケーションコードを再利用 |
| `external/QS3D-Platform` | pinned submodule | CAD ベンダー非依存の共有 contract / platform code |
| `tests/` | 複数の test project/executable | deterministic Core regression、architecture、host/runtime harness、focused contract tests |
| `scripts/` + `.github/workflows/` | Python/PowerShell/YAML | preflight、package、install/update、CI、release、runtime-proof tooling |

Host の build/runtime qualification には対象 major と一致する BricsCAD のインストールと有効なライセンスが必要です。BricsCAD の proprietary SDK binary、顧客 DWG、非公開プロジェクトデータ、第三者製品のソースは意図的にリポジトリへ含めていません。

## 機能マップ

本プロジェクトはすでに単なる prototype の段階を超えていますが、機能ごとの成熟度は同一ではありません。個別コマンドの maturity は [`docs/COMMANDS.md`](docs/COMMANDS.md) を正とし、この README を各機能の認証一覧として扱わないでください。

### セマンティック BIM / プロジェクトモデル

- Project、Zone、Floor/Level、Family/Type、セマンティック Element 状態。
- DWG に結び付いた project lifecycle、source/generated CAD handle ownership、project metadata。
- Dependency、dirty/freshness、regeneration、persistence、recovery contract。
- Project Browser / Workspace / Project Tools の同期。
- Model Health、preflight、release-readiness の検査面。

### 構造 authoring / 3D

- 柱、梁、スラブ、壁、開口、および関連する建築/構造 family の Direct Draw とセマンティック workflow。
- 現在の single-footing source/proof hardening を含む基礎 workflow。
- Ownership/rollback guard を伴う Plan-to-3D と native `Solid3d` 生成。
- 梁、柱、スラブ、構造壁、基礎向け Rebar 3D workflow。
- Steel detailing、weld/BOM、構造 CSV/reporting surface。

### 数量、Schedule、成果物

- Quantity/BQ review、filter、再計算、locate/reveal、model-evidence flow。
- Quick Takeoff と支援付き recognition/review path。
- Schedule Hub と、数量、仕上げ、材料、ドア/開口、curtain、鉄筋/BBS などの domain schedule。
- 対応 workflow では element/source provenance を保持した XLSX/CSV 出力。
- Cost、reporting、design report、project-information surface。

### MEP / Coordination

- 電気設備、照明、配線の authoring、tag、template、schema/readiness、host-export workflow。
- Coordination/clash、zone、dashboard、issue persistence。
- BCF import/export と external-clash exchange surface。
- より広いアーキテクチャには HTTP CAD worker、PostgreSQL/Supabase/RLS、RabbitMQ、object storage の integration code があります。ただし、**ソースが存在することは外部サービスが実環境で設定・稼働済みであることを意味しません。**

### BIM interchange、Planning、Review

- IFC / JSON import/export。各コマンドの成熟度は `docs/COMMANDS.md` に記録されています。
- Task link、task list/export、4D、animation、planning/reporting。
- Ribbon、Workspace palette、Project Tools、Domain/Schedule/Rebar hubs、modeless WPF tools。
- Highlight/focus/isolate/section-style review と drawing-affinity safety path。

### 実験的な Web / Integration surface

リポジトリには health/settings/project/document/quantity/cost API、viewer、bridge validation などの web/integration test surface も含まれます。これらは QS3D 製品ファミリー周辺の統合機能であり、**この BricsCAD plugin リポジトリを単独 CAD の代替製品にするものではありません。**

## 証跡と Qualification モデル

最重要ルールは次のとおりです。

> **ソースに実装されていることと、ライセンス済み BricsCAD で production-qualified であることは同義ではありません。**

証跡の種類を明確に分けてください。

| 証跡 | 証明できること | 証明できないこと |
| --- | --- | --- |
| Static/source preflight | source shape、policy、security/package contract、deterministic source regression | BricsCAD native runtime behavior |
| Deterministic Core tests | CAD 非依存の domain、persistence、geometry、quantity、dependency、interchange | `NETLOAD`、WPF/Ribbon、native CAD API |
| Host build | 選択した BricsCAD SDK/major との compile compatibility | ライセンス済み host での正常実行 |
| Licensed host proof | テスト済み exact SHA、host major、scenario の runtime behavior | 他の major、他の DWG、未検証環境 |

プロジェクトには多数の source/preflight/Core/build 証跡がありますが、exact licensed-host lane の一部は license、COM、UI、マシン環境の制約で `BLOCKED` のままになる場合があります。これらを runtime PASS と表現してはいけません。

Product qualification には [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md)、[`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md)、runtime runbook、および exact SHA artifact を使用してください。

## アーキテクチャとソース共有モデル

```text
src/
  QS3D.Core/                 CAD 非依存の domain/application logic
  QS3D.BricsCAD.V25/         V25 net48/x64 BricsCAD + WPF host
  QS3D.BricsCAD.V26/         V26 net8.0-windows/x64 host project

external/QS3D-Platform/      pinned shared platform submodule
tests/                       deterministic / host-oriented tests
scripts/                     preflight, build, package, install/update, proof
docs/                        architecture, workflow, policy, qualification
.github/workflows/            automatic validation と controlled release/runtime workflows
```

V25 は確立済みの .NET Framework adapter です。V26 は V25 binary の名称変更ではなく、実際の .NET 8 host build です。V26 は互換性のある V25 application/UI source を再利用しつつ、host-specific entry/update 境界を分離しています。そのため **V25 の証跡を自動的に V26 の証跡として扱うことはできません**。逆も同様です。

`QS3D.Core` は CAD 非依存を維持する方針です。新しい vendor-neutral logic は proprietary BricsCAD API dependency を domain layer に持ち込まず、Core/Platform 境界へ配置することを優先してください。

## Persistence と Source of Truth

`.qsdb` sidecar は使い捨て cache ではなく製品データとして扱われます。実装には bounded input handling、identity/reference validation、save-time validation、atomic publication、backup/recovery、locking/revision、dirty/freshness contract が含まれます。

実際の source-of-truth モデルは **DWG の source geometry** と **`.qsdb` の semantic/project metadata** を組み合わせます。詳しくは [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md) を参照してください。

## Contributor Quick Start

### 1. pinned submodule とともに clone

```bash
git clone --recurse-submodules https://github.com/trinhtanphat/QS3D-BricsCAD.git
cd QS3D-BricsCAD
```

Submodule なしで clone 済みの場合:

```bash
git submodule sync --recursive
git submodule update --init --recursive
```

大きな変更の前に次を確認してください。

- [`AGENTS.md`](AGENTS.md)
- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md)
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md)
- [`CI_POLICY.md`](CI_POLICY.md)

### 2. Repository preflight

```bash
python scripts/preflight.py
python scripts/preflight-all.py
```

### 3. CAD 非依存 Core build / smoke tests

```bash
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

これらは BricsCAD SDK binaries を必要としません。

### 4. Host adapter build

`BrxMgd.dll`、`TD_Mgd.dll` などの proprietary BricsCAD binary を commit しないでください。

V25 例:

```powershell
$env:BRICSCAD_V25_DIR = '<BricsCAD V25 installation directory>'
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64
```

V26 例:

```powershell
$env:BRICSCAD_V26_DIR = '<BricsCAD V26 installation directory>'
dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release -p:Platform=x64
```

ある host major の project から別 major の SDK assembly を参照しないでください。

## インストールとロード

エンドユーザーは任意の build output をコピーするより、GitHub **Releases** の release bundle と付属 installer/checksum 手順を利用してください。

ブラウザから取得した V25 package は Windows Mark-of-the-Web により、QS3D startup code が実行される前に managed dependency がブロックされることがあります。展開済み package の `INSTALL-QS3D.cmd` を優先してください。直接 `NETLOAD` を用いた明示的なトラブルシュート時のみ、同一の検証済み release package に含まれる `UNBLOCK-QS3D.cmd` を使用してください。

Package provenance/integrity の問題を隠すために BricsCAD の trusted-path/security 設定を弱めないでください。

## コマンド一覧

QS3D には operational、authoring、structural、MEP、coordination、quantity、schedule、interchange の多数のコマンドがあります。維持されている正規の入口は次です。

- [`docs/COMMANDS.md`](docs/COMMANDS.md) — コマンド名、目的、maturity。
- [`docs/README.md`](docs/README.md) — ドキュメント入口。
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — アーキテクチャマップ。

コマンド群は頻繁に変化するため、この README では完全な一覧を重複掲載しません。

## CI、PR、Merge

現在の CI は task branch と protected PR に対して自動 validation を行います。

- `agent/**` / `integration/**` への push は共有 `.github/workflows/ci.yml` validation の対象です。
- PR には安定した required contexts `preflight` と `core` があります。
- docs/repository-metadata-only 変更は lightweight tier を使用します。
- source/build-relevant 変更は changed-path classifier に基づき、より強い source/Core/V25 validation を使用します。
- release/runtime publishing は別の controlled lane です。

Green check は実際に検証された exact candidate のみを保証します。Hosted CI 自体は licensed-BricsCAD runtime proof を生成しません。

標準 task workflow:

```text
Issue / Reservation v2
  -> agent/<globally-distinct-session-token>/issue-<N>-<scope>
  -> implement + validate
  -> canonical PR
  -> fresh required checks
  -> current + green + mergeable + collision-clean なら同一 task PR を merge
  -> verify main + task state を close/release
```

ドキュメント変更にも **`main` への direct-write 例外はありません**。[`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) と [`CI_POLICY.md`](CI_POLICY.md) を参照してください。

## 影響範囲が大きいリスク領域

Repository-wide review では、次の領域の変更に focused regression evidence が特に重要です。

- V25/V26 shared host source と framework/runtime compatibility。
- Drawing ownership、multi-DWG、modeless WPF lifecycle。
- Native geometry/boolean と source/generated object ownership。
- `.qsdb` identity、dirty/freshness、atomic save、recovery semantics。
- Quantity/export provenance と XLSX/CSV integrity。
- Installer/update/package origin と host-major isolation。
- External integrations、環境依存 credentials/connectivity。

これらは影響の大きい設計制約であり、自動的に blocker という意味ではありません。

## ドキュメントマップ

最初に [`docs/README.md`](docs/README.md) を参照してください。主要文書:

- [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) — product/host boundary。
- [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md) — Platform/CAD boundary と migration。
- [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md) — DWG/semantic source of truth。
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — architecture / dependency map。
- [`docs/COMMANDS.md`](docs/COMMANDS.md) — 正規コマンドカタログ。
- [`docs/HEALTH-AND-PREFLIGHT.md`](docs/HEALTH-AND-PREFLIGHT.md) — health/preflight model。
- [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md) — V25 runtime qualification。
- [`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md) — V26 runtime qualification。
- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) — protected-main merge authorization。
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md) — Reservation v2/canonical carrier。
- [`CI_POLICY.md`](CI_POLICY.md) — 現行 CI semantics。

## Release と Support Boundary

Packaged candidate と対応 release notes は GitHub **Releases** を使用してください。Published package、成功した source build、licensed-runtime qualification は別々の証跡クラスです。実際に使用する host major の release note と proof artifact を確認してください。

このリポジトリは proprietary BricsCAD SDK/runtime binaries を配布しません。Host execution が必要な場合、利用者または CI/runtime agent が有効な BricsCAD のインストールとライセンスを用意する必要があります。

## License

リポジトリのライセンス条件は [`LICENSE`](LICENSE) を参照してください。第三者および proprietary component にはそれぞれのライセンスが適用されます。
