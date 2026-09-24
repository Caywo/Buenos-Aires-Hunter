using UnityEngine;

public class PlayerMirar : MonoBehaviour
{
    public Camera cam;
    private float rotacionX = 0f;
    public float sensibilidadX = 5f;
    public float sensibilidadY = 5f;

    [Header("Recoil")]
    public float recoilReturnSpeed = 6f;
    private float recoilX = 0f;

    public void ProcessMirar(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;

        rotacionX -= (mouseY * Time.deltaTime) * sensibilidadY;
        rotacionX = Mathf.Clamp(rotacionX, -80f, 80f);

        // El recoil vuelve suavemente a 0
        recoilX = Mathf.Lerp(recoilX, 0f, Time.deltaTime * recoilReturnSpeed);

        // Rotación final = input del jugador + offset de recoil
        cam.transform.localRotation = Quaternion.Euler(rotacionX + recoilX, 0, 0);

        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * sensibilidadX);
    }

    void Awake()
    {
        if (cam == null)
            cam = GetComponentInChildren<Camera>();
    }

    // Método público para que WeaponShoot pida recoil
    public void AddRecoil(float amount)
    {
        recoilX -= amount;
    }
}