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
    [SerializeField] protected Item requiredItem;       // 소모/필요 아이템 (없으면 null)
    [SerializeField] protected Item rewardItem;         // 획득 아이템 (없으면 null)

    [Header("Cinemachine")]
    [SerializeField] private PlayerCameraController playerCamCtrl;
    [SerializeField] private CinemachineCamera myCam;
    private CinemachineBrain brain;

    public Item RequiredItem => requiredItem;           // 상호작용을 위해 필요한 아이템
    public Item RewardItem => rewardItem;               // 상호작용 후 얻는 보상 아이템

    IEnumerator Start()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);
        playerCamCtrl = PlayerCameraController.Instance;
        brain = Camera.main.GetComponent<CinemachineBrain>();
    }

    // 상호작용 가능 여부 판단
    public virtual bool CanInteract(int actorNumber)
    {
        // 일반적인 조사: 필요 아이템 없음.
        if (requiredItem == null) return true;

        // 상호작용에 필요한 아이템이 현재 슬롯(SelectedSlot)에 존재하는지 여부 판단
        return QuickSlotManager.Instance.CompareItem(requiredItem.ID); 
    }

    // 상호작용 응답
    public void RequestInteract(int actorNumber)
    {
        // 로컬에서 상호작용이 가능한지 검증 후
        if (CanInteract(actorNumber))
        {
            if (requiredItem != null)
                QuickSlotManager.Instance.RemoveItem();

            if (RewardItem != null)
                QuickSlotManager.Instance.AddItem(RewardItem);

            if (!isInteracting) StartCoroutine(EnterCamera());
            else StartCoroutine(ExitCamera());

            // 실제 상호작용은 RPC로 전달
            photonView.RPC(nameof(InteractRPC), RpcTarget.AllBuffered, actorNumber);
        }
    }

    [PunRPC]
    protected void InteractRPC(int actorNumber) => Interact(actorNumber);

    // 실제 상호작용 (문구 띄우기, 애니메이션, 아이템 획득 등..)
    public abstract void Interact(int actorNumber);

    // 포커싱 (입력 불가, UI 비활성화 등)
    public IEnumerator EnterCamera()
    {
        if (myCam == null || playerCamCtrl == null) yield break;

        isInteracting = true;

        myCam.Priority = 20;
        playerCamCtrl.playerCam.Priority = 0;

        UIManager.Instance.SetPlayerAimActive(!isInteracting);
        QuickSlotManager.Instance.SetActiveSlotParent(!isInteracting);
        if (type.Equals(InteractableType.Puzzle))  GameManager.Instance.EnterPuzzle();

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

        UIManager.Instance.SetPlayerAimActive(isInteracting);
        QuickSlotManager.Instance.SetActiveSlotParent(isInteracting);
        if (type.Equals(InteractableType.Puzzle))  GameManager.Instance.ExitPuzzle();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        isInteracting = false;

        Debug.Log("상호작용 종료 (카메라 원위치)");
    }

    // 시네머신 카메라 블렌딩 끝나는 시점
    private IEnumerator WaitForBlendComplete()
    {
        if (brain == null) yield break;

        yield return null;

        while (brain.ActiveBlend != null)
            yield return null;
    }
}