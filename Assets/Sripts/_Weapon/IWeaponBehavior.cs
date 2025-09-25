using UnityEngine;

public interface IWeaponBehavior
{
    // Вызывается при создании поведения (перед использованием)
    void Initialize(GameObject owner, WeaponBase data, HeroModifierSystem mods, HeroCombat combat);

    // Вызывается каждый кадр/внутри HeroCombat для обновления работы оружия
    void Activate();

    // Применяется при повышении уровня оружия
    void OnUpgrade(int level);
}