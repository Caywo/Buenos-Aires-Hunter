using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FpsCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text texto;
    [SerializeField] private float intervalo = 0.5f;
    [SerializeField] private Key teclaAlternar = Key.F3;

    private float acumulado;
    private int frames;
    private bool visible = true;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[teclaAlternar].wasPressedThisFrame)
        {
            visible = !visible;
            texto.enabled = visible;
        }

        // unscaledDeltaTime: sigue contando bien aunque Time.timeScale sea 0 (pausa, pantalla de victoria)
        acumulado += Time.unscaledDeltaTime;
        frames++;

        if (acumulado >= intervalo)
        {
            if (visible)
            {
                float fps = frames / acumulado;
                float ms = acumulado / frames * 1000f;
                texto.text = $"{fps:F0} FPS ({ms:F1} ms)";
            }

            acumulado = 0f;
            frames = 0;
        }
    }
}