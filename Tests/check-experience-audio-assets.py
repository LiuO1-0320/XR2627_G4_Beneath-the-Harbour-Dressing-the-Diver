"""Offline structural/signal checks; these do not replace Unity import or headset listening."""
from pathlib import Path
import re
import wave
import numpy as np
import yaml

root = Path('XRG4_BeneathTheHarber/Assets')
checks = 0
def check(ok, detail):
    global checks
    checks += 1
    if not ok: raise AssertionError(detail)

for path in (root/'Audio/Ambience').glob('*.wav'):
    with wave.open(str(path), 'rb') as f:
        check(f.getframerate() == 44100 and f.getsampwidth() == 2, str(path)+' format')
        data = np.frombuffer(f.readframes(f.getnframes()), dtype='<i2').astype(float).reshape(-1, f.getnchannels())/32768
    check(np.isfinite(data).all() and np.max(np.abs(data)) < .25, str(path)+' headroom')
    check(abs(data.mean()) < .001, str(path)+' DC offset')
    if 'Loop' in path.name:
        seconds = 64 if path.stem == 'MuseumGentleLoop' else 32
        check(len(data) == 44100*seconds, str(path)+' length')
        check(np.max(np.abs(data[-1]-data[0])) < .025, str(path)+' loop seam')
    else:
        check(np.max(np.abs(data[[0,-1]])) < .001, str(path)+' attack/release ends')

def documents(path):
    raw = path.read_text()
    raw = re.sub(r'^%.*\n', '', raw, flags=re.M)
    raw = re.sub(r'--- !u!\d+ &\d+', '---', raw)
    return list(yaml.safe_load_all(raw))

mix = documents(root/'Resources/ExperienceAudio/Experience.mixer')
names = {d['AudioMixerGroupController']['m_Name'] for d in mix if 'AudioMixerGroupController' in d}
check(names == {'Master','Narration','Ambience','Feedback'}, 'Mixer group names')
mapping = documents(root/'Resources/ExperienceAudio/DefaultAudioMap.asset')[0]['MonoBehaviour']
music_meta = (root/'Audio/Ambience/MuseumGentleLoop.wav.meta').read_text()
music_guid = re.search(r'^guid: (.+)$', music_meta, re.M).group(1)
check(mapping['museumMusic']['guid'] == music_guid, 'Museum music asset reference')
check({e['stage'] for e in mapping['stages']} == set(range(7)), 'All stage slots')
check(mapping['duckMultiplier'] < 1 and mapping['crossfadeSeconds'] >= 1, 'Comfort defaults')
for name in ['act1_helmetHistory.mp3','act2_helmetComponents.mp3','act3_Diver.mp3']:
    path = root/'Audio/Narration'/name
    check(path.exists() and path.stat().st_size > 1000, name+' available for AssetDatabase import')
for name, seconds, peak in [('WaterExitSplash',1.05,.46)]:
    with wave.open(str(root/'Resources/ExperienceAudio'/(name+'.wav')), 'rb') as f:
        check(f.getframerate()==44100 and f.getsampwidth()==2, name+' PCM format')
        data=np.frombuffer(f.readframes(f.getnframes()), dtype='<i2').astype(float)/32768
    check(abs(len(data)/44100-seconds)<.001, name+' duration')
    check(np.max(np.abs(data))<peak, name+' headroom')
    if 'Loop' in name: check(abs(data[0]-data[-1])<.025, name+' continuous loop seam')
    else: check(max(abs(data[0]),abs(data[-1]))<.001, name+' smooth endpoints')
print('PASS:', checks, 'offline audio asset checks (Unity import and listening still required)')
