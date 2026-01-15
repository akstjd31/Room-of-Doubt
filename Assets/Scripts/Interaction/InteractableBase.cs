using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public abstract class InteractableBase : MonoBehaviourPun, IInteractable
{
    [Header("Base Settings")]
    [SerializeField] protected string prompt;
    

    public int ViewId => photonView.ViewID;
    public virtual string Prompt => prompt;

    [Header("Item Interaction")]
    [SerializeField] protected Item requiredItem; // 소모/필요 아이템 (없으면 null)
    [SerializeField] protected Item rewardItem;   // 획득 아이템 (없으면 null)

    public Item RequiredItem => requiredItem;
    public Item RewardItem => rewardItem;

    // 상호작용 가능 여부 판단
    public virtual bool CanInteract(int actorNumber)
    {
        // 일반적인 조사: 필요 아이템 없음.
        if (requiredItem == null) return true;

        // 상호작용에 필요한 아이템이 현재 슬롯(SelectedSlot)에 존재하는지 여부 판단
        return QuickSlotManager.Instance.CompareItem(requiredItem); 
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


            // 실제 상호작용은 RPC로 전달
            photonView.RPC(nameof(InteractRPC), RpcTarget.AllBuffered, actorNumber);
        }
    }

    [PunRPC]
    protected void InteractRPC(int actorNumber) => Interact(actorNumber);

    // 실제 상호작용 (문구 띄우기, 애니메이션, 아이템 획득 등..)
    public abstract void Interact(int actorNumber);
}