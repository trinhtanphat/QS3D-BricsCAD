// Receipt I/O only. All UI actions must be performed explicitly with the
// documented Computer Use API in node_repl, observing between actions. This
// module neither injects input nor invents an ACK for an unperformed gesture.
import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { createHash } from 'node:crypto';

const sourcePath = fileURLToPath(import.meta.url);
const sourceHash = createHash('sha256').update(await fs.readFile(sourcePath)).digest('hex');
const artifactBase = path.resolve(path.dirname(sourcePath), '../../artifacts/issue-5718-local022');
const fields = ['schema', 'run_id', 'sequence', 'action', 'x', 'y', 'text', 'target_pid', 'stage'];
const clicks = new Set(['SelectTree', 'OpenCancelDialog', 'OpenCreateDialog',
  'AcceptCreateDialog', 'StartFirstDraw', 'FirstCentre', 'SecondCentre', 'OpenFamilyScope',
  'SelectFamilyScope', 'StartSecondDraw', 'RepeatCentre']);
const numericStages = { InputL1: '2', InputW1: '2', InputL2: '1', InputW2: '1', InputH1: '1', InputH2: '0', EditH2: '1000' };
function requireCondition(condition, message) { if (!condition) throw new Error(message); }

export function decodeObservedRequest(raw, runId, sequence, ownedPid) {
  requireCondition(typeof raw === 'string' && /^[\x20-\x7e]+$/.test(raw) && raw.length <= 4096, 'Noncanonical request encoding');
  requireCondition(/^[0-9a-f]{32}$/.test(runId) && Number.isInteger(sequence) && sequence >= 1 && sequence <= 100 &&
    Number.isInteger(ownedPid) && ownedPid > 0, 'Invalid expected allocation identity');
  const request = JSON.parse(raw);
  requireCondition(request && !Array.isArray(request) && Object.keys(request).join('|') === fields.join('|'), 'Request fields/order differ');
  requireCondition(JSON.stringify(request) === raw, 'Duplicate, escaped or noncanonical JSON');
  requireCondition(request.schema === 'QS3D_LOCAL022_UI_ACTION_V2' && request.run_id === runId &&
    request.sequence === sequence && request.target_pid === ownedPid, 'Request identity differs');
  requireCondition(Number.isInteger(request.x) && Number.isInteger(request.y) && request.x >= -32768 && request.x <= 32767 &&
    request.y >= -32768 && request.y <= 32767, 'Invalid proposed screen point');
  const allowed = (request.action === 'click' && request.text === '' && clicks.has(request.stage)) ||
    (request.action === 'text' && Object.hasOwn(numericStages, request.stage) && request.text === numericStages[request.stage]) ||
    (request.action === 'key' && ((request.stage === 'EndFirstDraw' && request.text === 'ENTER') ||
      ((request.stage === 'CancelDialog' || request.stage === 'EndSecondDraw') && request.text === 'ESC')));
  requireCondition(allowed, 'Unexpected stage/action/value; no hover or unrestricted commands');
  return Object.freeze(request);
}

async function boundedRead(file, maximum) {
  const stat = await fs.lstat(file);
  requireCondition(stat.isFile() && !stat.isSymbolicLink() && stat.size > 0 && stat.size <= maximum, 'Unsafe evidence file');
  return fs.readFile(file, 'utf8');
}

async function requireAbsent(file, message) {
  try { await fs.lstat(file); } catch (error) { if (error.code === 'ENOENT') return; throw error; }
  throw new Error(message);
}

export async function openObservedAllocation(allocationRoot, runId, ownedPid) {
  const root = path.resolve(allocationRoot);
  requireCondition(root.toLowerCase().startsWith((artifactBase + path.sep).toLowerCase()) &&
    path.dirname(root).toLowerCase() === artifactBase.toLowerCase(), 'Not a direct owned allocation');
  requireCondition((await fs.realpath(root)).toLowerCase() === root.toLowerCase(), 'Redirected allocation refused');
  const allocationRaw = await boundedRead(path.join(root, 'allocation.json'), 131072);
  const allocation = JSON.parse(allocationRaw);
  requireCondition(allocation.run_id === runId && allocation.ui_driver === 'OBSERVED_CLICK_V2' &&
    allocation.interactive_ui === true && allocation.observed_input_sha256?.toLowerCase() === sourceHash,
  'Allocation does not bind this observed driver');
  let nextSequence = 1;
  let outstanding = null;

  async function requireLiveEvidence() {
    requireCondition(await boundedRead(path.join(root, 'allocation.json'), 131072) === allocationRaw, 'Allocation changed');
    for (const terminal of ['receipt.json', 'phase-ui.json']) {
      try { await fs.lstat(path.join(root, terminal)); } catch (error) { if (error.code === 'ENOENT') continue; throw error; }
      throw new Error('Terminal marker exists; no more input or acknowledgements');
    }
  }

  return Object.freeze({
    async read() {
      await requireLiveEvidence();
      const suffix = String(nextSequence).padStart(4, '0');
      // Do not return a request that another driver already completed.
      await requireAbsent(path.join(root, `ui-ack-${suffix}.private.json`), 'Acknowledged request cannot be replayed');
      const requestPath = path.join(root, `ui-action-${suffix}.private.json`);
      let raw;
      try { raw = await boundedRead(requestPath, 4096); } catch (error) { if (error.code === 'ENOENT') return null; throw error; }
      const request = decodeObservedRequest(raw, runId, nextSequence, ownedPid);
      outstanding = { raw, request, requestPath, suffix };
      return request;
    },
    async acknowledge(request, proof) {
      // The caller attests only after actual tool success AND refreshed target
      // observations. These are operator receipts, not independent proof of UI
      // correctness; the probe's product/native assertions remain mandatory.
      await requireLiveEvidence();
      requireCondition(outstanding && request === outstanding.request, 'Request was not the last observed request');
      requireCondition(await boundedRead(outstanding.requestPath, 4096) === outstanding.raw, 'Request changed');
      const expectedOperations = request.action === 'text' ? ['click', 'selectAll', 'typeText'] :
        request.action === 'key' ? ['pressKey'] : ['click'];
      requireCondition(proof && proof.completed === true && proof.refreshed === true &&
        typeof proof.windowApp === 'string' && /bricscad/i.test(proof.windowApp) &&
        Number.isInteger(proof.windowId) && typeof proof.observationId === 'string' && proof.observationId.length > 0 &&
        JSON.stringify(proof.operations) === JSON.stringify(expectedOperations), 'Missing completed observed input attestation');
      const ackPath = path.join(root, `ui-ack-${outstanding.suffix}.private.json`);
      const temporary = ackPath + '.tmp';
      const body = JSON.stringify({ schema: 'QS3D_LOCAL022_UI_ACK_V2', run_id: runId, sequence: nextSequence, status: 'SENT' });
      const stream = await fs.open(temporary, 'wx');
      try { await stream.writeFile(body, 'utf8'); await stream.sync(); } finally { await stream.close(); }
      await requireLiveEvidence();
      // link is exclusive: unlike rename, it cannot replace an existing ACK.
      await fs.link(temporary, ackPath);
      await fs.unlink(temporary);
      nextSequence++;
      outstanding = null;
    },
  });
}
