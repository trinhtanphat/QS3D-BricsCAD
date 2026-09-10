# LOCAL-021 / #4041 licensed runtime evidence

Updated: 2026-09-09 UTC. Status: **PARTIAL_NATIVE_PASS / QUANTITY_RUNTIME_FAIL**.
This is not aggregate LOCAL-021 PASS and does not qualify a customer release.

## Exact tested product

- Official installed package: `v0.2.0-preview.1`, unchanged during these tests.
- Product source: `d8adfc0e2661e1f1210b81d7d98ebbc62e722333`.
- Official V25 ZIP SHA-256:
  `14bebc4730ba1858038ce4d0ea86a2f1cb9d203130edc73865997ba7273e3a5f`.
- Installed adapter SHA-256:
  `CFD563182888E4149FCA5CCE6983AA7C265F68AB093C4DBC372A66E0F67BEE84`.
- Installed Core SHA-256:
  `B6592DBDD99445AF0F606A2732277A365AE81C1F1154CB722F1EDD5BEA38A32A`.
- Host: licensed BricsCAD Ultimate V25.2.10, Windows x64. Runtime markers
  verified native major 25, x64, exact installed loader and visible shell panels.
- Contains the earlier Workspace correction `80f609057bb95b58f08f3ea88ea22411b88cb558`.

The repository evidence/probe commit is distinct from the tested product source.
Current Git/CI success must not be substituted for runtime evidence on these
installed bytes. The official-package verifier defect is separately owned by
#6231; these observations do not claim that verifier passed.

## Licensed observations

| Scenario | Actual result |
| --- | --- |
| Installed DemandLoad shell | PASS: Ribbon, Workspace and right panel opened; graceful exit and protected-state cleanup. Launch-to-marker was 43.389 seconds, **not** isolated plugin latency or proof of no UI lag. |
| Workspace Móng > Móng Bè > Add | Observed PASS: `+ Add` created and auto-selected `Móng Bè-1`, initially zero instances. |
| Dedicated initial Properties | Partial: Family/type, level, 500-to-800 mm edit, `bottom_level`/`top_level` choices observed. Full strict labels/material/WBS matrix not completed. Generic `Bề dày` was later observed after selection/scope changes; not accepted as strict-schema PASS. |
| Native initial raft | PASS for the bounded observation: production closed-boundary authoring created 4 x 6 x 0.8 m solid, volume 19.2 m3, native bounds (0,0,0)-(4000,6000,800) mm. |
| 800-to-900 mm regeneration | PASS for the bounded observation: actual Family edit plus production `QS3DBUILD3D`; volume 21.6 m3, top Z 900 mm. |
| Original uninterrupted return/save | NOT COMPLETED: 800 mm semantic edit was accepted, but the UI session timed out before final rebuild/Enter. Original drawing was not falsely accepted as QSAVED. |
| Recovered-fixture 900-to-800 rebuild | PASS: retained disposable autosave copied to a fresh allocation; asserted 900 mm before production rebuild, then 800 mm/19.2 m3, one solid, two source polylines and old generated output no longer live. Production QS3DSAVE and native QSAVE executed. |
| Independent cold reopen | PASS for the saved synthetic fixture: a fresh host opened that exact QSAVED drawing, preserved one solid/two sources, 19.2 m3 and 0-800 mm bounds. Saved QSDB contained one Foundation with matching thickness/height/elevations. Cold-open DWG bytes unchanged. |
| Quantity Insight after cold open | **FAIL**: selected current 19.2 m3 solid opened isolated Quantity Insight, but the panel showed zero rows/zero totals and a visible `Project changed while the quantity...` error. |
| Quantity Insight Tính lại | **FAIL** for report availability: product reported regeneration of one element but still displayed zero rows. |
| Quantity Insight Mở BQ | **FAIL**: `QS3DBQ error: không thể hoàn tất thao tác.` No BQ window opened. Source correction already owned by #6198 / PR #6224; no duplicate source lane created. |
| Final native geometry after report failure | Actual native read still returned one solid and 19.2 m3; test drawing/project saved. Zero report rows must not be interpreted as zero physical quantity. |
| Top-level geometry, exact formwork, highlight, export | NOT RUN / unproven. Required 7.071 / 9.192 / 0 Top / 0 End / 6.771 / 8.802 and Aggregate=Detail remain mandatory. |
| MCP/tunnel, V26, HiDPI, performance qualification | NOT RUN in these allocations. MCP tests remained paused. |

The recovered-fixture sequence is useful evidence, but does not retroactively
make the interrupted UI session an atomic successful qualification run.

## Allocations and cleanup

1. `installed-preview1-raft-ui-01`, run
   `651d47a46e454dfaa8cab0ff3e66cdcb`: terminal timeout at
   `2026-09-09T01:07:36.791Z`. Forced containment used after the UI bound.
   Profile/current-pointer/inventory, nonce and tunnel-preference recovery
   verified; installed DLLs/registration unchanged, no remaining CAD process.
2. `recovered-native-02`, run `6c59c069ccf64ab6a60dcc33652de4aa`:
   rebuild/save assertions passed. The runner then exited 1 because process
   enumeration still reported CAD immediately after its owned process exited.
   Cold phase was **not started**. Original receipt retained; a separate terminal
   recovery receipt records the independently verified cleanup. This was not a
   two-phase PASS and not a new product geometry failure.
3. `recovered-cold-03`, run `ab6dcc479e5b4a2eaee3e79ce890086c`:
   `2026-09-09T01:28:09.9125586Z` to `01:28:51.5964921Z`, exit 0.
   Native cold checks, saved semantics, graceful exit, profile/current-pointer/
   inventory restoration, nonce removal, tunnel preference restoration,
   unchanged installed state and zero CAD all verified. No force-close fallback.
4. `quantity-ui-04`, run `8634fe8cf5254e2c98238a2913bc761c`:
   `2026-09-09T01:32:00.7025524Z` to `01:37:50.2742097Z`, observation
   runner exit 0 with **no automatic product PASS**. Real Quantity Insight/BQ
   failure recorded above. Graceful host exit, no force-close fallback, exact
   profile/current-pointer/inventory and preference restoration, unchanged
   installed DLLs/registration, zero remaining CAD. An incidental host Cut
   occurred during an attempted context-menu action; native Undo restored the
   solid before a 19.2 m3 precheck. Therefore right-click dispatch is unproven.

Observed profile inventory fingerprint before/after these cleanups:
`285c6889f88f326f165c9b539d83ed47555c03f8343d2826c7c6638a3462cf0c`.
This is a profile-inventory/current-pointer claim, not an unmeasured claim about
every global UI preference or every registry value.

All allocations are consumed. Do not replay them or overwrite their markers.

## Harness identity and evidence provenance

- Native probe now published at
  `tests/QS3D.LocalQualification.Raft/raft-continuation-probe.lsp`:
  `6CEEAF2278B6621AE60A5C8D3D04E0A4565B4007E430639D36D6F07BEF053DE3`.
  Its checked-in copy is byte-identical to the probe actually executed.
- Private rebuild wrapper:
  `61CA434498588CF139A1AC6F7F9A69BD4268D8B239074255217362CC40E574C4`.
- Private cold wrapper:
  `8F3E550C69F7C28733C7DA16C2CE28D5BB1958B827043698C8B22E53F43EA090`.
- Private observed-UI wrapper:
  `F3683312E1E50E46E964DFCCA9E124F4452B5C73B4E0B6C52D9E75D1B90FBD3B`.

Public sanitized execution records:
[rebuild admission](https://github.com/trinhtanphat/QS3D-BricsCAD/issues/4041#issuecomment-5594340178),
[rebuild/cleanup result](https://github.com/trinhtanphat/QS3D-BricsCAD/issues/4041#issuecomment-5594367493),
[cold result](https://github.com/trinhtanphat/QS3D-BricsCAD/issues/4041#issuecomment-5594380037),
[quantity UI admission](https://github.com/trinhtanphat/QS3D-BricsCAD/issues/4041#issuecomment-5594398536),
[source handoff](https://github.com/trinhtanphat/QS3D-BricsCAD/issues/6198#issuecomment-5594437181).

Raw DWG/QSDB, screenshots, local wrappers, private recovery metadata, process
dumps and proprietary binaries are not included in this change. Publishing the
probe does not claim the machine-specific launchers were published or qualified
as a portable harness.

## Next licensed work

Consume the existing #6198 correction and rerun the invalidated report/Quantity
Insight rows on an exact eligible fixed candidate. Continue strict schema,
top-level geometry, exact formwork/aggregate-detail parity, highlight, export and
full save/reopen acceptance under #4041. Keep #4041 open and its evidence PR draft
while this acceptance remains incomplete. Do not close parent #72/#4034 or infer
MCP completion from these results.
# 2026-09-09 continued Quantity Insight observation

The earlier evidence PR #6238 merged at
`51f5b60e85cd6c422660f94b665648f518518abb`; its implementation reservation was
released, not the incomplete licensed acceptance. Historical keep-draft wording
below describes that earlier checkpoint, not the current PR state. The sole
continued carrier is #4041 / PR #6280,
`agent/local021-review-20260909-a7810f3c/issue-4041-raft-quantity`.
It publishes `run-quantity-observation.ps1` and its host-free guard tests without
machine-specific paths or private fixture payloads.

## Consumed quantity-ui-06: NOT QUALIFIED, blank presentation

- Exact product source: `af6c585190efb80581e286add7027540e7cc7c52`, local unsigned
  post-merge package, **not published-release qualification**.
- Executed clean pushed harness: `7480328802a458e70f1842cb9cf48a5518daea56`.
- Run ID: `b9056b5190ee4a07b7adf2f4eac9b8f5`; V25.2.10 x64; fresh Default clone.
- Adapter SHA-256: `48f8ceb4c0456451b34c0b798f0ac255d0a9dd944711fc6751235e7f9c440c93`.
- Core SHA-256: `90e9d9dfc1a2fc80d64400401fdcbf454cecd2d53d71cac96b73a5391411f383`.
- Runner SHA-256: `20f31fc2c90711cbdfd22053384278d1d77b0d0fe3d87a175b8101f6ea960958`.
- Started `2026-09-09T07:04:57.6749653Z`; ended
  `2026-09-09T07:09:44.3898553Z` (under five minutes).

Actual runtime identity/baseline was verified and the native raft wireframe
was visible. Quantity Insight opened but its body remained blank after ordinary
maximize and float actions. The host responded to Escape and native commands.
Calling production `QS3D` displayed blank Workspace/Right interiors as well.
Production `QS3DBQ` opened its actual modeless window, but its raster stayed
white and supported accessibility returned null. No quantities, exact faces,
highlight, schema, top-level placement or export were qualified in this run.

There is a useful but non-isolating control: LOCAL022 allocation67 used the same
frozen product and Default-profile clone and visibly rendered BQ, including its
fresh-process cold result. It differed in drawing, loaded test observer and
startup sequence. This does **not** identify a source/GPU/driver/capture cause.
Do not rerun consumed06 unchanged or convert the other fixture's BQ PASS into
raft/Quantity Insight evidence.

The operator ended observation early. Final receipt is
`OBSERVATION_FINISHED_NOT_AGGREGATE_PASS`, with aggregate false and process exit
code zero for the observation runner, **not product PASS**. Owned-host forced
close fallback was required; graceful exit is false. Profile inventory/current
pointer, nonce removal, original autostart, installed payload/registration and
original fixture preservation were verified. Independent final process inspection
found zero BricsCAD hosts. Private copied fixture/receipts remain local.

Before this allocation, the new wrapper rejected concatenated fixture-path input
before allocating any host/profile/preferences. Parenthesized path expressions
and a replay of the exact AST expression fixed that harness-only error; focused
tests passed before the clean pushed execution above. It was not a product
failure or a consumed host allocation.

## Consumed quantity-ui-07: startup mode isolated; visual qualification still fails

This fresh allocation changed only native startup window mode from `Hidden` to
`Maximized`. Product/input hashes, Default-profile clone, startup script and
runtime checks match allocation06 above. It did not install or patch QS3D,
change rendering policy or test MCP/tunnels. The published runner keeps Hidden
as the default, allows only these two explicit modes, and records the choice.

- Clean pushed executed harness: `9daaa032af3ac8eb3cb6f68c734bfb55e897f0eb`.
- Run ID: `f9721f5cd0ae489e81ce0a3087fc790e`; licensed V25.2.10 x64.
- Runner SHA-256: `2fcae156452c710664b0b39a9b27349544c5a6ffd45c5d63dcb47633b4bc89ec`.
- Product source: `af6c585190efb80581e286add7027540e7cc7c52`; same adapter/Core
  hashes as06, not a published release.
- Input DWG SHA-256: `8c4f38bc72927527bcd49504300bde31c0c06db44e7c3f5c1a73c153a2bc4ebd`.
- Input QSDB SHA-256: `d4e719b46bc5c63faa00b10694ff06bd1d8e985215f38d3b2a311c50bf4fa884`.
- Started `2026-09-09T07:21:42.0145148Z`; ended
  `2026-09-09T07:26:10.6018902Z` (about four and a half minutes).

The native raft wireframe was visible and the exact runtime baseline passed.
Quantity Insight stayed grey blank; actual production `QS3DBQ` opened a white
modeless window. Initial maximized startup alone therefore did not resolve the
observed blank surfaces. No GPU/driver/capture/source cause is inferred.

Supported Computer Use accessibility exposed the real BQ controls and one
summary row: count1, gross19.2, deduction0, net19.2m3, formwork16m2. A real
`Tinh lai` click was followed by fresh row identities and unchanged values.
A real `Dien giai chi tiet` radio click changed the mode hint and name header
and yielded one detail row with the same values. These are bounded live
data/control observations only: the raster remained white. After a row-click
attempt, selection/explanation postconditions were not confirmed; Locate was
not invoked. No visible quantity, face/highlight, export, read-only project
digest, cold BQ, complete formwork matrix or aggregate PASS is claimed.

The BQ was closed, followed by native host close and an observed `No` on the
test-copy SaveChanges dialog. Terminal receipt is
`OBSERVATION_FINISHED_NOT_AGGREGATE_PASS`, aggregate false, graceful exit true,
forced fallback false. Profile inventory/current pointer, nonce removal,
original autostart bytes/time, protected installed payload/registration and
original fixture preservation all passed. Independent final host count was0.
Private copied fixture and raw receipts remain local, not deleted or committed.

Source diagnosis is returned to
[#6285](https://github.com/trinhtanphat/QS3D-BricsCAD/issues/6285), without a new
source reservation or an unsupported workaround. Preserve accepted LOCAL02267
visible warm/cold BQ evidence and the consumed05/06/07 boundaries separately.
Do not rerun07 unchanged while waiting for a meaningful corrected candidate or
newly justified diagnostic. #4041 and parent #72/#4034 remain incomplete.
