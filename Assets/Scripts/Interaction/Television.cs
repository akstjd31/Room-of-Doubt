using System.Collections;
using UnityEngine;

public class Television : InteractableBase
{
    [SerializeField] private HintPaper hintPaper;
    private bool isOn = false;
    public override void Interact(int actorNumber)
    {
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
}
