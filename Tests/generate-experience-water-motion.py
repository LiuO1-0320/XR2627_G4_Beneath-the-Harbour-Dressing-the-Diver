"""Original local exit splash and periodic underwater movement bed. No external samples."""
from pathlib import Path
import wave
import uuid
import numpy as np

root = Path('XRG4_BeneathTheHarber/Assets/Resources/ExperienceAudio')
rate = 44100
rng = np.random.RandomState(40316)

def save(name, signal, peak):
    signal = signal / max(abs(signal)) * peak
    path = root / (name + '.wav')
    with wave.open(str(path), 'wb') as f:
        f.setnchannels(1); f.setsampwidth(2); f.setframerate(rate)
        f.writeframes((signal * 32767).astype('<i2').tobytes())
    meta = Path(str(path) + '.meta')
    if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n')
    print(name, 'seconds=', len(signal)/rate, 'peak=', max(abs(signal)))

t = np.arange(int(rate * 1.05)) / rate
spray = np.convolve(rng.normal(size=len(t)), np.ones(8)/8, mode='same')
drips = (.5+.5*np.sin(2*np.pi*19*t)) * np.sin(2*np.pi*(620*t-140*t*t))
envelope = (1-np.exp(-t/.025)) * np.exp(-t*4) * np.minimum(1,(1.05-t)/.15)
save('WaterExitSplash', (spray + .13*drips)*envelope, .45)

