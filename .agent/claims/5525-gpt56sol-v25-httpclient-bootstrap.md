# Agent reservation — issue #5525

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-20260903-v25-httpclient-bootstrap
Canonical carrier: agent/gpt56sol-20260903-v25-httpclient-bootstrap/issue-5525
Lane-Key: issue-5525
Ownership-Key: release.v25.held-asset.httpclient-bootstrap
Branch: agent/gpt56sol-20260903-v25-httpclient-bootstrap/issue-5525
Expected-Paths: scripts/upload-v25-held-release-asset.ps1; scripts/preflight-v25-cloud-held-release-assets.py; .agent/claims/5525-gpt56sol-v25-httpclient-bootstrap.md

Scope: guard and fix the Windows PowerShell System.Net.Http assembly bootstrap required by the V25 single-stream held-release uploader, without weakening held-asset identity/TOCTOU protections.
