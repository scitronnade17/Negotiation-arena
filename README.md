# Арена переговоров

Симулятор переговоров: игрок отвечает NPC репликами, ИИ оценивает их попадание в
уязвимость персонажа и превращает это в HP-бой.

## Стек

- **Unity 6000.3.18f1**, URP, целевая платформа — **WebGL**
- UI: legacy **uGUI** (`UnityEngine.UI`), один `SampleScene` с несколькими Canvas-экранами
  (переключаются через `SetActive`, отдельных сцен нет)
- Прогресс: `PlayerPrefs`
- LLM: **DeepSeek v4 Flash** через **Yandex AI Studio** (`ai.api.cloud.yandex.net/v1/responses`)