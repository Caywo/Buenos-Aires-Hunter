SILENT TREES - LOW POLY PACK
Documentation & Quick Start Guide

Thank you for downloading Silent Trees - Low Poly Pack!

========================================================================

1. PACKAGE CONTENT
------------------
* Prefabs: 8 ready-to-use prefabs
    - 3 Deciduous (Tree_Deciduous_01, _02, _03)
    - 3 Deciduous Autumn variants (Tree_Deciduous_autumn_01, _02, _03)
    - 2 Coniferous (Tree_Coniferous_01, _02)
* Models: 8 unique low-poly .fbx files
    - 5 base tree models (3 Deciduous + 2 Coniferous)
    - 3 Deciduous Autumn variants (evergreen trees intentionally have no autumn variant)
* Textures: 4 PNG textures (256x256) for foliage
    - crown_g  - Deciduous (summer / green)
    - crown_y  - Deciduous (autumn / yellow)
    - crown_dg - Coniferous (dark green)
    - crown_b  - Coniferous (blue spruce)
* Shaders: 1 custom URP shader for vertex color trunk rendering
    - Shader/VertexColorLitSimple.shader
* Demo scene: Demo_Scenes/Demo.unity showcases all 8 prefab variants.


2. QUICK SETUP (UNIVERSAL RENDER PIPELINE)
------------------------------------------
1. Import the Package: Drag and drop the .unitypackage into your Unity project
   or import it via the Asset Store Manager.
2. Render Pipeline: Make sure your project uses the Universal Render Pipeline (URP).
3. Add to Scene: Drag any prefab from the Prefabs folder directly into your scene
   or terrain.


3. CUSTOM VERTEX COLOR SHADER
-----------------------------
The tree trunks rely on vertex colors for shading. Standard URP/Lit shaders may
not display the trunk colors correctly when applied to the trunk mesh.

* The included shader is located at: Shader/VertexColorLitSimple.shader
  (Shader name in the Inspector: "SilentTrees/VertexColorLitSimple")
* The trunk material is already configured to use this shader.
* If your tree trunks appear black or untextured, reassign the trunk material
  to the SilentTrees/VertexColorLitSimple shader.


4. COLLIDERS & PHYSICS
----------------------
* All prefabs come pre-configured with a basic Capsule Collider aligned to the
  trunk for optimal performance.
* Foliage and upper branches intentionally do not have colliders to avoid
  collision glitches with cameras, projectiles, and airborne objects.


SUPPORT & CONTACT
-----------------
If you have any questions, feedback, or issues regarding this asset, feel free
to contact us:
* Publisher: V&E Games
* Email: berkana.jara@gmail.com