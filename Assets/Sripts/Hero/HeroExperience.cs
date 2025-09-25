using System;
using System.Collections;
using UnityEngine;

public class HeroExperience : MonoBehaviour
{
    public event Action<int> OnLevelUp;
    public event Action OnInitialChoice;
    public event Action<int> OnExperienceCollected;

    [SerializeField] private AudioClip levelUpSound;
    [SerializeField] private float volume = 1f;

    // Текущее состояние
    public int CurrentExp { get; private set; }
    public int CurrentLevel { get; private set; } = 1;
    public int ExpToNextLevel { get; private set; } = 100; // заглушка для остальной логики число 100 ни на что не влияет

    // Конфигурация роста опыта
    private const int MAX_DEFINED_LEVEL = 50;
    private const float GROWTH_RATE = 1.12f; // мягкий экспоненциальный рост
    private const int BASE_EXP = 50;        // требование для уровня 1
    private int[] expTable;                  // таблица требований для уровней 1..MAX_DEFINED_LEVEL
    private int fixedPostMaxExp;             // значение для уровней >= MAX_DEFINED_LEVEL (фиксированное)

    private void Awake()
    {
        BuildExpTable();
        ExpToNextLevel = GetExpRequirementForLevel(CurrentLevel);
    }

    private void Start()
    {
        StartCoroutine(DelayedInitialChoice());

        // Инициализация UI (если UIController присутствует)
        if (GameStatsUIController.Instance != null)
            GameStatsUIController.Instance.UpdateXPUI(CurrentExp, ExpToNextLevel, CurrentLevel);

        // Подписка на локальную нотификацию — обновляем UI при сборе опыта/уроне
        OnExperienceCollected += amt =>
        {
            if (GameStatsUIController.Instance != null)
                GameStatsUIController.Instance.UpdateXPUI(CurrentExp, ExpToNextLevel, CurrentLevel);
        };

        OnLevelUp += lvl =>
        {
            if (GameStatsUIController.Instance != null)
                GameStatsUIController.Instance.UpdateXPUI(CurrentExp, ExpToNextLevel, CurrentLevel);
        };
    }

    private IEnumerator DelayedInitialChoice()
    {
        yield return new WaitForEndOfFrame();
        OnInitialChoice?.Invoke();
    }

    /// <summary>
    /// Построение таблицы требований опыта для уровней 1..MAX_DEFINED_LEVEL
    /// </summary>
    private void BuildExpTable()
    {
        expTable = new int[MAX_DEFINED_LEVEL + 1]; // 1-based index удобнее
        for (int lvl = 1; lvl <= MAX_DEFINED_LEVEL; lvl++)
        {
            double val = BASE_EXP * Math.Pow(GROWTH_RATE, lvl - 1);
            expTable[lvl] = Mathf.Max(1, Mathf.RoundToInt((float)val));
        }
        fixedPostMaxExp = expTable[MAX_DEFINED_LEVEL];
    }

    /// <summary>
    /// Получить требование опыта для заданного уровня (уровень — текущий уровень героя).
    /// Возвращает значение ExpToNextLevel для перехода с этого уровня на следующий.
    /// </summary>
    public int GetExpRequirementForLevel(int level)
    {
        if (level <= 0) return expTable[1];
        if (level >= MAX_DEFINED_LEVEL) return fixedPostMaxExp;
        return expTable[level];
    }

    /// <summary>
    /// Добавляет опыт и обрабатывает возможные повышения уровня.
    /// </summary>
    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        CurrentExp += amount;
        OnExperienceCollected?.Invoke(amount);

        // Повышаем уровень, пока есть опыт
        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            CurrentLevel++;

            // Определяем требование для следующего уровня
            ExpToNextLevel = GetExpRequirementForLevel(CurrentLevel);

            // Воспроизводим звук повышения уровня (если задан)
            PlayLevelUpSound();

            OnLevelUp?.Invoke(CurrentLevel);

            // Обновляем UI после повышения (дополнительно)
            if (GameStatsUIController.Instance != null)
                GameStatsUIController.Instance.UpdateXPUI(CurrentExp, ExpToNextLevel, CurrentLevel);
        }

        // Обновляем UI если не было повышения
        if (GameStatsUIController.Instance != null)
            GameStatsUIController.Instance.UpdateXPUI(CurrentExp, ExpToNextLevel, CurrentLevel);
    }

    private void PlayLevelUpSound()
    {
        if (levelUpSound == null) return;
        // у тебя есть SettingsManager.PlaySFX в проекте — можно так:
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.PlaySFX(levelUpSound, volume);
        }
        else
        {
            // fallback: попытка проиграть через AudioSource на объекте
            var aud = GetComponent<AudioSource>();
            if (aud == null) aud = gameObject.AddComponent<AudioSource>();
            aud.PlayOneShot(levelUpSound, volume);
        }
    }

    // Вспомогательные методы для отладки / UI
    public int GetExpRequirementForNextLevel() => ExpToNextLevel;

    public int GetMaxDefinedLevel() => MAX_DEFINED_LEVEL;

    // Пример: метод, который возвращает таблицу (подходит для UI, отладки)
    public int[] GetExpTableCopy()
    {
        int[] copy = new int[expTable.Length];
        Array.Copy(expTable, copy, expTable.Length);
        return copy;
    }

    // Сохранение/загрузка прогресса оставил на тебя — сейчас логика предполагает, что прогресс в начале игры чистый.
}
