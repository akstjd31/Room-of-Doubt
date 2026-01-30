public static class HintKeys
{
    public const string WIRE_COLOR_MAP  = "WIRE_COLOR_MAP";   // 색 → 색 힌트
    public const string WIRE_PORT_MAP   = "WIRE_PORT_MAP";    // 포트 → 포트 힌트
    public const string WIRE_PARTIAL    = "WIRE_PARTIAL";     // 와이어 퍼즐 일부 공개 힌트
    public const string KEYPAD_PASSWORD = "KEYPAD_PASSWORD";  // 키 패드 비밀번호
    public const string KEYPAD_DIGIT    = "KEYPAD_DIGIT";     // 특정 자릿수만 (새로 추가)
    public static readonly string[] WirePuzzle =
    {
        // 와이어 퍼즐
        WIRE_COLOR_MAP,
        WIRE_PORT_MAP,
        // WIRE_PARTIAL,   
    };
}

public static class ItemKeys
{
    public const string LAMP = "ITEM_LAMP";
}


public static class HintPools
{
    // 시작 시 지급될 '종이 힌트' 종류만 정의
    public static readonly string[] Start =
    {
        HintKeys.WirePuzzle[0],
        // HintKeys.WIRE_PORT_MAP,
        //HintKeys.WIRE_PARTIAL,
        HintKeys.KEYPAD_PASSWORD
    };
}



public static class PuzzleKeys
{
    public const string KEY_WIRE_SEED = "PUZ_WIRE_SEED";
    public const string KEYPAD_SEED = "KEYPAD_SEED";
}

public static class RoomPropKeys
{
    // Start hint specs (Room Custom Properties)
    public const string START_A_LAMP = "START_HINT_A_LAMP";
    public const string START_B_LAMP = "START_HINT_B_LAMP";
    public const string START_C_LAMP = "START_HINT_C_LAMP";
    public const string START_D_LAMP = "START_HINT_D_LAMP";
    
    public const string START_READY = "START_HINT_READY";

    public const string START_A_ID  = "START_HINT_A_ID";
    public const string START_A_PAY = "START_HINT_A_PAY";

    public const string START_B_ID  = "START_HINT_B_ID";
    public const string START_B_PAY = "START_HINT_B_PAY";

    public const string START_C_ID  = "START_HINT_C_ID";
    public const string START_C_PAY = "START_HINT_C_PAY";

    public const string START_D_ID  = "START_HINT_D_ID";
    public const string START_D_PAY = "START_HINT_D_PAY";

    // Player Custom Properties
    public const string ROLE = "ROLE";

    // Room global state example (light)
    // public const string LAMP_OWNER_ACTOR = "LAMP_OWNER_ACTOR"; // 주인
    // public const string LAMP_ON          = "LAMP_ON";          // bool
}

public static class PlayerPropKeys
{
    public const string LAMP_ON = "LAMP_ON";
}
