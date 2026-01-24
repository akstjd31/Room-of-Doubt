public static class HintKeys
{
    public const string WIRE_COLOR_MAP = "WIRE_COLOR_MAP";   // 색 → 색 힌트
    public const string WIRE_PORT_MAP  = "WIRE_PORT_MAP";    // 포트 → 포트 힌트
    public const string WIRE_PARTIAL   = "WIRE_PARTIAL";     // 일부 공개 힌트

    public static readonly string[] All =
    {
        // 와이어 퍼즐
        WIRE_COLOR_MAP,
        WIRE_PORT_MAP,
        WIRE_PARTIAL,   
    };
}

public static class ItemKeys
{
    public const string LAMP = "ITEM_LAMP";
}


public static class HintPools
{
    // Wire 퍼즐 "시작 힌트"로 허용되는 키만!
    public static readonly string[] WireStart =
    {
        HintKeys.WIRE_COLOR_MAP,
        HintKeys.WIRE_PORT_MAP,
        HintKeys.WIRE_PARTIAL,
        ItemKeys.LAMP, // 너가 wire seed 기반으로 램프 패턴/전원 힌트도 줄 거면 포함
    };
}



public static class PuzzleKeys
{
    public const string KEY_WIRE_SEED = "PUZ_WIRE_SEED";
}

public static class RoomPropKeys
{
    // Start hint specs (Room Custom Properties)
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
}

public static class PlayerPropKeys
{
    public const string LAMP_ON = "LAMP_ON";
}
