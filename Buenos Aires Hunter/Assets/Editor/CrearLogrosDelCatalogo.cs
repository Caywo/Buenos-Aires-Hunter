#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramienta del editor: crea los assets LogroData del catálogo en Assets/Logros.
/// Menú: Logros > Crear logros del catálogo.
/// No pisa nada: si ya existe un LogroData con el mismo id, lo saltea.
/// IMPORTANTE: este archivo tiene que estar dentro de una carpeta llamada "Editor" (ej: Assets/Editor).
/// </summary>
public static class CrearLogrosDelCatalogo
{
    private struct Definicion
    {
        public string id;
        public string titulo;
        public string descripcion;
        public bool secreto;

        public Definicion(string id, string titulo, string descripcion, bool secreto = false)
        {
            this.id = id;
            this.titulo = titulo;
            this.descripcion = descripcion;
            this.secreto = secreto;
        }
    }

    // Los ids tienen que coincidir con IdsLogros (LogrosJugador.cs)
    private static readonly Definicion[] Catalogo =
    {
        // Combate
        new Definicion(IdsLogros.PrimeraBaja,        "Se armó el quilombo",          "Eliminá a tu primer enemigo."),
        new Definicion(IdsLogros.Bajas100,           "Cien por ciento picante",      "Eliminá a 100 enemigos entre todas tus partidas."),
        new Definicion(IdsLogros.BajasPuno5,         "Ahorrando munición",           "Eliminá a 5 enemigos a puñetazos."),
        new Definicion(IdsLogros.BajaEnElAire,       "Salto de fe",                  "Eliminá a un enemigo mientras estás en el aire.", true),
        new Definicion(IdsLogros.Racha3,             "Lluvia de plomo",              "Eliminá a 3 enemigos en menos de 3 segundos."),

        // Extracción
        new Definicion(IdsLogros.PrimeraExtraccion,  "Me voy a tomar el colectivo",  "Extraé con éxito por primera vez."),
        new Definicion(IdsLogros.ExtraccionAlLimite, "El último bondi a fin de mes", "Extraé con menos del 10% de vida."),
        new Definicion(IdsLogros.ExtraccionEnRojo,   "Con la SUBE en rojo",          "Extraé quedándote con saldo negativo."),

        // Supervivencia
        new Definicion(IdsLogros.SupervRonda5,       "Un domingo en la plaza",       "Llegá a la ronda 5 en Supervivencia."),
        new Definicion(IdsLogros.SupervRonda15,      "Aguantando los trapos",        "Llegá a la ronda 15 en Supervivencia."),
        new Definicion(IdsLogros.SupervSinDano,      "No me quemo con nada",         "Pasá una ronda de Supervivencia sin recibir daño."),

        // Tienda
        new Definicion(IdsLogros.TiendaPrimera,      "Cliente VIP",                  "Hacé tu primera compra en la tienda."),
        new Definicion(IdsLogros.TiendaGasto1000,    "Fiado nunca",                  "Gastá $1000 en total en la tienda."),

        // Duelo
        new Definicion(IdsLogros.DueloPrimero,       "A cara de perro",              "Ganá tu primer duelo."),
        new Definicion(IdsLogros.DueloRondaPerfecta, "Ni para el pasaje",            "Ganá una ronda de duelo sin recibir daño."),
        new Definicion(IdsLogros.DueloBarrida,       "Pasó la aplanadora",           "Ganá un duelo sin perder ninguna ronda."),
    };

    [MenuItem("Logros/Crear logros del catálogo")]
    public static void Crear()
    {
        const string carpeta = "Assets/Logros";
        if (!AssetDatabase.IsValidFolder(carpeta))
            AssetDatabase.CreateFolder("Assets", "Logros");

        // ids que ya existen, para no duplicar
        var existentes = new HashSet<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:LogroData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<LogroData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null) existentes.Add(data.id);
        }

        int creados = 0;
        foreach (Definicion d in Catalogo)
        {
            if (existentes.Contains(d.id)) continue;

            var asset = ScriptableObject.CreateInstance<LogroData>();
            asset.id = d.id;
            asset.titulo = d.titulo;
            asset.descripcion = d.descripcion;
            asset.secreto = d.secreto;

            AssetDatabase.CreateAsset(asset, carpeta + "/" + d.id + ".asset");
            creados++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Logros] Se crearon {creados} logros nuevos en {carpeta} " +
                  $"({Catalogo.Length - creados} ya existían). Ahora arrastralos a la lista 'Logros' del componente Logros UI.");
    }
}
#endif
