using System.Collections;
using TMPro;
using UnityEngine;
using Photon.Pun;

public class HintPaper : InteractableBase
{
    [SerializeField] private TMP_Text text;
    public bool InitComplete { get; private set; }

    protected override IEnumerator InitRoutine()
    {
        InitComplete = false;
        yield break; 
    }

    // 힌트 내용 세팅하기
    public void SetHintText(string val)
    {
        if (text == null)
            text = this.transform.GetChild(0).GetComponent<TMP_Text>();
            
        text.text = val;

        if (hintData.HasValue) return;

        hintData = new HintData { hintKey = HintKeys.KEYPAD_DIGIT, payload = val };
        InitComplete = true;
    }

    public override void Interact(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (rewardItem != null)
            PhotonNetwork.Destroy(this.gameObject);
    }
}