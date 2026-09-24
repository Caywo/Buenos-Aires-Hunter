using UnityEngine;

public class PlayerMirar : MonoBehaviour
{
    public Camera cam;
    private float rotacionX = 0f;
    public float sensibilidadX = 5f;
    public float sensibilidadY = 5f;

    public void ProcessMirar(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;

        rotacionX -= (mouseY * Time.deltaTime) * sensibilidadY;
        rotacionX = Mathf.Clamp(rotacionX, -80f, 80f);
        cam.transform.localRotation = Quaternion.Euler(rotacionX, 0, 0);
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * sensibilidadX);
    }

    void Awake()
    {
        if (cam == null)
            cam = GetComponentInChildren<Camera>();
    }
}
