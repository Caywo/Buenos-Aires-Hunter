using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool isGrounded;

    public float speed = 20f;
    public float gravity = -20f;
    public float jumpHeight = 3f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public void ProcessMove(Vector2 input)
    {
        isGrounded = controller.isGrounded;

        Vector3 direccionMovimiento = Vector3.zero;
        direccionMovimiento.x = input.x;
        direccionMovimiento.z = input.y;

        controller.Move(
            transform.TransformDirection(direccionMovimiento)
            * speed
            * Time.deltaTime
        );

        if (isGrounded && playerVelocity.y < 0)
            playerVelocity.y = -2f;

        playerVelocity.y += gravity * Time.deltaTime;

        controller.Move(playerVelocity * Time.deltaTime);
    }

    public void Saltar()
    {
        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}