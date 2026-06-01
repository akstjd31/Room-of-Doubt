# 🏚 Room of Doubt

<a href="https://youtu.be/Vpu2HRS5BHc">
    <img width="1193" height="655"
         alt="image"
         src="https://github.com/user-attachments/assets/d200ecf8-f86d-46e3-9d55-d2d85a8502cc" />
</a>

> 📺 이미지를 클릭하면 플레이 영상을 확인할 수 있습니다.


> Photon PUN2 기반의 3D 협동 방탈출 게임

---

# 📑 목차

- [📌 프로젝트 개요](#-프로젝트-개요)
- [🎮 게임 소개](#-게임-소개)
- [⚙ 주요 시스템](#-주요-시스템)
- [🛠 기술적 구현](#-기술적-구현)
- [🌐 네트워크 구조](#-네트워크-구조)
- [🤔 기술적 고민 및 해결](#-기술적-고민-및-해결)
- [📈 성과](#-성과)
- [💡 회고](#-회고)

---

# 📌 프로젝트 개요

| 항목    | 내용                      |
| ----- | ----------------------- |
| 프로젝트명 | Room of Doubt           |
| 개발 기간 | 2026.01.14 ~ 2026.02.04 |
| 개발 인원 | 1명                      |
| 담당 역할 | 클라이언트 / 네트워크            |
| 개발 환경 | Unity 6000.2.10f1       |
| 네트워크  | Photon PUN2             |
| 플랫폼   | PC                      |
| 장르    | 3D 협동 방탈출               |

---

# 🎮 게임 소개

플레이어들은 제한된 공간에 갇힌 상태로 게임을 시작합니다.

맵 곳곳에 배치된 퍼즐을 해결하고,
아이템을 획득하며,
다른 플레이어와 협력하여 탈출 조건을 충족해야 합니다.

모든 플레이어는 퍼즐 진행 상태와 아이템 정보를 공유하며,
최종 탈출 조건을 달성하면 게임이 종료됩니다.

---

# ⚙ 주요 시스템

## 시작 아이템 시스템

* 플레이어별 역할(A, B, C) 부여
* 시작 시 랜덤 힌트 및 랜턴 지급
* Room Custom Property 기반 상태 공유
* 중도 참가자 상태 동기화 지원

<img width="600" height="300" alt="ezgif com-video-to-gif-converter (5)" src="https://github.com/user-attachments/assets/8a73cb8a-baf0-4b41-9954-a72b2c5431bc" />

---

## 퍼즐 시스템

### Keypad Puzzle

* 숫자 입력 기반 퍼즐
* 정답 검증
* 완료 상태 동기화

<img width="600" height="300" alt="ezgif com-video-to-gif-converter (1)" src="https://github.com/user-attachments/assets/29a81fac-bac6-4de2-b162-6c84ba543de3" />

### Wire Puzzle

* 전선 연결 퍼즐
* 연결 상태 검증
* 완료 이벤트 처리

<img width="600" height="300" alt="ezgif com-video-to-gif-converter (2)" src="https://github.com/user-attachments/assets/1886e811-045f-4489-99ef-1b177433ed1e" />

---

## 공유 인벤토리 시스템

* 아이템 획득
* 아이템 제거
* 플레이어 간 정보 공유

<img width="600" height="300" alt="ezgif com-video-to-gif-converter (3)" src="https://github.com/user-attachments/assets/19edeb1f-a26a-4719-8bcf-e30c387b2427" />

---

## 상호작용 시스템

* 조사, 아이템 사용, 퍼즐 진입 등 다양한 상호작용 지원
* 아이템 요구 조건 검증
* 상호작용 결과 동기화
* 공통 상호작용 흐름 재사용

<img width="600" height="300" alt="ezgif com-video-to-gif-converter (4)" src="https://github.com/user-attachments/assets/3c628d4a-0e0c-469c-ab24-f317380c762f" />


---

# 🛠 기술적 구현

## 1. Room Custom Property 기반 시작 아이템 동기화

### 문제

게임 시작 시 각 플레이어가 로컬에서 랜덤하게 시작 아이템을 생성하면서

* 플레이어마다 다른 아이템 정보가 생성됨
* 동일한 게임 방에서도 서로 다른 상태를 보게 됨
* 늦게 접속한 플레이어는 현재 아이템 정보를 확인할 수 없음

### 해결

Room Custom Property를 활용하여

방장이 시작 아이템 정보를 생성한 뒤
Room 단위 데이터로 저장하도록 구조를 변경했습니다.

이후 모든 플레이어는 Room Property를 참조하여
동일한 시작 아이템 정보를 사용하도록 구현했습니다.

### 결과

* 모든 플레이어가 동일한 시작 아이템 정보 공유
* 데이터 불일치 문제 해결
* 중도 참가자 상태 동기화 지원

```csharp
var props = new PhotonHashtable
{
    { RoomPropKeys.START_READY, true }
};

props[idKey] = hintKey;        // 시작 힌트
props[payKey] = payload;       // 힌트 데이터
props[lampKey] = isLampOwner;  // 랜턴 소유 여부

room.SetCustomProperties(props);
```

---

## 2. 공유 인벤토리 시스템

### 문제

아이템 정보를 플레이어별로 관리할 경우

* 플레이어마다 서로 다른 아이템 상태가 발생할 수 있음
* 아이템 이동 및 사용 시 데이터 불일치가 발생할 수 있음

### 해결

공유 인벤토리를 단일 기준 데이터로 설정하고,

모든 아이템 이동 요청을 Master Client가 처리하도록 설계했습니다.

이후 검증된 인벤토리 상태를 RPC를 통해
전체 플레이어에게 동기화하도록 구현했습니다.

### 결과

* 플레이어 간 아이템 상태 일관성 확보
* 데이터 불일치 문제 해결
* 안정적인 협동 플레이 환경 구축

```csharp
[PunRPC]
private void RequestMoveRPC(
    SlotType fromType, int fromIdx,
    SlotType toType, int toIdx,
    ...)
{
    if (!PhotonNetwork.IsMasterClient) return;

    (sharedItems[toIdx], sharedItems[fromIdx])
        = (sharedItems[fromIdx], sharedItems[toIdx]);

    photonView.RPC(
        nameof(SyncInventoryRPC),
        RpcTarget.All,
        Flatten(sharedItems));
}
```

---

## 3. 상호작용 시스템

### 문제

방탈출 게임에서는 조사, 아이템 사용, 퍼즐 진입 등 다양한 상호작용이 필요했습니다.

각 오브젝트마다 상호작용 로직을 개별적으로 작성할 경우

* 중복 코드가 증가함
* 아이템 요구 조건 처리 방식이 달라질 수 있음
* 카메라 전환 및 UI 제어 흐름이 분산될 수 있음

### 해결

`InteractableBase` 추상 클래스를 설계하여  
상호작용 가능 여부 판단, 아이템 조건 검사, 보상 지급, RPC 호출 흐름을 공통화했습니다.

각 오브젝트는 `Interact()`만 구현하도록 분리하여  
상호작용 흐름은 공통으로 유지하고, 실제 동작만 개별 오브젝트에서 정의할 수 있도록 구성했습니다.

### 결과

* 상호작용 오브젝트 구현 방식 통일
* 아이템 요구 조건 및 보상 지급 로직 재사용
* 조사, 퍼즐, 아이템 사용 오브젝트 확장 용이
* RPC 기반 상호작용 동기화 가능

```csharp
public void RequestInteract(int actorNumber)
{
    if (isTransitioning) return;

    if (!CanInteract(actorNumber))
    {
        UIManager.Instance.ShowMessage(prompt);
        return;
    }

    if (rewardItem != null)
    {
        ItemInstance instance = new ItemInstance(rewardItem.ID, hintData);
        bool flag = QuickSlotManager.Local.AddItem(instance);

        if (!flag) return;
    }

    isInteracting = !isInteracting;

    photonView.RPC(nameof(InteractRPC), RpcTarget.All, actorNumber);
}

[PunRPC]
protected void InteractRPC(int actorNumber)
    => Interact(actorNumber);

public abstract void Interact(int actorNumber);
```

---

# 🌐 네트워크 구조

## Photon PUN2 활용

### RPC

* 아이템 획득
* 아이템 사용
* 상호작용 이벤트

### Room Custom Property

* 퍼즐 완료 상태
* 게임 진행 상태
* 탈출 조건 관리

### Photon View

* 플레이어 위치 동기화
* 플레이어 행동 동기화

<img width="1000" height="650" alt="mermaid-diagram" src="https://github.com/user-attachments/assets/17a80de5-e9c2-433e-864e-8d5b162b89c7" />

---

# 🤔 기술적 고민 및 해결

## 왜 Room Custom Property를 사용했는가?

퍼즐 진행 상태는 특정 플레이어가 아닌

게임 룸 전체가 공유해야 하는 데이터였습니다.

따라서 Room Custom Property를 사용하여

중도 참가자를 포함한 모든 플레이어가 동일한 상태를 확인할 수 있도록 설계했습니다.

---

## 왜 공유 인벤토리를 사용했는가?

협동 게임에서는

아이템 정보가 플레이어 개인보다

팀 전체의 자산으로 동작해야 했습니다.

이를 위해 공유 데이터를 기준으로 관리하도록 설계했습니다.

---

## 왜 RPC를 사용했는가?

아이템 획득 및 사용은

즉시 반영되어야 하는 이벤트 성 데이터였습니다.

따라서 RPC를 활용하여 실시간으로 상태를 전파했습니다.

---

# 📈 성과

* Photon PUN2 기반 멀티플레이 구현
* Room Custom Property 활용 경험
* RPC 기반 이벤트 동기화 구현
* 공유 인벤토리 시스템 설계
* 협동 퍼즐 시스템 구현

---

# 💡 회고

이번 프로젝트를 통해

실시간 멀티플레이 환경에서 발생하는
데이터 동기화 문제를 직접 경험할 수 있었습니다.

특히

* Room Custom Property
* RPC
* 공유 데이터 구조

를 활용하며

'어떤 데이터를 공유해야 하는가' 와
'어떤 방식으로 동기화해야 하는가' 에 대해 깊이 고민할 수 있었습니다.
