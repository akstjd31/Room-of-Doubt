using UnityEngine;
using Photon.Pun;

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

        // 퀵슬롯 매니저에서 현재 선택된 아이템을 가져오는 로직이 필요합니다.
        // 예: GameManager.Instance.playerData[actorNumber].GetComponent<QuickSlotManager>().CurrentItem;
        
        // 아이템이 필요한 경우에 대한 비교 로직은 하단 Interact나 
        // 외부 Interactor 스크립트에서 추가로 처리하도록 설계하는 것이 유연합니다.
        return true; 
    }

    // 실제 상호작용 (문구 띄우기, 애니메이션, 아이템 획득 등..)
    public abstract void Interact(int actorNumber);
}