using UnityEngine;

public interface IUpgrade
{
    string GetUpgradeID();
    string GetTitle(int level);
    string GetDescription(int level);
    Sprite Icon { get; }
    string[] Tags { get; }
    int MaxLevel { get; }
    void Apply(int level);
}