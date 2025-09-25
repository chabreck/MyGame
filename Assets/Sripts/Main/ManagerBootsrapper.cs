using UnityEngine;

[DisallowMultipleComponent]
public class ManagerBootstrapper : MonoBehaviour
{
    public GameObject[] prefabsToPreload;

    private void Awake()
    {
        if (prefabsToPreload == null || prefabsToPreload.Length == 0) return;

        for (int i = 0; i < prefabsToPreload.Length; i++)
        {
            GameObject prefab = prefabsToPreload[i];
            if (prefab == null) continue;

            if (GameObject.Find(prefab.name) != null) continue;

            var inst = Instantiate(prefab);
            inst.name = prefab.name;
            UnityEngine.Object.DontDestroyOnLoad(inst);
        }
    }
}