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

        // 생성되자마자 리스너 트랜스폼 등록
        var al = GetComponentInChildren<AudioListener>();
        if (al != null) LocalListener.Transform = al.transform;
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
    
    private void Update()
    {
        var listener = LocalListener.Transform;
        if (listener == null) return;

        // 두 사이 거리 비교 및 레이를 쏴 사이에 장애물이 있는 경우 볼륨 조절
        Vector3 from = this.transform.position;
        Vector3 to = listener.transform.position;
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return;

        bool blocked = Physics.Raycast(from, dir / dist, dist , occlusionMask);

        float target = blocked ? blockedVolume : openVolume;
        audioSource.volume = Mathf.Lerp(audioSource.volume, target, Time.deltaTime * lerpSpeed);
    }

    private void OnTalkStarted(InputAction.CallbackContext ctx) => recorder.TransmitEnabled = true;
    private void OnTalkCanceled(InputAction.CallbackContext ctx) => recorder.TransmitEnabled = false;
}
