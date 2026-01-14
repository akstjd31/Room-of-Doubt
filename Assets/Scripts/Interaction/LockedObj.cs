using Photon.Pun;
using UnityEngine;

public class LockedObj : InteractableBase
{
    public override void Interact(int actorNumber)
    {
        if (requiredItem == null) return;

        if (GameManager.Instance.playerQuickSlotMgrData.TryGetValue(actorNumber, out QuickSlotManager quickSlotMgr))
        {
            // 현재 퀵 슬롯에 존재하는 아이템이 현 RequiredItem과 같다면?
            // 아이템 사용 후 슬롯도 비워주는 작업 필요

            // 만약 같은 아이템이라면 해당 슬롯 비워주기 + 오브젝트 처리(애니메이션 재생, 사운드 등)
            if (quickSlotMgr.CompareItem(RequiredItem))
            {
                quickSlotMgr.RemoveItem();
                this.gameObject.SetActive(false);
            }
        }
    }
}
