using System.Collections;
using TMPro;
using UnityEngine;

public class HintPaper : InteractableBase
{
    [SerializeField] private TMP_Text text;

    private void Awake()
    {
        if (text == null)
            text = this.transform.GetChild(0).GetComponent<TMP_Text>();

        var content = QuickSlotManager.Local.ReadFocusedHint();
        if (content != null)
            text.text = content;
    }

    public override void Interact(int actorNumber)
    {
        throw new System.NotImplementedException();
    }

    protected override IEnumerator InitRoutine()
    {
        yield return null;
    }

    public void SetHintText(string description)
    {
        text.text = description;
    }
}
