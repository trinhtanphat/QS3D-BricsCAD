# V25 Column Tie post-commit outcome

Carrier: #7110 / C03.

## Contract

`QS3DREBARTIES3D` captures PICKFIRST once, binds the existing project, and revalidates the active managed document plus native database generation before geometry mutation.

Pre-commit failures restore the captured `ProjectStateSnapshot`. If restore also fails, the operation and restore exceptions remain chained through an aggregate failure.

After `transaction.Commit()` succeeds, disposal of transaction/document-lock objects must not be presented as a failed mutation. A post-commit disposal fault returns the committed tie count with a bounded cleanup-warning discriminator.

UI publication remains ordered as model-tree refresh, Regen, palette status, then editor output, with native-generation revalidation around each boundary. Public text must not include raw exception messages or exception type names.

## Verification

Run `python scripts/preflight-v25-column-tie-postcommit-outcome.py`, `python scripts/preflight.py`, `git diff --check`, and the admitted locked-reference V25 compile. Licensed Teigha cleanup timing and visible BricsCAD UI behavior remain LOCAL_ONLY until executed in a licensed V25 session.