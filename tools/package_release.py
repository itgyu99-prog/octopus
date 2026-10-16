from pathlib import Path
import hashlib
import json
import shutil
import sys
import zipfile


def package_naver_source(root):
    """Package the working Unity sources without builds, history or preview art."""
    from datetime import datetime, timezone

    project = root / 'Source' / '문어_유한맵'
    output = root / '문어지지마_네이버전송_Unity소스.zip'
    temporary = output.with_suffix('.zip.tmp')
    prefix = '문어지지마_Unity소스/'
    selected = ('Assets', 'Packages', 'ProjectSettings')
    excluded_parts = {
        'library', 'temp', 'obj', 'logs', 'build', 'builds', '.git', '.vs',
        '.vscode', '.idea', '.codex', '.agents', 'usersettings', '__pycache__',
        'memorycaptures', 'recordings',
    }
    excluded_suffixes = {'.zip', '.log', '.tmp', '.bak', '.csproj', '.sln', '.slnx'}
    prototype = 'Assets/Editor/WaterAlternativesChecks.cs'

    def excluded(relative):
        name = relative.removesuffix('.meta')
        parts = Path(name).parts
        return (name == prototype
                or any(part.lower() in excluded_parts
                       or part.lower().startswith('my project') for part in parts)
                or Path(name).suffix.lower() in excluded_suffixes
                or Path(name).name.startswith('.')
                or Path(name).name.lower().startswith(
                    ('water-balloon', 'water-option-', 'water-redesign-', 'water-basic-')))

    paths = {}
    directories = set(selected)
    for folder in selected:
        for path in sorted((project / folder).rglob('*')):
            relative = path.relative_to(project).as_posix()
            if excluded(relative):
                continue
            if path.is_symlink():
                raise RuntimeError(f'Source symlink requires review: {relative}')
            if path.is_file():
                paths[relative] = path
                directories.update(parent.as_posix() for parent in Path(relative).parents
                                   if parent.as_posix() != '.')
            elif path.is_dir():
                directories.add(relative)

    required = (
        'Assets/Scenes/SampleScene.unity', 'Assets/Scripts/Elements/WaterVfx.cs',
        'Assets/Scripts/Elements/TreeRainAbility.cs',
        'Assets/Scripts/Elements/ElectricSparkBurst.cs',
        'Assets/Scripts/UI/DevMenu.cs', 'Packages/manifest.json',
        'Packages/packages-lock.json', 'ProjectSettings/ProjectVersion.txt',
        'ProjectSettings/EditorBuildSettings.asset',
    )
    for name in required:
        if name not in paths:
            raise RuntimeError(f'Missing required source: {name}')
    for name in [*paths, *directories]:
        if name.startswith('Assets/') and not name.endswith('.meta'):
            if name + '.meta' not in paths:
                raise RuntimeError(f'Missing asset metadata: {name}')
        if name.endswith('.meta') and name[:-5] not in paths and name[:-5] not in directories:
            raise RuntimeError(f'Orphan asset metadata: {name}')
    dependencies = json.loads(paths['Packages/manifest.json'].read_text(encoding='utf-8'))
    if any(value.startswith('file:') for value in dependencies['dependencies'].values()):
        raise RuntimeError('Local package dependency requires explicit inclusion')

    version = paths['ProjectSettings/ProjectVersion.txt'].read_text(encoding='utf-8')
    unity_version = version.split('m_EditorVersion: ', 1)[1].splitlines()[0].strip()
    readme = f'''# 문어지지마 — 네이버 전송용 Unity 소스

Unity 버전: {unity_version}

## 열기
1. ZIP 전체를 로컬 폴더에 압축 해제합니다.
2. Unity Hub에서 Add / 디스크에서 프로젝트 추가로 이 README와 Assets,
   Packages, ProjectSettings가 함께 있는 문어지지마_Unity소스 폴더를 선택합니다.
3. Unity {unity_version}로 열고 패키지 다운로드와 최초 에셋 임포트가 끝날 때까지 기다립니다.
   최초 복원에는 인터넷 연결이 필요할 수 있습니다. Library 등 캐시는 자동 생성됩니다.
4. Assets/Scenes/SampleScene.unity를 열고 Play를 누릅니다.

## 포함 범위
- 압축 생성 시점의 실제 작업 소스입니다. Git에 아직 커밋하지 않은 파일도 포함합니다.
- 풀 속성 UI와 현재 불·전기·탄산수 등 문어 게임 구현을 포함합니다.
- 기존 기본 물 공격과 WaterVfx.cs는 유지합니다.
- 새 물풍선 GIF/PNG 시안과 물 공격 대안 프로토타입 WaterAlternativesChecks는 제외합니다.
- Assets 및 대응 .meta, Packages, ProjectSettings를 포함합니다.
- 정령기사, 중복 My project, Library/Temp/obj/Logs/Build, 실행 배포, Git 이력,
  검토용 docs 이미지, 기존 ZIP은 포함하지 않습니다.

SOURCE_MANIFEST.json에는 각 원본 파일의 SHA-256이 있습니다.
압축 무결성·원본 일치·필수 소스·메타 쌍을 검사한 소스 전달본이며,
이 패키징 작업에서 Unity 실행·게임 플레이·컴파일 검사를 새로 수행하지는 않았습니다.
네이버 로그인·업로드·메일 전송은 수행하지 않았습니다.
'''
    if temporary.exists():
        raise RuntimeError(f'Previous unfinished package preserved: {temporary}')
    hashes = {}
    with zipfile.ZipFile(temporary, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for directory in sorted(directories):
            archive.writestr(prefix + directory + '/', b'')
        for name, path in sorted(paths.items()):
            data = path.read_bytes()
            hashes[name] = hashlib.sha256(data).hexdigest()
            archive.writestr(prefix + name, data)
        archive.writestr(prefix + 'README.md', readme.encode('utf-8'))
        manifest = {
            'created_utc': datetime.now(timezone.utc).isoformat(),
            'source': project.relative_to(root).as_posix(),
            'basis': 'working tree including uncommitted and untracked source files',
            'unity_version': unity_version,
            'source_files': len(paths),
            'sha256': hashes,
        }
        archive.writestr(prefix + 'SOURCE_MANIFEST.json',
                         json.dumps(manifest, ensure_ascii=False, indent=2).encode('utf-8'))

    with zipfile.ZipFile(temporary) as archive:
        if archive.testzip() is not None:
            raise RuntimeError('ZIP CRC validation failed')
        expected = {prefix + name for name in paths}
        expected.update(prefix + directory + '/' for directory in directories)
        expected.update((prefix + 'README.md', prefix + 'SOURCE_MANIFEST.json'))
        if len(archive.namelist()) != len(expected) or set(archive.namelist()) != expected:
            raise RuntimeError('Unexpected or duplicate ZIP entries')
        for name, path in paths.items():
            archived_hash = hashlib.sha256(archive.read(prefix + name)).hexdigest()
            current_hash = hashlib.sha256(path.read_bytes()).hexdigest()
            if archived_hash != hashes[name] or current_hash != hashes[name]:
                raise RuntimeError(f'Source changed during packaging: {name}')
    temporary.replace(output)
    print(json.dumps({
        'file': str(output), 'bytes': output.stat().st_size,
        'source_files': len(paths), 'asset_meta_files': sum(
            name.startswith('Assets/') and name.endswith('.meta') for name in paths),
        'unity_version': unity_version,
        'sha256': hashlib.sha256(output.read_bytes()).hexdigest(),
        'verification': 'PASS: CRC, all source hashes, required files, metadata pairs, exact allowlist',
    }, ensure_ascii=False, indent=2))


if '--naver-source' in sys.argv:
    package_naver_source(Path(__file__).resolve().parents[1])
    sys.exit(0)

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
