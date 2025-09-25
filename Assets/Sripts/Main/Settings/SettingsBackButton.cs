using UnityEngine;

public class SettingsBackButton : MonoBehaviour
{
    public void OnBack()
    {
        if (SettingsPanelManager.Instance != null)
        {
            SettingsPanelManager.Instance.Hide();
        }
        else
        {
            Debug.LogWarning("[SettingsBackButton] OnBack: SettingsPanelManager.Instance is null");
            SettingsPanelManager.EnsureInstanceExists();
            SettingsPanelManager.Instance?.Hide();
        }
    }
}