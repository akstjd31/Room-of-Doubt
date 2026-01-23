using System.Collections;
using TMPro;
using UnityEngine;

public class ArcadeMachine : InteractableBase
{
    [SerializeField] private TMP_Text text;
    public override void Interact(int actorNumber)
    {
        text.gameObject.SetActive(true);
    }

    protected override IEnumerator InitRoutine()
    {
        text.gameObject.SetActive(false);
        yield break;
    }
}
