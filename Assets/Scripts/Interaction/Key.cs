using Photon.Pun;
using UnityEngine;

public class Key : MonoBehaviourPun, IInteractable
{
    [SerializeField] private string prompt;
    public int ViewId => photonView.ViewID;

    public string Prompt => prompt;

    [SerializeField] private Item item;
    public Item RewardItem => item;

    public bool CanInteract(int actorViewId, Item equippedItem = null)
    {
        return true;   
    }

    public void Interact(int actorViewId, Item usedItem = null)
    {
        PhotonView actorPv = PhotonView.Find(actorViewId);

        if (actorPv != null)
        {
            var quickMgr = actorPv.GetComponent<QuickSlotManager>();
            quickMgr.AddItem(RewardItem);
        }

        this.gameObject.SetActive(false);
    }

    public void ClientApplyState()
    {
        
    }
}
