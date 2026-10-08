using UnityEngine;

/// <summary>
/// Datos del arma (los lee el servidor) + feedback visual/sonoro (retroceso, corredera, ADS).
/// Ya NO dispara ni gestiona munición: eso lo hace PlayerCombat con validación del servidor.
/// </summary>
public class WeaponShoot : MonoBehaviour
{
    [Header("Referencias")]
    public Transform slide;
    public Transform weaponHolder;
    public PlayerLook mouseLook;
    public Camera playerCamera;

    [Header("Slide (corredera)")]
    public Vector3 slideRecoilOffset = new Vector3(0, 0, -0.03f);
    public float slideReturnSpeed = 10f;

    [Header("Recoil de arma (kick)")]
    public float weaponKickBack = 0.05f;
    public float weaponReturnSpeed = 8f;

    [Header("Recoil de cámara")]
    public float cameraRecoilAmount = 2f;

    [Header("Disparo (los valida el servidor)")]
    public bool automatico = true;
    public float fireRate = 0.15f;
    public float range = 100f;
    public float damage = 20f;
    public LayerMask hitMask = ~0;

    [Header("Munición (valores iniciales)")]
    public int magazineSize = 30;
    public int reserveAmmo = 90;

    [Header("Recarga")]
    public float reloadTime = 1.8f;

    [Header("Apuntado (ADS)")]
    public Transform aimPoint;
    public float aimSpeed = 10f;
    public float aimFOV = 40f;

    [Header("Sonido")]
    public AudioClip shootSound;
    public AudioClip reloadSound;

    private float defaultFOV;
    private bool isAiming = false;
    private bool esLocal;
    private Vector3 hipFirePosition;
    private Vector3 slideInitialPos;
    private Vector3 weaponInitialPos;
    private AudioSource audioSource;

    public void Init(Transform weaponHolder, PlayerLook mouseLook, Camera playerCamera, bool esLocal)
    {
        this.weaponHolder = weaponHolder;
        this.mouseLook = mouseLook;
        this.playerCamera = playerCamera;
        this.esLocal = esLocal;

        if (playerCamera != null)
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
        // Tus disparos suenan 2D; los de otros jugadores suenan en 3D
        audioSource.spatialBlend = esLocal ? 0f : 1f;
    }

    void Update()
    {
        AnimateSlide();
        AnimateWeaponKick();
        if (esLocal) AnimateAim();
    }

    /// <summary>Feedback de un disparo. conRecoilDeCamara solo para el dueño.</summary>
    public void PlayShootEffects(bool conRecoilDeCamara)
    {
        if (slide != null)
            slide.localPosition = slideInitialPos + slideRecoilOffset;

        if (weaponHolder != null)
            weaponHolder.localPosition = weaponInitialPos + new Vector3(0, 0, -weaponKickBack);

        if (conRecoilDeCamara && mouseLook != null)
            mouseLook.AddRecoil(cameraRecoilAmount);

        if (shootSound != null && audioSource != null)
            audioSource.PlayOneShot(shootSound);
    }

    public void PlayReloadEffects()
    {
        if (reloadSound != null && audioSource != null)
            audioSource.PlayOneShot(reloadSound);
        // TODO: disparar la animación de recarga aquí
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
        if (weaponHolder == null || aimPoint == null || playerCamera == null) return;

        Vector3 targetPos = isAiming ? aimPoint.localPosition : hipFirePosition;
        weaponInitialPos = Vector3.Lerp(weaponInitialPos, targetPos, Time.deltaTime * aimSpeed);

        float targetFOV = isAiming ? aimFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * aimSpeed);
    }
}