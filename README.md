# 이름 없는 마법사와 봉인된 XX가지의 사랑 (NNMXXL)

> Unity 2D 개인 프로젝트 | 미소녀 수집형 RPG  
> 26.01.30 ~ 26.03.09 (4주)

`#Data-Driven` `#Extensibility` `#Localization`

- 🖥 PC 빌드 : [Google Drive](https://drive.google.com/file/d/1JcMraikedWYOwiNvy6q3i7Ylwwv8fKnV/view?usp=sharing)

---

## 개요

캐릭터 수집·편성·전투로 구성된 모바일 수집형 RPG.  
콘텐츠 추가 비용을 최소화하는 **Data-Driven 설계**와  
한국어·일본어를 지원하는 **현지화 시스템** 구현에 집중하였습니다.

---

## 주요 구현

### 🗂 State 기반 Scene 전환
수집형 RPG는 캐릭터·스토리·가차 등 독립 콘텐츠가 많아 SceneManager 직접 호출 방식으로는 화면 흐름 추적과 뒤로가기 관리가 복잡해집니다.

→ `Stack<GameState>` 기반 GlobalUIManager(DontDestroyOnLoad Singleton)를 설계해, 각 Scene이 현재 State를 받아 패널 활성화 여부를 스스로 갱신하는 구조로 구현함. 뒤로가기는 `Pop`, 메인 콘텐츠 전환 시에는 `Clear`로 히스토리를 초기화합니다.

---

### 📊 CSV 기반 Data-Driven 설계
Stage·Enemy·Dialog·다국어 텍스트 등 게임 규칙을 Logic에서 완전히 분리합니다.

- **World/Stage** : StageID·좌표·이전Stage를 CSV로 관리. World 전환 시 Data를 읽어 Node 위치와 연결선을 동적으로 생성합니다. 새 World 추가 시 Script 수정 없이 CSV만 작성하면 Map 구성이 가능합니다.
- **Dialog** : Speaker·Emotion·Voice·BGM·BackGround·SpeakerPosition을 한 행에 정의. CSV 추가만으로 새 장면 구성이 가능합니다.
- **다국어** : Key-Value 구조로 텍스트를 분리해 Build 없이 언어 교체. 실시간으로 수치가 바뀌는 텍스트는 String Formatting Parameter로 처리합니다.

---

### ⚔️ Position 기반 전투 AI
단순 Stat 비교만으로는 전투 균형을 잡기 어려워, **역할 × Slot 위치** 조합으로 Target 우선순위를 결정하는 AI를 설계했습니다..

- 딜러(전열): 정면 적 우선 / 딜러(중열): 최강 적 / 딜러(후열): 체력 최저 적
- 힐러·버퍼도 Position에 따라 회복·실드 범위가 달라지도록 구현함.

배틀 타이머(60초), AUTO 토글, 1x/2x/3x 배속 지원합니다.

---

### 🎰 가차 보장 시스템
- **10연 PL 보장** : 10연 내 PL 캐릭터 미등장 시 마지막 1회에서 보장.
- **50연 Pick-Up 보장** : 뽑기 Stack이 50에 도달하면 Pick-Up 캐릭터 확정 지급.
- 확률 계산은 `0~9999` 정수 범위를 사용해 소수점 오차 없이 정확한 비율을 구현했습니다.

---

### 🌐 일본어 현지화
- **TMP Fallback** : 한국어를 Main Font로, 일본어를 Fallback List에 등록해 언어 변경 시 Script 수정 없이 TMP가 자동으로 적합한 Font를 선택합니다.
- **후리가나** : RubyTextMeshPro 라이브러리를 도입해 한자 위에 후리가나를 자동 표시합니다.

---

### ☁️ Firebase 연동
- Firebase Authentication : 게스트 로그인 / Google 로그인 지원.
- Firestore : 캐릭터·편성·진행 상황을 클라우드에 저장·복원.
- Addressables : 에셋 분리 로드로 초기 빌드 용량 최적화.

---

## 기술 스택

| 항목 | 내용 |
|---|---|
| Engine | Unity 6 |
| Language | C# |
| DB / Auth | Firebase Firestore, Firebase Authentication |
| Asset 관리 | Addressables |
| UI Text | TextMeshPro, RubyTextMeshPro |
| Data | CSV (ScriptableObject 혼용) |
| 네트워크 | - (싱글플레이) |

---

## 링크

- 🎬 YouTube : *(https://www.youtube.com/watch?v=JZxLnJpGODo)*
- 💻 GitHub : *(https://github.com/ecstaticshine/NNMXXLove)*
