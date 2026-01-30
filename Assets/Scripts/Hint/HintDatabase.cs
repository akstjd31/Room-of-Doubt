using UnityEngine;
using System.Collections.Generic;

public class HintDatabase : Singleton<HintDatabase>
{
    public string Render(string hintKey, string payload)
    {
        // 매니저 참조 (전체 번호 확인용)
        KeyPadManager keyPadMgr = FindFirstObjectByType<KeyPadManager>(FindObjectsInactive.Include);
        WirePuzzleManager wireMgr = FindFirstObjectByType<WirePuzzleManager>(FindObjectsInactive.Include);

        switch (hintKey)
        {
            // 1. 전체 비밀번호를 다 보여주는 경우
            case HintKeys.KEYPAD_PASSWORD:
                string fullCode = keyPadMgr != null ? keyPadMgr.GetCollect() : payload;
                return $"적혀있는 전체 비밀번호: <color=#00FF00>{fullCode}</color>";

            // 2. 특정 자릿수 하나만 알려주는 경우 (신규)
            case HintKeys.KEYPAD_DIGIT:
                return ParseDigitPayload(payload);

            // 3. 전선 퍼즐 힌트
            case HintKeys.WIRE_COLOR_MAP:
                return wireMgr != null ? wireMgr.BuildColorHintText() : "전선 정보를 읽을 수 없습니다.";

            default:
                return "기록된 내용을 해독할 수 없습니다.";
        }
    }

    /// <summary>
    /// "POS=2|VAL=7" 형태의 페이로드를 읽어 "2번째 숫자는 7이다" 문장 생성
    /// </summary>
    private string ParseDigitPayload(string payload)
    {
        if (string.IsNullOrEmpty(payload)) return "종이가 너무 낡아 글자를 알아볼 수 없습니다.";

        var data = new Dictionary<string, string>();
        foreach (var part in payload.Split('|'))
        {
            var kv = part.Split('=');
            if (kv.Length == 2) data[kv[0]] = kv[1];
        }

        if (data.TryGetValue("POS", out string pos) && data.TryGetValue("VAL", out string val))
        {
            // 상황별 문구 리스트
            string[] templates = {
            $"[누군가의 기록]\n\"놈들이 비밀번호를 바꿨다.\n다행히 <color=#FFD700>{pos}번째</color> 숫자만큼은 <color=#00FF00>{val}</color>인 것을 확인했다.\"",

            $"[낡은 일기장]\n\"내 기억력이 예전만 못하다.\n잊지 않기 위해 적어둔다.\n<color=#FFD700>{pos}번째</color> 칸은 <color=#00FF00>{val}</color>이다.\"",

            $"[벽에 휘갈겨진 낙서]\n\"탈출하고 싶다면 기억해라...\n<color=#FFD700>{pos}</color>... 그 자리의 숫자는... <color=#00FF00>{val}</color>이다...\"",

            $"[희미한 쪽지]\n\"금고의 <color=#FFD700>{pos}번째</color> 다이얼을\n<color=#00FF00>{val}</color>에 맞추니 딸깍하는 소리가 들렸다.\"",

            $"[수첩 조각]\n\"단서는 흩어져 있다.\n내가 찾은 건 <color=#FFD700>{pos}번</color> 자리가 <color=#00FF00>{val}</color>이라는 것뿐이다.\""
        };

            // 자릿수(pos)를 시드로 사용하여 각 종이마다 고정된 문구가 나오게 하거나, Random 사용
            int index = int.Parse(pos) % templates.Length;
            return templates[index];
        }

        return "일부 숫자가 보이지만 문맥을 파악하기 어렵습니다.";
    }
}