using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private BillboardType billboardType;
    private Vector3 originalRotation;
    private Camera cam;
    public bool lockX;
    public bool lockY;
    public bool lockZ;
    public enum BillboardType { LookAtCamera, CameraForward };

    private void Awake()
    {
        originalRotation = transform.rotation.eulerAngles;
    }

    private void LateUpdate()
    {
        // Si todavía no hay cámara (o se desactivó), la busca de nuevo y espera al próximo frame
        if (cam == null || !cam.isActiveAndEnabled)
        {
            cam = Camera.main;
            if (cam == null) {
                Debug.LogWarning("[Billboard] No hay ninguna cámara activa con el tag MainCamera.", this);

                return; 
            }
        }

        switch (billboardType)
        {
            case BillboardType.LookAtCamera:
                transform.LookAt(cam.transform.position, Vector3.up);
                break;
            case BillboardType.CameraForward:
                transform.forward = cam.transform.forward;
                break;
            default:
                break;
        }
        Vector3 e = transform.rotation.eulerAngles;
        if (lockX) e.x = originalRotation.x;
        if (lockY) e.y = originalRotation.y;
        if (lockZ) e.z = originalRotation.z;
        transform.rotation = Quaternion.Euler(e);
    }
}