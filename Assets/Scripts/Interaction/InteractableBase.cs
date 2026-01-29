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
    [SerializeField] protected string prompt;
    public int ViewId => photonView.ViewID;
    public virtual string Prompt => prompt;
    [SerializeField] protected bool isInteracting;                         // 현재 상호작용중인지?

    [Header("Item Interaction")]
    [SerializeField] protected Item requiredItem;       // 줍기 위해 요구되는 아이템 (없으면 null)
    [SerializeField] protected Item rewardItem;         // 획득 아이템 (없으면 null)
    [SerializeField] protected Item needItem;           // 이걸 사용하기 위해 필요로 하는 아이템 (ex. 건전지 등)
    public Item NeedItem => needItem;

    [Header("Cinemachine")]
    [SerializeField] protected PlayerCameraController playerCamCtrl;
    [SerializeField] protected CinemachineCamera myCam;
    [SerializeField] private CinemachineBrain brain;

    [SerializeField] private bool isTransitioning;
    private Coroutine transitionCor;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom);

        if (Camera.main != null)
            brain = Camera.main.GetComponent<CinemachineBrain>();

        yield return InitRoutine();
    }

    protected abstract IEnumerator InitRoutine();

    // 상호작용 가능 여부 판단
    public virtual bool CanInteract(int actorNumber)
    {
        // 일반적인 조사: 요구되는 아이템 & 사용하기 위해 필요로 하는 아이템이 없음.
        if (requiredItem == null && needItem == null) return true;

        // 해당 오브젝트와의 상호작용으로 요구되는 아이템이 null이 아니라면 현재 슬롯(SelectedSlot)에 존재하는지 여부 판단
        if (requiredItem != null && needItem == null) return QuickSlotManager.Local.CompareItem(requiredItem.ID);
        
        return true;
    }

    // 상호작용 응답
    public void RequestInteract(int actorNumber)
    {
        Debug.Log("잘 들어왔는데");

        // 연타 방지용
        if (isTransitioning) { Debug.Log("이게 문제라고?"); return; }

        // 로컬에서 상호작용이 가능한지 검증 후
        if (!CanInteract(actorNumber))
        {
            Debug.Log("CanInteract가 false 라고?");
            UIManager.Instance.ShowMessage(prompt);
            return;
        }

        Debug.Log("CanInteract 통과!");

        // 상호작용을 위해 필요 아이템 존재 & 소모 아이템일 경우
        if (requiredItem != null && requiredItem.ConsumeType.Equals(ConsumeType.Consumable))
            QuickSlotManager.Local.RemoveItem();

        if (rewardItem != null)
        {
            Debug.Log("설마 여길 들어오진 않겠지?");
            ItemInstance instance = new ItemInstance(rewardItem.ID, HintData.Empty);
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

    // 포커싱 해제
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
}