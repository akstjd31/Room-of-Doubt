using System.Collections;
using UnityEngine;

public class Television : InteractableBase
{
    private bool isOn = false;
    public override void Interact(int actorNumber)
    {
        // 리모컨이 있는가?
        if (requiredItem == null)
        {
            ShowLocalPrompt(actorNumber);
            return;
        }
            
        isOn = !isOn;

        string s = isOn ? "켜짐" : "꺼짐";
        Debug.Log(s);
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }

    private void ShowLocalPrompt(int actorNumber)
    {
        if (Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
            UIManager.Instance.ShowMessage(prompt);
    }
}
