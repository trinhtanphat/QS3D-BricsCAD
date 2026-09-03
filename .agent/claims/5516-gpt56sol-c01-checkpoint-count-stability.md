# Agent reservation — issue #5516

Status: ACTIVE
Reservation-Protocol: v2
Canonical owner/session: account:trinhtanphat|session:gpt56sol-c01-20260903-checkpoint-count-stability
Canonical carrier: agent/gpt56sol-c01-20260903/issue-5516-checkpoint-count-stability
Lane-Key: issue-5516
Ownership-Key: core.persistence.checkpoint-count-stability
Branch: agent/gpt56sol-c01-20260903/issue-5516-checkpoint-count-stability
Expected-Paths: src/QS3D.Core/Persistence/ProjectPersistenceCheckpoint.cs; tests/QS3D.Core.SmokeTests/ProjectPersistenceCheckpointCountStabilitySmoke.cs; .agent/claims/5516-gpt56sol-c01-checkpoint-count-stability.md

Scope: make persistence checkpoint capture fail closed when caller-controlled known Count evidence changes after enumeration while preserving bounded one-pass behavior for unknown-count streams, duplicate/missing-element validation, and the 100,000-element ceiling.