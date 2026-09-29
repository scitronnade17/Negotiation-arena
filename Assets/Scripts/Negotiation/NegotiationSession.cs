using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TurnOutcome
{
    public string NpcReply;
    public HitQuality HitQuality;
    public SpriteReaction SpriteReaction;
    public int DamageToAgent;
    public int DamageToPlayer;
    public int AgentHp;
    public int PlayerHp;
    public bool AgentDefeated;
    public bool PlayerDefeated;
}

[Serializable]
public class TurnRecord
{
    public string PlayerMessage;
    public string NpcReply;
    public HitQuality Quality;
}

public class NegotiationSession : MonoBehaviour
{
    public NegotiationClient client;
    public ScenarioDefinition scenario;
    public Difficulty difficulty = Difficulty.Medium;
    public Tone tone = Tone.Neutral;

    public int PlayerHp { get; private set; }
    public int AgentHp { get; private set; }

    public List<TurnRecord> History { get; } = new List<TurnRecord>();

    public void BeginSession()
    {
        PlayerHp = DifficultyRules.StartingHp(difficulty);
        AgentHp = DifficultyRules.StartingHp(difficulty);
        History.Clear();
    }

    public void TakeTurn(string playerMessage, Action<TurnOutcome> onOutcome, Action<string> onError)
    {
        string systemPrompt = BuildSystemPrompt();
        client.Evaluate(systemPrompt, playerMessage, result =>
        {
            var quality = result.ParseHitQuality();
            int dmgToAgent = DifficultyRules.PlayerDamageToAgent(quality);
            int dmgToPlayer = DifficultyRules.AgentDamageToPlayer(difficulty, quality);

            AgentHp = Mathf.Max(0, AgentHp - dmgToAgent);
            PlayerHp = Mathf.Max(0, PlayerHp - dmgToPlayer);

            History.Add(new TurnRecord
            {
                PlayerMessage = playerMessage,
                NpcReply = result.npc_reply,
                Quality = quality
            });

            var outcome = new TurnOutcome
            {
                NpcReply = result.npc_reply,
                HitQuality = quality,
                SpriteReaction = result.ParseSpriteReaction(),
                DamageToAgent = dmgToAgent,
                DamageToPlayer = dmgToPlayer,
                AgentHp = AgentHp,
                PlayerHp = PlayerHp,
                AgentDefeated = AgentHp <= 0,
                PlayerDefeated = PlayerHp <= 0
            };
            onOutcome?.Invoke(outcome);
        }, onError);
    }

    private string BuildSystemPrompt()
    {
        return
            "Ты — NPC в симуляторе переговоров.\n" +
            "Твоя текущая ситуация: " + scenario.context + "\n" +
            "Твоя уязвимость (ключ к победе): " + scenario.vulnerability + "\n" +
            "Твоя манера общения: " + ToneLibrary.Description(tone) + "\n" +
            "Текущая сложность игры: " + DifficultyRules.RulesDescription(difficulty) + "\n\n" +
            "ВАЖНО: ты живой человек в эмоциональной ситуации, а не ассистент и не бот техподдержки. " +
            "Никогда не комментируй сообщение игрока со стороны и не давай ему инструкций. " +
            "ЗАПРЕЩЕНО писать фразы вроде: \"уточните запрос\", \"сообщение не содержит информации/смысла\", " +
            "\"я не могу на это ответить\", \"предлагаю вернуться к обсуждению\" — это разрушает персонажа.\n" +
            "ЭТО ЖИВОЙ УСТНЫЙ РАЗГОВОР, А НЕ ПЕРЕПИСКА. Игрок говорит с тобой вслух, а не пишет сообщения. " +
            "СТРОГО ЗАПРЕЩЕНО использовать слова \"пишешь\", \"написал\", \"сообщение\", \"текст\", \"печатаешь\" " +
            "и любые другие отсылки к тому, что это переписка — реагируй так, будто слышишь слова собеседника вживую.\n" +
            "Если реплика игрока короткая, невнятная, бессмысленная или пустая — это тоже \"miss\", но реагируй " +
            "ЖИВОЙ ЭМОЦИЕЙ персонажа, как будто собеседник действительно что-то невнятное сказал или промолчал. " +
            "Например (не копируй дословно, это просто иллюстрация тона): \"Что? Я тебе о серьёзном, а ты мне " +
            "что несёшь?\" или \"...Ты вообще меня слушаешь сейчас?\" — растерянно, раздражённо или язвительно, " +
            "смотря по твоему тону, но всегда от первого лица как сам персонаж.\n\n" +
            "Оцени реплику игрока по hit_quality: \"perfect\" — игрок бьёт в уязвимость и предлагает решение; " +
            "\"partial\" — правильный вектор, но мало конкретики; \"miss\" — игрок игнорирует уязвимость, давит, хамит, " +
            "или пишет что-то невпопад/бессмысленное.\n\n" +
            "npc_reply должен быть короткой репликой (2-4 предложения), в стиле живого диалога, а не монологом.\n\n" +
            "Ответь СТРОГО одним JSON-объектом и ничем больше — без markdown, без блоков кода, без пояснений до или после. " +
            "Ровно в этом формате (замени только значения):\n" +
            "{\"npc_reply\":\"<твой ответ игроку в заданном тоне>\",\"hit_quality\":\"<perfect|partial|miss>\",\"sprite_reaction\":\"<approval|neutral|disapproval>\"}";
    }
}
