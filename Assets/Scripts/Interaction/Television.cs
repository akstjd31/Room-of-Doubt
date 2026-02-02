using System.Collections;
using UnityEngine;

public class Television : InteractableBase
{
    [SerializeField] private HintPaper hintPaper;
    private bool isOn = false;
    public override void Interact(int actorNumber)
    {
        if (requiredItem == null) return;

        var slot = QuickSlotManager.Local.GetFocusedSlot();
        if (slot == null)
        {
            ShowLocalPrompt(actorNumber, "TV를 키려면 뭔가 필요한 것 같다.");
            return;
        }

        var inst = slot.current;
        if (inst == null) return;

        // 현재 포커싱된 슬롯에 있는 아이템이 이 오브젝트가 요구하는 아이템인지 확인
        if (!requiredItem.RequiredPart.ID.Equals(inst.installedPartId))
        {
            ShowLocalPrompt(actorNumber, "이곳에 쓰는 아이템이 아닌 것 같다.");
            return;
        }
        
        // 슬롯에 있는 이 동일한 아이템의 부품이 끼워져 있는지 확인
        if (!inst.HasInstalledPart)
        {
            ShowLocalPrompt(actorNumber, "리모컨에 뭔가 문제가 있는 것 같다.");
            return;
        }

        isOn = !isOn;
        hintPaper.gameObject.SetActive(isOn);
    }

    protected override IEnumerator InitRoutine()
    {
        if (hintPaper != null)
        {
            yield return new WaitUntil(() => hintPaper.InitComplete);
            hintPaper.gameObject.SetActive(false);
        }

        yield break;
    }

    private void ShowLocalPrompt(int actorNumber, string p)
    {
        if (Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
            UIManager.Instance.ShowMessage(p);
    }
}
