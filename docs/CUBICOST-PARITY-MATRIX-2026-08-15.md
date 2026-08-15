# Cubicost public feature inventory and QS3D parity map

Updated: 2026-08-15 (UTC+7)
Issue: #1611
Baseline: `1f10fba3faded5685cff0682216e2058665b4fe7`

## Scope and interpretation

This inventory records the **publicly documented Cubicost feature surface that can be verified from current Glodon product/solution/help pages and official project references**. It is intentionally not a claim that every private, region-specific, legacy, experimental, or edition-only button inside every Cubicost build has been enumerated.

The implementation strategy is clean-room product parity: reproduce useful workflows and domain capabilities from public behavior descriptions, not Cubicost source code, proprietary assets, file formats, or reverse engineering.

Status vocabulary:

- `EXISTING` — materially present in current QS3D source already.
- `PARTIAL` — QS3D has a related foundation but not the full advertised workflow.
- `NEW_CORE_1611` — host-neutral foundation added by issue #1611.
- `ADAPTER_NEXT` — requires BricsCAD-native extraction/command/UI wiring after the Core contract.
- `SEPARATE_SERVICE` — better owned by QS3D Platform/cloud rather than the BricsCAD plugin repository.
- `FORMAT_SCOPE` — requires a separately approved external-format/OCR/import implementation lane.

## 1. Cubicost suite / 5D lifecycle capabilities

| Public capability | QS3D status | QS3D direction |
|---|---|---|
| 3D BIM quantity takeoff | EXISTING | Keep semantic model + quantity engines |
| 4D time/schedule connection | PARTIAL | Add construction schedule/cost time-phasing in a dedicated lane |
| 5D quantity + cost control | PARTIAL | Extend current RateBook/estimate/revision surfaces with #1611 advanced cost contracts |
| Architecture/structure quantity workflow | EXISTING | Continue TAS-equivalent quantity workflows |
| Rebar quantity workflow | EXISTING | Continue TRB-equivalent rebar planners/schedules/cutting/procurement |
| MEP quantity workflow | NEW_CORE_1611 | Add BricsCAD extraction/recognition adapter next |
| Digital BQ / cost management | PARTIAL | #1611 adds buildup, benchmark, tender and claim foundations |
| Cross-discipline quantity/cost integration | PARTIAL | Use shared semantic IDs + unified reporting |
| Real-time quantity/cost update after model change | PARTIAL | Existing regeneration/revision; deepen linked estimate refresh |
| Project segmentation / construction zones | EXISTING | Reuse ProjectZoneService and zone-based reporting |
| Drawing revision management | EXISTING | Reuse revision compare/review infrastructure |
| Multi-disciplinary model coordination | PARTIAL | #1611 adds clash contract; native envelope extraction remains adapter work |
| Mainstream BIM interoperability | PARTIAL | Existing IFC/BCF/semantic interchange; broader IFC/RVT remains issue #84 / FORMAT_SCOPE |
| Third-party data compatibility | PARTIAL | Keep guarded interchange boundaries |
| Full lifecycle use from bidding through construction/closure | PARTIAL | Tender + progress foundations added; enterprise lifecycle remains multi-product |

## 2. TAS — architecture and structure quantity takeoff

Publicly documented/advertised capabilities:

1. BIM-based architecture and structure quantity takeoff.
2. Automatic modeling by identifying DWG drawings.
3. Automatic modeling by identifying PDF drawings.
4. Import IFC models for one-click/rapid modeling.
5. Import RVT/Revit models for one-click/rapid modeling.
6. Localized built-in measurement rules.
7. One-click quantity calculation.
8. BIM-based automatic deductions between intersecting/related elements.
9. Instant quantity recalculation after model changes.
10. Visible quantity information in the 3D model.
11. Trace quantity results back to model objects.
12. Inspect 3D deduction relationships.
13. Inspect calculation expressions.
14. Built-in quantity reports.
15. Custom report templates.
16. Share/reuse report templates.
17. Custom classification conditions.
18. Flexible quantity extraction/grouping.
19. Project segmentation.
20. Quantity tabulation by zone/segment.
21. Drawing revision/change management.
22. Quantity revision comparison/update.
23. Match information from diverse data sources to BIM model data.
24. Architecture/civil main works quantity workflows.
25. Structural works quantity workflows.
26. Steel-work quantity workflows (advertised on Glodon product listings).
27. Earthwork quantity workflows (advertised on Glodon product listings).
28. Finishes quantity workflows.
29. Precast/component quantity workflows (advertised on Glodon product listings).
30. PDF import scale/location workflows for takeoff.
31. Visual checking/validation of quantities against standardized measurement rules.

QS3D mapping:

- `EXISTING`: semantic architecture/structure elements, quantity rules, calculation settings, deduction gate/planner, measurement trace, quantity explanation, zones, revisions, reports/templates/export paths.
- `PARTIAL`: automated recognition breadth and construction-stage revision UX.
- `FORMAT_SCOPE`: direct RVT/native Revit ingestion and richer PDF recognition.
- `ADAPTER_NEXT`: broader DWG auto-recognition should feed the existing semantic model rather than duplicate the quantity engine.

## 3. TRB — rebar quantity takeoff

Publicly documented/advertised capabilities:

1. 3D BIM rebar modeling and quantity takeoff.
2. Drawing-recognition-assisted rebar modeling.
3. Rebar modeling from DWG/PDF/JPG/tracing workflows.
4. Built-in country/local measurement rules.
5. Built-in BS design-rule support.
6. Built-in ACI design-rule support.
7. Built-in Eurocode design-rule support.
8. Country-specific calculation nodes/rules.
9. Intelligent consideration of reinforcement relationships between structural elements.
10. Automatic rebar quantity calculation.
11. Automatic deductions/relationship-aware calculations where applicable.
12. Quantity recalculation after model changes.
13. Graphical/visual calculation settings.
14. Flexible classification of rebar quantities.
15. Custom quantity extraction/filtering.
16. Rich professional rebar reports.
17. Rebar schedule/report output.
18. Reinforcement checking.
19. Detection of missing/unassigned reinforcement.
20. Automatic location/navigation to reinforcement issues.
21. Beam reinforcement layout workflows.
22. Rebar data synchronization/update workflows.
23. Link rebar quantities to BQ/cost workflows.
24. Zone/segment-based rebar quantities.
25. 3D inspection/traceability for rebar calculation results.

QS3D mapping:

- `EXISTING`: beam longitudinal/stirrup, column ties, wall/slab mesh, shape/distribution planning, rebar schedule, stock demand, cutting optimization, procurement reporting, weight calculations and smoke coverage.
- `PARTIAL`: country-code presets and full visual reinforcement-check UX need dedicated rule packs/UI qualification.
- `ADAPTER_NEXT`: native 3D rebar review/navigation and broader drawing recognition.

## 4. TME/TMEC — MEP quantity takeoff

Publicly documented/advertised capabilities:

1. BIM-based mechanical/electrical/plumbing quantity takeoff.
2. Fast MEP identification.
3. Accurate MEP modeling.
4. Standardized built-in MEP calculation rules for BQ generation.
5. Instant identification of MEP devices/equipment for quantity extraction.
6. Intelligent air-duct identification.
7. Air-duct classification by system.
8. Air-duct classification by specification/size.
9. Flexible region-based quantification.
10. Multi-dimensional MEP quantity classification.
11. 3D MEP visualization.
12. Advanced clash detection.
13. Cross-discipline coordination support.
14. MEP quantity reporting for cost estimation.
15. Multi-disciplinary model integration for architecture/structure/rebar/MEP costing.

QS3D mapping:

- `NEW_CORE_1611`: `MepElement`, MEP system/specification/region/kind classification and deterministic count/length/area/volume aggregation.
- `NEW_CORE_1611`: host-neutral hard-clash and clearance-clash detection over adapter-provided geometry envelopes.
- `ADAPTER_NEXT`: BricsCAD entity recognition/extraction, native 3D clash highlighting/navigation and richer geometry-level clash precision.
- `PARTIAL`: calculation-rule authoring can reuse the existing QS3D quantity-rule infrastructure once MEP native entities are bound to semantic MEP elements.

## 5. TBQ — digital BQ, estimating and cost management

Publicly documented/advertised capabilities:

1. Central project cost data management.
2. BQ/item library.
3. Resource/build-up-rate library.
4. Reusable report-template library.
5. Historical BQ data accumulation.
6. Historical unit-rate data reuse.
7. Intelligent application of historical rates to similar BQ items.
8. Smart/batch unit-rate application with matching conditions and selectable ranges.
9. Linked resource/rate buildup.
10. Cost analysis by multiple dimensions.
11. Historical cost-data management.
12. Material/resource price-trend support.
13. Multi-dimensional cost benchmark analysis.
14. Role/permission management including read-only access.
15. Online/team collaboration.
16. Rapid tender BOQ compilation.
17. Standard/professional tender BOQ formats.
18. One-click standardized report generation.
19. PDF tender BQ identification/OCR.
20. Automatic formula/format generation from recognized tender BQ.
21. Intelligent price backfill.
22. Tender addendum handling.
23. Automatic marking of addendum changes.
24. Change-report generation.
25. Tender completeness checking.
26. Tender reasonability checking.
27. Import contractor tender documents for evaluation.
28. Automatic tender comparison.
29. Automatic marking of tender-version differences.
30. Tender evaluation/ranking support.
31. Multiple rounds of tender evaluation.
32. Modified-content visualization and review.
33. Link BQ data to BIM cost model.
34. Link TAS/TRB quantities into TBQ costing.
35. Synchronize quantity revisions into cost estimates.
36. Progress-claim workflows.
37. Predefined progress-claim formats.
38. Progress quantity/value traceability.
39. Monitor progress against budget/timeline.
40. Tender/quotation report printing/output.

QS3D mapping:

- `EXISTING`: RateBook, estimate lines, frozen estimate projections, revision cost impact, quantity/revision reports, XLSX/CSV export foundations.
- `NEW_CORE_1611`: resource-based `CostRateBuildUp` with direct cost, overhead, profit and computed unit rate.
- `NEW_CORE_1611`: `HistoricalCostCatalog` + multi-dimensional `CostBenchmarkService`.
- `NEW_CORE_1611`: tender requirement/bid comparison with completeness detection and deterministic complete-bid ranking.
- `NEW_CORE_1611`: progress-claim certification with contract-quantity cap, overclaim rejection, retention and net certified value.
- `FORMAT_SCOPE`: tender-PDF OCR, automatic inking/backfill and native PDF table recognition.
- `SEPARATE_SERVICE`: enterprise role/permission model, online multi-user tender collaboration and shared organization-wide cost database.

## 6. Cubicost Cloud / common data centre / collaboration

Publicly documented/advertised capabilities:

1. Integrated cloud collaboration.
2. Secure common data centre/project space.
3. Controlled project access.
4. View TAS models in the cloud.
5. View quantities in the cloud.
6. View calculation expressions/information in the cloud.
7. Model sharing.
8. Real-time comments.
9. Organize/manage cloud project data.
10. Remote collaboration/coordination among stakeholders.
11. TAS/TRB/TBQ collaborative interconnection.
12. Merge/reuse models.
13. Synchronize quantity and cost data.
14. Cloud-backed project decision support.

QS3D mapping: `SEPARATE_SERVICE`. These belong primarily in QS3D Platform/cloud APIs, with the BricsCAD plugin acting as a client. The Core interchange contracts can carry stable semantic data, but this repository must not become a multi-user server.

## 7. E-tender / subcontract inquiry

Publicly documented/advertised capabilities:

1. Electronic subcontract inquiry/tendering.
2. Reduce subcontract inquiry cost.
3. More efficient subcontract tendering.
4. More accurate subcontract tendering.
5. More secure subcontract tendering.
6. Online collaboration around tender/inquiry data.
7. Tender/build-up data accumulation and reuse.
8. Connection to upstream quantity/cost workflows.

QS3D mapping: `SEPARATE_SERVICE`. Reuse the #1611 tender/cost Core contracts where appropriate, but host supplier invitations, submissions, access control and online bid rounds in QS3D Platform rather than inside BricsCAD.

## 8. Cubicost Manager / product workspace

Publicly documented/advertised capabilities:

1. One-stop launch/workspace for Cubicost products.
2. Recent project access.
3. Search local project files.
4. Software download/install access.
5. Version-update notification.
6. Historical-version access.
7. Online learning/material access.
8. Project/work management.
9. Software-license/authorization management.
10. Utility/tool download access.
11. Local support/contact discovery.

QS3D mapping: `SEPARATE_SERVICE` / QS3D Platform desktop shell. Plugin-specific license/status UI may remain in adapter code, but product installation, updater, account/license portfolio and learning hub are platform concerns.

## 9. Clean-room implementation delivered in #1611

New source-safe foundations:

- `src/QS3D.Core/Mep/MepQuantity.cs`
  - semantic MEP kinds;
  - system/specification/region classification;
  - deterministic grouping;
  - device/item counts;
  - length/area/volume aggregation;
  - duplicate identity and finite-number guards.
- `src/QS3D.Core/Coordination/ClashDetection.cs`
  - host-neutral geometry envelopes;
  - hard clash detection;
  - configurable clearance clash detection;
  - discipline filtering;
  - deterministic result ordering.
- `src/QS3D.Core/Cost/AdvancedCostManagement.cs`
  - resource rate buildup;
  - overhead/profit composition;
  - historical cost catalog;
  - dimension-key cost benchmark statistics;
  - tender completeness + comparable-total ranking;
  - progress claim cap/rejection/retention/net certification.
- `tests/QS3D.Core.SmokeTests/CubicostParitySmoke.cs`
  - deterministic regression coverage for every new foundation above.

## 10. Remaining parity work by correct product boundary

### BricsCAD plugin adapter

- Recognize/bind real DWG MEP entities to `MepElement`.
- Extract accurate native envelopes/solids for coordination.
- Highlight, zoom and navigate clash results in BricsCAD.
- Add MEP/clash/cost panels and commands.
- Reuse existing quantity rules for discipline-specific MEP formulas.
- Feed cost/tender/progress results to existing report/XLSX infrastructure.

These require adapter implementation and later LOCAL_ONLY runtime/UI qualification where native BricsCAD behavior is involved.

### QS3D Platform/cloud

- Common data centre.
- Multi-user collaboration/comments.
- Organization/project RBAC.
- Enterprise cost/resource libraries.
- E-tender supplier portal and multi-round online tendering.
- Project/model cloud viewer.
- Cross-device synchronization.
- Cubicost-Manager-like launcher/updater/learning/license workspace.

### Separate approved format/AI lanes

- Native RVT/Revit import.
- Expanded IFC round-trip beyond issue #84's current boundaries.
- Tender-PDF OCR/table recognition/auto-inking.
- PDF/DWG/JPG intelligent recognition breadth comparable with dedicated Cubicost identification tools.

## Official public sources reviewed

- Glodon Global: `TAS & TRB` product page.
- Glodon Global: `TBQ` product page.
- Glodon Global: `5D BIM Digital Cost Management Solution`.
- Glodon Asia: Cubicost solution pages for TME and TBQ.
- Glodon product listings for TAS/TRB/TME/TBQ and E-tender.
- Glodon TRB product page and official help center.
- Glodon official case/project references describing zoning, revision linking, progress claims and model integration.
- Glodon Cubicost Manager product announcement.

Do not interpret this document as authorization to copy proprietary Cubicost implementation details, formats, branding or assets. QS3D parity remains an independent implementation based on public workflow requirements.
