using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PruebaInicialQA
{
    // Verifica que Unity Test Framework se encuentre operativo.
    [UnityTest]
    public IEnumerator PruebaFrameworkFunciona()
    {
        yield return null;

        Assert.Pass();
    }
    // Verifica que la escena MenuPrincipal cargue correctamente.
    [UnityTest]
    public IEnumerator CP_UI_01_CargaMenuPrincipal()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal");

        Assert.AreEqual(
        "MenuPrincipal",
        SceneManager.GetActiveScene().name
        );

    }
    // Verifica la existencia del Canvas principal del menú
    [UnityTest]
    public IEnumerator CP_UI_01_ExisteCanvas()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal");

        GameObject canvas = GameObject.Find("Canvas");

        Assert.IsNotNull(
        canvas,
        "No se encontró el Canvas principal."
        );
    }
    // Verifica la existencia del panel CanvasMenuprincipal.
    [UnityTest]
    public IEnumerator CP_UI_01_ExisteCanvasMenuPrincipal()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal");

        GameObject panel =
        GameObject.Find("CanvasMenuprincipal");

        Assert.IsNotNull(
        panel,
        "No se encontró CanvasMenuprincipal."
        );
    }
    // Verifica la existencia del botón de inicio de juego.
    [UnityTest]
    public IEnumerator CP_UI_01_ExisteBotonJugar()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal");

        GameObject boton =
            GameObject.Find("BotonJugar");

        Assert.IsNotNull(
            boton,
            "No se encontró BotonJugar."
        );
    }
    // Verifica que el componente PlayerMotor pueda instanciarse correctamente.
    [Test]
    public void CP_MOV_01_PlayerMotorDebeSerAccesible()
    {
        GameObject jugador = new GameObject();

        PlayerMotor motor =
            jugador.AddComponent<PlayerMotor>();

        Assert.IsNotNull(motor);
    }
    // Verifica que la velocidad de movimiento esté configurada correctamente.
    [Test]
    public void CP_MOV_01_VelocidadDebeSerMayorQueCero()
    {
        GameObject jugador = new GameObject();

        PlayerMotor motor =
            jugador.AddComponent<PlayerMotor>();

        Assert.Greater(
            motor.speed,
            0,
            "La velocidad de movimiento debe ser mayor que cero."
        );
    }
    // Verifica que la gravedad posea un valor válido para el movimiento del jugador.
    [Test]
    public void CP_MOV_01_GravedadDebeSerNegativa()
    {
        GameObject jugador = new GameObject();

        PlayerMotor motor =
            jugador.AddComponent<PlayerMotor>();

        Assert.Less(
            motor.gravity,
            0,
            "La gravedad debe ser un valor negativo."
        );
    }
    // Verifica que la altura de salto esté configurada correctamente.
    [Test]
    public void CP_MOV_02_AlturaSaltoDebeSerMayorQueCero()
    {
        GameObject jugador = new GameObject();

        PlayerMotor motor =
            jugador.AddComponent<PlayerMotor>();

        Assert.Greater(
            motor.jumpHeight,
            0,
            "La altura de salto debe ser mayor que cero."
        );
    }
    // Verifica que el jugador disponga de CharacterController para gestionar movimiento y colisiones.
    [Test]
    public void CP_MOV_01_PlayerMotorDebeTenerCharacterController()
    {
        GameObject jugador = new GameObject();

        CharacterController controller =
            jugador.AddComponent<CharacterController>();

        PlayerMotor motor =
            jugador.AddComponent<PlayerMotor>();

        Assert.IsNotNull(controller);
        Assert.IsNotNull(motor);
    }
    // Verifica que el método ProcessMove pueda ejecutarse sin errores.
    [UnityTest]
    public IEnumerator CP_MOV_01_ProcessMoveNoDebeGenerarExcepcion()
    {
        GameObject jugador = new GameObject();

        jugador.AddComponent<CharacterController>();

        PlayerMotor motor =
            jugador.AddComponent<PlayerMotor>();

        yield return null;

        motor.ProcessMove(new Vector2(1, 0));

        Assert.Pass();
    }
    // Verifica que el método Saltar pueda ejecutarse correctamente.
    [Test]
    public void CP_MOV_02_MetodoSaltarExiste()
    {
        GameObject jugador = new GameObject();

        jugador.AddComponent<CharacterController>();

        PlayerMotor motor =
            jugador.AddComponent<PlayerMotor>();

        Assert.DoesNotThrow(() =>
        {
            motor.Saltar();
        });
    }
    // Verifica que el componente PlayerLook pueda instanciarse correctamente.
    [Test]
    public void CP_CAM_01_PlayerLookDebeSerAccesible()
    {
        GameObject jugador = new GameObject();

        PlayerLook look =
            jugador.AddComponent<PlayerLook>();

        Assert.IsNotNull(look);
    }
    // Verifica que la sensibilidad horizontal tenga un valor válido.
    [Test]
    public void CP_CAM_01_SensibilidadXDebeSerMayorQueCero()
    {
        GameObject jugador = new GameObject();

        PlayerLook look =
            jugador.AddComponent<PlayerLook>();

        Assert.Greater(
            look.sensibilidadX,
            0
        );
    }
    // Verifica que la sensibilidad vertical tenga un valor válido.
    [Test]
    public void CP_CAM_01_SensibilidadYDebeSerMayorQueCero()
    {
        GameObject jugador = new GameObject();

        PlayerLook look =
            jugador.AddComponent<PlayerLook>();

        Assert.Greater(
            look.sensibilidadY,
            0
        );
    }
    // Verifica que la velocidad de recuperación del recoil sea válida.
    [Test]
    public void CP_CAM_01_RecoilReturnSpeedDebeSerMayorQueCero()
    {
        GameObject jugador = new GameObject();

        PlayerLook look =
            jugador.AddComponent<PlayerLook>();

        Assert.Greater(
            look.recoilReturnSpeed,
            0
        );
    }
    // Verifica que el procesamiento de movimiento de cámara se ejecute sin errores.
    [UnityTest]
    public IEnumerator CP_CAM_01_ProcessMirarNoDebeGenerarExcepcion()
    {
        GameObject jugador = new GameObject();

        Camera cam =
            new GameObject().AddComponent<Camera>();

        PlayerLook look =
            jugador.AddComponent<PlayerLook>();

        look.cam = cam;

        yield return null;

        Assert.DoesNotThrow(() =>
        {
            look.ProcessMirar(
                new Vector2(1, 1)
            );
        });
    }
    // Verifica que el componente WeaponShoot pueda instanciarse correctamente.
    [Test]
    public void CP_COMB_01_WeaponShootDebeSerAccesible()
    {
        GameObject arma = new GameObject();

        WeaponShoot weapon =
            arma.AddComponent<WeaponShoot>();

        Assert.IsNotNull(weapon);
    }
    // Verifica que la cadencia de disparo tenga un valor válido.
    [Test]
    public void CP_COMB_01_FireRateDebeSerMayorQueCero()
    {
        GameObject arma = new GameObject();

        WeaponShoot weapon =
            arma.AddComponent<WeaponShoot>();

        Assert.Greater(
            weapon.fireRate,
            0
        );
    }
    // Verifica que el alcance de disparo esté configurado correctamente.
    [Test]
    public void CP_COMB_01_RangoDebeSerMayorQueCero()
    {
        GameObject arma = new GameObject();

        WeaponShoot weapon =
            arma.AddComponent<WeaponShoot>();

        Assert.Greater(
            weapon.range,
            0
        );
    }
    // Verifica que el daño del arma posea un valor válido.
    [Test]
    public void CP_COMB_01_DanioDebeSerMayorQueCero()
    {
        GameObject arma = new GameObject();

        WeaponShoot weapon =
            arma.AddComponent<WeaponShoot>();

        Assert.Greater(
            weapon.damage,
            0
        );
    }
    // Verifica que la velocidad de apuntado tenga un valor válido.
    [Test]
    public void CP_COMB_02_AimSpeedDebeSerMayorQueCero()
    {
        GameObject arma = new GameObject();

        WeaponShoot weapon =
            arma.AddComponent<WeaponShoot>();

        Assert.Greater(
            weapon.aimSpeed,
            0
        );
    }
    // Verifica que el campo de visión al apuntar tenga un valor válido.
    [Test]
    public void CP_COMB_02_AimFOVDebeSerMayorQueCero()
    {
        GameObject arma = new GameObject();

        WeaponShoot weapon =
            arma.AddComponent<WeaponShoot>();

        Assert.Greater(
            weapon.aimFOV,
            0
        );
    }
    // Verifica que el modo de apuntado pueda activarse sin errores.
    [Test]
    public void CP_COMB_02_SetAimingNoDebeGenerarExcepcion()
    {
        GameObject arma = new GameObject();

        WeaponShoot weapon =
            arma.AddComponent<WeaponShoot>();

        Assert.DoesNotThrow(() =>
        {
            weapon.SetAiming(true);
        });
    }
    // Verifica la existencia de la pared invisible en el escenario.
    [UnityTest]
    public IEnumerator CP_FIS_01_ParedInvisibleDebeExistir()
    {
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

        GameObject pared =
            GameObject.Find("ParedInvisible");

        Assert.IsNotNull(
            pared,
            "No se encontró ParedInvisible."
        );
    }
    // Verifica que la pared invisible disponga de un componente Collider.
    [UnityTest]
    public IEnumerator CP_FIS_01_ParedInvisibleDebeTenerCollider()
    {
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

        GameObject pared =
            GameObject.Find("ParedInvisible");

        Assert.IsNotNull(pared);

        Collider collider =
            pared.GetComponent<Collider>();

        Assert.IsNotNull(
            collider,
            "ParedInvisible no posee Collider."
        );
    }
    // Verifica que la pared invisible funcione como colisión física y no como Trigger.
    [UnityTest]
    public IEnumerator CP_FIS_01_ParedInvisibleNoDebeSerTrigger()
    {
        yield return SceneManager.LoadSceneAsync("MapaExtraccion1");

        GameObject pared =
            GameObject.Find("ParedInvisible");

        Assert.IsNotNull(pared);

        BoxCollider collider =
            pared.GetComponent<BoxCollider>();

        Assert.IsNotNull(collider);

        Assert.IsFalse(
            collider.isTrigger,
            "La pared invisible no debería ser trigger."
        );
    }



}
