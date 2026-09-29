using System;

[Serializable]
public class NegotiationResult
{
    public string npc_reply;
    public string hit_quality;
    public string sprite_reaction;

    public HitQuality ParseHitQuality()
    {
        if (string.Equals(hit_quality, "perfect", StringComparison.OrdinalIgnoreCase)) return HitQuality.Perfect;
        if (string.Equals(hit_quality, "partial", StringComparison.OrdinalIgnoreCase)) return HitQuality.Partial;
        return HitQuality.Miss;
    }

    public SpriteReaction ParseSpriteReaction()
    {
        if (string.Equals(sprite_reaction, "approval", StringComparison.OrdinalIgnoreCase)) return SpriteReaction.Approval;
        if (string.Equals(sprite_reaction, "disapproval", StringComparison.OrdinalIgnoreCase)) return SpriteReaction.Disapproval;
        return SpriteReaction.Neutral;
    }
}
