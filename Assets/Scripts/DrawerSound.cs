using UnityEngine;

public class DrawerSound : MonoBehaviour
{
    private AudioSource audioSource;
    [SerializeField] private AudioClip[] drawerSounds;

    private void Awake()
    {
        audioSource = this.GetComponent<AudioSource>();
    }
    
    private void OnDrawerOpenSoundEvent() => audioSource.PlayOneShot(drawerSounds[0]);
    private void OnDrawerCloseSoundEvent() => audioSource.PlayOneShot(drawerSounds[1]);
}
