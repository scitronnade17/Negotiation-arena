# Арена переговоров

Симулятор переговоров: игрок отвечает NPC репликами, ИИ оценивает их попадание в
уязвимость персонажа и превращает это в HP-бой.
Сама игра по ссылке: [https://scitronnade17.github.io/Negotiation-arena/](https://play.unity.com/en/games/20a82ec9-807d-4bbc-8d46-e2d5f1339602/arena-peregovorov)

## Стек

- **Unity 6000.3.18f1**, URP, целевая платформа — **WebGL**
- UI: legacy **uGUI** (`UnityEngine.UI`), один `SampleScene` с несколькими Canvas-экранами
  (переключаются через `SetActive`, отдельных сцен нет)
- Прогресс: `PlayerPrefs`
- LLM: **DeepSeek v4 Flash** через **Yandex AI Studio** (`ai.api.cloud.yandex.net/v1/responses`)
