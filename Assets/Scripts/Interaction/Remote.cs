using UnityEngine;
using Photon.Pun;

public class Remote : InteractableBase
{
    [SerializeField] private GameObject tapeObj;
    private void OnEnable()
    {
        RefreshTapeState();
    }

    public void RefreshTapeState()
    {
        if (tapeObj == null) return;
        if (hostItem == null) return;
        if (needItem == null) return;
        if (QuickSlotManager.Local == null) return;

        var qs = QuickSlotManager.Local;

        ItemInstance hostInst = null;
        int max = qs.GetMaxSlotCount();

        // 1) 퀵슬롯에서 hostItem 찾기
        for (int i = 0; i < max; i++)
        {
            var inst = qs.GetItemInstanceByIndex(i);
            if (inst == null) continue;

            if (inst.itemId == hostItem.ID)
            {
                hostInst = inst;
                break;
            }
        }

        // 2) hostItem이 없으면 테이프는 켜둠
        if (hostInst == null)
        {
            tapeObj.SetActive(true);
            return;
        }

        // 3) 부품 장착 여부 확인
        bool installed =
            !string.IsNullOrEmpty(hostInst.installedPartId) &&
            hostInst.installedPartId == needItem.ID;

        // 4) 결과 반영
        tapeObj.SetActive(!installed);
    }

    public override void Interact(int actorNumber)
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(this.gameObject);
    }

    protected override System.Collections.IEnumerator InitRoutine()
    {
        yield break;
    }
}
