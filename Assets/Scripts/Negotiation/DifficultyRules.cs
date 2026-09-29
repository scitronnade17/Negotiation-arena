public static class DifficultyRules
{
    public static int StartingHp(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy: return 100;
            case Difficulty.Medium: return 75;
            case Difficulty.Hard: return 50;
            default: return 100;
        }
    }

    public static int PlayerDamageToAgent(HitQuality quality)
    {
        switch (quality)
        {
            case HitQuality.Perfect: return 25;
            case HitQuality.Partial: return 15;
            default: return 0;
        }
    }

    public static int AgentDamageToPlayer(Difficulty difficulty, HitQuality quality)
    {
        if (quality == HitQuality.Perfect) return 0;

        if (quality == HitQuality.Partial)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return 0;
                case Difficulty.Medium: return 5;
                case Difficulty.Hard: return 10;
                default: return 0;
            }
        }

        switch (difficulty)
        {
            case Difficulty.Easy: return 15;
            case Difficulty.Medium: return 25;
            case Difficulty.Hard: return 40;
            default: return 0;
        }
    }

    public static string RulesDescription(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Easy:
                return "Будь снисходителен. Если игрок хотя бы немного движется в правильном направлении, засчитывай это как частичное попадание.";
            case Difficulty.Medium:
                return "Оценивай логично. Ошибки наказываются, правильные действия поощряются.";
            case Difficulty.Hard:
                return "Будь крайне придирчив. Игрок должен сформулировать аргумент идеально, иначе это промах. Никаких поблажек за \"воду\".";
            default:
                return "";
        }
    }
}
