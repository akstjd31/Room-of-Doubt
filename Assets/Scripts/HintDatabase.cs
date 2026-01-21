using UnityEngine;

public class HintDatabase : MonoBehaviour
{
    public static HintDatabase Instance;

    private void Awake()
    {
        Instance = this;
    }

    public string Render(int hintId, string payload)
    {
        // payload는 지금 "wireSeed" 하나만 들어간다고 가정
        // 나중에 여러 퍼즐 seed를 합치면: "wireSeed|cabSeed|picSeed" 형태로 확장 가능
        int wireSeed = ParseFirstSeed(payload);

        // 현재 씬의 WirePuzzleManager를 찾는다 (비활성 오브젝트까지 찾으려면 true)
        WirePuzzleManager wireMgr = FindObjectOfType<WirePuzzleManager>(true);

        // 퍼즐 매니저가 없으면 최소한의 메시지 반환
        if (wireMgr == null)
            return $"[힌트 생성 실패] WirePuzzleManager를 찾을 수 없습니다. (seed={wireSeed})";

        // hintId에 따라 표현을 다르게
        switch (hintId)
        {
            case 2001: // A: 색 -> 색 매핑표
                return wireMgr.BuildColorHintText() ?? "힌트를 생성할 수 없습니다.";

            case 2002: // B: 포트번호 -> 포트번호 매핑표
                return wireMgr.BuildPortPairHintText();

            case 2003: // C: 부분 공개 (예: 2개만)
                return wireMgr.BuildPartialHintText(revealCount: 2);

            case 2004: // D: 안내형(예시)
                return "배선 퍼즐의 규칙: 같은 색을 연결하는 것이 아니라, 위/아래의 “정답 매핑”에 따라 연결해야 합니다.\n"
                     + "다른 플레이어의 힌트를 참고하세요.";

            default:
                return $"알 수 없는 힌트(hintId={hintId})";
        }
    }

    private int ParseFirstSeed(string payload)
    {
        if (string.IsNullOrEmpty(payload)) return 0;

        // 단일 seed: "12345"
        // 멀티 seed: "12345|67890|111" -> 첫 번째만 사용
        var parts = payload.Split('|');
        if (int.TryParse(parts[0], out int seed))
            return seed;

        return 0;
    }
}
