using UnityEditor;

public class ModelPost : AssetPostprocessor
{
    public override uint GetVersion() { return 2; }
    void OnPreprocessModel()
    {
        var mi = (ModelImporter)assetImporter; bool ch = assetPath.Contains("/Chars/");
        mi.importCameras = false; mi.importLights = false; mi.bakeAxisConversion = true;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        if (assetPath.Contains("/Pets/")) { mi.isReadable = true; mi.animationType = ModelImporterAnimationType.Legacy; mi.importAnimation = true; mi.globalScale = 1; return; }
        if (assetPath.Contains("/Soldier/")) { mi.animationType = ModelImporterAnimationType.Human; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; mi.importAnimation = false; mi.materialImportMode = ModelImporterMaterialImportMode.None; return; }
        if (assetPath.Contains("/Anim/")) { mi.animationType = ModelImporterAnimationType.Human; mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; mi.importAnimation = true; mi.materialImportMode = ModelImporterMaterialImportMode.None; return; }
        if (assetPath.Contains("/GunsR/")) { mi.animationType = ModelImporterAnimationType.None; mi.importAnimation = false; return; }
        if (ch) { mi.animationType = ModelImporterAnimationType.Legacy; mi.importAnimation = true; }
        else { mi.animationType = ModelImporterAnimationType.None; mi.importAnimation = false; }
    }
    void OnPreprocessAnimation()
    {
        if (!assetPath.Contains("/Anim/")) return;
        var mi = (ModelImporter)assetImporter; var cl = mi.defaultClipAnimations;
        foreach (var c in cl)
        {
            bool loop = c.name.Contains("Loop") || c.name.Contains("Aim_Neutral");
            c.loopTime = loop; c.loopPose = loop;
            c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
            c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
        }
        mi.clipAnimations = cl;
    }
    void OnPreprocessTexture()
    {
        var ti = (TextureImporter)assetImporter;
        if (assetPath.Contains("/Icons/")) { ti.alphaIsTransparency = true; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.wrapMode = UnityEngine.TextureWrapMode.Clamp; ti.mipmapEnabled = true; }
        if (assetPath.Contains("/Tex/") || assetPath.Contains("/PH/")) { ti.wrapMode = UnityEngine.TextureWrapMode.Repeat; ti.anisoLevel = 8; ti.maxTextureSize = 1024;
            if (assetPath.EndsWith("_n.jpg")) ti.textureType = TextureImporterType.NormalMap;
            if (assetPath.EndsWith("_m.png")) ti.sRGBTexture = false;
            if (assetPath.EndsWith("_d.png")) { ti.alphaIsTransparency = true; ti.mipMapsPreserveCoverage = true; ti.alphaTestReferenceValue = 0.45f; }
            if (assetPath.Contains("/PH/soldier") || assetPath.Contains("/PH/ak")) ti.maxTextureSize = 2048; }
        if (assetPath.Contains("/Sky/")) { ti.textureShape = TextureImporterShape.TextureCube; ti.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap; ti.mipmapEnabled = true; ti.maxTextureSize = 2048; ti.textureCompression = TextureImporterCompression.CompressedHQ; }
    }
}
