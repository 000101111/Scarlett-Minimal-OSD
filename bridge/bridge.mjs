import readline from 'readline';
import { FocusriteClient } from './src/client.js';
import { loadConfig, KEY_PATH } from './src/config.js';
import { OcaBlock } from 'aes70/src/controller/ControlClasses/OcaBlock.js';
import { OcaGain } from 'aes70/src/controller/ControlClasses/OcaGain.js';
import { OcaMute } from 'aes70/src/controller/ControlClasses/OcaMute.js';
import { OcaBooleanActuator } from 'aes70/src/controller/ControlClasses/OcaBooleanActuator.js';

function emit(data) {
  process.stdout.write(JSON.stringify(data) + '\n');
}

process.on('unhandledRejection', (err) => {
  emit({ type: 'state', connected: false, gain: null, group: 'UNKNOWN', mute: false, dim: false, device: null, error: err?.message || String(err) });
});

let currentSession = null;
let currentGainObj = null;
let currentSwitchObj = null;
let currentMinPos = 0;
let lastState = null;

const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
  terminal: false,
});

rl.on('line', async (line) => {
  if (!line || !line.trim()) return;
  try {
    const data = JSON.parse(line.trim());
    if (!currentSession || !currentGainObj || !currentSwitchObj) return;

    if (data.cmd === 'change_gain' && typeof data.delta === 'number') {
      const cur = typeof lastState?.gain === 'number' ? lastState.gain : -20.0;
      const target = Math.min(6.0, Math.max(-127.0, Number((cur + data.delta).toFixed(1))));
      await currentGainObj.SetGain(target);
      currentSession.connection.sendCommands();
      currentSession.connection.flush();
    } else if (data.cmd === 'set_gain' && typeof data.value === 'number') {
      const target = Math.min(6.0, Math.max(-127.0, Number(data.value.toFixed(1))));
      await currentGainObj.SetGain(target);
      currentSession.connection.sendCommands();
      currentSession.connection.flush();
    } else if (data.cmd === 'set_group' && typeof data.group === 'string') {
      const isAlt = data.group.toUpperCase() === 'ALT';
      const targetPos = isAlt ? (currentMinPos + 1) : currentMinPos;
      await currentSwitchObj.SetPosition(targetPos);
      currentSession.connection.sendCommands();
      currentSession.connection.flush();
    } else if (data.cmd === 'toggle_group') {
      const isCurrentlyAlt = lastState?.group === 'ALT';
      const targetPos = isCurrentlyAlt ? currentMinPos : (currentMinPos + 1);
      await currentSwitchObj.SetPosition(targetPos);
      currentSession.connection.sendCommands();
      currentSession.connection.flush();
    }
  } catch (err) {
    emit({ type: 'command_error', error: err?.message || String(err) });
  }
});

async function run() {
  const config = await loadConfig();

  while (true) {
    let client = null;
    try {
      client = new FocusriteClient(config, { keyPath: KEY_PATH });
      const session = await client.connect();

      // Resolve main outputs block (ONo 4112)
      const block = new OcaBlock(4112, session.device);
      const members = await block.GetMembers();
      const m4113 = members.find((m) => (m.ONo ?? m.MemberObjectIdentification?.ONo) === 4113);
      if (!m4113) throw new Error('ActiveMonitorGroup 4113 not found');

      const objSwitch = session.device.resolve_object(m4113);
      const objGain = new OcaGain(4118, session.device);
      const objMute = new OcaMute(4116, session.device);
      const objDim = new OcaBooleanActuator(4117, session.device);

      let posNames = ['main', 'alt'];
      try {
        posNames = await objSwitch.GetPositionNames();
      } catch {}

      currentSession = session;
      currentGainObj = objGain;
      currentSwitchObj = objSwitch;

      while (client.connected) {
        const pGain = objGain.GetGain();
        const pPos = objSwitch.GetPosition();
        const pMute = objMute.GetState();
        const pDim = objDim.GetSetting();

        session.connection.sendCommands();
        session.connection.flush();

        const [rGain, rPos, rMute, rDim] = await Promise.all([pGain, pPos, pMute, pDim]);

        const rawGain = typeof rGain?.item === 'function' ? rGain.item(0) : (rGain?.[0] ?? rGain);
        const gainVal = Number(Number(rawGain).toFixed(1));
        const pos = rPos?.values ? rPos.values[0] : (Array.isArray(rPos) ? rPos[0] : (typeof rPos?.item === 'function' ? rPos.item(0) : rPos));
        const minPos = rPos?.values ? rPos.values[1] : 0;
        currentMinPos = typeof minPos === 'number' ? minPos : 0;

        const posIndex = (typeof pos === 'number' && typeof minPos === 'number') ? (pos - minPos) : 0;
        const groupName = (posNames[posIndex] || (posIndex === 1 ? 'alt' : 'main')).toUpperCase();
        const isMute = Number(rMute) === 1;
        const isDim = Boolean(rDim);

        const curState = {
          type: 'state',
          connected: true,
          gain: gainVal,
          group: groupName,
          mute: isMute,
          dim: isDim,
          device: 'Scarlett 16i16 4th Gen',
        };

        if (
          !lastState ||
          lastState.connected !== curState.connected ||
          lastState.gain !== curState.gain ||
          lastState.group !== curState.group ||
          lastState.mute !== curState.mute ||
          lastState.dim !== curState.dim
        ) {
          lastState = curState;
          emit(curState);
        }

        await new Promise((r) => setTimeout(r, 40));
      }
    } catch (err) {
      currentSession = null;
      currentGainObj = null;
      currentSwitchObj = null;

      emit({
        type: 'state',
        connected: false,
        gain: null,
        group: 'UNKNOWN',
        mute: false,
        dim: false,
        device: null,
        error: err.message,
      });
      try { client?.close(); } catch {}
      await new Promise((r) => setTimeout(r, 1000));
    }
  }
}

run().catch((err) => {
  emit({ type: 'fatal', error: err.stack || err.message });
  process.exit(1);
});
