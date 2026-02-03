using System.Collections;
using UnityEngine;

public class SwitchLight : InteractableBase
{
    [SerializeField] private GameObject spotLight;
    [SerializeField] private FindNearByGrowHint[] hintAreas;
    private AudioSource audioSource;
    [SerializeField] private AudioClip switchSound;
    private bool isOn = true;
    public override void Interact(int actorNumber)
    {
        // 해당 퍼즐 (모든 불을 키는 그런 퍼즐) 이 해결안되었다면 기능 사용 X
        // if (!GameManager.Instance.WirePuzzleSolved)
        // {
        //     prompt = "불이 안켜진다.";
        //     UIManager.Instance.ShowMessage(prompt);
        //     return;
        // }

        isOn = !isOn;
        audioSource.PlayOneShot(switchSound);
        if (spotLight == null) return;
        spotLight.SetActive(isOn);

        if (hintAreas == null) return;
        foreach (FindNearByGrowHint hArea in hintAreas)
        {
            if (hArea.GlowHint == null) continue;
            hArea.GlowHint.SetGlowVisible(isOn);
            // hArea.SetLayer(isOn);
        }
    }

    protected override IEnumerator InitRoutine()
    {
        hintAreas = GetComponentsInChildren<FindNearByGrowHint>();
        audioSource = this.GetComponent<AudioSource>();
        yield break;
    }
}
