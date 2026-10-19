# 네트워크

LAN·Relay, 접속·준비, 서버 권한·상태 동기화

경로는 작업 폴더 기준. 공통 파일은 필요한 함수만 읽는다.

## 주 담당 파일

- `Source/문어_유한맵/Assets/Scripts/Net/GameBootstrap.cs`
- `Source/문어_유한맵/Assets/Scripts/Net/NetworkPrefabFactory.cs`
- `Source/문어_유한맵/Assets/Scripts/Net/RelaySession.cs`

## 관련 파일

- `Source/문어_유한맵/Assets/DefaultNetworkPrefabs.asset`
- `Source/문어_유한맵/Assets/Scripts/World/OctopusBossPattern.cs`
- `Source/문어_유한맵/Packages/manifest.json`

## `Assets/Scripts/World/GameSession.cs` 진입 함수

- `OnNetworkSpawn` — 145행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `OnNetworkDespawn` — 166행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `CanStart` — 199행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SelectStartingElementRpc` — 108행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `SetReadyRpc` — 218행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `BroadcastActors` — 2111행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)
- `OnActorsMessage` — 2176행 (행 번호는 색인 생성 시점, 수정 후 함수명으로 재검색)

## 범위 검색

```powershell
python tools/project_index.py --category network
python tools/project_index.py --category network --find "검색어"
```

이 파일은 자동 생성된다. 수동 작업 메모는 같은 폴더의 `NOTES.md`에 기록한다.
