import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';
import { decodeObservedRequest, openObservedAllocation } from './local022-observed-input.mjs';

const runId = '0123456789abcdef0123456789abcdef';
const value = { schema: 'QS3D_LOCAL022_UI_ACTION_V2', run_id: runId, sequence: 1,
  action: 'click', x: 94, y: 471, text: '', target_pid: 12345, stage: 'SelectTree' };
const raw = JSON.stringify(value);
if (process.argv.includes('--producer')) {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  const requests = JSON.parse(Buffer.concat(chunks).toString('utf8').trim());
  assert.equal(requests.length, 21);
  for (const request of requests)
    assert.equal(decodeObservedRequest(JSON.stringify(request), runId, 1, request.target_pid).stage, request.stage);
  console.log('PASS: all 21 actual C# serializer stage/action/value records are accepted by the JS consumer.');
}
assert.equal(decodeObservedRequest(raw, runId, 1, 12345).stage, 'SelectTree');
for (const patch of [
  { schema: 'QS3D_LOCAL022_UI_ACTION_V1' }, { run_id: runId.toUpperCase() },
  { sequence: 0 }, { sequence: 2 }, { target_pid: 0 }, { target_pid: 12346 },
  { action: 'move' }, { action: 'drag' }, { x: 1.5 }, { x: 32768 }, { y: -32769 },
  { text: '1' }, { action: 'text', text: 'arbitrary command' },
  { action: 'key', text: 'CTRL+A' }, { stage: 'UnknownStage' }, { stage: 'SelectTree\u00ad' },
  { stage: 'CancelDialog', action: 'click', text: '' },
  { stage: 'InputL1', action: 'text', text: '2' }, { stage: 'InputH1', action: 'text', text: '1' },
]) assert.throws(() => decodeObservedRequest(JSON.stringify({ ...value, ...patch }), runId, 1, 12345));
for (const bad of [raw.replace('"x":94', '"x":94,"x":95'),
  raw.replace('"x":94', '"x":94,"\\u0078":95'),
  raw.replace('"x":94', '"\\u0078":94'), raw + '\n', ' ' + raw,
  JSON.stringify({ ...value, extra: true }), raw.replace('94', '9.4e1')])
  assert.throws(() => decodeObservedRequest(bad, runId, 1, 12345));
for (const [action, text, stage] of [['text', '1000', 'EditH2'], ['key', 'ENTER', 'EndFirstDraw'], ['key', 'ESC', 'EndSecondDraw'], ['key', 'ESC', 'CancelDialog']])
  assert.equal(decodeObservedRequest(JSON.stringify({ ...value, action, text, stage }), runId, 1, 12345).text, text);
console.log('PASS: observed V2 decoder accepts only exact canonical stage/action/nonce/PID/sequence; no hover, ambiguous keys or unrestricted input.');

const base = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../artifacts/issue-5718-local022');
await fs.mkdir(base, { recursive: true });
const fixtureRoot = await fs.mkdtemp(path.join(base, 'observed-protocol-contract-'));
try {
  const hash = createHash('sha256').update(await fs.readFile(new URL('./local022-observed-input.mjs', import.meta.url))).digest('hex');
  const allocation = { run_id: runId, ui_driver: 'OBSERVED_CLICK_V2', interactive_ui: true, observed_input_sha256: hash,
    contract_fixture_only: true, licensed_runtime_executed: false };
  const allocationPath = path.join(fixtureRoot, 'allocation.json');
  await fs.writeFile(allocationPath, JSON.stringify({ ...allocation, observed_input_sha256: 'bad' }), { flag: 'wx' });
  await assert.rejects(openObservedAllocation(fixtureRoot, runId, 12345), /bind this observed driver/);
  await fs.writeFile(allocationPath, JSON.stringify(allocation));
  const io = await openObservedAllocation(fixtureRoot, runId, 12345);
  assert.equal(await io.read(), null);
  await fs.writeFile(path.join(fixtureRoot, 'ui-action-0001.private.json'), raw, { flag: 'wx' });
  const request = await io.read();
  await assert.rejects(io.acknowledge(request, { completed: false }), /attestation/);
  const proof = { completed: true, refreshed: true, windowApp: 'test:bricscad', windowId: 1,
    observationId: 'contract-fixture-only', operations: ['click'] };
  await io.acknowledge(request, proof);
  const ack = JSON.parse(await fs.readFile(path.join(fixtureRoot, 'ui-ack-0001.private.json'), 'utf8'));
  assert.equal(ack.schema, 'QS3D_LOCAL022_UI_ACK_V2');
  assert.equal(ack.sequence, 1);
  await assert.rejects(io.acknowledge(request, proof), /last observed request/);
  const restarted = await openObservedAllocation(fixtureRoot, runId, 12345);
  await assert.rejects(restarted.read(), /cannot be replayed/);
  assert.equal(await io.read(), null);
  await fs.writeFile(path.join(fixtureRoot, 'receipt.json'), '{"contract_fixture_terminal":true}', { flag: 'wx' });
  await assert.rejects(io.read(), /Terminal marker exists/);
  await fs.unlink(path.join(fixtureRoot, 'receipt.json'));
  await fs.writeFile(allocationPath, JSON.stringify({ ...allocation, changed: true }));
  await assert.rejects(io.read(), /Allocation changed/);
} finally {
  // Remove only the generated files in this exact direct-child test fixture.
  assert.equal(path.dirname(fixtureRoot), base);
  for (const item of await fs.readdir(fixtureRoot, { withFileTypes: true })) {
    assert.equal(item.isFile(), true);
    await fs.unlink(path.join(fixtureRoot, item.name));
  }
  await fs.rmdir(fixtureRoot);
}
console.log('PASS: actual V2 receipt I/O binds driver hash, refuses replay/invalid completion/changed allocation/terminal state, and publishes exclusive V2 ACKs; no CAD or input executed.');

const helperHash = createHash('sha256').update(await fs.readFile(new URL('./local022-observed-input.mjs', import.meta.url))).digest('hex');
for (const scenario of ['valid', 'notOptedIn', 'gap', 'orphanAck', 'futureRequest', 'invalidAck', 'wrongPid', 'wrongRun', 'wrongHash', 'terminal', 'changedHistory']) {
  const root = await fs.mkdtemp(path.join(base, 'observed-resume-contract-'));
  try {
    const allocation = { run_id: runId, ui_driver: 'OBSERVED_CLICK_V2', interactive_ui: true,
      observed_input_sha256: scenario === 'wrongHash' ? 'bad' : helperHash,
      operator_wait_policy: scenario === 'notOptedIn' ? 'WALL_CLOCK_V1' : 'PAUSE_FOR_OPERATOR_V1',
      contract_fixture_only: true, licensed_runtime_executed: false };
    await fs.writeFile(path.join(root, 'allocation.json'), JSON.stringify(allocation), { flag: 'wx' });
    const action = sequence => JSON.stringify({ ...value, sequence, target_pid: scenario === 'wrongPid' ? 7 : 12345 });
    const ack = sequence => JSON.stringify({ schema: 'QS3D_LOCAL022_UI_ACK_V2',
      run_id: scenario === 'wrongRun' ? 'fedcba9876543210fedcba9876543210' : runId, sequence, status: 'SENT' });
    if (scenario !== 'orphanAck') await fs.writeFile(path.join(root, 'ui-action-0001.private.json'), action(1));
    await fs.writeFile(path.join(root, 'ui-ack-0001.private.json'), ack(1) + (scenario === 'invalidAck' ? '\n' : ''));
    await fs.writeFile(path.join(root, `ui-action-${scenario === 'gap' ? '0003' : '0002'}.private.json`), action(scenario === 'gap' ? 3 : 2));
    if (scenario === 'futureRequest') await fs.writeFile(path.join(root, 'ui-action-0003.private.json'), action(3));
    if (scenario === 'terminal') await fs.writeFile(path.join(root, 'phase-ui.json'), '{}');
    if (!['valid', 'changedHistory'].includes(scenario)) {
      await assert.rejects(openObservedAllocation(root, runId, 12345, { resume: true }), undefined, scenario);
      continue;
    }
    const resumed = await openObservedAllocation(root, runId, 12345, { resume: true });
    const request = await resumed.read();
    assert.equal(request.sequence, 2, 'resume must skip only exact acknowledged prefix');
    assert.equal(await resumed.read(), request, 'unchanged repeated observation must retain request identity');
    await assert.rejects(fs.stat(path.join(root, 'ui-ack-0002.private.json')), { code: 'ENOENT' });
    if (scenario === 'changedHistory') {
      await fs.writeFile(path.join(root, 'ui-ack-0001.private.json'), ack(1) + '\n');
      await assert.rejects(resumed.read(), /history changed/);
    } else {
      await assert.rejects(resumed.acknowledge(request, { completed: false }), /attestation/);
      await fs.writeFile(path.join(root, 'ui-action-0002.private.json'), action(2).replace('"x":94', '"x":95'));
      await assert.rejects(resumed.read(), /Request changed/);
    }
  } finally {
    assert.equal(path.dirname(root), base);
    for (const item of await fs.readdir(root, { withFileTypes: true })) {
      assert.equal(item.isFile(), true);
      await fs.unlink(path.join(root, item.name));
    }
    await fs.rmdir(root);
  }
}
console.log('PASS: explicit paused resume validates exact contiguous receipt history, rejects identity/gap/future/terminal/tamper errors and never invents ACKs.');
