using System.Collections;
using UnityEngine;

public class Television : InteractableBase
{
    [SerializeField] private GameObject screenObj;
    private bool isOn = false;
    public override void Interact(int actorNumber)
    {
        if (requiredItem == null)
        {
            prompt = "TV가 안켜진다.";
            ShowLocalPrompt(actorNumber, prompt);
            return;
        }

        var slot = QuickSlotManager.Local.GetFocusedSlot();
        if (slot == null) return;

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
            ShowLocalPrompt(actorNumber, prompt);
            return;
        }

        isOn = !isOn;
        screenObj.SetActive(isOn);
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }

    private void ShowLocalPrompt(int actorNumber, string p)
    {
        if (Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
            UIManager.Instance.ShowMessage(p);
    }
}
