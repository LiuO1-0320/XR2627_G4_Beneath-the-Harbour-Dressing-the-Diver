"""Original, deterministic museum underscore. Only writes the new music asset.
64-second seamless stereo loop: soft sustained chords and sparse felt-like notes.
No third-party samples, speech, percussion or network dependencies.
"""
from pathlib import Path
import wave
import numpy as np

RATE, SECONDS = 44100, 64
n = RATE * SECONDS
mix = np.zeros((n, 2), dtype=np.float64)

def note(midi, start, duration, gain, pan, attack):
    t = np.arange(int(duration * RATE)) / RATE
    hz = 440 * 2 ** ((midi - 69) / 12)
    # Zero slope at both ends; soft upper partials without hard struck transients.
    env = np.minimum(1, t / attack) ** 2
    env *= np.minimum(1, (duration - t) / min(4, duration * .5)) ** 2
    env *= np.exp(-t / (duration * .8))
    tone = (np.sin(2*np.pi*hz*t) + .16*np.sin(2*np.pi*2*hz*t)
            + .035*np.sin(2*np.pi*3*hz*t)) * env * gain
    indices = (np.arange(len(t)) + int(start * RATE)) % n
    mix[indices, 0] += tone * np.sqrt(1 - pan)
    mix[indices, 1] += tone * np.sqrt(pan)

# Cmaj9, Am7, Fmaj9, Gsus: slow consonant movement, no dramatic cadence.
for k, chord in enumerate(((48,55,59,62,64), (45,52,55,60,64),
                            (41,48,55,57,60), (43,50,55,57,60))):
    for j, pitch in enumerate(chord):
        note(pitch, k*16 - 4, 24, .8 if j == 0 else .48, .25+j*.12, 5)
for k, pitch in enumerate((72, 67, 74, 71, 69, 72, 76, 67)):
    note(pitch, k*8+2, 10, .25, .35+(k%3)*.15, .22)

mix *= .042 / np.sqrt(np.mean(mix**2))
assert np.max(np.abs(mix)) < .25
assert np.max(np.abs(mix[0]-mix[-1])) < .005
path = Path('XRG4_BeneathTheHarber/Assets/Audio/Ambience/MuseumGentleLoop.wav')
with wave.open(str(path), 'wb') as f:
    f.setnchannels(2)
    f.setsampwidth(2)
    f.setframerate(RATE)
    f.writeframes((mix*32767).astype('<i2').tobytes())
print(f'{path.name}: {SECONDS}s stereo; peak {20*np.log10(np.max(np.abs(mix))):.1f} dBFS; RMS {20*np.log10(np.sqrt(np.mean(mix**2))):.1f} dBFS; seam delta {np.max(np.abs(mix[0]-mix[-1])):.6f}')
