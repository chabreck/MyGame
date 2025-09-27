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

    public int CurrentExp { get; private set; }
    public int CurrentLevel { get; private set; } = 1;
    public int ExpToNextLevel { get; private set; } = 100;

    private const int MAX_DEFINED_LEVEL = 50;
    private const float GROWTH_RATE = 1.12f;
    private const int BASE_EXP = 50;
    private int[] expTable;
    private int fixedPostMaxExp;

    private void Awake()
    {
        BuildExpTable();
        ExpToNextLevel = GetExpRequirementForLevel(CurrentLevel);
    }

    private void Start()
    {
        StartCoroutine(DelayedInitialChoice());

        if (GameStatsUIController.Instance != null)
            GameStatsUIController.Instance.UpdateXPUI(CurrentExp, ExpToNextLevel, CurrentLevel);

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

    private void BuildExpTable()
    {
        expTable = new int[MAX_DEFINED_LEVEL + 1];
        for (int lvl = 1; lvl <= MAX_DEFINED_LEVEL; lvl++)
        {
            double val = BASE_EXP * Math.Pow(GROWTH_RATE, lvl - 1);
            expTable[lvl] = Mathf.Max(1, Mathf.RoundToInt((float)val));
        }
        fixedPostMaxExp = expTable[MAX_DEFINED_LEVEL];
    }

    public int GetExpRequirementForLevel(int level)
    {
        if (level <= 0) return expTable[1];
        if (level >= MAX_DEFINED_LEVEL) return fixedPostMaxExp;
        return expTable[level];
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        CurrentExp += amount;
        OnExperienceCollected?.Invoke(amount);

        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            CurrentLevel++;

            ExpToNextLevel = GetExpRequirementForLevel(CurrentLevel);

            PlayLevelUpSound();

            OnLevelUp?.Invoke(CurrentLevel);

            if (GameStatsUIController.Instance != null)
                GameStatsUIController.Instance.UpdateXPUI(CurrentExp, ExpToNextLevel, CurrentLevel);
        }

        if (GameStatsUIController.Instance != null)
            GameStatsUIController.Instance.UpdateXPUI(CurrentExp, ExpToNextLevel, CurrentLevel);
    }

    private void PlayLevelUpSound()
    {
        if (levelUpSound == null) return;
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

    public int GetExpRequirementForNextLevel() => ExpToNextLevel;

    public int GetMaxDefinedLevel() => MAX_DEFINED_LEVEL;

    public int[] GetExpTableCopy()
    {
        int[] copy = new int[expTable.Length];
        Array.Copy(expTable, copy, expTable.Length);
        return copy;
    }
}
