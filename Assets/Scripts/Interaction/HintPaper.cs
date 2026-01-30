using System.Collections;
using TMPro;
using UnityEngine;
using Photon.Pun;

public class HintPaper : InteractableBase
{
    [SerializeField] private TMP_Text text;

    protected override IEnumerator InitRoutine()
    {
        // 이제 종이 스스로 데이터를 요청하지 않습니다.
        // GameManager가 보내주는 데이터를 기다립니다.
        yield break; 
    }

    public void SetHintText(string val)
    {
        if (text == null)
            text = this.transform.GetChild(0).GetComponent<TMP_Text>();
            
        text.text = val;

        if (hintData.HasValue) return;
        
        hintData = new HintData { hintKey = HintKeys.KEYPAD_DIGIT, payload = val };
        Debug.Log("힌트 세팅 완료: " + val);
    }

    public override void Interact(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        PhotonNetwork.Destroy(this.gameObject);
    }
}