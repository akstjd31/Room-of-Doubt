using System.Collections;
using TMPro;
using UnityEngine;
using Photon.Pun;

public class HintPaper : InteractableBase
{
    [SerializeField] private TMP_Text text;

    private void Awake()
    {
        if (text == null)
            text = this.transform.GetChild(0).GetComponent<TMP_Text>();

        // var content = QuickSlotManager.Local.ReadFocusedHint();
        // if (content != null)
        //     text.text = content;
    }

    public override void Interact(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (requiredItem != null) rewardItem = null;
        PhotonNetwork.Destroy(this.gameObject);
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
