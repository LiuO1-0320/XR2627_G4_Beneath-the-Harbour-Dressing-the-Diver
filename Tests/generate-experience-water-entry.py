"""Original diving-style splash: water impact, spray and bubbling tail; local PCM."""
from pathlib import Path
import numpy as np
import wave
import uuid
p=Path('XRG4_BeneathTheHarber/Assets/Resources/ExperienceAudio/WaterEntrySoft.wav')
rate=44100
t=np.arange(int(rate*1.2))/rate
rng=np.random.RandomState(40315)
noise=rng.normal(size=len(t))
spray=np.convolve(noise,np.ones(5)/5,mode='same')
body=np.convolve(rng.normal(size=len(t)),np.ones(35)/35,mode='same')
attack=1-np.exp(-t/0.018)
tail=np.minimum(1, (1.2-t)/.15)
impact=np.sin(2*np.pi*(100*t-18*t*t))*np.exp(-t*12)
bubbles=np.sin(2*np.pi*(380*t-100*t*t))*(.5+.5*np.sin(2*np.pi*13*t))*np.exp(-t*5)
data=(spray*np.exp(-t*5)+2.5*body*np.exp(-t*3)+.3*impact+.12*bubbles)*attack*tail
data=data/max(abs(data))*.5
with wave.open(str(p),'wb') as f:
    f.setnchannels(1); f.setsampwidth(2); f.setframerate(rate)
    f.writeframes((data*32767).astype('<i2').tobytes())
meta=Path(str(p)+'.meta')
if not meta.exists(): meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
print('Created 1.2 s diving-style splash; peak <= -6 dBFS, smooth attack and release')
