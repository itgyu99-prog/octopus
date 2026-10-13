# 검증 결과

2026-10-13, Unity 6000.5.7f1 / Windows.

- `Build/Validation/AshenOath.exe` 개발 빌드 컴파일 성공, 종료 0.
- `Build/Windows/AshenOath.exe` 일반 빌드 성공, 오류 0, 종료 0. 일반 실행 파일을 별도 실행해 8초 이상 시작 화면을 유지하고 런타임 오류가 없음을 확인한 뒤 검증 프로세스만 종료했다. 로그는 `windows-build.log`, `windows-smoke.log`다.
- `-oath-selftest` 실제 실행: 25개 검사 통과, `OATH_ALL_CHECKS_PASS`, 종료 0.
- 확인 항목: 시작 화면, 새 게임, 실제 룬 접촉 획득, 네 정령 표시/추종, 일시정지, 회피/쿨타임/무적, 각 정령 3초 쿨타임 및 적 피해, 경험치→축복→적용, 처치→서약 충전→해방, 최종 파동 보스 생성, 보스 처치 승리, 체력 0 패배, 재시작 초기화, 처음 화면 복귀.
- `Build/Validation/Screenshots/title.png`, `contracts.png`, `blessing.png`, `victory.png` 캡처. 시작/전투/강화 화면의 한글·배치·정령 표시를 직접 이미지로 확인했다.
- 초기 화면 검사 후 석조 바닥과 개별 속성 표식을 개선하고 재빌드·전체 검사 통과를 확인했다.
- 고정 로그: `Source/정령기사/Logs/validation-build.log`, `play-check.log`. 로그/빌드/스크린샷은 Git 소스 커밋에서 제외.
- 자동 검사는 입력 함수와 실제 게임 루프를 사용한다. 장시간 수동 생존 플레이 및 사람 기준 난이도 조정은 아직 수행하지 않았다. 이 게임은 1인용이므로 협동/Relay 검증 대상이 아니다.
