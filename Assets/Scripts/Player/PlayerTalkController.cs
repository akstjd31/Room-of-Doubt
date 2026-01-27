using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Voice.Unity;

[RequireComponent(typeof(Recorder))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerTalkController : MonoBehaviourPun
{
    [Header("References")]
    private Recorder recorder;
    private PlayerInput playerInput;
    private InputAction talkAction;
    private AudioSource audioSource;

    [Header("Occlusion")]
    [SerializeField] private LayerMask occlusionMask;
    [SerializeField] private float blockedVolume = 0.1f;
    [SerializeField] private float openVolume = 1f;
    [SerializeField] private float lerpSpeed = 5f;

    private void Awake()
    {
        recorder = this.GetComponent<Recorder>();
        playerInput = this.GetComponent<PlayerInput>();
        audioSource = this.GetComponent<AudioSource>();

        talkAction = playerInput.actions["Talk"];
    }

    private void OnEnable()
    {
        if (!photonView.IsMine) return;

        talkAction.started += OnTalkStarted;
        talkAction.canceled += OnTalkCanceled;
    }

    private void OnDisable()
    {
        if (talkAction != null)
        {
            talkAction.started -= OnTalkStarted;
            talkAction.canceled -= OnTalkCanceled;
        }
    }

    private void Start()
    {
        if (!photonView.IsMine)
        {
            recorder.TransmitEnabled = false;
            recorder.enabled = false;
            return;
        }

        recorder.TransmitEnabled = false;
    }

    private void OnTalkStarted(InputAction.CallbackContext ctx) => recorder.TransmitEnabled = true;
    private void OnTalkCanceled(InputAction.CallbackContext ctx) => recorder.TransmitEnabled = false;
}
