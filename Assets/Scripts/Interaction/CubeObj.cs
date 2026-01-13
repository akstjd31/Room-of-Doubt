using Photon.Pun;
using UnityEngine;

public class CubeObj : MonoBehaviourPun, IInteractable
{
    [SerializeField] private string prompt;
    public int ViewId => photonView.ViewID;

    public string Prompt => prompt;

    public bool CanInteract(int actorId)
    {  
        // 잠금 상태라면 false
        return true;
    }
    
    // 나 상호작용 할거야! 하고 모든 클라이언트로부터 수행할 메서드
    public void Interact(int actorId)
    {   
        Debug.Log("상호작용 수행!");
        this.gameObject.SetActive(false);
    }

    public void ClientApplyState()
    {
        // 클라이언트 상태 전달용
    }
}
