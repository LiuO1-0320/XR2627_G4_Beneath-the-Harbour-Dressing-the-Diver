"""Check functional names and Unity resource paths without changing assets."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
assets = root / 'XRG4_BeneathTheHarber/Assets'
checks = 0

def check(condition, message):
    global checks
    checks += 1
    if not condition:
        raise AssertionError(message)

for folder in ('script', 'Editor'):
    for path in (assets / folder).glob('*.cs'):
        text = path.read_text(encoding='utf-8-sig')
        check(not path.name.startswith('M4'), str(path))
        check(Path(str(path) + '.meta').exists(), 'Missing meta: ' + str(path))
        active = '\n'.join(line for line in text.splitlines() if 'APIUpdating.MovedFrom(' not in line)
        check(not re.search(r'\b(?:IM4|M4)\w*\b', active), 'Old live identifier: ' + str(path))
        for resource in re.findall(r'Resources.Load<[^>]+>\("([^"]+)"\)', text):
            parent = assets / 'Resources' / resource
            check(any(p.suffix != '.meta' for p in parent.parent.glob(parent.name + '.*')),
                  'Missing Resources asset: ' + resource)

for resource in ('Resources/ExperienceAudio/DefaultAudioMap.asset',
                 'Resources/ExperienceAudio/Experience.mixer'):
    check((assets/resource).exists(), resource)
    check((assets/(resource+'.meta')).exists(), resource+' meta')

for path in assets.rglob('*'):
    check(not path.name.startswith('M4'), 'Old asset filename: ' + str(path))

print(f'PASS: {checks} functional naming and resource checks')
