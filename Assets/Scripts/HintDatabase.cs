using UnityEngine;

public class HintDatabase : Singleton<HintDatabase>
{
    public string Render(string hintKey, string payload)
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
        switch (hintKey)
        {
            case WireHintKeys.COLOR_MAP:
                return wireMgr.BuildColorHintText();

            case WireHintKeys.PORT_MAP:
                return wireMgr.BuildPortPairHintText();

            case WireHintKeys.PARTIAL:
                return wireMgr.BuildPartialHintText(2);
        }

        return "알 수 없는 힌트";
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
