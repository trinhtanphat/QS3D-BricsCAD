# Work claim — drawing identity scalar revision ordering

- Status: `BLOCKED_SOURCE_WRITE`
- Agent: `chatgpt-gpt56sol-drawing-identity-scalar-revision-20260813`
- Registered: `2026-08-13T20:16:00+07:00`
- Baseline main SHA: `e51d19df145f576b9f3f2e12a68d01fa926076c4`
- Claim commit: `c2d6f65c644786a9e86741f3c408b2f9a14cb186`
- Priority: P0 persisted drawing-identity revision/atomicity regression.

## Confirmed defect

The drawing-identity coordinator still contains explicit revision touches from the older mutation contract. Persisted drawing scalars now own their own revision advances, so those old touches cause redundant revision changes. Legacy adoption also needs capacity validation for all changed project scalars before the first mutation.

## Reserved scope

- `src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs`
- `scripts/preflight-project-context-drawing-identity-touch-order.py`
- this claim file

## Intended bounded change

- remove obsolete explicit revision ownership from path/fingerprint synchronization;
- preserve path-only synchronization as one scalar-owned revision advance;
- validate capacity for every changed project identity scalar before legacy adoption starts mutating state;
- preserve element-snapshot validation and existing element fingerprint adoption;
- update the focused static preflight to the scalar-owned revision contract.

## Evidence

- older touch-order source fix: `74b7c4e3c9fcc3def7d3f4f32436f887aa8eb6be`;
- older touch-order gate: `d4cadaae9da056b8b32dd96da70181daea69346b`;
- later persisted-scalar revision ownership: `0c7dbe5612c0db6e5252f3ec1db6385b06771e0e`;
- current scalar regression requires each changed persisted scalar to advance exactly once: `2ed57a33f390e89e623684483b431724708a373b`.

## Blocker

The attempted exact-SHA production-file update was rejected by the platform before a commit was created. The source file remains unchanged. No source fix, test PASS, GitHub Actions run, or BricsCAD runtime PASS is claimed for this lane.

## Completion condition

Refresh `main`, recheck ownership, apply the bounded source change plus focused static-gate update, verify exact remote diff/readback, then change this claim to `COMPLETED`.
