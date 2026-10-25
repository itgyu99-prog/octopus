# 우주 · 음양 픽셀 스킬

운석파편, 헬파이어, 정령, 암흑물질 특수요소를 우주(Cosmos=1)와 음양(YinYang=2)으로 바꿨습니다. 5단계 보스 보상은 두 특수요소를 하나씩 줍니다. 30초 쿨타임, 변신 시 즉시 발동, 10단계 패시브 규칙은 기존 시스템을 따릅니다.

## 제작 에셋

- 128×128 RGBA 스프라이트 88장. 각 스프라이트는 실제 64×64 픽셀 그리드에서 Nearest Neighbor로 2배 확대했으며 알파는 픽셀 단위로 분리돼 있습니다.
- 8 프레임 가로 시트 11종: 별 생성, 별 반짝임, 유성 투사체, 이동 파편, 충돌 섬광, 검은 음, 흰 양, 회전 궤적, 충돌, 대폭발, 원형 충격파.
- 각 스프라이트는 별도 프레임 PNG, 시트 PNG와 투명 배경 GIF 미리보기도 제공합니다.
- [cosmos_sheet.png](cosmos_sheet.png), [yin_yang_sheet.png](yin_yang_sheet.png)는 검수용입니다. 실제 시트 프레임은 모두 투명 배경입니다.
- [comparison.png](comparison.png)는 원본 참고 이미지와 Unity 게임 렌더의 비교입니다.
- [runtime_preview.gif](runtime_preview.gif)는 Unity 카메라로 캡처한 우주와 음양의 동시 미리보기이며, [cosmos_gameplay.gif](cosmos_gameplay.gif)와 [yin_yang_gameplay.gif](yin_yang_gameplay.gif)는 각 스킬의 게임 캡처입니다.
- [validation.json](validation.json)은 프레임 수와 Unity 검증 결과를 기록합니다.

`Assets/Resources/Art/CelestialAtlas.bytes`는 Unity 런타임 아틀라스입니다. `CelestialArt`가 Point 필터, Clamp, 밉맵 없는 텍스처에서 중앙 피벗으로 각 프레임을 만듭니다. 일반 PNG 임포트 시에도 Filter Mode=Point, Compression=None, Generate Mip Maps=Off를 사용하세요.

## 게임 효과

**우주:** 1단계에 54개 별을 넓게 순차 생성합니다. 생성된 별은 주변 적을 초당 24유닛으로 추적하고, 적이 처치되면 다음 대상으로 방향을 바꿉니다. 명중 시 반경 1.3, 피해 65의 별빛 폭발을 냅니다. 피해와 액터 위치는 서버가 관리합니다.

**음양:** 플레이어 양쪽에 흑백 태극 구체가 반경 5.85의 원형 궤도로 돕니다. 2.8초에 걸쳐 회전 속도가 오르고 원은 좁아집니다. 구체는 플레이어 이동을 따라가며 중앙에서 부딪힙니다. 충돌 후 230 피해, 픽셀 파편 폭발, 원형 충격파를 보여 줍니다. 폭발 피해는 한 번만 계산합니다.

## 검증 및 한계

Unity 메뉴 `문어/우주 음양 적용 검증`에서 시트 로딩, 별 순차 생성과 추적 피해, 조기 음양 피해 차단, 1회 충돌 피해, 효과 소멸, 구식 ID 거부, 보스 보상을 검사합니다. 최신 결과는 `Logs/Celestial/results.txt`에 기록했습니다. 98개 체크가 통과했고 런타임 오류는 없었습니다.

참고 자료는 정지 이미지이므로 실제 원본 애니메이션은 주어지지 않았습니다. 프레임의 시간 순서와 지속시간은 이미지의 연출 단계와 사용자가 적어 준 설명에 맞춰 구성했습니다.
