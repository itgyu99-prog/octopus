"""범주별 파일 조회/검색과 작업 안내 색인 생성. Unity 파일은 변경하지 않는다."""
import argparse
import json
import re
import sys
from pathlib import Path

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

ROOT = Path(__file__).resolve().parents[1]
CATALOG = json.loads((ROOT / 'tools/project_categories.json').read_text(encoding='utf-8'))
PROJECTS = {'octopus': CATALOG, **CATALOG.get('additional_projects', {})}
selector = argparse.ArgumentParser(add_help=False)
selector.add_argument('--project', choices=PROJECTS, default='octopus')
selection, _ = selector.parse_known_args()
CONFIG = PROJECTS[selection.project]
PROJECT = ROOT / CONFIG['project']
CATEGORIES = CONFIG['categories']
COMMAND = 'python tools/project_index.py' + ('' if selection.project == 'octopus' else f' --project {selection.project}')


def expand(patterns):
    return sorted({p for pattern in patterns for p in PROJECT.glob(pattern) if p.is_file()})


def files(category):
    return list(dict.fromkeys(expand(category['primary']) +
                              [PROJECT / p for p in category.get('anchors', {})] +
                              expand(category['related'])))


def relative(path):
    return path.relative_to(ROOT).as_posix()


def refresh():
    primary = [p for c in CATEGORIES.values() for p in expand(c['primary'])]
    source = set(PROJECT.glob('Assets/Scripts/**/*.cs')) | set(PROJECT.glob('Assets/Editor/**/*.cs'))
    missing = source - set(primary)
    duplicates = {p for p in primary if primary.count(p) > 1}
    if missing or duplicates:
        raise SystemExit('분류 누락/중복: ' + ', '.join(relative(p) for p in sorted(missing | duplicates)))
    for c in CATEGORIES.values():
        for pattern in c['primary'] + c['related']:
            if not expand([pattern]):
                raise SystemExit('존재하지 않는 경로: ' + pattern)
        for path, names in c.get('anchors', {}).items():
            content = (PROJECT / path).read_text(encoding='utf-8-sig')
            for name in names:
                if not re.search(r'^\s*(?:(?:public|private|protected|internal|static|override|async)\s+)*[\w<>\[\],?]+\s+' + re.escape(name) + r'\s*\(', content, re.M):
                    raise SystemExit('존재하지 않는 함수: ' + name)
    folder = ROOT / CONFIG.get('docs', 'docs/구조')
    folder.mkdir(parents=True, exist_ok=True)
    overview = ['# 프로젝트 작업 색인', '', f"수정 기준: `{CONFIG['project']}`. 요청에 해당하는 범주 하나부터 읽는다.", '', '| 범주 | 작업 내용 | 검색 키 |', '|---|---|---|']
    for key, c in CATEGORIES.items():
        overview.append(f"| [{c['title']}]({key}/README.md) | {c['purpose']} | `{key}` |")
        target = folder / key
        target.mkdir(exist_ok=True)
        lines = [f"# {c['title']}", '', c['purpose'], '', '경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.', '', '## 주 담당 파일', '']
        owned = expand(c['primary'])
        lines += [f'- `{relative(p)}`' for p in owned] or ['- 현재 독립 파일 없음. 아래 공통 파일의 진입 함수를 사용한다.']
        lines += ['', '## 관련 파일', '']
        lines += [f'- `{relative(p)}`' for p in expand(c['related'])]
        for path, names in c.get('anchors', {}).items():
            lines += ['', f'## `{path}` 진입 함수', '']
            content = (PROJECT / path).read_text(encoding='utf-8-sig').splitlines()
            for name in names:
                for number, line in enumerate(content, 1):
                    if re.search(r'^\s*(?:(?:public|private|protected|internal|static|override|async)\s+)*[\w<>\[\],?]+\s+' + re.escape(name) + r'\s*\(', line):
                        lines.append(f'- `{name}` — {number}행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)')
                        break
        lines += ['', '## 범위 검색', '', '```powershell', f'{COMMAND} --category {key}', f'{COMMAND} --category {key} --find "검색어"', '```', '', '이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.']
        (target / 'README.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    overview += ['', '## 읽는 순서', '', '1. 위 표에서 범주를 선택하고 해당 README만 읽는다.', '2. 함수명으로 위치를 검색하고 필요한 부분만 읽는다.', '3. 연관 동작을 바꿀 때만 관련 범주로 확장한다.', '', '전체 탐색 대신 `python tools/project_index.py --locate "파일명"`을 사용한다.', '파일 추가·이름 변경 후 `python tools/project_index.py --refresh`로 색인을 갱신한다.', '', '## 보관·도구 위치', '', '- `docs/검증`: 결과 로그 요약과 확인 이미지. 검증 요청이 있을 때만 읽는다.', '- `개발기록.md`, `작업결과.md`: 과거 개발 기록. 현재 동작·수치는 코드와 JSON으로 확인한다.', '- `실행안내.md`: 게임 실행 및 접속 안내.', '- `tools/package_release.py`: 배포 패키징. 나머지 기존 구현/수정 도구는 과거 코드 재작성용이므로 일반 작업에서 실행하지 않는다.', '- `My project`, Windows 폴더, ZIP: 별도 프로젝트·배포 사본. 기본 검색에서 제외.', '- `Library`, `Temp`, `Build`, `Logs`: Unity 생성물. 기본 검색에서 제외.', '', '현재 몬스터와 여러 시스템은 GameSession.cs를 공유한다. 이 색인은 실제 위치를 안내하며 코드·Resources 경로·Unity GUID는 유지한다.']
    if selection.project != 'octopus':
        overview = overview[:overview.index('## 읽는 순서')]
        overview += ['## 사용법', '', f'`{COMMAND} --category game`', '', f'`{COMMAND} --refresh`', '', '별도 게임 프로젝트다. 기존 문어 게임 파일을 변경하지 않는다. 실행·조작·검증 안내는 프로젝트 README.md를 참조한다.']
    elif CATALOG.get('additional_projects'):
        overview += ['', '## 별도 게임 프로젝트', '']
        for key, project in CATALOG['additional_projects'].items():
            overview.append(f"- `{project['project']}` — `python tools/project_index.py --project {key} --category game`")
    (folder / 'README.md').write_text('\n'.join(overview) + '\n', encoding='utf-8')
    print(f'색인 갱신 완료: {len(CATEGORIES)}개 범주, C# {len(source)}개 분류 확인')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', choices=PROJECTS, default='octopus')
    parser.add_argument('--category', choices=CATEGORIES)
    parser.add_argument('--find', help='파일 내용의 대소문자 무시 리터럴 검색')
    parser.add_argument('--locate', help='Assets/Packages/ProjectSettings 파일명 검색')
    parser.add_argument('--refresh', action='store_true')
    parser.add_argument('--limit', type=int, default=30)
    args = parser.parse_args()
    if args.limit < 1:
        parser.error('--limit는 1 이상이어야 합니다')
    if args.refresh:
        refresh()
        return
    if args.locate:
        paths = sorted(p for base in ('Assets', 'Packages', 'ProjectSettings')
                       for p in (PROJECT / base).rglob('*')
                       if p.is_file() and not p.name.endswith('.meta') and args.locate.casefold() in p.name.casefold())
    elif args.category:
        paths = files(CATEGORIES[args.category])
    else:
        for key, c in CATEGORIES.items():
            print(f"{key}: {c['title']} — {c['purpose']}")
        return
    count = 0
    for path in paths:
        if args.find:
            if path.suffix not in ('.cs', '.json', '.txt', '.shader', '.md'):
                continue
            for number, line in enumerate(path.read_text(encoding='utf-8-sig').splitlines(), 1):
                if args.find.casefold() in line.casefold():
                    count += 1
                    if count <= args.limit:
                        print(f'{relative(path)}:{number}: {line.strip()}')
        else:
            count += 1
            if count <= args.limit:
                print(relative(path))
    print(f'총 {count}건 / 출력 {min(count, args.limit)}건')


if __name__ == '__main__':
    main()
