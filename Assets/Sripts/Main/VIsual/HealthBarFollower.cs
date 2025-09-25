using UnityEngine;

[DisallowMultipleComponent]
public class HealthBarWorldFollow : MonoBehaviour
{
    [Tooltip("Target player transform (drag player here). If null, will try to find Tag \"Player\"")]
    public Transform target;

    [Tooltip("Should the bar face the camera?")]
    public bool faceCamera = true;

    Camera cam;
    Vector3 worldOffset;

    void Start()
    {
        if (target == null)
        {
            var tgo = GameObject.FindGameObjectWithTag("Player");
            if (tgo != null) target = tgo.transform;
        }

        cam = Camera.main;

        // запоминаем мировое смещение (то место, где ты поставил полосу в редакторе)
        if (target != null)
            worldOffset = transform.position - target.position;
        else
            worldOffset = Vector3.zero;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // жёстко позиционируем полосу в мировых координатах так, как было выставлено в редакторе
        transform.position = target.position + worldOffset;

        if (faceCamera)
        {
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                // повернуть чтобы лицевая сторона смотрела на камеру
                transform.LookAt(cam.transform);
                transform.Rotate(0f, 180f, 0f); // тк LookAt смотрит лицом "в" камеру, переворачиваем
            }
        }
    }

    // можно вызвать, если игрок создаётся позже
    public void SetTarget(Transform t)
    {
        target = t;
        if (target != null) worldOffset = transform.position - target.position;
    }
}