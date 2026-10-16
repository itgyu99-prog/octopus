# 최근 변경

- 2026-10-13: HUD 속성 4칸, 중복 획득 레벨 표시 제거, 합성창 6개 조합, 재료 유지 및 3칸 안내를 반영했다.
- 보상 UI에 1/2·2/2 선택 차수와 동료 완료 상태를 표시한다. 속성 강화 카드에 피해·반복 발사·불 범위 효과를 표시한다.
- 혼자 테스트 시작 기능을 유지한다. 특수요소 안내는 5보스 처치 조건이며 개발 메뉴도 같은 해금 조건을 따른다.

## 2026-10-13 시야·속성 몬스터·대시

- HUD에 Shift 대시 현재/최대 횟수, 다음 충전까지 남은 시간과 게이지 추가.

## 2026-10-14 Esc 우선순위 및 나가기 확인

- `OctopusPlayer.Update`가 플레이 중 Back 입력을 먼저 소비한다. `GameHud.HandleBack`는 개발 메뉴 → 합성창 닫기 → 필수 선택/전환 무시 → 나가기 확인 순서로 처리하며 같은 프레임 중복 호출을 무시한다.
- `DevMenu.CloseIfOpen`, `CombinePanel.CloseCombineIfOpen`, `GameHud.BuildExitConfirmation`을 연결했다. 확인창 Esc는 취소이며 명시적인 나가기 버튼만 LeaveToLobby를 호출한다. 확인 중 이동/공격/합성/개발 메뉴 입력을 차단하며 게임 시뮬레이션은 계속 진행한다.
- 방장 확인창은 친구의 연결도 종료됨을 알린다. 참가자는 자신만 나가는 문구를 표시한다. WaitingRoom의 즉시 나가기는 플레이 중 중복 실행되지 않도록 제한했다. Hide는 확인창/합성창을 초기화한다.
- Unity 에디터 Play Mode 자동 검사에서 우선순위·중복 프레임·선택/전환·확인/취소·다음 세션 초기화·명시적 호스트 종료 통과. 참가자의 실제 이탈은 두 PC 검증이 남아 있다.

## 2026-10-16 전기 UI 원격 확인

- origin/main(수정 전 772c006)에도 GameplayRules.BasicElements의 Electric, WaitingRoom의 4개 속성 버튼, GameHud의 4슬롯 아이콘 연결, Art/Element_Electric.bytes와 meta가 추적되어 있었다. 누락 아님.
- 원격 TreeAbility.cs의 `damage * rank( == 5` 문법 오류가 전체 스크립트 컴파일을 막고 있어 `damage * (rank == 5`로 수정. 팀원은 main을 pull하고 Source/문어_유한맵을 열어 컴파일 완료 후 실행해야 한다. 팀원 PC의 실제 프로젝트/브랜치는 미확인.
