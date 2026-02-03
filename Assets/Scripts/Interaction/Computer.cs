using System.Collections;
using UnityEngine;

public class Computer : InteractableBase
{
    [SerializeField] private HintPaper hintPaper;
    private bool isOn = false;
    public override void Interact(int actorNumber)
    {
        if (isOn) return;
        if (requiredItem == null) return;

        requiredItem = null;
        isOn = true;
        hintPaper.gameObject.SetActive(isOn);
    }

    protected override IEnumerator InitRoutine()
    {
        while (playerCamCtrl == null)
        {
            playerCamCtrl = FindLocalCamCtrl();
            if (playerCamCtrl == null)
                yield return null; // 다음 프레임
        }

        if (hintPaper != null)
        {
            yield return new WaitUntil(() => hintPaper.InitComplete);
            hintPaper.gameObject.SetActive(false);
        }
    }
}
