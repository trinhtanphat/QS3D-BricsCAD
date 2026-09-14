# Trimble construction lifecycle acceptance

## Acceptance scope

The P2 Trimble construction-lifecycle lane is accepted only when protected `main` retains the complete post-procurement chain without host-specific dependencies in Core:

1. approved supplier/vendor governance before commitment and purchase-order execution;
2. subcontract commitment value including approved variation;
3. purchase-order ordered/delivered/remaining value and cancelled-order exclusion;
4. package-level procurement progress through ordered/commitment and delivered/ordered ratios;
5. actual-vs-commitment cost, commitment variance and cost-to-complete;
6. planned-vs-actual field progress and schedule variance;
7. deterministic package project-control aggregation;
8. stable versioned ERP/project-controls export with project/revision evidence supplied by the host boundary;
9. fail-closed behavior for unapproved suppliers, invalid delivery values and undefined non-zero/zero-denominator ratios.

## Evidence on main

Production authority is split intentionally between `QsTrimbleConstructionLifecycle.cs` and `QsTrimbleProjectControlsInterop.cs`. `QsTrimbleConstructionLifecycleSmoke` exercises supplier governance, variations, cancelled PO exclusion, delivered value, actual cost, commitment remaining, schedule variance, deterministic package ordering, procurement/cost ratios, stable CSV shape and fail-closed invalid ratios.

`scripts/preflight-trimble-construction-lifecycle.py` is the integration regression oracle. It ensures these production, smoke and documentation contracts stay discoverable by Shared CI without duplicating the implementation or adding BricsCAD/AutoCAD/WPF/WinForms dependencies to `QS3D.Core`.

## Six-benchmark convergence rule

This document is Trimble evidence only. The overall parity epic must remain open until CostX, Cubicost, Autodesk Takeoff/Forma, Solibri, Trimble and QuantBIM all have current-main acceptance evidence and their active carriers have terminal exact-head qualification. A GREEN Trimble lane does not authorize closing sibling benchmark gaps.
