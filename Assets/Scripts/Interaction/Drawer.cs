using System.Collections;
using UnityEngine;

public class Drawer : InteractableBase
{
    private Animator anim;

    private bool isOpen;
    [SerializeField] private KeyPad keyPad;

    private void Awake()
    {
        anim = this.transform.parent.GetComponent<Animator>();

        isOpen = false;

        if (keyPad == null)
            keyPad = GetComponentInParent<KeyPad>() ?? GetComponentInChildren<KeyPad>(true);
    }

    public override void Interact(int actorNumber)
    {
        if (keyPad == null)
        {
            isOpen = !isOpen;
            anim.SetBool("IsOpen", isOpen);
            return;
        }
        else
        {
            // 서랍에 키패드가 달려있는 경우 해결 필요
            if (keyPad.IsSolved())
            {
                if (prompt.Length >= 1) prompt = "";

                // 마우스 커서가 안보일 때 == 트랜잭션 종료 시
                if (!Cursor.visible)
                {
                    isOpen = !isOpen;
                    anim.SetBool("IsOpen", isOpen);
                }
            }
        }
    }

    protected override IEnumerator InitRoutine()
    {
        yield break;
    }


}
