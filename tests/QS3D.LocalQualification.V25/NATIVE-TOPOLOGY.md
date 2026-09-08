# LOCAL-022 native fixture topology

Native marker V3 adds mandatory `native_brep_topology`,
`saved_native_brep_topology`, `reopened_native_brep_topology` assertions to the
existing V2 section/ownership/volume/persistence matrix. Old V1/V2 markers cannot
satisfy V3. Physical UI defaults and acceptance do not change.

The independent fixture oracle describes the expected box or lower rectangular
prism plus two-axis tapered top as outward planar quadrilateral faces. It compares
the actual licensed native BREP's complete inventories, not volume/bounds alone:

- one complex and one exterior shell, with the complete expected face count;
- all expected unique vertices and straight edges, with no missing/extra/duplicate
  entries; native edge curves must be linear and contain both endpoint vertices;
- each expected face exactly once, one exterior four-edge loop per face, no holes;
- native plane position, outward orientation and native face area;
- every expected edge incident to two expected faces and Euler characteristic2.

The box fixtures require8 vertices/12 edges/6 faces; the two-axis taper fixtures
require12 vertices/20 edges/10 faces. Traversal order and individual edge direction
are irrelevant. The comparison runs on every initial/regenerated/saved/cold solid.
BREP/face/loop/edge/vertex/surface wrappers are disposed. No geometry is repaired,
rebuilt, appended or substituted by the verifier.

Scope is the explicit LOCAL-022 metre-scale fixtures. One-axis taper is rejected
as outside this fixture set, not silently mapped to a tested shape; arbitrary
customer solids, all dimensions, numeric extremes and every kernel pathology are
not qualified. Finite section checks remain separately required. Native execution
and cleanup must pass on fresh exact-SHA V25 first, then V26. Host-free tests use
independent coordinates/areas and reject malformed inventories; they validate the
oracle, not the proprietary adapter runtime. Both actual SDK builds are required.

References: [Bricsys BREP API](https://developer.bricsys.com/bricscad/help/en_US/V24/DevRef/source/html/e4ebad1b-c355-f015-caf1-005f521dd7a7.htm),
[native boundary loops](https://developer.bricsys.com/bricscad/help/en_US/V24/DevRef/source/html/c520ce81-e1cb-97fa-c432-5a3b54b18d62.htm),
[native curve linearity](https://developer.bricsys.com/bricscad/help/en_US/V24/DevRef/source/html/7199650b-670c-82fd-6c90-b841ceff24ad.htm).
