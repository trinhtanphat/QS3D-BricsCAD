# REB-01A claim
Status: ACTIVE
Agent: gpt56sol-rebar-procurement-model-20260814-0800
Baseline: a761e9c88d7df029b4c8a5c61bb6b30ba92d1e19
Scope: src/QS3D.Core/Rebar/RebarStockDemand.cs; tests/QS3D.Core.SmokeTests/RebarStockDemandSmoke.cs; tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs.
Goal: add a canonical validated rebar bar-length procurement demand model that keeps required length, allowance, kerf, procurement and off-cut quantities explicit and separate from BBS presentation; add deterministic managed smoke regressions. REB-02 cutting optimisation and report/host projection are out of scope.
