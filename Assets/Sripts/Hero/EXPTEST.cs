using UnityEngine;

public class ExperienceTester : MonoBehaviour
{
    [SerializeField] private HeroExperience heroExperience;
    [SerializeField] private int expAmount = 100; // сколько опыта добавлять за нажатие

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X)) // меняй кнопку если нужно
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