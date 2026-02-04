using UnityEngine;
using Unity.Cinemachine;
using Photon.Pun;
using System.Collections;
using WebSocketSharp;


[RequireComponent(typeof(PhotonView))]
public abstract class InteractableBase : MonoBehaviourPun, IInteractable
{
    public enum InteractableType { Normal, Puzzle };

    [Header("Base Settings")]
    public InteractableType type;                           
    [SerializeField] protected string prompt;           // 특정 상황에서 출력할 문구
    public int ViewId => photonView.ViewID;
    public virtual string Prompt => prompt;
    [SerializeField] protected bool isInteracting;      // 현재 상호작용중인지?

    [Header("Item Interaction")]
    [SerializeField] protected Item requiredItem;       // 상호작용 위해 요구되는 아이템 (없으면 null)
    [SerializeField] protected Item rewardItem;         // 획득 아이템 (없으면 null)
    [SerializeField] protected Item hostItem;           // 부품 본체가 되는 아이템
    [SerializeField] protected Item needItem;           // 필요 부품 아이템
    protected HintData hintData = HintData.Empty;


    [Header("Cinemachine")]
    [SerializeField] protected PlayerCameraController playerCamCtrl;
    [SerializeField] protected CinemachineCamera myCam;
    [SerializeField] private CinemachineBrain brain;

    [SerializeField] private bool isTransitioning;
    private Coroutine transitionCor;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        yield return new WaitUntil(() => Camera.main != null);
        brain = Camera.main.GetComponent<CinemachineBrain>();

        yield return InitRoutine();
    }

    protected abstract IEnumerator InitRoutine();

    // 상호작용 가능 여부 판단
    public virtual bool CanInteract(int actorNumber)
    {
        // 일반적인 조사: 요구되는 아이템 & 사용하기 위해 필요로 하는 아이템이 없음. 
        if (requiredItem == null) return true;

        // 해당 오브젝트와의 상호작용으로 요구되는 아이템이 null이 아니라면 현재 슬롯(SelectedSlot)에 존재하는지 여부 판단 
        return QuickSlotManager.Local.CompareItem(requiredItem.ID);
    }


    // 상호작용 응답
    public void RequestInteract(int actorNumber)
    {
        // 연타 방지용
        if (isTransitioning) return;

        // 로컬에서 상호작용이 가능한지 검증 후
        if (!CanInteract(actorNumber))
        {
            UIManager.Instance.ShowMessage(prompt);
            return;
        }

        // 상호작용을 위해 필요 아이템 존재 & 소모 아이템일 경우
        if (requiredItem != null)
        {
            var slot = QuickSlotManager.Local.GetFocusedSlot();
            if (slot == null)
            {
                prompt = "TV를 키려면 뭔가 필요한 것 같다.";
                UIManager.Instance.ShowMessage(prompt);
                return;
            }

            if (requiredItem.ConsumeType.Equals(ConsumeType.Consumable))
            {
                // 현재 슬롯에 같은 아이템이 있는 경우
                if (requiredItem.ID.Equals(slot.current.itemId))
                {
                    QuickSlotManager.Local.RemoveItem();
                }
            }

            // 영구적인 아이템인 경우
            else
            {
                // 요구하는 아이템의 부품이 현 슬롯에 없거나 해당 아이템의 부품이 끼워져 있지 않은 상태라면
                if (!requiredItem.RequiredPart.ID.Equals(slot.current.installedPartId) ||
                    !slot.current.HasInstalledPart)
                {
                    prompt = "이곳에 쓰는 아이템이 아닌 것 같다.";
                    UIManager.Instance.ShowMessage(prompt);
                    return;
                }
            }
        }

        if (rewardItem != null)
        {
            ItemInstance instance = new ItemInstance(rewardItem.ID, hintData);
            bool flag = QuickSlotManager.Local.AddItem(instance);

            // 만약 아이템이 들어갈 자리가 없다?
            if (!flag) return;
        }

        isInteracting = !isInteracting;

        if (transitionCor != null)
            StopCoroutine(transitionCor);

        if (myCam != null || playerCamCtrl != null)
            transitionCor = StartCoroutine(TransitionRoutine(isInteracting));

        // 실제 상호작용은 RPC로 전달
        photonView.RPC(nameof(InteractRPC), RpcTarget.All, actorNumber);
    }

    [PunRPC]
    protected void InteractRPC(int actorNumber) => Interact(actorNumber);

    // 실제 상호작용 (문구 띄우기, 애니메이션, 아이템 획득 등..)
    public abstract void Interact(int actorNumber);

    // 카메라 전환
    private IEnumerator TransitionRoutine(bool enter)
    {
        isTransitioning = true;

        if (enter)
            yield return EnterCamera();
        else
            yield return ExitCamera();

        isTransitioning = false;
        transitionCor = null;
    }

    // 트랜잭션 시작
    public IEnumerator EnterCamera()
    {
        if (myCam == null || playerCamCtrl == null) yield break;

        this.gameObject.GetComponent<Collider>().enabled = false;

        myCam.Priority = 20;
        playerCamCtrl.playerCam.Priority = 0;

        // 순간 이동이 가능해서 이 위치에 둠
        UIManager.Instance.SetPlayerAimActive(false);
        QuickSlotManager.Local.SetActiveSlotParent(false);
        GameManager.Instance.EnterInteracting();

        yield return WaitForBlendComplete();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("상호작용 시작 (카메라 이동)");
    }

    // 트랜잭션 종료
    public IEnumerator ExitCamera()
    {
        if (myCam == null || playerCamCtrl == null) yield break;

        myCam.Priority = 0;
        playerCamCtrl.playerCam.Priority = 20;

        yield return WaitForBlendComplete();

        UIManager.Instance.SetPlayerAimActive(true);
        QuickSlotManager.Local.SetActiveSlotParent(true);
        GameManager.Instance.ExitInteracting();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        this.gameObject.GetComponent<Collider>().enabled = true;
        Debug.Log("상호작용 종료 (카메라 원위치)");
    }

    // 시네머신 카메라 블렌딩 끝나는 시점
    private IEnumerator WaitForBlendComplete()
    {
        if (brain == null) { yield return null; yield break; }

        yield return null;

        while (brain.ActiveBlend != null)
            yield return null;
    }

    // 내 로컬 카메라 컨트롤러 스크립트를 찾기
    protected PlayerCameraController FindLocalCamCtrl()
    {
        // 비활성 오브젝트까지 포함하려면 Resources.FindObjectsOfTypeAll도 가능하지만
        // 보통은 활성 기준이면 FindObjectsOfType로 충분.
        var all = FindObjectsOfType<PlayerCameraController>(true);
        foreach (var c in all)
        {
            if (c != null && c.photonView != null && c.photonView.IsMine)
                return c;
        }
        return null;
    }

    // 호스트 아이템에 부품을 끼워넣는 시도
    public virtual bool TryInstallToHost(ItemInstance draggedPart, out string failReason)
    {
        failReason = null;

        if (needItem == null || hostItem == null)
        {
            failReason = "설정이 안 된 오브젝트입니다.";
            return false;
        }

        if (draggedPart == null || !draggedPart.itemId.Equals(needItem.ID))
        {
            failReason = "필요한 아이템이 아닙니다!";
            return false;
        }

        // hostItem이 부품 요구 아이템인지 확인
        if (!hostItem.RequiresPart)
        {
            failReason = "이 아이템은 부품이 필요 없습니다.";
            return false;
        }

        // hostItem이 요구하는 부품이 현재 필요한 부품(needItem)과 일치하는지 확인
        if (hostItem.ID.Equals(needItem.ID))
        {
            failReason = "이 부품은 해당 아이템에 맞지 않습니다.";
            return false;
        }

        // 퀵 슬롯에서 호스트 아이템이 있는지 확인
        int hostSlot = QuickSlotManager.Local.FindFirstSlotIndexByItemId(hostItem.ID);
        if (hostSlot < 0)
        {
            failReason = $"{hostItem.ItemName}이(가) 퀵슬롯에 없습니다.";
            return false;
        }

        // 안에 내용물 확인
        var hostInst = QuickSlotManager.Local.GetItemInstanceByIndex(hostSlot);
        if (hostInst == null)
        {
            failReason = "호스트 아이템이 없습니다.";
            return false;
        }

        // 장착된 부품이 혹여나 있으면?
        if (!string.IsNullOrEmpty(hostInst.installedPartId))
        {
            failReason = "이미 부품이 장착되어 있습니다.";
            return false;
        }

        hostInst.installedPartId = needItem.ID;

        // UI/스냅샷 갱신
        QuickSlotManager.Local.UpdateSlotData(hostSlot, hostInst);

        return true;
    }

}