using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput), typeof(HeroMovement))]
public class HeroInputSystem : MonoBehaviour
{
    private PlayerInput playerInput;
    private HeroMovement heroMovement;

    private InputAction moveAction;
    private InputAction dashAction;
    
    public Vector2 MoveInput { get; private set; }
    public bool DashInput { get; private set; }

    private void Awake()
    {
        // Получаем нужные компоненты
        playerInput  = GetComponent<PlayerInput>();
        heroMovement = GetComponent<HeroMovement>();

        if (playerInput == null)
        {
            Debug.LogError("HeroInputSystem: нет PlayerInput на объекте");
            enabled = false;
            return;
        }

        if (playerInput.actions == null)
        {
            Debug.LogError("HeroInputSystem: в PlayerInput не назначен Input Actions Asset");
            enabled = false;
            return;
        }

        // Ищем экшены по имени
        moveAction = playerInput.actions.FindAction("Move", false);
        dashAction = playerInput.actions.FindAction("Dash", false);
        if (moveAction == null || dashAction == null)
        {
            Debug.LogError($"HeroInputSystem: не найдены действия! Move? {moveAction!=null}, Dash? {dashAction!=null}");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        moveAction.Enable();
        dashAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        dashAction.Disable();
    }

    private void Update()
    {
        // Читать ввод
        MoveInput = moveAction.ReadValue<Vector2>().normalized;
        DashInput = dashAction.WasPressedThisFrame();

        // Отправлять в HeroMovement
        heroMovement.SetMovementInput(MoveInput);
        if (DashInput)
            heroMovement.TryDash();
    }
}