# Last Blade

칼 하나로 끝없이 몰려오는 적 사이에서 최대한 오래 버티는 **2D 탑다운 뱀서라이크** 게임입니다.
승리 조건은 없고, 죽을 때까지의 **생존 시간**이 곧 점수입니다.

## 게임 소개

- 무기는 **칼 하나**. 마우스로 클릭한 방향으로 휘두르고, 휘두른 횟수에 맞춰 스킬이 함께 발동합니다.
- 레벨업(최대 레벨 10)할 때마다 게임이 멈추고, **신규 스킬 또는 능력치 강화** 중 2장에서 1장을 고릅니다.
- 사각형으로 제한된 맵, PC(Windows), 1920×1080 고정.

### 조작

| 동작 | 입력 |
|---|---|
| 이동 | WASD / 방향키 |
| 공격 | 마우스 좌클릭 (클릭한 방향, 1클릭 1회) |
| 대쉬 | Space (스킬 획득 후) |
| 일시정지 | ESC |

### 스킬과 능력치

- 스킬 4종: 대쉬, 검기 날리기, 회전 베기, 냉기 속성 부여
- 능력치 4종: 이동 속도, 체력, 공격 속도, 공격력
- 각 항목은 최대 3단계. 레벨업은 10번뿐이라 무엇을 고르느냐가 빌드 차이가 됩니다.

자세한 수치는 [`CLAUDE.md`](CLAUDE.md)에 있습니다.

## 개발 환경

- Unity **6000.3.27f1** (Unity 6)
- URP 17.3 (2D Renderer), Input System 1.20
- 메인 씬: `Assets/Scenes/GameScene.unity`

### 실행

1. Unity Hub에서 **Add → Add project from disk**로 이 폴더를 추가합니다. (위 버전의 에디터가 필요합니다)
2. `Assets/Scenes/GameScene.unity`를 열고 Play.
3. 에디터 Game 뷰 해상도를 **1920×1080**으로 맞춥니다.

## 폴더 구조

```
Assets/
  Scenes/      GameScene.unity
  Scripts/     모든 게임 스크립트
  Data/        스킬·능력치 수치 (AbilityData ScriptableObject)
  Prefabs/     적, 젬, 검기, 피해 숫자, 사망 이펙트
  Animations/  플레이어·적 애니메이션
  Materials/   머티리얼
  Sprites/     스프라이트 (PPU 16 / Point / 압축 없음)
  Settings/    URP·Input 설정
docs/          기획서 PDF
CLAUDE.md      최신 기획과 구현 노트 (이 프로젝트의 기준 문서)
```

## 문서

- [`CLAUDE.md`](CLAUDE.md) — **최신** 기획·구현 상태·결정 사항. 수치가 다르면 이 문서가 우선입니다.
- [`docs/Last_Blade_기획서.pdf`](docs/Last_Blade_기획서.pdf) — 2026-10-27 기준 최초 기획서. 이후 바뀐 내용은 반영되어 있지 않습니다.

개발 진행도(마일스톤)는 `CLAUDE.md`의 "마일스톤" 항목을 보세요.

## 수정할 때

- 수치는 코드가 아니라 `Assets/Data/*.asset`, 프리팹, Inspector에서 고칩니다. 스킬 수치를 바꾸면 설명 문구(`descriptions`)도 같이 고쳐야 합니다.
- Unity가 만든 `.meta` 파일은 **반드시 해당 파일과 함께 커밋**합니다. (`Library`, `Temp`, `Logs`, `UserSettings`는 `.gitignore`로 제외됩니다)
- 새 스프라이트는 PPU 16 / Filter Mode Point / Compression None / Mesh Type Full Rect / Mip Maps off로 맞춥니다.
- 기획이 바뀌면 `CLAUDE.md`를 같이 수정해 두세요.

## 에셋 크레딧

- 스프라이트: [v1.1 dungeon crawler 16X16 pixel pack](https://o-lobster.itch.io/simple-dungeon-crawler-16x16-pixel-pack) by **o_lobster** (CC0 1.0)

## 라이선스

코드 라이선스는 아직 정하지 않았습니다.
