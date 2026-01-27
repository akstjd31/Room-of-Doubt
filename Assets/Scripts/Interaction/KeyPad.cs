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
        if (keyPadMgr == null) yield break;
    }

    public bool IsSolved() => keyPadMgr.IsSolved;
}
