using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip failureSound;
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip itemPickUpSound;
    [SerializeField] private AudioClip promptSound;
    [SerializeField] private AudioClip lightOnSound;
    [SerializeField] private AudioClip putInItemSound;

    private void Awake()
    {
        Instance = this;

        audioSource = this.GetComponent<AudioSource>();
    }

    public void PlayButtonClickSound() => audioSource.PlayOneShot(buttonClickSound);
    public void PlayFailureSound() => audioSource.PlayOneShot(failureSound);
    public void PlayCorrectSound() => audioSource.PlayOneShot(correctSound);
    public void PlayItemPickUpSound() => audioSource.PlayOneShot(itemPickUpSound);
    public void PlayPromptSound() => audioSource.PlayOneShot(promptSound);
    public void PlayLightOnSound() => audioSource.PlayOneShot(lightOnSound);
    public void PlayPutInItemSound() => audioSource.PlayOneShot(putInItemSound);
}
