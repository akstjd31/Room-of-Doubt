using UnityEngine;
using Unity.Cinemachine;
using Photon.Pun;
using System.Collections;


[RequireComponent(typeof(PhotonView))]
public abstract class InteractableBase : MonoBehaviourPun, IInteractable
{
    public enum InteractableType { Normal, Puzzle };

    [Header("Base Settings")]
    public InteractableType type;
    [SerializeField] protected string prompt;
    public int ViewId => photonView.ViewID;
    public virtual string Prompt => prompt;
    [SerializeField] protected bool isInteracting;                         // 현재 상호작용중인지?

    [Header("Item Interaction")]
    [SerializeField] protected ItemInstance requiredItem;       // 소모/필요 아이템 (없으면 null)
    [SerializeField] protected ItemInstance rewardItem;         // 획득 아이템 (없으면 null)

    [Header("Cinemachine")]
    [SerializeField] private PlayerCameraController playerCamCtrl;
    [SerializeField] private CinemachineCamera myCam;
    [SerializeField] private CinemachineBrain brain;

    public ItemInstance RequiredItem => requiredItem;           // 상호작용을 위해 필요한 아이템
    public ItemInstance RewardItem => rewardItem;               // 상호작용 후 얻는 보상 아이템

    private bool isTransitioning;
    private Coroutine transitionCor;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        yield return new WaitUntil(() => PlayerCameraController.Instance != null);

        playerCamCtrl = PlayerCameraController.Instance;

        if (Camera.main != null)
            brain = Camera.main.GetComponent<CinemachineBrain>();

        yield return InitRoutine();
    }

    protected abstract IEnumerator InitRoutine();

    // 상호작용 가능 여부 판단
    public virtual bool CanInteract(int actorNumber)
    {
        // 일반적인 조사: 필요 아이템 없음.
        if (requiredItem == null) return true;

        // 상호작용에 필요한 아이템이 현재 슬롯(SelectedSlot)에 존재하는지 여부 판단
        return QuickSlotManager.Instance.CompareItem(requiredItem.itemId);
    }

    // 상호작용 응답
    public void RequestInteract(int actorNumber)
    {
        // 연타 방지용
        if (isTransitioning) return;

        // 로컬에서 상호작용이 가능한지 검증 후
        if (!CanInteract(actorNumber)) return;

        if (requiredItem != null)
            QuickSlotManager.Instance.RemoveItem();

        if (RewardItem != null)
            QuickSlotManager.Instance.AddItem(RewardItem);

        isInteracting = !isInteracting;
        
        if (transitionCor != null)
            StopCoroutine(transitionCor);

        if (myCam != null || playerCamCtrl != null)
            transitionCor = StartCoroutine(TransitionRoutine(isInteracting));

        // 실제 상호작용은 RPC로 전달
        photonView.RPC(nameof(InteractRPC), RpcTarget.AllBuffered, actorNumber);
    }

    [PunRPC]
    protected void InteractRPC(int actorNumber) => Interact(actorNumber);

    // 실제 상호작용 (문구 띄우기, 애니메이션, 아이템 획득 등..)
    public abstract void Interact(int actorNumber);

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

    // 포커싱
    public IEnumerator EnterCamera()
    {
        if (myCam == null || playerCamCtrl == null) yield break;

        myCam.Priority = 20;
        playerCamCtrl.playerCam.Priority = 0;
        
        // 순간 이동이 가능해서 이 위치에 둠
        UIManager.Instance.SetPlayerAimActive(false);
        QuickSlotManager.Instance.SetActiveSlotParent(false);
        GameManager.Instance.EnterInteracting();

        yield return WaitForBlendComplete();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("상호작용 시작 (카메라 이동)");
    }

    // 포커싱 해제
    public IEnumerator ExitCamera()
    {
        if (myCam == null || playerCamCtrl == null) yield break;

        myCam.Priority = 0;
        playerCamCtrl.playerCam.Priority = 20;

        yield return WaitForBlendComplete();

        UIManager.Instance.SetPlayerAimActive(true);
        QuickSlotManager.Instance.SetActiveSlotParent(true);
        GameManager.Instance.ExitInteracting();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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
}