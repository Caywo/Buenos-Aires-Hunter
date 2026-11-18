
using UnityEngine;
using UnityEditor;

public class ReemplazarMeshColliders
{
    [MenuItem("Tools/Optimizacion/Reemplazar MeshColliders seleccionados")]
    private static void Reemplazar()
    {
        GameObject[] seleccionados = Selection.gameObjects;

        if (seleccionados.Length == 0)
        {
            Debug.LogWarning("No hay objetos seleccionados.");
            return;
        }

        int reemplazados = 0;

        foreach (GameObject objeto in seleccionados)
        {
            MeshCollider[] colliders =
                objeto.GetComponentsInChildren<MeshCollider>(true);

            foreach (MeshCollider meshCollider in colliders)
            {
                Mesh mesh = meshCollider.sharedMesh;

                if (mesh == null)
                    continue;

                GameObject propietario = meshCollider.gameObject;

                Undo.RegisterCompleteObjectUndo(
                    propietario,
                    "Reemplazar MeshCollider"
                );

                BoxCollider box =
                    Undo.AddComponent<BoxCollider>(propietario);

                box.sharedMaterial = meshCollider.sharedMaterial;
                box.isTrigger = meshCollider.isTrigger;
                box.enabled = meshCollider.enabled;

                Bounds bounds = mesh.bounds;

                box.center = bounds.center;
                box.size = bounds.size;

                Undo.DestroyObjectImmediate(meshCollider);

                reemplazados++;
            }
        }

        Debug.Log($"Se reemplazaron {reemplazados} MeshColliders.");
    }
}
