# Claim coordination note

The Curtain Wall division-count lane is owned by the adjacent active claim. Before source writes, current `main` and overlapping claims must be rechecked; if an earlier conflicting owner is found, this lane will be closed without touching source.
