using System.Collections;
using UnityEngine;

public class Computer : InteractableBase
{
    [SerializeField] private HintPaper hintPaper;
    private bool isOn = false;
    public override void Interact(int actorNumber)
    {
        if (isOn) return;

        if (requiredItem == null)
        {
            prompt = "PC와 연결할 케이블이 없는 것 같다.";
            ShowLocalPrompt(actorNumber, prompt);
        }

        var slot = QuickSlotManager.Local.GetFocusedSlot();
        if (slot == null) return;

        var inst = slot.current;
        if (inst == null) return;

        // 현재 들고 있는 아이템이랑 다르면
        if (!requiredItem.ID.Equals(inst.itemId))
        {
            ShowLocalPrompt(actorNumber, "이곳에 쓰는 아이템이 아닌 것 같다.");
            return;
        }

        requiredItem = null;
        isOn = true;
        slot.Clear();
    }

    protected override IEnumerator InitRoutine()
    {
        throw new System.NotImplementedException();
    }

    private void ShowLocalPrompt(int actorNumber, string p)
    {
        if (Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
            UIManager.Instance.ShowMessage(p);
    }
}
