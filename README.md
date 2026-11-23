# 애식 하이먼 (Aesik Haiman) - 엘소드 팬 캐릭터 2D 액션 RPG

엘소드(Elsword) 세계관의 팬 캐릭터 "애식 하이먼"을 주인공으로 한 2D 액션 RPG 프로토타입.
몬스터에게 점령당한 온천 마을의 네 구역을 "스파 가디언" 클래스의 스킬로 하나씩 탈환한다.

**플레이**: https://worlgom.itch.io/aesikhaiman (브라우저에서 바로 실행)

## 게임 구조

4개의 독립된 스테이지(씬)로 구성되며, 씬 전환 간에도 점수와 경과 시간이 유지된다.

| 스테이지 | 적 구성 | 클리어 조건 |
|---|---|---|
| Stage1 | 몹 3마리 | 전멸 시 문(StageDoor) 파괴 가능 → Stage2 |
| Stage2 | 몹 4마리 (수호 골렘 1 포함) | 전멸 시 포탈(StagePortal) 오픈 → Stage3 |
| Stage3 | 몹 4마리 (수호 골렘 1 포함) | 전멸 시 문 파괴 가능 → Stage4 |
| Stage4 | 몹 2마리 + 보스 1마리 | 보스 처치 시 게임 클리어 |

## 조작법

| 키 | 기능 |
|---|---|
| ← / → | 좌우 이동 |
| ↑ / Space | 점프 (2단 점프 가능) |
| Z | 기본 공격 (3타 콤보) |
| X | 우산 콤보 특수 공격 (찌르기 → 펼치기 → 회전) |
| V | 강수량 게이지 축적 (누르고 있는 동안) |
| Q / W / E / R | 스파 가디언 스킬 5종 (첨벙첨벙 / 계란 먹어 / 왜 목말라 / 온천수가 / 온천의 공주) |
| Esc | 일시정지 |

## 폴더 구조

```
Assets/
  Scripts/
    Player/     - PlayerController(이동/점프/콤보), PlayerHealth(피격/넉백/무적)
    Enemy/      - TestDummy(일반 몹/수호 골렘), BossMonster, BossSpawner
    Stage/      - StageDoor, StagePortal, StageKillTracker (스테이지 클리어 조건)
    Skills/SpaGuardian/ - 스파 가디언 스킬 5종 (각 스킬 전용 VFX 포함)
    Systems/    - RainGaugeSystem, BuffSystem, HealSystem
    Game/       - GameManager(스코어/타이머/게임오버/클리어), GameProgress(씬 전환 간 상태 유지)
    Audio/      - AudioManager
    UI/         - 디버그/스코어/게임오버/클리어 UI
    Debug/      - 테스트용 디버그 키 (H/N/M/B/J)
  Editor/       - ProjectSetupTool(4개 스테이지 씬 전체 자동 생성), WebGLBuildTool(itch.io용 WebGL 빌드)
  Sprites/      - 몬스터/보스/배경/스킬 이펙트 아트 (AI 생성 + 배경 제거 가공)
  Prefabs/      - Player, 몬스터/보스, 스킬 이펙트 프리팹
  Scenes/       - Stage1~Stage4.unity
```

## 사용 기술

- Unity 6000.3.18f1 (C#), 2D 스프라이트 렌더링
- AI 이미지 생성(ChatGPT, Perchance) 기반 아트 파이프라인
- Python(numpy/PIL/scipy) 기반 알파 배경 제거 스크립트

## 열기

Unity Hub에서 "Add" → 이 폴더(`AesikHaiman`) 선택. Unity 6000.3.18f1 필요.
Build Settings에 Stage1~4 씬이 순서대로 등록되어 있음.

씬 전체(프리팹/아트 포함)는 `Tools > Aesik Haiman > Setup Test Scene` 메뉴로 언제든 재생성 가능
(기존 씬을 새로 덮어씀 - 커스텀 배치를 했다면 주의).
