using Photon.Pun;
using UnityEngine;

public class Key : InteractableBase
{
    // 상호 작용 시 열쇠 획득
    public override void Interact(int actorNumber)
    {
        if (GameManager.Instance.playerData.TryGetValue(actorNumber, out GameObject playerObj))
        {
            var quickMgr = playerObj.GetComponent<QuickSlotManager>();
            quickMgr.AddItem(RewardItem);
        }

        // 나중에 오브젝트 풀로 처리를 하든 해야됨.
        this.gameObject.SetActive(false);
    }
}
