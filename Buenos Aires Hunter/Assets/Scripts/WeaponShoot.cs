using UnityEngine;

public class WeaponShoot : MonoBehaviour
{
    [Header("Referencias")]
    public Transform slide;
    public Transform weaponHolder;
    public PlayerMirar mouseLook;
    public Camera playerCamera;

    [Header("Slide (corredera)")]
    public Vector3 slideRecoilOffset = new Vector3(0, 0, -0.03f);
    public float slideReturnSpeed = 10f;

    [Header("Recoil de arma (kick)")]
    public float weaponKickBack = 0.05f;
    public float weaponReturnSpeed = 8f;

    [Header("Recoil de cámara")]
    public float cameraRecoilAmount = 2f;

    [Header("Disparo")]
    public float fireRate = 0.15f;
    public float range = 100f;
    public float damage = 20f;
    public LayerMask hitMask = ~0;

    [Header("Apuntado (ADS)")]
    public Transform aimPoint;
    public float aimSpeed = 10f;
    public float aimFOV = 40f;
    private float defaultFOV;
    private bool isAiming = false;
    private Vector3 hipFirePosition;

    [Header("Sonido")]
    public AudioClip shootSound;
    private AudioSource audioSource;

    private Vector3 slideInitialPos;
    private Vector3 weaponInitialPos;
    private float nextFireTime = 0f;

    public void Init(Transform weaponHolder, PlayerMirar mouseLook, Camera playerCamera)
    {
        this.weaponHolder = weaponHolder;
        this.mouseLook = mouseLook;
        this.playerCamera = playerCamera;

        defaultFOV = playerCamera.fieldOfView;
    }

    void Start()
    {
        if (slide != null) slideInitialPos = slide.localPosition;
        if (weaponHolder != null)
        {
            weaponInitialPos = weaponHolder.localPosition;
            hipFirePosition = weaponInitialPos;
        }


        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        AnimateSlide();
        AnimateWeaponKick();
        AnimateAim();
    }


    public void Disparar()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireRate;
        Shoot();
    }

    void Shoot()
    {
    if (slide != null)
        slide.localPosition = slideInitialPos + slideRecoilOffset;

    if (weaponHolder != null)
        weaponHolder.localPosition = weaponInitialPos + new Vector3(0, 0, -weaponKickBack);

    if (mouseLook != null)
        mouseLook.AddRecoil(cameraRecoilAmount);

    if (shootSound != null)
        audioSource.PlayOneShot(shootSound);

    if (playerCamera == null)
    {
        Debug.LogWarning("WeaponShoot: falta asignar Player Camera para el raycast.");
        return;
    }

    Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
    RaycastHit hit;

    if (Physics.Raycast(ray, out hit, range, hitMask))
    {
        Debug.Log($"Impacto en: {hit.collider.name}");

        // Sistema de daño pendiente de incorporar más adelante
    }
}

    void AnimateSlide()
    {
        if (slide == null) return;
        slide.localPosition = Vector3.Lerp(slide.localPosition, slideInitialPos, Time.deltaTime * slideReturnSpeed);
    }

    void AnimateWeaponKick()
    {
        if (weaponHolder == null) return;
        weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, weaponInitialPos, Time.deltaTime * weaponReturnSpeed);
    }

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    void AnimateAim()
    {
        if (weaponHolder == null || aimPoint == null || playerCamera == null)
        {
            Debug.LogWarning($"AnimateAim cortado. weaponHolder null: {weaponHolder == null}, aimPoint null: {aimPoint == null}, playerCamera null: {playerCamera == null}");
            return;
        }

        Vector3 targetPos = isAiming ? aimPoint.localPosition : hipFirePosition;
        weaponInitialPos = Vector3.Lerp(weaponInitialPos, targetPos, Time.deltaTime * aimSpeed);

        float targetFOV = isAiming ? aimFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * aimSpeed);
    }

}