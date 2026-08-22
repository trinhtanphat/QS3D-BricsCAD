# Work claim — Construction Reference Search

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-construction-reference-search`
- Registered: `2026-08-11T21:45:00+07:00`
- Completed: `2026-08-11`
- Baseline main SHA: `fe6118179b10a81af833eb7db1ac3fb8eb5f554e`
- Priority: P2

## Reserved scope

Implement the owner-requested construction-reference search workflow inspired by the supplied `ván khuôn móng` search screenshot, without embedding/scraping third-party result pages inside the plugin.

## Reserved files

- `src/QS3D.BricsCAD.V25/ReferenceSearchCommands.cs` (new)
- `src/QS3D.BricsCAD.V25/UI/ReferenceSearchWindow.xaml` (new)
- `src/QS3D.BricsCAD.V25/UI/ReferenceSearchWindow.xaml.cs` (new)
- `scripts/preflight-construction-reference-search.py` (new)
- `docs/CONSTRUCTION-REFERENCE-SEARCH.md` (new)
- `docs/COMMANDS.md` — only the `QS3DREFSEARCH` command registration/documentation row or section
- `docs/LOCAL-AGENT-INBOX.md` — append/update only the dedicated construction-reference-search LOCAL_ONLY runtime item
- this claim file for close-out

## Contract

- expose `QS3DREFSEARCH` as a BricsCAD-hosted modeless research launcher;
- provide search categories matching the intent of the screenshot: Hình ảnh, Web, Video, Mua sắm, Video ngắn and Tin tức;
- URL-encode all user-entered query text and construct only fixed HTTPS search-provider URLs with SafeSearch enabled where supported;
- open results in the user's default browser via shell execution rather than scraping Google/Bing HTML or embedding the legacy WPF WebBrowser engine;
- include construction-oriented quick queries such as `Ván khuôn móng`, `Cốt thép móng`, `Chi tiết dầm`, `Chi tiết sàn`, `Cấu tạo tường` and `Mặt cắt móng`;
- bind the modeless window to its source BricsCAD Document and fail closed on document switch before launching a result;
- no project creation/mutation, CAD database writes, persistence, Core changes, Ribbon/Start Center/RightPanel edits, third-party API keys, HttpClient/WebClient scraping or new NuGet dependency;
- keep shared-doc edits narrowly scoped to command discoverability and the exact LOCAL_ONLY browser/modeless runtime scenario required by repository policy;
- do not dispatch/re-run GitHub Actions and do not claim licensed BricsCAD V25 runtime PASS remotely.

## Validation

Add a source-safe preflight that parses XAML, guards all category/query wiring, fixed HTTPS + escaped query construction, shell-browser launch, document affinity and absence of network scraping/project mutation APIs.

Register the remaining local-only V25/Windows browser-launch/document-switch qualification in the canonical local inbox; remote source/static evidence must not be promoted to `LOCAL_PASS`.

## Coordination

The three source files, focused preflight and dedicated feature doc are exclusive to this claim. `docs/COMMANDS.md` and `docs/LOCAL-AGENT-INBOX.md` are shared surfaces; this claim reserves only the minimal `QS3DREFSEARCH` documentation and dedicated local runtime item, and must preserve all concurrent entries.

## Close-out

- Implementation PR: `#491`.
- Source merge SHA: `32cb862b79ab1230ca0e50ca9413eda3f99a228f`.
- Command-reference SHA: `faa52e1df4a9126f5a09bc70b364f5c6dac2a69a`.
- Canonical LOCAL_ONLY handoff SHA: `4ff7bc3b39e1b611f64a13493092eb3a512fdc94` (`LOCAL-015`).
- Source-safe validation: `scripts/preflight-construction-reference-search.py` PASS against the merged source/XAML/doc set; it guards XML/wiring, document affinity, URL escaping, fixed HTTPS/SafeSearch browser launch and absence of scraping/project-write APIs.
- Runtime disposition: licensed BricsCAD V25 + Windows default-browser/modeless qualification remains `LOCAL-015: OPEN / PENDING_LOCAL / DO_NOT_RETRY_REMOTE`; no remote `LOCAL_PASS` is claimed.
- GitHub Actions: not dispatched by this lane.

## Completion condition

Satisfied. The command/window/docs/preflight, canonical command reference and canonical LOCAL_ONLY runtime handoff are present on `main`; shared-document edits were limited to the reserved `QS3DREFSEARCH`/`LOCAL-015` entries.
