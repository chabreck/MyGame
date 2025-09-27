using UnityEngine;

public class ExperienceTester : MonoBehaviour
{
    [SerializeField] private HeroExperience heroExperience;
    [SerializeField] private int expAmount = 100;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (heroExperience != null)
            {
                heroExperience.AddExp(expAmount);
                Debug.Log($"Added {expAmount} EXP for testing!");
            }
            else
            {
                Debug.LogWarning("HeroExperience not assigned in Inspector!");
            }
        }
    }
}