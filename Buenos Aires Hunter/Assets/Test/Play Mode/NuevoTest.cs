using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PruebaInicialQA
{
    [UnityTest]
    public IEnumerator PruebaFrameworkFunciona()
    {
        yield return null;

        Assert.Pass();
    }
    [UnityTest]
    public IEnumerator CP_UI_01_CargaMenuPrincipal()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal");

        Assert.AreEqual(
        "MenuPrincipal",
        SceneManager.GetActiveScene().name
        );

    }
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
    [Test]
    public void CP_MOV_01_PlayerMotorDebeSerAccesible()
    {
        GameObject jugador = new GameObject();

        PlayerMotor motor =
            jugador.AddComponent<PlayerMotor>();

        Assert.IsNotNull(motor);
    }
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


}
