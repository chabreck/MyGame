using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

public class UpgradeSystem : MonoBehaviour
{
    public static bool IsChoosingUpgrade { get; private set; }

    [Serializable]
    public class UpgradeOption
    {
        public string upgradeID;
        public string Title;
        public string Description;
        public LocalizedString titleLocalized;
        public LocalizedString descriptionLocalized;
        public Sprite Icon;
        public UpgradeType Type;
        public int currentLevel = 1;
        public string[] tags;
        public MonoBehaviour UpgradeComponent;
        public IUpgrade UpgradeLogic;
        public WeaponBase Weapon;
        public UpgradeBase UpgradeData;
        public EvolutionRecipe EvolutionRecipeRef;
        public WeaponBase SourceWeapon;
        public UpgradeBase SourceUpgradeData;
    }

    public enum UpgradeType { NewWeapon, WeaponUpgrade, Upgrade, Evolution }

    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private UpgradeButton[] buttons;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private UpgradeSlotManager slotManager;

    [Header("Upgrades")]
    [SerializeField] private List<UpgradeBase> allUpgrades;

    [Header("Weapons")]
    [SerializeField] private List<WeaponBase> allWeapons = new List<WeaponBase>();

    [Header("Evolution")]
    [SerializeField] private List<EvolutionRecipe> evolutionRecipes = new List<EvolutionRecipe>();

    private HeroExperience exp;
    private HeroCombat combat;
    private Dictionary<string, UpgradeOption> activeUpgrades = new Dictionary<string, UpgradeOption>();
    private bool isInitialChoice = false;
    private Queue<int> pendingLevelQueue = new Queue<int>();
    private HashSet<string> appliedEvolutions = new HashSet<string>();
    private HashSet<string> appliedEvolutionPairs = new HashSet<string>();

    private void Awake()
    {
        exp = FindObjectOfType<HeroExperience>();
        combat = FindObjectOfType<HeroCombat>();
        if (exp != null)
        {
            exp.OnInitialChoice += ShowInitialChoices;
            exp.OnLevelUp -= ShowLevelUpChoices;
            exp.OnLevelUp += OnLevelUpQueued;
        }

        panel?.SetActive(false);
        if (buttons != null)
        {
            foreach (var b in buttons) b.Initialize(this);
        }

        if (slotManager == null) slotManager = FindObjectOfType<UpgradeSlotManager>();
    }

    private void OnDestroy()
    {
        if (exp != null)
        {
            exp.OnInitialChoice -= ShowInitialChoices;
            exp.OnLevelUp -= OnLevelUpQueued;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        combat = FindObjectOfType<HeroCombat>();
        exp = FindObjectOfType<HeroExperience>();

        pendingLevelQueue.Clear();
        activeUpgrades.Clear();
        appliedEvolutions.Clear();
        appliedEvolutionPairs.Clear();
        if (slotManager != null) slotManager.ClearSlots();

        if (panel != null) panel.SetActive(false);

        IsChoosingUpgrade = false;
        isInitialChoice = false;
    }

    private void OnLevelUpQueued(int lvl)
    {
        pendingLevelQueue.Enqueue(lvl);
        if (!IsChoosingUpgrade)
        {
            ShowNextPendingLevelChoice();
        }
    }

    private void ShowNextPendingLevelChoice()
    {
        if (pendingLevelQueue.Count == 0) return;
        int lvl = pendingLevelQueue.Dequeue();
        ShowLevelUpChoices(lvl);
    }

    private void ShowInitialChoices()
    {
        IsChoosingUpgrade = true;
        isInitialChoice = true;

        GameStateManager.Instance?.SetGameState(true, true);
        CancelInvoke(nameof(ClosePanel));

        int want = Math.Max(1, buttons != null ? buttons.Length : 3);
        var randomWeapons = allWeapons.OrderBy(x => Guid.NewGuid()).Take(Math.Min(want, allWeapons.Count)).ToList();

        ShowChoices("", CreateWeaponOptions(randomWeapons));
    }

    private List<UpgradeOption> CreateWeaponOptions(List<WeaponBase> weapons)
    {
        List<UpgradeOption> options = new List<UpgradeOption>();
        if (weapons == null) return options;

        foreach (var weapon in weapons)
        {
            if (weapon == null) continue;

            string uniqueId = weapon.name;
            string titleFallback = weapon.GetNameFallback();
            string descFallback = weapon.GetLevelEffectFallback(1) ?? "";

            var opt = new UpgradeOption
            {
                Type = UpgradeType.NewWeapon,
                upgradeID = uniqueId,
                tags = weapon.tags,
                Title = titleFallback,
                Description = descFallback,
                Icon = weapon.weaponIcon,
                Weapon = weapon,
                currentLevel = 1 
            };

            try { opt.titleLocalized = weapon.GetNameLocalized(); } catch { }
            try { opt.descriptionLocalized = weapon.GetLevelEffectLocalized(1); } catch { }

            options.Add(opt);
        }
        return options;
    }

    private void ShowLevelUpChoices(int lvl)
    {
        IsChoosingUpgrade = true;
        isInitialChoice = false;

        GameStateManager.Instance?.SetGameState(true, true);
        CancelInvoke(nameof(ClosePanel));

        var options = new List<UpgradeOption>();
        AddUpgradeOptions(options);
        AddNewWeapons(options);
        AddWeaponUpgrades(options);
        AddEvolutionOptions(options);

        if (slotManager != null && slotManager.IsFull())
        {
            options = options.Where(o => o.Type != UpgradeType.NewWeapon).ToList();
        }

        int want = Math.Max(1, buttons != null ? buttons.Length : 3);
        var final = PickOptionsForDisplay(options, want);

        ShowChoices($"Level {lvl}", final);
    }

    private void AddUpgradeOptions(List<UpgradeOption> options)
    {
        foreach (var upgData in allUpgrades)
        {
            if (upgData == null) continue;
            string id = upgData.name;
            int currentLevel = activeUpgrades.ContainsKey(id) ? activeUpgrades[id].currentLevel : 0;

            if (currentLevel < upgData.maxLevel)
            {
                int nextLevel = currentLevel + 1;
                var opt = new UpgradeOption
                {
                    upgradeID = id,
                    Title = upgData.GetNameFallback(),
                    Description = upgData.GetLevelEffectFallback(nextLevel),
                    Icon = upgData.upgradeIcon,
                    Type = UpgradeType.Upgrade,
                    tags = upgData.tags,
                    currentLevel = nextLevel,
                    UpgradeData = upgData
                };

                try { opt.titleLocalized = upgData.GetNameLocalized(); } catch { }
                try { opt.descriptionLocalized = upgData.GetLevelEffectLocalized(nextLevel); } catch { }

                options.Add(opt);
            }
        }
    }

    private void AddNewWeapons(List<UpgradeOption> options)
    {
        if (slotManager != null && slotManager.IsFull())
        {
            return;
        }

        var equippedWeapons = combat != null ? combat.GetEquippedWeaponData() : new List<WeaponBase>();
        var newWeapons = allWeapons.Where(w => w != null && !equippedWeapons.Contains(w) && !appliedEvolutions.Contains(w.name)).ToList();

        int free = slotManager != null ? slotManager.FreeSlots : int.MaxValue;
        if (free <= 0) return;

        var toAdd = newWeapons.Take(free).ToList();
        options.AddRange(CreateWeaponOptions(toAdd));
    }

    private void AddWeaponUpgrades(List<UpgradeOption> options)
    {
        var equippedInstances = combat != null ? combat.GetEquippedWeaponInstances() : new List<WeaponInstance>();

        foreach (var weaponInstance in equippedInstances)
        {
            if (weaponInstance.Level < weaponInstance.Data.maxLevel)
            {
                int nextLevel = weaponInstance.Level + 1;

                var opt = new UpgradeOption
                {
                    Type = UpgradeType.WeaponUpgrade,
                    upgradeID = weaponInstance.Data.name,
                    tags = weaponInstance.Data.tags,
                    Icon = weaponInstance.Data.weaponIcon,
                    Weapon = weaponInstance.Data,
                    currentLevel = nextLevel
                };

                try
                {
                    var nameLocalized = weaponInstance.Data.GetNameLocalized();
                    if (nameLocalized != null)
                        opt.titleLocalized = nameLocalized;
                }
                catch { }

                try
                {
                    var descLocalized = weaponInstance.Data.GetLevelEffectLocalized(nextLevel);
                    if (descLocalized != null)
                        opt.descriptionLocalized = descLocalized;
                }
                catch { }

                if (string.IsNullOrEmpty(opt.Title))
                    opt.Title = weaponInstance.Data.GetNameFallback() + "+";

                if (string.IsNullOrEmpty(opt.Description) && opt.descriptionLocalized == null)
                {
                    var fallback = weaponInstance.Data.GetLevelEffectFallback(nextLevel);
                    opt.Description = !string.IsNullOrEmpty(fallback) ? fallback : "Upgrade";
                }

                options.Add(opt);
            }
        }
    }

    private void AddEvolutionOptions(List<UpgradeOption> options)
    {
        if (evolutionRecipes == null || evolutionRecipes.Count == 0) return;
        
        var equippedInstances = combat != null ? combat.GetEquippedWeaponInstances() : new List<WeaponInstance>();
        var currentlyEquippedWeaponNames = new HashSet<string>(equippedInstances.Select(w => w.Data.name));
        
        foreach (var recipe in evolutionRecipes)
        {
            if (recipe == null || recipe.resultWeapon == null) continue;
            if (appliedEvolutions.Contains(recipe.resultWeapon.name)) continue;
            
            foreach (var weaponInst in equippedInstances)
            {
                if (weaponInst == null || weaponInst.Data == null) continue;
                if (weaponInst.Level < weaponInst.Data.maxLevel) continue;
                if (appliedEvolutions.Contains(weaponInst.Data.name)) continue;
                
                foreach (var kv in activeUpgrades)
                {
                    var upOpt = kv.Value;
                    if (upOpt == null || upOpt.UpgradeData == null) continue;
                    if (upOpt.currentLevel < upOpt.UpgradeData.maxLevel) continue;
                    
                    string evolutionPairKey = $"{weaponInst.Data.name}_{upOpt.UpgradeData.name}";
                    if (appliedEvolutionPairs.Contains(evolutionPairKey)) continue;
                    
                    if (!recipe.MatchesPair(weaponInst.Data, upOpt.UpgradeData)) continue;
                    
                    string resultWeaponName = recipe.resultWeapon.name;
                    if (appliedEvolutions.Contains(resultWeaponName)) continue;
                    if (currentlyEquippedWeaponNames.Contains(resultWeaponName)) continue;
                    
                    if (UnityEngine.Random.value > recipe.offerChance) continue;

                    var evoOpt = new UpgradeOption
                    {
                        Type = UpgradeType.Evolution,
                        upgradeID = $"evo_{recipe.resultWeapon.name}_{evolutionPairKey}",
                        Title = recipe.resultWeapon.GetNameFallback(),
                        Description = recipe.resultWeapon.GetLevelEffectFallback(1) ?? "Evolution",
                        Icon = recipe.resultWeapon.weaponIcon,
                        Weapon = recipe.resultWeapon,
                        EvolutionRecipeRef = recipe,
                        SourceWeapon = weaponInst.Data,
                        SourceUpgradeData = upOpt.UpgradeData
                    };

                    try { evoOpt.titleLocalized = recipe.resultWeapon.GetNameLocalized(); } catch { }
                    try { evoOpt.descriptionLocalized = recipe.resultWeapon.GetLevelEffectLocalized(1); } catch { }

                    options.Add(evoOpt);
                }
            }
        }
    }

    private List<UpgradeOption> PickOptionsForDisplay(List<UpgradeOption> allOptions, int count)
    {
        if (allOptions == null || allOptions.Count == 0) return new List<UpgradeOption>();

        var distinctById = allOptions.GroupBy(o => o.upgradeID).Select(g => g.First()).ToList();

        var upgrades = distinctById.Where(o => o.Type == UpgradeType.Upgrade).ToList();
        var newWeapons = distinctById.Where(o => o.Type == UpgradeType.NewWeapon).ToList();
        var weaponUpgrades = distinctById.Where(o => o.Type == UpgradeType.WeaponUpgrade).ToList();
        var evolutions = distinctById.Where(o => o.Type == UpgradeType.Evolution).ToList();

        var rnd = new System.Random();
        Func<List<UpgradeOption>, UpgradeOption> PopRandom = (list) =>
        {
            if (list == null || list.Count == 0) return null;
            int i = rnd.Next(0, list.Count);
            var el = list[i];
            list.RemoveAt(i);
            return el;
        };

        var result = new List<UpgradeOption>();

        var upgradesCopy = new List<UpgradeOption>(upgrades);
        if (upgradesCopy.Count > 0)
        {
            var pick = PopRandom(upgradesCopy);
            if (pick != null) result.Add(pick);
        }

        var pool = new List<UpgradeOption>();
        pool.AddRange(upgradesCopy);
        pool.AddRange(evolutions);
        pool.AddRange(newWeapons);
        pool.AddRange(weaponUpgrades);

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(0, i + 1);
            var tmp = pool[i];
            pool[i] = pool[j];
            pool[j] = tmp;
        }

        var usedIds = new HashSet<string>(result.Select(r => r.upgradeID));
        foreach (var o in pool)
        {
            if (result.Count >= count) break;
            if (usedIds.Contains(o.upgradeID)) continue;
            result.Add(o);
            usedIds.Add(o.upgradeID);
        }

        if (result.Count < count)
        {
            foreach (var o in distinctById)
            {
                if (result.Count >= count) break;
                if (usedIds.Contains(o.upgradeID)) continue;
                result.Add(o);
                usedIds.Add(o.upgradeID);
            }
        }

        return result.Take(count).ToList();
    }

    private void ShowChoices(string header, List<UpgradeOption> options)
    {
        if (slotManager != null && slotManager.IsFull())
        {
            options = options.Where(o => o.Type != UpgradeType.NewWeapon).ToList();
        }

        if (EventSystem.current == null)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        headerText.text = header;
        panel.SetActive(true);

        if (buttons != null)
        {
            foreach (var button in buttons)
            {
                button.ResetButton();
                button.gameObject.SetActive(false);
            }

            for (int i = 0; i < buttons.Length; i++)
            {
                if (i < options.Count)
                {
                    buttons[i].gameObject.SetActive(true);
                    buttons[i].Setup(options[i]);
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(buttons[i].transform as RectTransform);
                }
                else
                {
                    buttons[i].gameObject.SetActive(false);
                }
            }
        }

        if (slotManager == null)
        {
            slotManager = FindObjectOfType<UpgradeSlotManager>();
            if (slotManager == null)
            {
                Debug.LogError("UpgradeSlotManager not found in scene!");
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void SelectUpgrade(UpgradeOption opt)
    {
        if (opt == null) return;

        switch (opt.Type)
        {
            case UpgradeType.Upgrade:
                var upgData = allUpgrades.FirstOrDefault(u => u != null && u.name == opt.upgradeID);
                if (upgData != null)
                {
                    var behavior = upgData.CreateBehavior(combat != null ? combat.gameObject : GameObject.FindGameObjectWithTag("Player"));
                    if (behavior != null)
                    {
                        behavior.Initialize(combat != null ? combat.gameObject : GameObject.FindGameObjectWithTag("Player"), upgData);
                        behavior.OnUpgrade(opt.currentLevel);
                    }

                    if (activeUpgrades.ContainsKey(opt.upgradeID))
                    {
                        activeUpgrades[opt.upgradeID].currentLevel++;
                    }
                    else
                    {
                        opt.UpgradeData = upgData;
                        activeUpgrades.Add(opt.upgradeID, opt);
                        if (slotManager != null)
                        {
                            if (!slotManager.Contains(opt.upgradeID))
                                slotManager.AddUpgradeIcon(opt.upgradeID, opt.Icon);
                            else
                                slotManager.UpdateUpgradeIcon(opt.upgradeID, opt.Icon);
                        }
                    }
                }
                break;

            case UpgradeType.NewWeapon:
                combat?.EquipWeapon(opt.Weapon);

                bool success = true;
                if (slotManager != null)
                {
                    if (slotManager.Contains(opt.upgradeID))
                    {
                        slotManager.UpdateUpgradeIcon(opt.upgradeID, opt.Icon);
                        success = true;
                    }
                    else
                    {
                        success = slotManager.AddUpgradeIcon(opt.upgradeID, opt.Icon);
                    }
                }

                if (success)
                {
                    if (!activeUpgrades.ContainsKey(opt.upgradeID))
                        activeUpgrades.Add(opt.upgradeID, opt);
                }
                break;

            case UpgradeType.WeaponUpgrade:
                combat?.UpgradeWeapon(opt.Weapon);

                string baseId = opt.Weapon != null ? opt.Weapon.name : opt.upgradeID;

                if (slotManager != null)
                {
                    if (slotManager.Contains(baseId))
                    {
                        slotManager.UpdateUpgradeIcon(baseId, opt.Icon);
                    }
                    else
                    {
                        slotManager.AddUpgradeIcon(baseId, opt.Icon);
                    }
                }

                if (activeUpgrades.ContainsKey(baseId))
                {
                    activeUpgrades[baseId].currentLevel = opt.currentLevel;
                }
                else
                {
                    opt.upgradeID = baseId;
                    activeUpgrades[baseId] = opt;
                }
                break;

            case UpgradeType.Evolution:
                if (opt.SourceWeapon != null)
                {
                    TryCopyMissingObjectFields(opt.SourceWeapon, opt.Weapon);
                }

                combat?.ApplyEvolution(opt.Weapon, opt.SourceWeapon);

                if (opt.SourceWeapon != null)
                {
                    appliedEvolutions.Add(opt.SourceWeapon.name);
                    appliedEvolutions.Add(opt.Weapon.name);
                    string evolutionPairKey = $"{opt.SourceWeapon.name}_{opt.SourceUpgradeData.name}";
                    appliedEvolutionPairs.Add(evolutionPairKey);
                    
                    if (slotManager != null)
                    {
                        slotManager.ReplaceUpgradeId(opt.SourceWeapon.name, opt.Weapon.name, opt.Weapon.weaponIcon);
                    }
                    
                    activeUpgrades.Remove(opt.SourceWeapon.name);
                }
                break;
        }

        ClosePanel();

        if (pendingLevelQueue.Count > 0)
        {
            ShowNextPendingLevelChoice();
        }
    }

    private void TryCopyMissingObjectFields(WeaponBase from, WeaponBase to)
    {
        if (from == null || to == null) return;
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var fromType = from.GetType();
        var toType = to.GetType();
        var fromFields = fromType.GetFields(flags);
        foreach (var ff in fromFields)
        {
            if (!typeof(UnityEngine.Object).IsAssignableFrom(ff.FieldType)) continue;
            var fv = ff.GetValue(from) as UnityEngine.Object;
            if (fv == null) continue;
            var tf = toType.GetField(ff.Name, flags);
            if (tf == null) continue;
            var tv = tf.GetValue(to) as UnityEngine.Object;
            if (tv == null)
            {
                try { tf.SetValue(to, fv); }
                catch { }
            }
        }
    }

    private void ClosePanel()
    {
        panel?.SetActive(false);
        IsChoosingUpgrade = false;
        GameStateManager.Instance?.SetGameState(false, false);
    }

    public List<string> GetActiveUpgradeTags()
    {
        var allTags = new List<string>();
        foreach (var kv in activeUpgrades.Values)
        {
            if (kv == null) continue;
            if (kv.tags != null && kv.tags.Length > 0)
                allTags.AddRange(kv.tags);
        }
        return allTags.Distinct().ToList();
    }
}