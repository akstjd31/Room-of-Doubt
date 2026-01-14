using Photon.Pun;
using UnityEngine;

public class Key : MonoBehaviourPun, IInteractable
{
    [SerializeField] private string prompt;
    public int ViewId => photonView.ViewID;

    public string Prompt => prompt;

    [SerializeField] private Item item;
    public Item RewardItem => item;

    public bool CanInteract(int actorNumber, Item equippedItem = null)
    {
        return true;   
    }

    public void Interact(int actorNumber, Item usedItem = null)
    {
        if (GameManager.Instance.playerData.TryGetValue(actorNumber, out GameObject playerObj))
        {
            var quickMgr = playerObj.GetComponent<QuickSlotManager>();
            quickMgr.AddItem(RewardItem);
        }

        this.gameObject.SetActive(false);
    }

    public void ClientApplyState()
    {
        
    }
}
