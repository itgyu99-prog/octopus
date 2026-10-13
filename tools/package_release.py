from pathlib import Path
import hashlib
import json
import shutil
import sys
import zipfile

root = Path(__file__).resolve().parents[1]
project = root / 'Source' / '문어_유한맵'
original = root.parent / '문어_유한맵_HD_소스.zip'
selected = ('Assets', 'Packages', 'ProjectSettings')
current = {p.relative_to(project).as_posix(): p for folder in selected
           for p in (project / folder).rglob('*') if p.is_file()}
with zipfile.ZipFile(original) as archive:
    old = {i.filename.split('/', 1)[1]: i for i in archive.infolist()
           if not i.is_dir() and '/' in i.filename
           and i.filename.split('/', 1)[1].split('/')[0] in selected}
    changed = [name for name, p in current.items() if name in old
               and p.read_bytes() != archive.read(old[name])]
    unchanged_art = sum(name.startswith('Assets/Resources/')
                        and p.read_bytes() == archive.read(old[name])
                        for name, p in current.items() if name in old)
    changes = {'changed': sorted(changed), 'added': sorted(set(current) - set(old)),
               'removed': sorted(set(old) - set(current)),
               'unchanged_resource_files': unchanged_art}
(root / 'docs' / '변경파일.json').write_text(json.dumps(changes, ensure_ascii=False, indent=2), encoding='utf-8')

evidence = root / 'docs' / '검증'
evidence.mkdir(exist_ok=True)
for name in ('verification-host-final.log', 'verification-guest-final.log', 'relay-host.log', 'relay-guest.log', 'release-build.log', 'solo-test.log', 'solo-coop-host.log', 'solo-coop-guest.log', 'solo-release-build.log'):
    if not (root / name).exists():
        continue
    lines = (root / name).read_text(encoding='utf-8-sig', errors='replace').splitlines()
    markers = ('CHECK PASS:', 'SOLO TEST PASS', 'COOP HOST PASS', 'COOP CLIENT PASS', 'CHECK FAILED', 'SYSTEM DATA CHECKS PASS', '빌드 성공:', '빌드 실패:')
    kept = [line for line in lines if any(marker in line for marker in markers)]
    (evidence / name).write_text('# Original log: ' + name + '\n# Result lines only; full log remains in workspace.\n' + '\n'.join(kept) + '\n', encoding='utf-8')
screenshots = project / 'Build' / 'Validation' / 'Screenshots'
for p in screenshots.glob('*.png'):
    shutil.copy2(p, evidence / p.name)

def add_docs(z, prefix):
    for name in ('실행안내.md', '개발기록.md', '작업결과.md'):
        z.write(root / name, prefix + name)
    for p in (root / 'docs').rglob('*'):
        if p.is_file():
            z.write(p, prefix + p.relative_to(root).as_posix())

outputs = []
edition = '문어지지마_2안' + ('_혼자테스트' if '--solo' in sys.argv else '')
windows = root / (edition + '_Windows.zip')
with zipfile.ZipFile(windows, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    build = project / 'Build' / 'Windows'
    assert (build / 'OctopusSurvivors.exe').is_file()
    for p in build.rglob('*'):
        if p.is_file() and not any('DoNotShip' in part or 'ButDontShip' in part for part in p.parts):
            z.write(p, edition + '_Windows/' + p.relative_to(build).as_posix())
    for name in ('실행안내.md', '작업결과.md'):
        z.write(root / name, edition + '_Windows/' + name)
outputs.append(windows)
source = root / (edition + '_Unity소스.zip')
with zipfile.ZipFile(source, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as z:
    for name, p in sorted(current.items()):
        z.write(p, edition + '/' + name)
    add_docs(z, edition + '/')
outputs.append(source)
manifest = []
for p in outputs:
    with zipfile.ZipFile(p) as z:
        assert z.testzip() is None, p.name
        count = len(z.infolist())
    manifest.append({'file': p.name, 'bytes': p.stat().st_size, 'files': count,
                     'sha256': hashlib.sha256(p.read_bytes()).hexdigest()})
(root / 'docs' / '배포파일.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({'packages': manifest, 'changed': len(changed), 'added': len(changes['added']), 'unchanged_resources': unchanged_art}, ensure_ascii=True))
