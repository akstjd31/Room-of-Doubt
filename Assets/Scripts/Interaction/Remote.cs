using System.Collections;
using UnityEngine;
using Photon.Pun;

public class Remote : InteractableBase
{
    [SerializeField] private GameObject tapeObj;
    public override void Interact(int actorNumber)
    {
        Debug.Log("상호작용중!");
        if (rewardItem != null)
        {
            rewardItem = null;

            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.Destroy(this.gameObject);
            return;
        }
        
        Debug.Log("여기까지 올 수 있나?");
        if (needItem != null)
        {
            needItem = null;
            tapeObj.SetActive(false);

            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.Destroy(this.gameObject);
            return;
        }

        Debug.Log("상호작용의 끝");
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }
}
