# Agent Execution and CI Continuation Rule

This document defines a mandatory execution rule for every AI/automation agent working in this repository.

## Owner intent

When the repository owner asks an agent to `continue`, `continue all`, `fix bug`, `update code`, `fix CI`, `commit push git`, or otherwise continue an already-defined lane, the agent is expected to **do the work**, not merely report the current state.

A status message is useful only as a progress update. It is never a substitute for the next safe repository action when the agent has tools and authorization to perform that action.

## No status-only loop

An agent must not repeatedly stop at messages such as:

- CI is still running;
- CI failed;
- preflight is red;
- Core/V25 was skipped;
- the PR is conflicted;
- the branch moved;
- another agent pushed a commit;
- a required check is pending.

If there is a safe actionable next step, the agent must take it in the same working session.

## Mandatory CI autofix loop

For an assigned remote-safe lane, a CI failure is an instruction to continue debugging, not a stopping point.

The agent must run this loop:

```text
refresh exact live PR/branch/main state
  -> identify exact current branch SHA
  -> inspect exact failed workflow/run/job/step/log
  -> determine the smallest evidence-backed fix
  -> update source/tests/scripts/docs on the authorized task branch
  -> validate what can be validated with available tools
  -> commit and push the fix
  -> observe the fresh CI run for the exact new SHA
  -> if CI fails, read the new exact failure and repeat
  -> if CI succeeds, verify every required check on the exact current SHA
  -> only then move to the lane's authorized merge/handoff step
```

An agent must **not wait for the owner to say `continue` again after a CI failure** when the lane is already assigned and the failure is within the agent's executable scope.

## Do not stop before checking the result

After pushing a CI-relevant fix, the agent must inspect the resulting exact-head CI outcome before declaring the work complete or stopping the lane.

The following are not completion evidence:

- “commit pushed” without checking the resulting CI;
- “CI started” without checking its eventual result;
- an older green run from a different SHA;
- a cancelled/superseded run;
- a green preflight while required Core/V25 stages are still pending;
- a green branch run when protected-main policy requires a fresh PR/integration run on a newer merge candidate.

Completion requires fresh evidence on the exact current candidate required by repository policy.

## When CI is still running

Do not invent a fix before a failure exists. However, do not become idle or return a status-only response if useful non-conflicting work remains. While an exact-head run is in progress, the agent may:

- inspect the current diff for likely regressions;
- review adjacent source guards/tests/contracts;
- check concurrency and current `main` movement;
- prepare non-conflicting documentation or requested policy updates;
- inspect previous exact failure logs to verify the applied fix covers the root cause.

As soon as the current run completes, inspect it and act on any failure immediately.

## Concurrent-agent safety

Before every write, refresh the live branch head. If another agent moved the same branch:

1. do not overwrite or force-push;
2. inspect the new commits/diff;
3. preserve valid concurrent work;
4. continue from the new live lineage;
5. obtain fresh CI for the exact resulting SHA.

A superseded or cancelled CI run is not a failure of the code and must not be treated as merge evidence.

## Real stopping conditions

An agent may stop only when at least one of these is true:

- the exact required CI candidate is fully green and the lane has reached its authorized stopping point;
- a repository policy requires owner authorization for the next action, such as an unauthorized merge to `main`;
- the remaining work is genuinely `LOCAL_ONLY` or requires unavailable licensed/private hardware/runtime/resources;
- a tool/permission failure prevents the required operation and no safe alternative available to the agent can progress the lane;
- a concurrent agent has already completed the same fix and the agent has verified that duplicate work is unnecessary.

When a real blocker exists, report the exact blocker and evidence. Do not call ordinary CI failure or pending CI a blocker when the agent can continue the loop above.

## Merge safety remains unchanged

This execution-first rule does not weaken repository safety. Agents must still obey all existing rules, including:

- never force-push `main`;
- never bypass branch protection;
- never silently discard another agent's work;
- never merge red or stale CI;
- never claim success from a different/stale SHA;
- never merge to `main` without the owner authorization required by `AGENTS.md` and `docs/MAIN-WRITE-AUTHORIZATION.md`.

The required behavior is therefore: **act continuously within authorization, auto-fix CI failures until the exact required candidate is green, verify success, and only then stop at the legitimate policy boundary.**
