from pathlib import Path

source = Path('src/QS3D.BricsCAD.V25/McpCadViewStatusRuntime.cs').read_text(encoding='utf-8')

def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit('FAIL: ' + message)

ensure_start = source.find('private static void EnsureCommandTracking(Document')
ensure_end = source.find('private static CommandLifecycleSnapshot CommandSnapshot', ensure_start)
require(ensure_start >= 0 and ensure_end > ensure_start, 'EnsureCommandTracking block not found')
ensure = source[ensure_start:ensure_end]
require('CommandTrackers[document] = tracker;' in ensure,
        'tracker ownership must be published before native subscription begins')
require(ensure.find('CommandTrackers[document] = tracker;') < ensure.find('tracker.Subscribe();'),
        'authoritative tracker ownership must precede fallible Subscribe')
require('tracker.DetachBestEffort()' in ensure,
        'partial subscribe failure must attempt bounded retry-aware detach')
require('tracker.IsFullyDetached' in ensure,
        'registry removal must be conditioned on proven full detach')

start = source.find('private sealed class CommandTracker : IDisposable')
end = source.find('\n        }\n    }', start)
require(start >= 0 and end > start, 'CommandTracker block not found')
block = source[start:end]
for token in (
    '_willStartMayBeSubscribed', '_endedMayBeSubscribed',
    '_cancelledMayBeSubscribed', '_failedMayBeSubscribed',
    'AcceptCallbacks', 'DetachBestEffort()', 'IsFullyDetached'):
    require(token in block, 'missing conservative command subscription ownership token: ' + token)

for flag, add in (
    ('_willStartMayBeSubscribed = true', '_document.CommandWillStart += _willStart'),
    ('_endedMayBeSubscribed = true', '_document.CommandEnded += _ended'),
    ('_cancelledMayBeSubscribed = true', '_document.CommandCancelled += _cancelled'),
    ('_failedMayBeSubscribed = true', '_document.CommandFailed += _failed')):
    require(block.find(flag) >= 0 and block.find(flag) < block.find(add),
            'may-be-subscribed ownership must be published before fallible native add: ' + add)

require('if (!tracker.AcceptCallbacks) return;' in source,
        'stale/partial command callbacks must fail closed against authoritative tracker state')
require('CommandTrackers.Remove(removable)' in ensure,
        'bounded tracker eviction must remain supported')
require(ensure.find('CommandTrackers.Remove(removable)') > ensure.find('IsFullyDetached'),
        'tracker eviction must remove ownership only after detach is proven')
for phase in (b'"start"', b'"end"', b'"cancelled"', b'"failed"'):
    require(phase.decode('ascii') in block, 'command lifecycle phase must remain a C# string literal: ' + phase.decode('ascii'))

require('string.Equals(phase, "start", StringComparison.Ordinal)' in block,
        'command start comparison must retain its C# string literal')
require('twistRadians must be between -2π and 2π.' in source,
        'view twist diagnostic must preserve the UTF-8 pi literal')

print('PASS: MCP command tracker retains native event ownership across partial attach/detach failure')
