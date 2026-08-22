# Owner reference PDFs — provenance and implementation coverage

Date audited: 2026-08-14 (Asia/Ho_Chi_Minh)

This directory records the owner-supplied PDF set used for the QS3D-BricsCAD completion audit. The ChatGPT file Library currently exposes four of the five named source PDFs as raw binary files. Their source identities are preserved below by exact file name, byte size, and SHA-256 so a future binary import can be verified without guessing or silently substituting another document.

The connected GitHub mutation surface available during this audit accepts UTF-8 contents but does not expose a local-binary/file upload parameter. A temporary base64 reconstruction route was evaluated, but connector output truncates sufficiently large payloads and would risk corrupting the PDF. For that reason no fake, truncated, or byte-uncertain `.pdf` is committed here. Source code and this provenance manifest are committed normally.

## Recovered source PDFs

| Source file | Original bytes | Original SHA-256 | Audit / implementation status |
|---|---:|---|---|
| `Bao_cao_Dien_giai_khoi_luong_QS3D_BricsCAD(1).pdf` | 209,905 | `602fae30c063cd4f1e27e4c3c25010f7af3355a79f125c17a0044951887ab1c9` | Quantity explanation/exact-geometry requirements audited; implementation already exists on `main` and was retained. |
| `Mo_ta_chuc_nang_lenh_SE_BricsCAD.pdf` | 4,174,337 | `d48fb4be25882492546a9936bcb8f7eb980331a79e226023151da77f87b3cdd2` | Generic closed-profile `SE` audited and completed as all-or-nothing batch source workflow; see `e860b38b171edf284d7b6e457311ef8be6eabcc8` and guard `2ac289098c73e9873d466349701f1d6264c589d7`. |
| `so_sanh_bricscad_vs_autocad.pdf` | 54,931 | `e1614d8756b513b2a5ad4708818458e5b68a810ee96f1406ab8380b66678b79b` | Used as product/reference context; no unsafe AutoCAD-specific dependency was introduced by this completion lane. |
| `Yeu_cau_build_tool_QS3D_BricsCAD.pdf` | 148,915 | `2a06da1f9609aa63b72bcc13e672caf0753d498ba221dc5f8387c94ddc348dc7` | Build/tool/panel/Add/Delete/property/direct-draw requirements audited. Remaining Family Manager ergonomics were completed in the QS quick workflow commits listed below. |

## Missing source binary

`TEST QS3D.pdf` was named in the owner session and its test evidence was used earlier in the audit, but an exact raw binary with that title is **not currently retrievable** from either the conversation file surface or the persistent Library. Search on 2026-08-14 also did not find a clearly equivalent renamed PDF. The repository therefore intentionally does not fabricate a replacement.

The smoke runner issue derived from that test evidence is nevertheless tracked in:

- `docs/agent-work-claims/2026-08-14-0924-chatgpt-web-gpt56sol-smoke-runner-top-level-failure.md`

Its source containment is fixed; exact fresh Windows/.NET execution remains an evidence requirement rather than being inferred.

## Family Manager / sheet completion added by this audit

The owner-reference sheet/PDF audit exposed a usability gap in Family authoring. It is now covered by:

- `e07c6d0655b59aa6c89672bb60eec23b13815b0b` — claim/reservation
- `cea7a4f66e04059b5d2bb18c2e66dfabe8e52b7d` — category-aware QS form in Family Manager
- `a9d7c4438b5bee468261006989ec12cc30199e5e` — `Auto Family`, `Tạo & sử dụng`, `Lưu & Vẽ`, atomic Family mutation/activation and canonical `QS3DDRAWACTIVE` handoff
- `e1c7b48d0e57801796a5555266adccb150b1c75c` — focused static regression guard
- `d15271a3b1f46aacaf1dcd2ee81dc35f93b8901e` — source claim closeout

Canonical QS fields intentionally reuse the existing Direct Draw schema: `WidthM`, `DepthM`, `HeightM`, `ThicknessM`, `BottomOffsetM`.

## Other completion commits from the same owner-reference audit

- `d0cdc8113c101725b316dacd1eceaf727e665348` — contain registered-smoke failures directly in `Program.Main()`
- `69a00926c952672adb73d8ed384b19d31ba5b0e1` — smoke runner containment preflight
- `e860b38b171edf284d7b6e457311ef8be6eabcc8` — atomic `SE` closed-profile batch
- `2ac289098c73e9873d466349701f1d6264c589d7` — atomic `SE` preflight
- `6d230dd721aa075433299bc945c279f44d377d7a` — host `QS3DSETUP` with the BricsCAD modal-window API and contained nested diagnostics
- `15a48b5898e932302b93af39df9a17accb9f9f80` — `QS3DSETUP` host preflight
- `d3848568d4367aebc1bebd0d869367bd804c2a6a` — close stale `SE` ACTIVE claim against the final atomic contract
- `90f4a949f653bcd89df329d20a1424a313816a14` — record the latest smoke containment hardening while retaining `PENDING_FRESH_SMOKE`

## Evidence boundary

“Source completed” means the identified remote-safe code/documentation gap is implemented and pushed to `main`. It is not a substitute for licensed BricsCAD V25 native interaction evidence. WPF click/render acceptance, native Solid3d behavior, and fresh exact-SHA Windows smoke execution remain local/runtime evidence where their corresponding claim says `PENDING_LOCAL_*` or `PENDING_FRESH_SMOKE`.
