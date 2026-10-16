"""Deterministic original sound beds; no downloaded or historical recordings.
Run from repository root. Existing assets are deliberately not overwritten.
"""
from pathlib import Path
import wave
import uuid
import numpy as np

root = Path('XRG4_BeneathTheHarber/Assets')
audio = root / 'Audio/Ambience'
resources = root / 'Resources/ExperienceAudio'
audio.mkdir(parents=True, exist_ok=True)
resources.mkdir(parents=True, exist_ok=True)

def guid(path):
    meta = Path(str(path) + '.meta')
    if meta.exists():
        return next(x.split(': ', 1)[1] for x in meta.read_text().splitlines() if x.startswith('guid:'))
    value = uuid.uuid4().hex
    meta.write_text(f'fileFormatVersion: 2\nguid: {value}\n')
    return value

def wav(name, samples, rate=44100):
    path = audio / (name + '.wav')
    if not path.exists():
        with wave.open(str(path), 'wb') as f:
            f.setnchannels(1 if samples.ndim == 1 else samples.shape[1])
            f.setsampwidth(2)
            f.setframerate(rate)
            f.writeframes((np.clip(samples, -1, 1) * 32767).astype('<i2').tobytes())
    peak = np.max(np.abs(samples))
    print(f'{name}: {len(samples)/rate:.2f}s, peak={20*np.log10(peak):.1f} dBFS, RMS={20*np.log10(np.sqrt(np.mean(samples**2))):.1f} dBFS')
    return guid(path)

rate = 44100
count = rate * 32
t = np.arange(count) / rate
rng = np.random.RandomState(403)
freq = np.fft.rfftfreq(count, 1 / rate)

def bed(cutoff):
    # Periodic FFT filtering makes the loop continuous; remove DC and subsonic energy.
    spectrum = np.fft.rfft(rng.normal(size=count))
    spectrum *= (freq / (freq + 65))**2 / (1 + (freq / cutoff)**4)
    result = np.fft.irfft(spectrum, n=count)
    return result / np.std(result)

water = bed(1800) * (.032 + .008 * np.sin(2*np.pi*t/8))
wind = bed(450) * (.018 + .005 * np.cos(2*np.pi*t/16))
harbour = np.column_stack((water + wind, .93*water + bed(800)*.012 + wind))
deep = np.column_stack((bed(320), bed(350))) * (.034 + .004*np.sin(2*np.pi*t[:,None]/16))
clips = {'harbour': wav('HarbourSoftLoop', harbour), 'underwater': wav('UnderwaterSoftLoop', deep)}
for key, hz, duration in [('interaction', 440, .12), ('success', 520, .4)]:
    time = np.arange(int(rate*duration))/rate
    envelope = np.sin(np.pi*time/duration)**2
    signal = .10*envelope*(np.sin(2*np.pi*hz*time) + .25*np.sin(2*np.pi*hz*1.5*time))/1.25
    clips[key] = wav(key.capitalize()+'Soft', signal)

# Native Unity mixer asset: Master with three independently routable groups.
mixer = resources / 'Experience.mixer'
if not mixer.exists():
    uid = lambda: uuid.uuid4().hex
    groups = []
    values = {}
    for index, name in enumerate(['Master', 'Narration', 'Ambience', 'Feedback']):
        volume, pitch, effect_id, mix = uid(), uid(), uid(), uid()
        group_id, effect_ref = 24300000 + index, 24400000 + index
        values[volume], values[pitch] = (-3 if index == 0 else 0), 1
        children = '\n'.join(f'  - {{fileID: {24300000+i}}}' for i in range(1,4)) if index == 0 else '    []'
        groups.append(f'''--- !u!243 &{group_id}
AudioMixerGroupController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_AudioMixer: {{fileID: 24100000}}
  m_GroupID: {uid()}
  m_Children:
{children}
  m_Volume: {volume}
  m_Pitch: {pitch}
  m_Send: 00000000000000000000000000000000
  m_Effects:
  - {{fileID: {effect_ref}}}
  m_UserColorIndex: 0
  m_Mute: 0
  m_Solo: 0
  m_BypassEffects: 0
--- !u!244 &{effect_ref}
AudioMixerEffectController:
  m_ObjectHideFlags: 3
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name:
  m_EffectID: {effect_id}
  m_EffectName: Attenuation
  m_MixLevel: {mix}
  m_Parameters: []
  m_SendTarget: {{fileID: 0}}
  m_EnableWetMix: 0
  m_Bypass: 0
''')
    mixer.write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!241 &24100000
AudioMixerController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Experience
  m_OutputGroup: {fileID: 0}
  m_MasterGroup: {fileID: 24300000}
  m_Snapshots:
  - {fileID: 24500000}
  m_StartSnapshot: {fileID: 24500000}
  m_TargetSnapshot: {fileID: 24500000}
  m_SuspendThreshold: -80
  m_EnableSuspend: 1
  m_UpdateMode: 1
  m_ExposedParameters: []
  m_AudioMixerGroupViews: []
  m_CurrentViewIndex: 0
''' + ''.join(groups) + '''--- !u!245 &24500000
AudioMixerSnapshotController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_Name: Comfortable default
  m_AudioMixer: {fileID: 24100000}
  m_SnapshotID: ''' + uid() + '\n  m_FloatValues:\n' + ''.join(f'    {k}: {v}\n' for k,v in values.items()) + '  m_TransitionOverrides: {}\n')
mix_guid = guid(mixer)
script_guid = guid(root/'script/AudioMap.cs')
mapping = resources/'DefaultAudioMap.asset'
if not mapping.exists():
    mapping.write_text('''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ''' + script_guid + ''', type: 3}
  m_Name: DefaultAudioMap
  m_EditorClassIdentifier:
  mixer: {fileID: 24100000, guid: ''' + mix_guid + ''', type: 2}
''' + ''.join(f'  {name}Group: {{fileID: {24300001+i}, guid: {mix_guid}, type: 2}}\n' for i,name in enumerate(['narration','ambience','feedback'])) + ''.join(f'  {k}: {{fileID: 8300000, guid: {v}, type: 3}}\n' for k,v in clips.items()) + '  insideNarration: []\n  stages:\n' + ''.join(f'  - stage: {i}\n    narration: []\n' for i in range(7)) + '''  narrationVolume: 0.8
  ambienceVolume: 0.25
  feedbackVolume: 0.18
  duckMultiplier: 0.3
  crossfadeSeconds: 2.5
''')
guid(mapping)
for script in ['AudioDirector', 'AudioPolicy']:
    guid(root / f'script/{script}.cs')
