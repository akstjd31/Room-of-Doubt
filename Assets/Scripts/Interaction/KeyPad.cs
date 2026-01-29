using System.Collections;
using UnityEngine;

public class KeyPad : InteractableBase
{
    [SerializeField] private KeyPadManager keyPadMgr;

    private void Awake()
    {
        if (keyPadMgr == null)
        {
            keyPadMgr = this.GetComponent<KeyPadManager>();    
            keyPadMgr.Init();
            
            keyPadMgr.enabled = false;
        }
        
        type = InteractableType.Puzzle;
    }

    public override void Interact(int actorNumber)
    {
        if (keyPadMgr == null) return;

        keyPadMgr.enabled = isInteracting;
            
    }

    protected override IEnumerator InitRoutine()
    {
        while (playerCamCtrl == null)
        {
            playerCamCtrl = FindLocalCamCtrl();
            if (playerCamCtrl == null)
                yield return null; // 다음 프레임
        }
    }

    public bool IsSolved() => keyPadMgr.IsSolved;
}
