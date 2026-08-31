# QS3D для BricsCAD V25 + V26

[English](README.md) | [Tiếng Việt](README.vi.md) | [Русский](README.ru.md) | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

QS3D — clean-room плагин **BIM, семантического 3D-моделирования, координации и подсчёта объёмов для BricsCAD V25 и V26 x64**. Он работает внутри BricsCAD как управляемый плагин и не является самостоятельной CAD-системой.

> **Срез ревью — 31.08.2026:** README обновлён по базовой версии `main` `74a6aee92fc7066857e429b37fa2ff80e045ed9e`. Репозиторий активно развивается несколькими параллельными потоками, поэтому для релизных утверждений всегда сверяйтесь с текущим `main`, [`docs/README.md`](docs/README.md), [`docs/COMMANDS.md`](docs/COMMANDS.md) и CI/runtime-доказательствами, привязанными к точному SHA.

> **Семейство QS3D:** этот репозиторий содержит продукт QS3D, работающий в BricsCAD. Общий независимый от CAD-поставщика код развивается в соседнем `trinhtanphat/QS3D-Platform`, а отдельный desktop-продукт — в `trinhtanphat/QS3D-CAD`. См. [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) и [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md).

## Основные части репозитория

| Слой | Target | Назначение |
| --- | --- | --- |
| `QS3D.Core` | `netstandard2.0` | Независимая от CAD доменная модель, persistence, геометрия/объёмы, диагностика, отчёты и application services |
| `QS3D.BricsCAD.V25` | .NET Framework 4.8 / x64 | Адаптер BricsCAD V25, команды, WPF UI и интеграция с CAD |
| `QS3D.BricsCAD.V26` | `net8.0-windows` / x64 | Host-сборка BricsCAD V26 с отдельными V26-границами host/update и повторным использованием совместимого прикладного кода |
| `external/QS3D-Platform` | закреплённый submodule | Общие независимые от поставщика контракты и platform-код |
| `tests/` | несколько test-проектов | Детерминированные Core-регрессии, architecture tests, host/runtime harness и узкие contract tests |
| `scripts/` + `.github/workflows/` | Python/PowerShell/YAML | Preflight, упаковка, установка/update, CI, release и runtime-proof tooling |

Для сборки и runtime-квалификации host-адаптера нужна соответствующая установленная и лицензированная версия BricsCAD. Proprietary SDK-библиотеки BricsCAD, клиентские DWG, приватные проектные данные и исходный код сторонних продуктов намеренно не хранятся в репозитории.

## Карта возможностей

Проект значительно вышел за рамки прототипа, но зрелость функций неодинакова. Для статуса конкретных команд используйте [`docs/COMMANDS.md`](docs/COMMANDS.md); этот README — обзор, а не сертификат каждой функции.

### Семантический BIM и модель проекта

- Project, Zone, Floor/Level, Family/Type и семантическое состояние Element.
- Жизненный цикл проекта, привязанный к чертежу, ownership исходных/сгенерированных CAD-handle и метаданные проекта.
- Dependency, dirty/freshness, regeneration, persistence и recovery contracts.
- Синхронизация Project Browser / Workspace / Project Tools.
- Model Health, preflight и release-readiness поверхности.

### Конструктивное моделирование и 3D

- Direct Draw и семантические workflow для колонн, балок, плит, стен, проёмов и связанных архитектурно-конструктивных семейств.
- Workflow фундаментов, включая текущую source/proof-защиту одиночного фундамента.
- Plan-to-3D и контролируемое создание native `Solid3d` с ownership/rollback проверками.
- Rebar 3D для балок, колонн, плит, конструктивных стен и фундаментов.
- Steel detailing, weld/BOM и конструктивные CSV/reporting поверхности.

### Объёмы, ведомости и выдача данных

- Quantity/BQ review, фильтрация, перерасчёт, locate/reveal и model-evidence.
- Quick Takeoff и вспомогательные recognition/review пути.
- Schedule Hub и предметные ведомости для объёмов, отделки, материалов, дверей/проёмов, curtain-систем и арматуры/BBS.
- XLSX/CSV с provenance до элемента/источника там, где это поддерживает workflow.
- Cost, reporting, design-report и project-information функции.

### MEP и координация

- Авторинг электрооборудования/светильников/проводки, теги, шаблоны, schema/readiness и host-export.
- Coordination/clash, зоны, dashboard и сохранение issue-состояния.
- BCF import/export и external-clash обмен.
- В более широкой архитектуре присутствует код для HTTP CAD worker, PostgreSQL/Supabase/RLS, RabbitMQ и object storage; наличие исходников не означает, что внешние сервисы настроены и доступны в конкретной среде.

### BIM interchange, planning и review

- IFC и JSON import/export; зрелость каждой команды фиксируется в `docs/COMMANDS.md`.
- Task links, task list/export, 4D, animation и planning/reporting.
- Ribbon, Workspace palette, Project Tools, Domain/Schedule/Rebar hubs и modeless WPF-инструменты.
- Highlight/focus/isolate/section-style review и защита drawing affinity.

### Экспериментальная web/integration поверхность

В репозитории также есть web/integration тестовые поверхности: health/settings/project/document/quantity/cost API, viewer и bridge validation. Это интеграционные компоненты вокруг семейства QS3D; они **не** превращают BricsCAD-плагин в самостоятельную замену CAD.

## Модель доказательств и квалификации

Главное правило проекта:

> **Наличие реализации в исходниках не равно production-квалификации в лицензированном BricsCAD.**

Разделяйте уровни доказательств:

| Доказательство | Что подтверждает | Чего не подтверждает |
| --- | --- | --- |
| Static/source preflight | форму исходников, policy, security/package contracts, детерминированные source-regressions | native runtime BricsCAD |
| Детерминированные Core tests | domain, persistence, geometry, quantity, dependency, interchange без CAD | `NETLOAD`, WPF/Ribbon, native CAD API |
| Host build | компиляцию с выбранным SDK/major BricsCAD | успешный запуск в лицензированном host |
| Licensed host proof | runtime-поведение точного SHA, host major и проверенного сценария | другие major, DWG и непроверенные среды |

В истории проекта накоплен значительный объём source/preflight/Core/build доказательств, но некоторые exact licensed-host lanes могут оставаться `BLOCKED` из-за лицензии, COM, UI или ограничений окружения. Такие ячейки нельзя объявлять runtime PASS.

Для product qualification используйте [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md), [`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md), runtime runbooks и артефакты точного SHA.

## Архитектура и совместное использование исходников

```text
src/
  QS3D.Core/                 CAD-независимая domain/application логика
  QS3D.BricsCAD.V25/         BricsCAD + WPF host V25 net48/x64
  QS3D.BricsCAD.V26/         host V26 net8.0-windows/x64

external/QS3D-Platform/      закреплённый общий platform submodule
tests/                       deterministic и host-oriented tests
scripts/                     preflight, build, package, install/update, proof
docs/                        architecture, workflow, policy, qualification
.github/workflows/            автоматическая validation и контролируемые release/runtime lanes
```

V25 — основной .NET Framework adapter. V26 — реальная .NET 8 host-сборка, а не переименованный V25 binary. V26 повторно использует совместимый application/UI source V25, сохраняя отдельные host-specific entry/update границы. Поэтому **доказательства V25 нельзя автоматически переносить на V26**, и наоборот.

`QS3D.Core` должен оставаться независимым от CAD. Новый vendor-neutral код следует размещать в Core/Platform, не протягивая proprietary BricsCAD API в доменный слой.

## Persistence и источник истины

Sidecar `.qsdb` рассматривается как продуктовые данные, а не временный cache. Код содержит bounded input handling, проверки identity/reference, save-time validation, atomic publication, backup/recovery, locking/revision и dirty/freshness contracts.

Практическая модель source of truth объединяет **исходную геометрию DWG** и **семантические/project metadata в `.qsdb`**. См. [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md).

## Быстрый старт для разработчиков

### 1. Клонирование с закреплённым submodule

```bash
git clone --recurse-submodules https://github.com/trinhtanphat/QS3D-BricsCAD.git
cd QS3D-BricsCAD
```

Если репозиторий был клонирован без submodule:

```bash
git submodule sync --recursive
git submodule update --init --recursive
```

Перед существенными изменениями прочитайте:

- [`AGENTS.md`](AGENTS.md)
- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md)
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md)
- [`CI_POLICY.md`](CI_POLICY.md)

### 2. Preflight репозитория

```bash
python scripts/preflight.py
python scripts/preflight-all.py
```

### 3. Core build и smoke tests без CAD

```bash
dotnet build src/QS3D.Core/QS3D.Core.csproj -c Release
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

Эти команды не требуют BricsCAD SDK binaries.

### 4. Сборка host adapter

Не коммитьте `BrxMgd.dll`, `TD_Mgd.dll` и другие proprietary BricsCAD binaries.

V25:

```powershell
$env:BRICSCAD_V25_DIR = '<каталог установки BricsCAD V25>'
dotnet build src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj -c Release -p:Platform=x64
```

V26:

```powershell
$env:BRICSCAD_V26_DIR = '<каталог установки BricsCAD V26>'
dotnet build src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj -c Release -p:Platform=x64
```

Не используйте SDK assemblies одного host major для проекта другого major.

## Установка и загрузка

Конечным пользователям рекомендуется использовать release bundle и вложенные инструкции installer/checksum, а не копировать случайные build outputs. См. страницу **Releases** и host-specific release documentation.

Для V25-пакетов, скачанных браузером, Windows Mark-of-the-Web может блокировать managed dependencies до запуска startup-кода QS3D. Предпочтителен `INSTALL-QS3D.cmd` из распакованного пакета. Для осознанной диагностики через прямой `NETLOAD` используйте `UNBLOCK-QS3D.cmd` только из того же проверенного release package.

Не ослабляйте trusted-path/security настройки BricsCAD вместо исправления provenance или integrity пакета.

## Каталог команд

QS3D содержит множество operational, authoring, structural, MEP, coordination, quantity, schedule и interchange команд. Актуальные источники:

- [`docs/COMMANDS.md`](docs/COMMANDS.md) — команды, назначение и maturity;
- [`docs/README.md`](docs/README.md) — вход в документацию;
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — архитектурная карта.

Полный список не дублируется в README, поскольку он часто меняется.

## CI, PR и merge

Текущая модель CI автоматически валидирует task branches и protected PR:

- push в `agent/**` и `integration/**` может запускать `.github/workflows/ci.yml`;
- PR получает стабильные required contexts `preflight` и `core`;
- docs/repository-metadata-only изменения используют лёгкий tier;
- source/build-relevant изменения получают более сильную source/Core/V25 validation согласно changed-path classifier;
- release/runtime publishing остаётся отдельным контролируемым контуром.

Green check относится только к точному протестированному candidate. Hosted CI сам по себе не является licensed-BricsCAD runtime proof.

Стандартный workflow:

```text
Issue / Reservation v2
  -> agent/<globally-distinct-session-token>/issue-<N>-<scope>
  -> implement + validate
  -> canonical PR
  -> fresh required checks
  -> merge того же task PR, когда он current + green + mergeable + collision-clean
  -> verify main + закрыть/release task state
```

Для документации **нет исключения, разрешающего прямую запись в `main`**. См. [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) и [`CI_POLICY.md`](CI_POLICY.md).

## Зоны повышенного сквозного риска

При изменениях особенно важны сфокусированные regression evidence для:

- общего host-source V25/V26 и совместимости framework/runtime;
- drawing ownership, multi-DWG и modeless WPF lifecycle;
- native geometry/boolean и ownership source/generated объектов;
- `.qsdb` identity, dirty/freshness, atomic save и recovery;
- quantity/export provenance и целостности XLSX/CSV;
- installer/update/package-origin и host-major isolation;
- внешних интеграций, credentials и connectivity, зависящих от окружения.

Это архитектурные ограничения, а не автоматические blockers.

## Карта документации

Начните с [`docs/README.md`](docs/README.md). Ключевые документы:

- [`docs/PRODUCT-BOUNDARY.md`](docs/PRODUCT-BOUNDARY.md) — границы продукта и host.
- [`docs/QS3D-PLATFORM-MIGRATION.md`](docs/QS3D-PLATFORM-MIGRATION.md) — Platform/CAD boundary и migration.
- [`docs/SOURCE-OF-TRUTH.md`](docs/SOURCE-OF-TRUTH.md) — DWG/semantic source of truth.
- [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — архитектура и dependencies.
- [`docs/COMMANDS.md`](docs/COMMANDS.md) — авторитетный каталог команд.
- [`docs/HEALTH-AND-PREFLIGHT.md`](docs/HEALTH-AND-PREFLIGHT.md) — health/preflight.
- [`docs/LOCAL-V25-QUALIFICATION.md`](docs/LOCAL-V25-QUALIFICATION.md) — runtime qualification V25.
- [`docs/LOCAL-V26-QUALIFICATION.md`](docs/LOCAL-V26-QUALIFICATION.md) — runtime qualification V26.
- [`docs/MAIN-WRITE-AUTHORIZATION.md`](docs/MAIN-WRITE-AUTHORIZATION.md) — защищённый merge в main.
- [`docs/AGENT-WORK-REGISTRATION.md`](docs/AGENT-WORK-REGISTRATION.md) — Reservation v2/canonical carrier.
- [`CI_POLICY.md`](CI_POLICY.md) — текущая CI semantics.

## Релизы и границы поддержки

Используйте GitHub **Releases** для packaged candidates и release notes конкретной версии. Опубликованный package, успешная source build и licensed-runtime qualification — разные классы доказательств; проверяйте release note и proof именно для нужного host major.

Репозиторий не распространяет proprietary BricsCAD SDK/runtime binaries. Для host execution пользователь или CI/runtime agent должен предоставить собственную валидную установку и лицензию BricsCAD.

## Лицензия

Условия лицензирования репозитория находятся в [`LICENSE`](LICENSE). Сторонние и proprietary компоненты регулируются собственными лицензиями.
