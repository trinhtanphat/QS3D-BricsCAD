# #3681 local V25 live-BREP wall-contact result

- Status: `LOCAL_FAIL / SOURCE_FIX_REQUIRED`
- Local qualification issue: #3681
- Local execution carrier: #3679
- Source defect: #3687
- Tested exact Git SHA: `0062e0cd73a570a7ca774dfa8b3ff91e8df20f31`
- BricsCAD host: V25.2.10, Windows x64, licensed native runtime
- Plugin ProductVersion: `0.1.0-preview.10081`
- Plugin SHA-256: `1CDB3F649B6E1ACD5035BADFD36A83D9D01FFBA219608BD0A60CA53D91BD1997`
- Core ProductVersion: `0.1.0-preview.10081`
- Core SHA-256: `5ABFE8EC7EEC5EF7D6280CAAE378CE1F8AA5B16ECF5468E9230BEBF947FA9AC4`

## Gates executed

- exact clean carrier and pinned submodule: PASS;
- focused StructuralWall contact preflights: PASS;
- aggregate feature preflight: PASS, 1003 gates;
- Core Release build and deterministic smoke: PASS, zero warnings/errors and `ALL PASS`;
- V25 adapter `Release|x64` against the installed V25 references: PASS, zero warnings/errors;
- offline WPF theme/workspace/right-panel smoke: PASS;
- exact-candidate V25 `NETLOAD` runtime identity: PASS;
- Git-ignored managed BREP surface-type probe on production-generated solids: PASS;
- temporary OnStartup DemandLoad isolation: PASS, guarded `2 -> 4 -> 2`, installed Loader and payload hash unchanged;
- test-owned BricsCAD process cleanup after each completed session: PASS.

## Sanitized runtime result

The production baseline used `QS3DDRAWSTRUCTWALLADV` with length `1.468 m`, thickness `0.2 m` and height `0.8 m`.

| Scenario | Expected | Actual | Result |
|---|---:|---:|---|
| no concrete neighbor, gross formwork | `2.6688 m2` | `2.6688000000000054 m2` | PASS |
| no concrete neighbor, contact deduction | `0 m2` | `0 m2` | PASS |
| one full vertical end-face, captured with production `QS3DCOLUMN` | `0.1600 m2` | `0 m2` | FAIL |
| one full vertical end-face, net formwork | `2.5088 m2` | `2.6688000000000054 m2` | FAIL |
| same live neighbor penetrating `0.0001 m`, then recaptured | `0.1600 m2` | `0 m2` | FAIL |
| same live neighbor penetrating `0.05 m`, then recaptured | `0.1600 m2` | `0 m2` | FAIL |
| production Direct Draw Column with a live generated-solid owner, then recaptured | `0.1600 m2` | `0 m2` | FAIL |
| independent native intersection control for `0.05 m` overlap | `0.008 m3` | `0.008 m3` | PASS |
| disposable native wall-face topology | 6 faces with areas `0.16 x2`, `0.2936 x2`, `1.1744 x2` | exact expected multiset | PASS |
| managed surface classification on all 12 wall/Column faces | readable planar surfaces | `12/12 ExternalBoundedSurface`, `0/12` direct `PlanarEntity`, `12/12` planar `BaseSurface` | ROOT CAUSE |

Live bounding boxes confirmed the intended wall/neighbor placement, and the semantic Column capture completed. A second reproduction created the neighbor entirely through `QS3DDRAWCOLUMNADV`; both the wall and Column then had live generated-solid owner slots with exactly touching native bounds. Recapturing that Column source still published zero contact, ruling out the direct-Solid3d source fallback as the sole cause. The independent native intersection proves that the two disposable solids overlap and that the V25 boolean kernel can calculate their intersection. Exploding a disposable copy of the generated wall also returned all six expected native face areas, ruling out missing or malformed input face topology.

The exact runtime cause was then isolated with a temporary Git-ignored managed probe loaded beside the frozen candidate. Every one of the 12 production-generated wall/Column faces returned `Teigha.Geometry.ExternalBoundedSurface`; none cast directly to `PlanarEntity`. Each wrapper reported `IsPlane=true`, and every `BaseSurface` was `Teigha.Geometry.Plane` and therefore planar. `StructuralWallConcreteContactService.ReadVerticalFaces()` currently continues past any face whose direct `face.Surface as PlanarEntity` cast is null, so V25 deterministically creates zero vertical seeds before boolean subtraction. The residual-face path uses the same direct-cast assumption. This exactly explains why production refresh publishes `ConcreteContactAreaM2=0` despite valid geometry, candidate ownership, native topology and native intersection.

The first required full-contact case is therefore a real runtime failure. The two-end BLT control, partial contact, multi-neighbor union, top/bottom exclusion, stale-BREP clearing, full save/reopen, Undo/Redo and second-DWG matrix were not promoted to PASS or continued after this blocking source failure.

## Handoff

Issue #3687 contains the sanitized source-defect reproduction and exact managed-wrapper root cause. The local lane did not patch production source. A source agent must safely unwrap planar `ExternalBoundedSurface.BaseSurface` instances (including wrapper lifetime/disposal), cover both seed and residual classification with a deterministic regression/probe, and publish a new exact SHA; #3681 then reruns the complete licensed V25 matrix against that SHA.

Raw scripts, disposable DWGs, sidecars, runtime markers and native diagnostic output remain Git-ignored. No proprietary binary, customer drawing, local path, ProjectId or CAD Handle is included here.
