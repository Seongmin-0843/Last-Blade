# Last Blade — 2D 뱀서라이크

이 문서(CLAUDE.md)가 Last Blade의 최신 기획서이자 구현 기준이다. (초기 버전 PDF: `docs/Last_Blade_기획서.pdf`)
수치나 규칙이 다르면 **이 문서가 최우선**이다.

## 개요

2D 탑다운 뱀서라이크. 무기는 **칼 하나뿐**. 사각형으로 제한된 맵.
**모바일 전용 (Android + iOS), 가로 고정** / 트윈스틱: 왼쪽 스틱 이동 + 오른쪽 스틱 조준·공격.
⚠️ 기획서의 "PC(Windows) / 1920×1080 고정 / WASD + 마우스"를 대체함 (2026-11-25 변경).
승리 조건 없음 — 죽을 때까지의 **생존 시간이 곧 점수**. 한 판 8~12분.

**이번 버전에서 안 하는 것**: 칼 외 무기, 캐릭터 선택, 상점, 영구 성장, 재화,
스테이지 구분, 보스 패턴, 멀티플레이, 스토리.

## 프로젝트 규칙 (기획서 밖, 사용자가 정한 것)

- **메인 게임 씬은 `Assets/Scenes/GameScene.unity`.** 여기서 작업한다.
- **모든 스크립트는 `Assets/Scripts/` 안에 만든다.** (에디터 전용은 `Assets/Scripts/Editor/`)
- 스프라이트는 전부 `Assets/Sprites/` 아래. 픽셀 아트이므로 임포트 설정 통일:
  **PPU 16 / Filter Mode Point / Compression None / Mesh Type Full Rect / Mip Maps off.**
  → 16×16 스프라이트 = 정확히 1×1 월드 유닛.
  새 스프라이트를 넣으면 이 4개를 똑같이 맞춰줄 것.

### 카메라 (Pixel Perfect)

`Main Camera`에 URP `PixelPerfectCamera` 부착. 설정:

| 항목 | 값 |
|---|---|
| Assets PPU | 16 |
| Reference Resolution | 320 × 180 |
| Grid Snapping | **None** |
| Filter Mode | Point |
| Crop Frame | None |

- 1920×1080 ÷ 320×180 = **정확히 ×6 정수 배율** → 픽셀 깨짐 없음.
- 그 결과 Orthographic Size = 180 / (2×16) = **5.625**, 화면에 보이는 영역 = **20 × 11.25 units**.
- ⚠️ 기획서 §3의 "Ortho Size 5 → 17.8×10 units"는 **이 값으로 대체됨**.
- **맵 = 30 × 18 units, 원점 중심** (x ∈ [-15, 15], y ∈ [-9, 9]).
  1.5배 규칙으로는 30×16.875지만, PPU 16에서 타일 1칸 = 1 unit이라 정수 칸이 되게 18로 올림.
  타일 칸: x = -15..14 (30칸), y = -9..8 (18칸).
- 2D Renderer(`Assets/Settings/Renderer2D.asset`)의 Transparency Sort Mode = **Custom Axis (0,1,0)**.
  탑다운이라 Y가 낮은(화면 아래) 오브젝트가 앞에 그려진다.
- URP/해상도 쪽은 이미 맞게 돼 있음: MSAA Disabled, Render Scale 1,
  가로(Landscape Left/Right)만 허용, 세로 회전 꺼짐.
  기기 해상도는 제각각이다. PixelPerfectCamera가 정수 배율(세로 1080이면 ×6)을 고르고 남는 만큼 화면이 더 보인다
  (예: 2400×1080 → 25 × 11.25 units). 맵 30×18보다 크게 보이는 기기는 없어서 클램프·스폰은 그대로 맞다.
  에디터에서 모바일 화면을 볼 때는 **Device Simulator**로 볼 것(아래 "모바일 조작" 참고).

## 플레이어

| 스탯 | 값 |
|---|---|
| 최대 체력 | 100 |
| 이동 속도 | 5 units/s (8방향, 대각선 정규화) |
| 젬 흡수 반경 | 1.5 units |
| 피격 후 무적 | 0.5초 (무적 중 스프라이트 깜빡임) |

- 조작: 이동 왼쪽 스틱, 조준·공격 오른쪽 스틱, 대쉬(획득 후) 화면 버튼, 일시정지 화면 버튼(M6).
  PC(에디터 Game 뷰·PC 빌드)에서는 원래 조작: WASD 이동, 커서 조준 + 좌클릭 공격, Space 대쉬, 레벨업 1/2 키, 게임 오버 R.
- **바라보는 방향 = 조준(오른쪽 스틱) 기준 X flip.** 조준이 왼쪽이면 왼쪽, 오른쪽이면 오른쪽. 스틱을 놓으면 마지막 조준을 유지.
  이동 방향과 무관.
- 적과 닿으면 접촉 피해 + 무적 적용.
- 카메라는 부드럽게 추종하되 맵 경계에서 클램프(맵 밖이 안 보이게). 플레이어·적 모두 경계 밖 불가.
- 화면 = 20 × 11.25 units, 맵 = 30 × 18 units. (위 "카메라" 절 참고 — 기획서 수치에서 갱신됨)
- 경계는 **콜라이더 벽이 아니라 코드 Clamp** (`MapBounds.Clamp`). 적 300마리가 벽에 밀착할 때 물리 비용을 안 쓰려고.
- **플레이어 이동은 `FixedUpdate`가 아니라 `Update`에서, `Transform`으로 직접 한다.** (되돌리지 말 것)
  Rigidbody2D는 Kinematic으로 남겨두되 `Interpolate = None`이고 이동에 쓰지 않는다. 적 접촉도 물리가 아니라 `Enemy`의 거리 계산으로 판정한다.
  이유: 물리는 50Hz(Fixed Timestep 0.02), 카메라 `SmoothDamp`는 렌더 프레임(60Hz).
  서로 시계가 달라서 **플레이어만** 화면에서 떨려 보였다. 둘 다 렌더 프레임으로 맞추니 해결.
  (`MovePosition` + `Interpolate` 조합으로도 시도했으나 보간 결과는 꺾은선이라 60Hz 지수 곡선과 여전히 어긋남)
- 구석에 몰리는 건 난이도의 일부. 별도 장치 없음.

## 칼 공격

| 항목 | 값 |
|---|---|
| 방향 | 오른쪽 스틱 방향 |
| 범위 | 반지름 1.8 units, 각도 180° 부채꼴 (⚠️ 기획서의 90°를 대체함, 2026-11-01 변경). 적 몸통이 걸치면 맞는다 |
| 피해 | 10 |
| 넉백 | 0.3 units |
| 공격 딜레이 | 1초 |
| 타격 판정 | 칼을 휘두르는 0.3초 동안 매 프레임 범위 조회(콜라이더 ON/OFF 대신), 한 스윙당 적당 1회만 피해. 범위는 휘두른 순간의 위치에 고정(플레이어가 움직여도 따라가지 않음). ⚠️ 기획서의 0.15초를 대체함 |

- **오른쪽 스틱을 당기는 동안 조준, 손을 떼는 순간 1회 휘두름.** 홀드 연사 없음, 딜레이 중에 떼면 무시.
  스틱을 `aimThreshold`(0.3) 넘게 당겨야 조준으로 친다(살짝 건드린 건 무시).
- 칼을 상시로 들고, 칼끝이 조준 방향을 가리켜 공격 방향을 미리 보여준다. ⚠️ 기획서의 "조준 미리보기 없음"을 대체함 (2026-11-01 변경).
- **타격감 (작지만 핵심)**: 칼 궤적(TrailRenderer) / 적 흰색 점멸 0.1초 / 히트스톱 0.03초(스윙당 첫 타격에 1회) /
  피해 숫자 팝업 / 처치 시 작은 파티클.

## 적 (3종, 전부 몸통 박치기 근접)

| | 체력 | 이동 속도 | 접촉 피해 | 경험치 | 스폰 간격 | 등장 시간 |
|---|---|---|---|---|---|---|
| A | 10 | 1.5 | 5 | 1 | 0.5초 | 0:00~ |
| B | 30 | 2.0 | 8 | 1 | 0.7초 | 1:00~ |
| C | 80 | 2.5 | 12 | 1 | 1.0초 | 3:00~ |

**스폰 규칙**
- 맵 안쪽 가장자리에 고정 스폰 포인트 8개(모서리 4 + 각 변 중앙 4). 씬 오브젝트 목록으로 관리해
  나중에 추가·삭제·이동 가능하게.
- 종류별로 **독립 타이머**. 자기 등장 구간일 때만, **현재 카메라 화면 안에 들어온 포인트는 제외**하고
  랜덤 1곳에 1마리 생성.
- 동시 존재 최대 300마리. 오브젝트 풀링 필수.

**난이도 곡선** (ScriptableObject로 분리 — 코드 수정 없이 튜닝)

⚠️ 기획서의 등장 구간(A 0:00–3:00, B 1:00–6:00, 3:00 이후 A 제외, 6:00 이후 C만)은 **이 표로 대체됨** (2026-11-01 변경).
한번 등장한 적은 사라지지 않고 페이즈마다 종류가 누적된다.

| 페이즈 (경과) | 등장 | 체력 배율 |
|---|---|---|
| 1페이즈 (0:00–1:00) | A | ×1.0 |
| 2페이즈 (1:00–3:00) | A, B | ×1.2 |
| 3페이즈 (3:00–6:00) | A, B, C | ×1.5 |
| 3페이즈 (6:00~) | A, B, C | ×2.0에서 시작, 매분 +15% |

## 레벨업 & 능력

- 레벨 **0에서 시작, 최대 10** → 레벨업과 선택은 정확히 **10번**.
- 필요 경험치 = `5 + 4 × L`. (0→1은 5, 9→10은 41, 총 230)
- 젬은 A·B·C 전부 경험치 1로 동일. **체력 회복 아이템 없음.**
- 레벨업 시 `timeScale = 0`, 무작위 **2장 중 1장** 선택. 두 장은 서로 다름.
- 이미 가진 스킬이 뜨면 새로 얻는 대신 **그 스킬을 강화**.
- 3단계까지 올린 항목은 선택지 풀에서 **제외**.
- 레벨 10 이후 젬은 계속 흡수되지만 레벨업 없음. 경험치 바에 `MAX` 표시.
- **항목 8개 × 3단계 = 24단계인데 선택은 10번뿐 → 무엇을 고르느냐가 빌드 차이.** (의도된 설계)

### 스킬 4종 (획득이 1단계, 최대 3단계)

| 스킬 | 1단계 (획득) | 단계마다 | 3단계 |
|---|---|---|---|
| 대쉬 | 화면 버튼. 이동 방향(정지 중이면 조준 방향)으로 3 units 돌진. 쿨타임 8초 | 쿨타임 −2초 | 쿨타임 4초 |
| 검기 날리기 | 칼을 **5번째** 휘두를 때마다 조준 방향으로 검기 1개 발사. 피해 칼의 50%, 적 관통, 사거리 9 units (⚠️ 기획서의 6을 대체함, 2026-11-02 변경) | 검기 개수 +1 (30° 간격 부채꼴. ⚠️ 기획서의 ±15°를 대체함, 2026-11-02 변경) | 3개 |
| 회전 베기 | 칼을 **3번째** 휘두를 때마다 주변 360° 베기. 피해 칼의 80%, 반지름 2 units | 반지름 +0.5 units | 반지름 3 units |
| 냉기 속성 부여 (기획서 밖, 2026-11-17 추가) | 기본 공격에 맞은 적이 30% 확률로 3초 빙결(정지·접촉 피해 없음, 넉백은 받음). 칼 잔상이 푸르게 바뀌고 휘두를 때 얼음 결정이 흩날림, 적이 얼 때도 결정이 튐 | 확률 +10% | 50% |

검기·회전 베기의 스윙 카운트는 **획득한 순간부터** 센다.

### 능력치 4종 (최대 3단계)

| 능력치 | 1단계당 | 3단계 |
|---|---|---|
| 이동 속도 | +10% | +30% (5 → 6.5 units/s) |
| 체력 | 최대 체력 +20 (올린 만큼 즉시 회복) | +60 (100 → 160) |
| 공격 속도 | 공격 딜레이 −10% | −30% (1초 → 0.7초) |
| 공격력 | 칼 피해 +20% (검기·회전 베기에도 적용) | +60% |

※ 공격 속도가 오르면 스윙 횟수가 늘어 **검기·회전 베기 발동도 같이 빨라진다** (의도된 시너지).

## UI / 화면

화면 3개: **타이틀 / 인게임 / 게임 오버**. 레벨업 선택창·일시정지는 인게임 위에 겹치는 팝업.

- **타이틀** — 제목, 시작, 종료, 최고 기록(생존 시간·처치 수)
- **인게임 HUD** — 좌상단 체력 바 / 상단 전체 폭 경험치 바 + 레벨(10이면 MAX) /
  상단 중앙 생존 시간 `mm:ss` / 우상단 처치 수 / 하단에 보유 스킬·능력치 작은 아이콘 + 레벨
- **레벨업 팝업** — 카드 2장. 탭해서 선택.
  카드 내용: 이름, 아이콘, 신규는 `NEW` 표시, 강화는 `현재 단계 → 다음 단계`, 한 줄 설명(예: "공격 딜레이 −10%")
- **일시정지** — 화면의 일시정지 버튼(`pause_button`). 계속하기 / 타이틀로
- **게임 오버** — 생존 시간, 처치 수, 도달 레벨, 갱신 시 `NEW RECORD`, 다시 하기 / 타이틀

## 기록

- 저장: `PlayerPrefs`. 서버·랭킹 없음.
- 항목: 최고 생존 시간(초), 최다 처치 수, 최고 레벨.
- 시점: 게임 오버 직후, 이전 기록보다 클 때만.
- 플레이 중 생존 시간이 최고 기록을 넘기는 순간 HUD에 "신기록!" 잠깐 표시.

## 기술 구조

씬 2개 (타이틀, 게임).

| 스크립트 | 역할 |
|---|---|
| `GameManager` | 게임 상태(진행/일시정지/오버), 생존 시간, 처치 수 |
| `PlayerController` | 이동, 맵 경계 제한, 조준 기준 X flip |
| `PlayerHealth` | 체력, 피격, 무적 시간, 사망 이벤트 |
| `SwordAttack` | 오른쪽 스틱 조준·놓으면 공격, 딜레이 타이머, 부채꼴 판정, 스윙 횟수 카운트(검기 5·회전 3) |
| `Enemy` | 추적 이동, 피격·사망, 젬 드롭 |
| `EnemySpawner` | 종류별 타이머, 랜덤 스폰 포인트, 시간 구간 적용, 풀 사용 |
| `ExpGem` / `ExperienceManager` | 젬 흡수, 경험치 계산, 레벨업 이벤트 |
| `AbilityData` (SO) | 스킬·능력치 이름, 아이콘, 단계별 수치 |
| `UpgradeManager` | 레벨업 시 무작위 2개 추첨, 선택 반영, 최대 레벨 10 제한 |
| `UIManager` / `SaveManager` | HUD 갱신, 팝업 / 최고 기록 저장 |
| `PlayerSkills` + `DashSkill` / `SwordWaveSkill` / `SpinSlashSkill` | 보유·단계 관리 + 개별 스킬 |

**규칙**
- 적·젬·피해 숫자·검기는 `UnityEngine.Pool.ObjectPool`로 재사용.
  **플레이 중 `Instantiate`/`Destroy` 금지.**
- 적끼리는 충돌시키지 않는다 (레이어 분리). 플레이어와의 접촉만 판정.
- 카메라는 맵 경계로 클램프.
- 성능 목표: **적 300마리에서 60fps** (모바일 실기기 기준, `GameManager.targetFrameRate`로 60을 직접 켠다. 모바일 기본은 30).
- **수치를 코드에 직접 쓰지 말 것.** 플레이어·칼·스킬·능력치·적·스폰·경험치·맵 크기·카메라 등
  웬만한 전부를 ScriptableObject 또는 Inspector 노출 필드로 분리. (기획서가 반복 강조하는 요구사항)

**환경** (기획서보다 최신 — 실제 프로젝트 기준)
- Unity **6000.3.27f1**, URP 17.3 (2D Renderer), Input System 1.20 (`Assets/Settings/InputSystem_Actions.inputactions` 기설치)
- 기획서에는 "Unity 2022 LTS 이상 / Input Manager 또는 Input System 중 편한 쪽"으로 적혀 있지만
  → **Input System으로 간다.**
- 빌드: Android(IL2CPP, ARM64) + iOS. 가로 고정. Unity 6000.3.27f1에 Android·iOS 모듈 설치돼 있음.
  **iOS 최종 빌드는 Mac + Xcode가 필요하다** (Windows에서는 Xcode 프로젝트까지만 뽑힌다).
  번들 ID(`com.DefaultCompany...`)는 스토어에 올리기 전에 정할 것.
- **웹(itch.io)**: Web 빌드. 압축 Gzip + **Decompression Fallback 켬**(itch.io는 Content-Encoding 헤더를 안 보내서 없으면 로딩에서 멈춤).
  웹에서는 `targetFrameRate`를 무시하고 -1(브라우저 갱신)로 돈다. 빌드 폴더 **내용물**을 zip(루트에 index.html), itch.io는 HTML 프로젝트 /
  Viewport 960×540(320×180의 ×3) / Mobile friendly + Landscape / Fullscreen button.
  WebGL 템플릿은 `Assets/WebGLTemplates/FillWindow`(캔버스가 창 전체를 채움, 하단 바 없음). 기본 템플릿은 960×600 고정 + 하단 바라 itch 임베드에서 잘렸다.
  ⚠️ `Assets/Settings/Build Profiles/*.asset`은 Player Settings를 따로 들고 있다. 웹 설정을 바꾸면 프로필 쪽도 같이 바꿀 것.

## 마일스톤

"칼로 적을 베는 재미"를 먼저 확인하고 시스템을 얹는 순서. 각 단계는 완료 기준을 만족하면 다음으로.

| | 작업 | 완료 기준 |
|---|---|---|
| M1 | 프로젝트 세팅, 플레이어 이동, 카메라 추종, 사각 제한 맵 + 경계 클램프 | 맵 위를 자유롭게 돌아다닐 수 있다 |
| M2 | 마우스 클릭 칼 공격(90° 부채꼴), 적 A 추적·피격·사망 | 칼로 적을 베는 맛이 있다 |
| M3 | 스폰 포인트 시스템, 오브젝트 풀링, 플레이어 체력·피격·게임 오버 | 적 100마리가 몰려와도 끊김 없이 죽을 수 있다 |
| M4 | 경험치 젬, 레벨업 2택 팝업, 스킬 3종·능력치 4종, 최대 레벨 10 | 한 판 안에서 빌드가 달라진다 |
| M5 | 적 B·C 추가, 시간대별 등장 구간과 난이도 곡선 | 10분 이상 플레이가 의미 있게 어렵다 |
| M6 | UI/HUD 정리, 최고 기록 저장, 사운드·이펙트, 밸런스 조정 | 처음 보는 사람이 설명 없이 한 판 할 수 있다 |

**현재 진행도: M1 완료 (2026-10-28). M2 완료 (2026-11-01). M3 구현 완료 (2026-11-01). M4 구현 완료, 사용자 플레이테스트 대기 (2026-11-02). M5 중 적 B·C 추가 완료 (2026-11-20), 페이즈별 체력 배율(난이도 SO)은 남음.**
**2026-11-25: 모바일(트윈스틱) 전환 + PC 조작 병행, 세로 안내, 임시 시작 화면, 웹(itch.io) 빌드 설정. 실기기 플레이테스트 진행 중.**

### M1에서 만든 것

`Assets/Scripts/`
- `MapBounds.cs` — 맵 사각형(30×18) 한 곳에서 관리. `Clamp(pos, inset)`을 플레이어와 카메라가 공유한다.
  `OnDrawGizmos`로 씬 뷰에 노란 테두리를 그린다(타일 칠하는 범위 기준).
- `PlayerController.cs` — 이동(대각 정규화), 경계 Clamp, 조준 기준 X flip. (M1 당시엔 WASD·커서였고 2026-11-25 모바일 전환)
- `CameraFollow.cs` — `SmoothDamp` 추종 + 화면 절반만큼 안쪽으로 Clamp(맵 밖이 안 보이게). `smoothTime` 기본 0.15.

`Assets/Scenes/GameScene.unity`
```
Main Camera   PixelPerfectCamera + CameraFollow(target=Player, bounds=Map)
Global Light 2D
Grid > floor  Tilemap (Order in Layer -10), Floor_Random으로 x -15..14 / y -9..8 전부 칠함
Player        knight_idle_anim_f0 / Rigidbody2D(Kinematic, Interpolate None) / CircleCollider2D(r=0.3) / PlayerController
Map           MapBounds (30 x 18)
```

`Assets/Sprites/tiles/tilesmap/Floor_Random.asset`
- **Rule Tile**. 규칙 1개, Output = Random, floor_1~10 스프라이트 10장. Collider Type = None.
- 칸마다 랜덤 바닥이 깔린다. 랜덤은 칸 좌표 해시라 매번 같은 결과(씬 다시 열어도 안 바뀜).
- 바닥 종류를 바꾸고 싶으면 **이 에셋 하나만** 고치면 이미 깔린 맵 전체가 바뀐다.
- 벽도 하려면 Random이 아니라 **RuleTile 이웃 규칙**으로 모서리·변을 자동 연결하는 게 맞다.

### 입력
프로젝트 전역 Input Actions를 코드에서 `InputSystem.actions.FindAction("Player/Move", true)`로 가져온다.
Inspector 참조 없음(`Project Settings > Input System Package`에 이미 등록돼 있음).
새 액션은 `Assets/Settings/InputSystem_Actions`에 추가할 것.

### 모바일 조작 (2026-11-25)
Input System의 **OnScreenStick / OnScreenButton**이 가상 게임패드에 값을 넣고, 게임 코드는 액션만 읽는다(터치 코드 없음).

| 화면 | 컨트롤 경로 | 액션 |
|---|---|---|
| `Canvas/LeftStick/Knob` (왼쪽 아래) | `<Gamepad>/leftStick` | `Player/Move` |
| `Canvas/RightStick/Knob` (오른쪽 아래) | `<Gamepad>/rightStick` | `Player/Look` → `SwordAttack` |
| `Canvas/DashButton` (오른쪽 스틱 왼쪽) | `<Gamepad>/buttonEast` | `Player/Dash` (획득 전엔 꺼져 있고 `DashSkill.Upgrade`가 켬) |

- 스틱은 바탕 원(장식, 지름 420) + `Knob`(실제 터치 영역, 지름 220) 구조, 이동 범위 140, 화면 가장자리에서 300. 대쉬 버튼 지름 220. (2026-11-25 폰에서 작다고 해서 키움) 그림은 Unity 기본 `Knob` 스프라이트를 반투명으로 쓴다(전용 아트 없음).
- 조작 UI는 Canvas에서 `LevelUpPanel`보다 앞에 둬서 레벨업 창이 덮고 터치도 막는다.
- Canvas Scaler는 **Match = Height(1)**. 가로 폰은 세로 1080 기준으로 맞추고 넓은 만큼 좌우로 늘어난다.
- **조작 모드는 `GameManager.TouchControls`(= `Application.isMobilePlatform`)로 갈린다.**
  - 에디터 **Game 뷰** → PC 조작. 화면 스틱(`Canvas/TouchControls`)은 꺼진다.
  - 에디터 **Simulator 뷰**(Game 탭 왼쪽 위 드롭다운에서 Simulator) → 터치 조작. 마우스가 터치로 들어가서 스틱을 하나씩 끌 수 있다.
  - 폰 빌드 → 터치 조작.
- `Player/Look`에는 `<Gamepad>/rightStick`만 남겼다. 원래 붙어 있던 `<Pointer>/delta`가 마우스 이동·손가락 드래그를 조준으로 넣어서 오작동했었다.
- 휘두르는 도중 조준을 바꿔도 판정 방향은 휘두른 순간(`swingAim`)에 고정된다.
- 두 손가락 동시 조작은 실기기에서 확인할 것.
- **세로 화면 안내**: `Screen.height > Screen.width`이면 `Canvas/PortraitNotice`(검은 화면 + "화면을 가로로 돌려주세요")를 맨 위에 덮고 게임을 멈춘다(`GameManager`, `InputBlocked`에 포함).
  웹(itch.io) 빌드는 방향을 고정할 수 없어서 넣었다. 세로로 보면 맵(18 units)보다 위아래가 더 보여 맵 밖이 드러난다.
- 노치(Safe Area)는 따로 처리하지 않았다. 스틱이 화면 가장자리에서 300 떨어져 있어 대부분 안 가려진다.

### M2에서 만든 것
- `SwordAttack.cs` — 조준 입력(`Player/Look`), 공격 딜레이, 부채꼴 판정, 스윙 횟수(`SwingCount`), 칼 회전. 평소에는 칼끝이 조준 방향을 가리킨다.
- 칼 궤적은 `SwordPivot` 자식 `SwordTrail`의 **TrailRenderer**(휘두르는 동안만 emitting). 처음엔 `slash_effect_anim` 스프라이트였는데 VFX로 교체함(2026-11-17, 사용자 결정). 회전 베기·검기는 여전히 스프라이트.
- `Enemy.cs` — 추적, 넉백, 흰색 점멸(`Assets/Materials/SpriteFlash`로 머티리얼 교체), 사망. 수치는 프리팹 `Assets/Prefabs/Enemy_A`에 있다.
- `CombatEffects.cs` — 히트스톱, 피해 숫자(`DamagePopup`), 처치 이펙트(`SpriteEffect`). 시작 시 64개씩 미리 만들고, 모자라면 연출만 건너뛴다.
- `Assets/Animations` — 플레이어 Idle/Run, 슬라임 Run. 적은 항상 쫓아오므로 idle 상태를 두지 않았다.
- 슬라임 run·슬래시·처치 이펙트 스프라이트는 프레임마다 잘린 크기가 달라 흔들려서, 슬라이스를 16×16 전체로 통일했다.
- `Enemy` 레이어(6번)는 어떤 레이어와도 물리 접촉을 계산하지 않는다(범위 조회에는 영향 없음). 적 A는 체력 10이라 한 방에 죽어서 점멸·넉백이 안 보인다.

### M3에서 만든 것
- `EnemySpawner.cs` — 스폰 포인트 8개(`EnemySpawner` 자식 오브젝트)에서 화면 밖 포인트만 골라 스폰. 최대 300마리(전 종류 합계).
- `PlayerHealth.cs` — 체력, 무적, 깜빡임, 사망.
- `GameManager.cs` — 게임 오버, 생존 시간, 처치 수, 히트스톱. **`Time.timeScale`은 여기서만 바꾼다.** M4의 일시정지·레벨업도 여기에 추가할 것.
- 체력·시간·처치 수와 게임 오버 표시는 `GameManager.OnGUI`의 임시 화면이고, M6에서 UI로 교체한다. Active Input Handling이 Input System 전용이라 OnGUI 버튼은 클릭을 못 받으므로, 다시 하기는 화면 아무 곳 탭이다.
- 적은 플레이어 중심 0.6 units에서 멈추고, 0.7 units 안이면 접촉 피해를 준다.
- 플레이테스트 후 `moveSpeed`, `smoothTime` 조정

### M4에서 만든 것
- `ExperienceManager.cs` / `ExpGem.cs` — 젬 풀 300개, 흡수 반경 안에 들어오면 플레이어에게 날아간다. 프리팹 `Assets/Prefabs/ExpGem`(potion_yellow).
- `UpgradeManager.cs` — 3단계 미만 항목 중 2개 추첨. `LevelUpUI.cs` — 카드 표시, 탭으로 선택. `PlayerSkills.cs` — 단계 보관, 수치 전달.
- `Assets/Data/*.asset`(`AbilityData`) — 7개 항목의 이름·설명·단계별 수치. 튜닝은 여기서 하고, **수치를 바꾸면 `descriptions` 문구도 같이 고칠 것**(문구는 수치에서 자동 생성되지 않는다).
- `DashSkill` / `SwordWaveSkill`(`SwordWave` 프리팹, slash_effect_anim_f0) / `SpinSlashSkill`(씬의 `SpinSlash` 오브젝트) — Player에 **꺼진 채로** 붙어 있고 획득하면 켜진다. 검기·회전 베기는 `SwordAttack.Swung` 이벤트를 켜진 뒤부터 세므로 "획득한 순간부터 카운트"가 저절로 맞는다.
- `FrostSkill`(냉기 속성 부여, 2026-11-17) — `SwordAttack.Hit`을 듣고 확률로 `Enemy.Freeze`. 검기·회전 베기 타격에는 적용 안 됨. 휘두르는 동안(`SwordAttack.IsSwinging`) 잔상 칼날 위에서 결정을 흩날린다(수치는 `FrostSkill`). 결정 파티클은 `CombatEffects`가 Awake에서 코드로 만드는 ParticleSystem 하나에 `Emit`(머티리얼 `Assets/Materials/FrostShard` = URP Particles/Unlit, 텍스처 없는 사각형을 45° 돌린 마름모. Sprite-Lit-Default는 파티클에서 보라색으로 나옴). 수치는 `CombatEffects`의 빙결 파티클 필드에서 조정.
- `DashCooldownBar.cs` — Player 자식. 대쉬 쿨타임 동안 발밑 바가 차오르고, 다 차면 한 번 반짝이고 사라진다. 그림 파일 없이 흰색 텍스처로 만든 단색 막대이고, 크기·색은 자식 `Background`·`Fill`의 Transform과 SpriteRenderer에서 바꾼다.
- 레벨업 UI는 `Canvas` > `LevelUpPanel`. 카드는 `menu_button`을 Sliced로 늘린 Button이고, 누르면 `menu_button_press`로 바뀐다. 이를 위해 두 스프라이트에 9-slice 테두리를 넣고 press의 슬라이스를 96×32로 맞췄다. Canvas Scaler의 Reference PPU 96은 스프라이트 1픽셀 = 화면 6픽셀(게임 화면과 같은 배율)이 되게 맞춘 값이다.
- EventSystem의 Send Navigation Events는 **꺼져 있다.** 켜면 레벨업 중 게임패드 버튼(대쉬 버튼이 쓰는 buttonEast 등)으로 카드가 눌릴 수 있다.
- **임시 시작 화면** (2026-11-25): `Canvas/StartPanel`의 "게임 시작" 버튼을 눌러야 시작한다. 그전에는 `timeScale` 0이고 `InputBlocked`. 다시 하기(씬 리로드)에도 다시 뜬다. M6 타이틀 씬이 생기면 지울 것.
- `GameManager.InputBlocked` — 게임 오버·레벨업 중 플레이어 입력 차단. 히트스톱 중에는 막지 않는다. 접촉 피해와 젬 흡수는 `deltaTime`이 0이면(멈춤) 동작하지 않는다.
- ⚠️ 레벨업 카드에 **아이콘이 없다.** 전용 아이콘이 없고 임시 그림을 쓰지 않기로 해서(사용자 결정) 비워뒀다. 하단 보유 아이콘도 마찬가지.

### M5에서 만든 것 (진행 중)
- 적 B(`Assets/Prefabs/Enemy_B`, 고블린)·C(`Enemy_C`, 플라잉 크리처) — `Enemy_A`를 복제해 스탯·스프라이트·애니메이터만 바꿨다. 애니메이션 `goblin_run`·`fly`(10fps), 컨트롤러 `Goblin`·`FlyingCreature`.
  고블린 run·fly 스프라이트도 슬라이스를 16×16 전체로 통일했다(슬라임과 같은 이유).
- `EnemySpawner.enemyTypes` — 종류별 프리팹·스폰 간격·등장 시간(`GameManager.SurvivalTime` 기준). 등장하면 계속 나온다. 종류마다 풀이 따로이고, 각각 300개씩 미리 만든다.
- **남은 것:** 페이즈별 체력 배율(난이도 곡선 SO).

## 보유 에셋 매핑

`Assets/v1.1 dungeon crawler 16X16 pixel pack` (16×16 픽셀 던전 크롤러 팩)

- 플레이어 ← `heroes/knight/knight_idle_anim_f0~5`, `knight_run_anim_f0~5`, `weapon_sword_1.png`
- 적 A ← `enemies/slime` (idle/run 6프레임)
- 적 B ← `enemies/goblin` (idle/run 6프레임)
- 적 C ← `enemies/flying creature` (fly 4프레임)
- 이펙트 ← `effects (new)/slash_effect_anim`(칼 궤적), `hit_effect_anim`(타격),
  `enemy_afterdead_explosion_anim`(적 사망), `explosion_anim`
- 배경 ← `tiles/floor/floor_1~10`, 경계 ← `tiles/wall/*`
- UI ← `ui (new)/health_ui`, `menu_button(_press)`, `pause_button(_press)`, `crosshair_1~3`

경험치 젬 ← `props_itens/potion_yellow`, 검기 ← `slash_effect_anim_f0`, 레벨업 카드 ← `menu_button`, `menu_button_press` (사용자 지정).

**부족한 에셋** — 임시 그림을 만들지 않고 비워둔다(사용자 결정):
능력 아이콘 7종(스킬 3 + 능력치 4), 대쉬 잔상, 사운드 전부.

## 주의 / 미해결

1. **PDF 표가 깨져서 추출된다.** §3 플레이어 스탯과 §6 능력치 표가 레이아웃 붕괴로 값이 섞여 나온다.
   위 표의 수치는 §6의 "5 → 6.5 units/s", "100 → 160", "1초 → 0.7초" 역산으로 복원한 것.
   기획서가 갱신되면 이 두 표부터 다시 대조할 것.
2. **스폰 포인트 후보 (M3에서 확인, 문제 없음).** 맵 30×18, 화면 20×11.25에서 화면 안으로 제외되는 포인트는 최대 3개(플레이어가 구석에 있을 때)라 5개 이상 남는다.
   **미해결:** 적끼리 밀어내지 않아서 플레이어를 둘러싼 적이 작은 고리 하나로 겹친다(300마리가 7~8마리처럼 보이고, 한 번에 벨 수 있다).
   플레이테스트 후 대응 여부를 결정할 것.

3. **Grid Snapping은 None이어야 한다 (해결됨, 되돌리지 말 것).**
   처음에 Pixel Snapping으로 뒀더니 플레이어 이동이 눈에 띄게 끊겼다. 원인은
   `PixelPerfectCamera`가 **카메라 위치**(`PixelSnap()`)와 **스프라이트 정점**(`pixelSnapSpacing`)을
   서로 **독립적으로** 픽셀 격자에 반올림하기 때문. 두 반올림의 경계 통과 시점이 어긋나서
   등속 이동 중에도 화면상 위치가 ±1픽셀 튄다. 카메라가 계속 따라다니는 게임에서는 못 쓴다.
   - 더 또렷한 레트로 느낌을 원하면 None 대신 **Upscale Render Texture**를 쓸 것.
     320×180으로 한 번 래스터화한 뒤 ×6 업스케일이라 카메라·스프라이트가 같은 저해상도 공간에
     있어서 떨림이 없다. 대신 움직임이 의도적으로 뚝뚝 끊기고, 2D 라이트도 저해상도가 된다.
3. 기획서가 직접 "초안, 테스트 후 조정"으로 못 박은 값 — **전부 Inspector/SO 노출 대상**:
   공격 딜레이 1초, 부채꼴 각도, 대쉬 거리·쿨타임, 검기 5스윙·50%, 회전 베기 3스윙·80%,
   스폰 포인트 개수·위치, 맵 크기, 적 전 수치.
4. 적 A·B·C의 최종 이름·외형은 에셋 확정 후 결정 (위 매핑은 보유 팩 기준 잠정안).
