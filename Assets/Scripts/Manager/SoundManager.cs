using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip failureSound;
    [SerializeField] private AudioClip correctSound;

    private void Awake()
    {
        Instance = this;

        audioSource = this.GetComponent<AudioSource>();
    }

    public void PlayButtonClickSound() => audioSource.PlayOneShot(buttonClickSound);
    public void PlayFailureSound() => audioSource.PlayOneShot(failureSound);
    public void PlayCorrectSound() => audioSource.PlayOneShot(correctSound);
}
