from pathlib import Path

WORKFLOW = Path('.github/workflows/release-v25-cloud.yml')
text = WORKFLOW.read_text(encoding='utf-8')

patch_marker = "$publishedRelease = Invoke-RestMethod -Method Patch -Uri $releaseUri"
patch_at = text.find(patch_marker)
if patch_at < 0:
    raise SystemExit('release workflow must publish through the expected final PATCH boundary')

after = text[patch_at:]
required = {
    'published draft state': "$publishedRelease.draft -ne $false",
    'published prerelease state': "$publishedRelease.prerelease -ne $true",
    'published tag identity': "$publishedRelease.tag_name",
    'published target identity': "$publishedRelease.target_commitish",
    'published asset set': "$publishedRelease.assets",
    'verified asset ids': "$verifiedReleaseAssetIds",
}
missing = [name for name, marker in required.items() if marker not in after]
if missing:
    raise SystemExit('final publication response is not rebound to verified release identity: ' + ', '.join(missing))

if 'Compare-Object' not in after:
    raise SystemExit('final publication must compare published asset identity against the verified asset IDs')

print('PASS: final V25 publication response remains bound to verified tag, target, prerelease state, and asset identity')
