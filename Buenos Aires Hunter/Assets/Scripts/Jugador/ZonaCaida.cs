using UnityEngine;

public class ZonaCaida : MonoBehaviour
{
    public Transform spawnPoint;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var cc = other.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false; // necesario para poder teletransportar

        other.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        if (cc != null) cc.enabled = true;

        var rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // en Unity 6; en versiones viejas: rb.velocity
            rb.angularVelocity = Vector3.zero;
        }
    }
}