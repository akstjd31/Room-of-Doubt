using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;

public class KeyPadManager : MonoBehaviourPunCallbacks
{
    public static List<KeyPadManager> AllKeypads = new List<KeyPadManager>();
    private const int MAX_CODE_LENGTH = 4;


    [Header("Answer")]
    [SerializeField] private string collect; // 정답

    [Header("Runtime")]
    [SerializeField] private int[] codes;
    [SerializeField] private int currentNumLength;
    [SerializeField] private TMP_Text screenText;
    [SerializeField] private LayerMask numPadMask;

    private Queue<int> hintIndexQueue;

    public bool IsSolved { get; private set; }
    [SerializeField] private bool isFinal;   // 최종 탈출하기 위한 키패드인가?
    public bool IsFinal => isFinal;

    private string input;
    private string result;

    private void Awake()
    {
        AllKeypads.Add(this);
    }

    public void Init()
    {
        codes = new int[MAX_CODE_LENGTH];
        input = "";
        currentNumLength = 0;

        EnsureSharedAnswer();
    }

    private void OnDestroy()
    {
        AllKeypads.Remove(this);
    }

    private void Update()
    {
        if (IsSolved) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 10f, numPadMask))
            {
                SoundManager.Instance.PlayButtonClickSound();
                var numComp = hit.transform.GetComponent<Number>();
                if (numComp == null) return;

                string nStr = numComp.NumStr;
                if (string.IsNullOrEmpty(nStr)) return;

                // 인트 변환 시도 (#, * 제외)
                if (int.TryParse(nStr, out int n))
                {
                    if (currentNumLength >= MAX_CODE_LENGTH)
                    {
                        ResetLocalInput();
                    }

                    codes[currentNumLength] = n;
                    input += nStr;
                    currentNumLength++;
                }
                // #, * 누름: 완료(?) 버튼
                else
                {
                    result = string.Join("", new List<int>(codes).ConvertAll(i => i.ToString()).ToArray());
                    Debug.Log($"[KeyPad] Input Result = {result}");

                    if (collect == result)
                    {
                        if (!IsSolved)
                        {
                            if (isFinal)
                            {
                                SuccessLocal();
                            }
                            else
                            {
                                photonView.RPC(nameof(SuccessRPC), RpcTarget.AllBuffered);
                            }

                            SoundManager.Instance.PlayCorrectSound();
                        }
                    }
                    else
                    {
                        SoundManager.Instance.PlayFailureSound();
                        ResetLocalInput();
                    }

                    input = "";
                    if (screenText != null && !IsSolved)
                        screenText.text = "";
                }


                if (screenText != null)
                    screenText.text = input;
            }
        }
    }

    private void EnsureSharedAnswer()
    {
        if (!PhotonNetwork.InRoom) return;
        if (!PhotonNetwork.IsMasterClient) return;

        string newAnswer = GenerateRandomDigits(MAX_CODE_LENGTH);
        var props = new Hashtable { { PuzzleKeys.KEYPAD_SEED, newAnswer } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        // 자릿수 인덱스 리스트 생성 (0, 1, 2, 3)
        List<int> indices = new List<int>();
        for (int i = 0; i < MAX_CODE_LENGTH; i++) indices.Add(i);

        // 인덱스 셔플 (랜덤한 순서로 힌트를 배출하기 위함)
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int r = UnityEngine.Random.Range(0, i + 1);
            (indices[i], indices[r]) = (indices[r], indices[i]);
        }
        hintIndexQueue = new Queue<int>(indices);

        // 정답 공유
        photonView.RPC(nameof(SetAnswerRPC), RpcTarget.AllBuffered, newAnswer);

        Debug.Log("정답: " + newAnswer);
    }

    public bool TryGetNextHint(out int position, out char value)
    {
        position = -1;
        value = ' ';

        // 1. 마스터가 아니거나 큐가 비었으면 실패
        if (!PhotonNetwork.IsMasterClient || hintIndexQueue == null || hintIndexQueue.Count == 0)
            return false;

        // 2. 마스터가 큐에서 하나 꺼냄
        int idx = hintIndexQueue.Dequeue();
        position = idx + 1;
        value = collect[idx];

        Debug.Log($"[KeyPad] 힌트 생성: {position}번째 자리 = {value}");
        return true;
    }
    // 0 ~ 9 까지 랜덤 수 생성
    private string GenerateRandomDigits(int length)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(UnityEngine.Random.Range(0, 10));
        return sb.ToString();
    }

    [PunRPC]
    private void SetAnswerRPC(string answer) => collect = answer;

    private void ResetLocalInput()
    {
        currentNumLength = 0;
        for (int i = 0; i < codes.Length; i++)
            codes[i] = 0;
        input = "";
    }

    // 로컬 전용 처리
    private void SuccessLocal()
    {
        IsSolved = true;

        Debug.Log("해결! (로컬)");
        if (screenText != null)
        {
            screenText.fontSize = 1200;
            screenText.text = "UNLOCK!";
        }
    }

    // 성공 결과만 공유
    [PunRPC]
    private void SuccessRPC()
    {
        if (IsSolved) return;

        IsSolved = true;

        Debug.Log("해결! (RPC)");

        if (screenText != null)
        {
            screenText.fontSize = 1200;
            screenText.text = "UNLOCK!";
        }
    }

    public string GetCollect() => collect;
}
