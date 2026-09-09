#!/usr/bin/env python3
"""Regression guard for malformed and mutable PR-edited workflow evidence."""

from __future__ import annotations

import importlib.util
import io
import json
import os
from pathlib import Path
from urllib.parse import parse_qs, urlsplit

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts" / "preflight-ci-edited-evidence.py"


class _Response:
    def __init__(self, payload: object) -> None:
        self._stream = io.StringIO(json.dumps(payload))

    def __enter__(self):
        return self._stream

    def __exit__(self, exc_type, exc, tb) -> bool:
        self._stream.close()
        return False


def _load_module():
    spec = importlib.util.spec_from_file_location("qs3d_ci_edited_evidence_shape", SOURCE)
    if spec is None or spec.loader is None:
        raise RuntimeError("could not load edited-evidence verifier")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def _page(url: str) -> int:
    values = parse_qs(urlsplit(url).query).get("page", [])
    if len(values) != 1:
        raise RuntimeError(f"evidence query must contain one page parameter: {url}")
    return int(values[0])


def main() -> int:
    module = _load_module()
    valid = {
        "id": 100,
        "head_sha": "a" * 40,
        "status": "completed",
        "conclusion": "success",
        "name": module.WORKFLOW_NAME,
        "event": "pull_request",
        "pull_requests": [{
            "number": 123,
            "head": {"sha": "a" * 40},
            "base": {"ref": "main", "sha": "b" * 40},
        }],
    }

    def malformed_urlopen(request, timeout=20):
        del timeout
        page = _page(request.full_url)
        if page != 1:
            raise RuntimeError(f"malformed fixture unexpectedly requested page {page}")
        return _Response({"workflow_runs": [valid, "malformed-entry"]})

    module.urlopen = malformed_urlopen
    try:
        module.fetch_prior_runs("trinhtanphat/QS3D-BricsCAD", "token", "a" * 40)
    except RuntimeError as exc:
        text = str(exc)
        if "page 1" not in text or "index 1" not in text or "non-object" not in text:
            print("ERROR: malformed evidence failed closed without deterministic page/index diagnostics:", text)
            return 1
    else:
        print("ERROR: malformed workflow_runs member was silently accepted")
        return 1

    calls: list[int] = []

    def paged_urlopen(request, timeout=20):
        del timeout
        page = _page(request.full_url)
        calls.append(page)
        if page == 1:
            return _Response({"workflow_runs": [{"id": index} for index in range(100)]})
        if page == 2:
            return _Response({"workflow_runs": [{"id": 100}]})
        raise RuntimeError(f"valid fixture unexpectedly requested page {page}")

    module.urlopen = paged_urlopen
    runs = module.fetch_prior_runs("trinhtanphat/QS3D-BricsCAD", "token", "a" * 40)
    if len(runs) != 101 or calls != [1, 2]:
        print(f"ERROR: valid 100+1 evidence pagination regressed: count={len(runs)} calls={calls}")
        return 1

    # GitHub's workflow-run REST object exposes immutable run.head_sha, but its
    # nested pull_requests head/base records drift with the live PR. A valid-
    # looking nested exact base therefore must not let edited-event runtime skip
    # source/build validation. Exercise verify_runtime, not just the helper, so
    # a future optimization cannot accidentally restore mutable-snapshot trust.
    emitted: list[bool] = []
    original_env = os.environ.copy()
    original_fetch = module.fetch_prior_runs
    original_emit = module.emit_reuse_output
    try:
        os.environ.update({
            "GITHUB_REPOSITORY": "trinhtanphat/QS3D-BricsCAD",
            "GITHUB_TOKEN": "token",
            "QS3D_EXPECTED_HEAD_SHA": "a" * 40,
            "QS3D_EXPECTED_BASE_SHA": "b" * 40,
            "QS3D_EXPECTED_BASE_REF": "main",
            "QS3D_EXPECTED_PR_NUMBER": "123",
            "GITHUB_RUN_ID": "200",
        })
        module.fetch_prior_runs = lambda repository, token, expected_sha: [valid]
        module.emit_reuse_output = emitted.append
        module.verify_runtime()
    finally:
        module.fetch_prior_runs = original_fetch
        module.emit_reuse_output = original_emit
        os.environ.clear()
        os.environ.update(original_env)

    if emitted != [False]:
        print(
            "ERROR: edited-event runtime trusted mutable nested PR head/base evidence; "
            f"reuse outputs={emitted}"
        )
        return 1

    print("PASS: PR-edited evidence rejects malformed members and never reuses mutable nested PR base metadata")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
