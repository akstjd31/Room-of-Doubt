using System.Collections;
using UnityEngine;
using Photon.Pun;

public class LiftObject : InteractableBase
{
    private AudioSource audiosource;
    [SerializeField] private AudioClip[] objectSound;
    
    public override void Interact(int actorNumber)
    {
        // 로컬만 실행
        if (PhotonNetwork.LocalPlayer.ActorNumber != actorNumber)
            return;

        // 물체 들어올리기
        float delta = isInteracting ? 0.3f : -0.3f;
        transform.position += new Vector3(0f, delta, 0f);

        audiosource.PlayOneShot(isInteracting ? objectSound[0] : objectSound[1]);
    }

    protected override IEnumerator InitRoutine()
    {
        audiosource = this.GetComponent<AudioSource>();
        yield break;
    }
}
