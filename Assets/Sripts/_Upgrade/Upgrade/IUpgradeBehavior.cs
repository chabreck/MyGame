using UnityEngine;

public interface IUpgradeBehavior
{
    // Вызывается один раз при создании/привязке (перед OnUpgrade)
    void Initialize(GameObject owner, UpgradeBase data);

    // Вызывается при применении уровня (включая первый раз)
    void OnUpgrade(int level);
    void Activate();
}