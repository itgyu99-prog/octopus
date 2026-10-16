# 회색 문어 캐릭터 에셋

사용자 그림의 회색 몸체·둥근 머리·짧은 네 촉수·3자 입을 기준으로 만든 픽셀 에셋이다. 현재 게임 캐릭터에는 미적용 상태다.

## 투명 PNG

| 동작 | 스프라이트 시트 | 프레임 수 | 권장 시간 |
|---|---|---:|---|
| 대기 | [idle-64.png](idle-64.png) | 4 | 350 / 200 / 90 / 350 ms |
| 이동 | [walk-64.png](walk-64.png) | 4 | 각 130 ms |
| 멈춤 | [stop-64.png](stop-64.png) | 4 | 100 / 90 / 120 / 650 ms |
| 피격 | [hit-64.png](hit-64.png) | 4 | 100 / 90 / 140 / 750 ms |
| 승리 | [victory-64.png](victory-64.png) | 8 | 150 / 120 / 170 / 120 / 130 / 120 / 150 / 280 ms |

각 시트는 64×64 셀을 가로로 붙인 RGBA PNG다. 정규화된 발 기준 pivot=(0.5, 0.125). 원본 생성 시트는 source/에 보존했다. 셀 분리·공통 크기 조정은 원본을 다시 그리지 않고 nearest-neighbor로 수행했다. 동작별 원본 비례를 유지하고 프레임 간 발 기준을 맞췄다.

Unity 가져오기 권장: Sprite (2D and UI), Multiple, Grid by Cell Size 64×64, Filter Mode Point, Compression None, Generate Mip Maps 끔. Pixels Per Unit 48을 시작점으로 삼고 게임의 기존 캐릭터 월드 크기에 맞춘다. 피격·멈춤은 앞 3프레임을 한 번 재생한 뒤 마지막 프레임에서 대기로 전환한다. GIF의 마지막 긴 대기 시간은 미리보기의 반복 간격이다.

## 미리보기

- [전체 동작 GIF](all-states-preview.gif)
- [승리 춤 GIF](victory-preview.gif)
- [대기](idle-preview.gif), [이동](walk-preview.gif), [멈춤](stop-preview.gif), [피격](hit-preview.gif)

승리 모션은 [ATEEZ 공식 BAD Choreography Demo (SAN Ver.)](https://www.youtube.com/watch?v=vcbIP2YvDOo)의 상체 기울임·손을 이마 쪽으로 올리는 동작·가슴 앞 팔 동작을 짧은 문어 촉수 동작으로 각색했다. 원곡 음원이나 영상 프레임은 에셋에 포함하지 않았다.

제작: imagegen 내장 도구. 정확한 생성 프롬프트는 [prompts.json](prompts.json), 셀 크기·피벗·프레임별 시간은 [manifest.json](manifest.json)에 기록했다. PNG 알파와 시트 크기, 모든 GIF의 프레임 디코딩을 확인했다.
