#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

public class RPGMakerToRuleTile
{
    const int fullSize = 48;
    const int halfSize = fullSize / 2;

    static string GetScriptPath([System.Runtime.CompilerServices.CallerFilePath] string filename = null)
    {
        return Path.GetDirectoryName(filename[(Application.dataPath.Length - "Assets".Length)..]);
    }

    public static Texture2D FlipTextureVertically(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Texture2D flipped = new(width, height);
        Color[] pixels = texture.GetPixels();
        Color[] pixelsFlipped = new Color[pixels.Length];

        for (int i = 0; i < height; i++)
        {
            System.Array.Copy(pixels, i * width, pixelsFlipped, (height - i - 1) * width, width);
        }

        flipped.SetPixels(pixelsFlipped);
        flipped.Apply();
        return flipped;
    }

    public static Texture2D GetTexture2D(Texture2D texture, int x, int y, int width, int height)
    {
        var pixels = texture.GetPixels(x, y, width, height, 0);
        Texture2D subTexture = new(width, height);
        subTexture.SetPixels(pixels);
        subTexture.Apply();
        return subTexture;
    }

    public static void SetPixels(Texture2D dst, Texture2D src, int x, int y)
    {
        dst.SetPixels(x, y, src.width, src.height, src.GetPixels());
    }

    public static Texture2D BuildTile(Texture2D topLeft, Texture2D topRight, Texture2D bottomLeft, Texture2D bottomRight, Texture2D wall = null)
    {
        var texture = new Texture2D(topLeft.width * 2, topLeft.height * 2 + (wall == null ? 0 : topLeft.height * 4));
        SetPixels(texture, topLeft, 0, 0);
        SetPixels(texture, topRight, topLeft.width, 0);
        SetPixels(texture, bottomLeft, 0, topLeft.height);
        SetPixels(texture, bottomRight, topLeft.width, topLeft.height);

        if (wall != null) SetPixels(texture, wall, 0, topLeft.height * 2);
        return texture;
    }

    public static Texture2D BuildTileset(params Texture2D[] textures)
    {
        var tileset = new Texture2D(textures[0].width * 47, textures[0].height);

        int i = 0;
        foreach (Texture2D texture in textures)
        {
            SetPixels(tileset, texture, i * textures[0].width, 0);
            i++;
        }

        return tileset;
    }

    public static void SavePNG(Texture2D texture, string path)
    {
        // Encode texture into PNG
        byte[] bytes = FlipTextureVertically(texture).EncodeToPNG();

        // For testing purposes, also write to a file in the project folder
        File.WriteAllBytes(path, bytes);

        AssetDatabase.Refresh();
    }

    public static void SaveTileset(Texture2D texture, string path, bool isWall)
    {
        SavePNG(texture, path);
        var preset = AssetDatabase.LoadAssetAtPath<Preset>(GetScriptPath() + "/" + (isWall ? "Wall" : "Ground") + "TextureImporter.preset");
        var sprite = AssetImporter.GetAtPath(path);
        preset.ApplyTo(sprite);
        AssetDatabase.ImportAsset(path);
    }

    [MenuItem("Assets/Create/RPGMaker/Ground RuleTile", priority = 0)]
    public static void CreateGroundRuleTile()
    {
        Generate(false);
    }

    [MenuItem("Assets/Create/RPGMaker/Wall RuleTile", priority = 0)]
    public static void CreateWallRuleTile()
    {
        Generate(true);
    }

    public static void Generate(bool isWall)
    {

        string filePath = EditorUtility.OpenFilePanel("Overwrite with png", "", "png");
        if (filePath == null)
        {
            Debug.Log("<color=red>RPGMakerToRuleTile: Must select a file!</color>");
            return;
        }

        // Load PNG into texture
        var rawData = File.ReadAllBytes(filePath);
        Texture2D input = new(2, 2);
        input.LoadImage(rawData);

        Debug.Log(input);


        // Grab filename without path or suffix
        var filePrefix = Path.GetFileNameWithoutExtension(filePath);

        // Get current 'Project View' folder
        var dstPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
        if (dstPath.Contains(".")) dstPath = dstPath.Remove(dstPath.LastIndexOf('/'));

        // Setup tileset and ruletile paths
        var tilesetPath = dstPath + "/" + filePrefix + ".png";
        var ruleTilePath = dstPath + "/" + filePrefix + ".asset";


        // Invert Y access so coordinates map as if the image is right-up (textures index bottom-to-top)
        input = FlipTextureVertically(input);

        // Build single ceiling
        var tempCeilS = GetTexture2D(input,                   0, 0, fullSize,     fullSize);

        // Build partial corner ceiling
        var tempCeilCornerTL = GetTexture2D(input, halfSize * 2, 0, halfSize,     halfSize);
        var tempCeilCornerTR = GetTexture2D(input, halfSize * 3, 0, halfSize,     halfSize);
        var tempCeilCornerBL = GetTexture2D(input, halfSize * 2,    halfSize,     halfSize,     halfSize);
        var tempCeilCornerBR = GetTexture2D(input, halfSize * 3,    halfSize,     halfSize,     halfSize);

        // Build partial top row of combination ceiling
        var tempCeilTTLL = GetTexture2D(input,     0,               halfSize * 2, halfSize,     halfSize);
        var tempCeilTTLR = GetTexture2D(input,     halfSize,        halfSize * 2, halfSize,     halfSize);
        var tempCeilTTRL = GetTexture2D(input,     halfSize * 2,    halfSize * 2, halfSize,     halfSize);
        var tempCeilTTRR = GetTexture2D(input,     halfSize * 3,    halfSize * 2, halfSize,     halfSize);

        // Build partial one down from top row of combination ceiling
        var tempCeilTBLL = GetTexture2D(input,     0,               halfSize * 3, halfSize,     halfSize);
        var tempCeilTBLR = GetTexture2D(input,     halfSize,        halfSize * 3, halfSize,     halfSize);
        var tempCeilTBRL = GetTexture2D(input,     halfSize * 2,    halfSize * 3, halfSize,     halfSize);
        var tempCeilTBRR = GetTexture2D(input,     halfSize * 3,    halfSize * 3, halfSize,     halfSize);

        // Build partial one up from bottom row of combination ceiling
        var tempCeilBTLL = GetTexture2D(input,     0,               halfSize * 4, halfSize,     halfSize);
        var tempCeilBTLR = GetTexture2D(input,     halfSize,        halfSize * 4, halfSize,     halfSize);
        var tempCeilBTRL = GetTexture2D(input,     halfSize * 2,    halfSize * 4, halfSize,     halfSize);
        var tempCeilBTRR = GetTexture2D(input,     halfSize * 3,    halfSize * 4, halfSize,     halfSize);

        // Build partial bottom row of combination ceiling
        var tempCeilBBLL = GetTexture2D(input,     0,               halfSize * 5, halfSize,     halfSize);
        var tempCeilBBLR = GetTexture2D(input,     halfSize,        halfSize * 5, halfSize,     halfSize);
        var tempCeilBBRL = GetTexture2D(input,     halfSize * 2,    halfSize * 5, halfSize,     halfSize);
        var tempCeilBBRR = GetTexture2D(input,     halfSize * 3,    halfSize * 5, halfSize,     halfSize);

        // Build partial wall combos (without ceilings)
        var tempWallLL = isWall ? GetTexture2D(input,       0,               halfSize * 6, halfSize,     halfSize * 4) : null;
        var tempWallLR = isWall ? GetTexture2D(input,       halfSize,        halfSize * 6, halfSize,     halfSize * 4) : null;
        var tempWallRL = isWall ? GetTexture2D(input,       halfSize * 2,    halfSize * 6, halfSize,     halfSize * 4) : null;
        var tempWallRR = isWall ? GetTexture2D(input,       halfSize * 3,    halfSize * 6, halfSize,     halfSize * 4) : null;

        // Build wall combos (without ceilings)
        var tempWallS = isWall ? new Texture2D(fullSize, fullSize * 2) : null;
        var tempWallL = isWall ? new Texture2D(fullSize, fullSize * 2) : null;
        var tempWallM = isWall ? new Texture2D(fullSize, fullSize * 2) : null;
        var tempWallR = isWall ? new Texture2D(fullSize, fullSize * 2) : null;
        
        // Single
        if (isWall) SetPixels(tempWallS,   tempWallLL,   0,        0);
        if (isWall) SetPixels(tempWallS,   tempWallRR,   halfSize, 0);

        // Left
        if (isWall) SetPixels(tempWallL,   tempWallLL,   0,        0);
        if (isWall) SetPixels(tempWallL,   tempWallLR,   halfSize, 0);

        // Middle
        if (isWall) SetPixels(tempWallM,   tempWallLR,   0,        0);
        if (isWall) SetPixels(tempWallM,   tempWallRL,   halfSize, 0);

        // Right
        if (isWall) SetPixels(tempWallR,   tempWallRL,   0,        0);
        if (isWall) SetPixels(tempWallR,   tempWallRR,   halfSize, 0);

        // ---- Build walls with ceilings ---

        // Single Wall
        var wallS = new Texture2D(fullSize, isWall ? fullSize * 3 : fullSize);
        SetPixels(wallS, tempCeilS, 0, 0);
        if (isWall) SetPixels(wallS, tempWallS, 0, fullSize);

        // Empty
        var empty = BuildTile(tempCeilBTRL,            tempCeilBTLR,     tempCeilTBRL,     tempCeilTBLR);

        // Four corners
        var cornerTBLR = BuildTile(tempCeilCornerTL,   tempCeilCornerTR, tempCeilCornerBL, tempCeilCornerBR);

        // Wall top-bottom edge, left-right edge
        var wallTBE = BuildTile(tempCeilTTLR,          tempCeilTTRL,     tempCeilBBLR,     tempCeilBBRL, tempWallM);
        var wallLRE = BuildTile(tempCeilTBLL,          tempCeilTBRR,     tempCeilBTLL,     tempCeilBTRR);

        // Wall top-left-right edge, bottom-left-right edge, left-top-bottom edge, right-top-bottom edge
        var wallTLRE = BuildTile(tempCeilTTLL,         tempCeilTTRR,     tempCeilTBLL,     tempCeilTBRR);
        var wallBLRE = BuildTile(tempCeilBTLL,         tempCeilBTRR,     tempCeilBBLL,     tempCeilBBRR, tempWallS);
        var wallLTBE = BuildTile(tempCeilTTLL,         tempCeilTTLR,     tempCeilBBLL,     tempCeilBBLR, tempWallL);
        var wallRTBE = BuildTile(tempCeilTTRL,         tempCeilTTRR,     tempCeilBBRL,     tempCeilBBRR, tempWallR);

        // Wall top edge, bottom edge, left edge, right edge
        var wallTE = BuildTile(tempCeilTTRL,           tempCeilTTLR,     tempCeilTBRL,     tempCeilTBLR);
        var wallBE = BuildTile(tempCeilBTRL,           tempCeilBTLR,     tempCeilBBRL,     tempCeilBBLR, tempWallM);
        var wallLE = BuildTile(tempCeilBTLL,           tempCeilBTLR,     tempCeilTBLL,     tempCeilTBLR);
        var wallRE = BuildTile(tempCeilBTRL,           tempCeilBTRR,     tempCeilTBRL,     tempCeilTBRR);

        // Wall top edge left-right corners, bottom edge left-right corners, left edge top bottom corners, right edge top bottom corners
        var wallTELRC = BuildTile(tempCeilTTLR,        tempCeilTTRL,     tempCeilCornerBL, tempCeilCornerBR);
        var wallBELRC = BuildTile(tempCeilCornerTL,    tempCeilCornerTR, tempCeilBBLR,     tempCeilBBRL, tempWallM);
        var wallLETBC = BuildTile(tempCeilTBLL,        tempCeilCornerTR, tempCeilBTLL,     tempCeilCornerBR);
        var wallRETBC = BuildTile(tempCeilCornerTL,    tempCeilTBRR,     tempCeilCornerBL, tempCeilBTRR);

        // Wall top corners, bottom corners, left corners, right corners 
        var wallTC = BuildTile(tempCeilCornerTL,       tempCeilCornerTR, tempCeilTBRL,     tempCeilTBLR);
        var wallBC = BuildTile(tempCeilBTRL,           tempCeilBTLR,     tempCeilCornerBL, tempCeilCornerBR);
        var wallLC = BuildTile(tempCeilCornerTL,       tempCeilBTLR,     tempCeilCornerBL, tempCeilTBLR);
        var wallRC = BuildTile(tempCeilBTRL,           tempCeilCornerTR, tempCeilTBRL,     tempCeilCornerBR);

        // Wall top-left edge, top-right edge, bottom-left edge, bottom-right edge
        var wallTLE = BuildTile(tempCeilTTLL,          tempCeilTTLR,     tempCeilTBLL,     tempCeilTBLR);
        var wallTRE = BuildTile(tempCeilTTRL,          tempCeilTTRR,     tempCeilTBRL,     tempCeilTBRR);
        var wallBLE = BuildTile(tempCeilBTLL,          tempCeilBTLR,     tempCeilBBLL,     tempCeilBBLR, tempWallL);
        var wallBRE = BuildTile(tempCeilBTRL,          tempCeilBTRR,     tempCeilBBRL,     tempCeilBBRR, tempWallR);

        // Wall top-left edge w/ corner, top-right edge w/ corner, bottom-left edge /w corner, bottom-right edge w/ corner
        var wallTLEC = BuildTile(tempCeilTTLL,         tempCeilTTLR,     tempCeilTBLL,     tempCeilCornerBR);
        var wallTREC = BuildTile(tempCeilTTRL,         tempCeilTTRR,     tempCeilCornerBL, tempCeilTBRR);
        var wallBLEC = BuildTile(tempCeilBTLL,         tempCeilCornerTR, tempCeilBBLL,     tempCeilBBLR, tempWallL);
        var wallBREC = BuildTile(tempCeilCornerTL,     tempCeilBTRR,     tempCeilBBRL,     tempCeilBBRR, tempWallR);

        // Wall top edge left corner, top edge right corner, bottom edge left corner, bottom edge right corner
        var wallTELC = BuildTile(tempCeilTTLR,         tempCeilTTRL,     tempCeilCornerBL, tempCeilTBLR);
        var wallTERC = BuildTile(tempCeilTTLR,         tempCeilTTRL,     tempCeilTBRL,     tempCeilCornerBR);
        var wallBELC = BuildTile(tempCeilCornerTL,     tempCeilBTLR,     tempCeilBBLR,     tempCeilBBRL, tempWallM);
        var wallBERC = BuildTile(tempCeilBTRL,         tempCeilCornerTR, tempCeilBBLR,     tempCeilBBRL, tempWallM);

        // Wall left edge top corner, right edge top corner, left edge bottom corner, right edge bottom corner
        var wallLETC = BuildTile(tempCeilTBLL,         tempCeilCornerTR, tempCeilBTLL,     tempCeilTBLR);
        var wallRETC = BuildTile(tempCeilCornerTL,     tempCeilTBRR,     tempCeilTBRL,     tempCeilBTRR);
        var wallLEBC = BuildTile(tempCeilTBLL,         tempCeilBTLR,     tempCeilBTLL,     tempCeilCornerBR);
        var wallREBC = BuildTile(tempCeilBTRL,         tempCeilTBRR,     tempCeilCornerBL, tempCeilBTRR);


        // Corner top-left, top-right, bottom-left, bottom-right 
        var cornerTL = BuildTile(tempCeilCornerTL,     tempCeilBTLR,     tempCeilTBRL,     tempCeilTBLR);
        var cornerTR = BuildTile(tempCeilBTRL,         tempCeilCornerTR, tempCeilTBRL,     tempCeilTBLR);
        var cornerBL = BuildTile(tempCeilBTRL,         tempCeilBTLR,     tempCeilCornerBL, tempCeilTBLR);
        var cornerBR = BuildTile(tempCeilBTRL,         tempCeilBTLR,     tempCeilTBRL,     tempCeilCornerBR);

        // Corner top-left bottom-right, bottom-left top-right
        var cornerTLBR = BuildTile(tempCeilCornerTL,   tempCeilBTLR,     tempCeilTBRL,     tempCeilCornerBR);
        var cornerBLTR = BuildTile(tempCeilBTRL,       tempCeilCornerTR, tempCeilCornerBL, tempCeilTBLR);

        // Corner top
        var cornerTLTRBL = BuildTile(tempCeilCornerTL, tempCeilCornerTR, tempCeilCornerBL, tempCeilTBLR);
        var cornerTLTRBR = BuildTile(tempCeilCornerTL, tempCeilCornerTR, tempCeilTBRL,     tempCeilCornerBR);
        var cornerTLBLBR = BuildTile(tempCeilCornerTL, tempCeilBTLR,     tempCeilCornerBL, tempCeilCornerBR);
        var cornerTRBLBR = BuildTile(tempCeilBTRL,     tempCeilCornerTR, tempCeilCornerBL, tempCeilCornerBR);


        // Build tileset
        var tileset = BuildTileset(
            wallS,        empty,        cornerTBLR,                // Single Wall, Empty, Four corners
            wallTBE,      wallLRE,                                 // Wall top-bottom edge, left-right edge
            wallTLRE,     wallBLRE,     wallLTBE,     wallRTBE,    // Wall top-left-right edge, bottom-left-right edge, left-top-bottom edge, right-top-bottom edge
            wallTE,       wallBE,       wallLE,       wallRE,      // Wall top edge, bottom edge, left edge, right edge
            wallTELRC,    wallBELRC,    wallLETBC,    wallRETBC,   // Wall top edge left-right corners, bottom edge left-right corners, left edge top bottom corners, right edge top bottom corners
            wallTC,       wallBC,       wallLC,       wallRC,      // Wall top corners, bottom corners, left corners, right corners 
            wallTLE,      wallTRE,      wallBLE,      wallBRE,     // Wall top-left edge, top-right edge, bottom-left edge, bottom-right edge
            wallTLEC,     wallTREC,     wallBLEC,     wallBREC,    // Wall top-left edge w/ corner, top-right edge w/ corner, bottom-left edge /w corner, bottom-right edge w/ corner
            wallTELC,     wallTERC,     wallBELC,     wallBERC,    // Wall top edge left corner, top edge right corner, bottom edge left corner, bottom edge right corner
            wallLETC,     wallRETC,     wallLEBC,     wallREBC,    // Wall left edge top corner, right edge top corner, left edge bottom corner, right edge bottom corner
            cornerTL,     cornerTR,     cornerBL,     cornerBR,    // Corner top-left, top-right, bottom-left, bottom-right 
            cornerTLBR,   cornerBLTR,                              // Corner top-left bottom-right, bottom-left top-right
            cornerTLTRBL, cornerTLTRBR, cornerTLBLBR, cornerTRBLBR // Corner top
        );

        // Save tileset asset
        SaveTileset(tileset, tilesetPath, isWall);
        Debug.Log($"<color=magenta>RPGMakerToRuleTile</color>: <color=cyan>created tileset at </color><color=orange>'{tilesetPath}'</color>");

        // Load sprites from asset
        var sprites = AssetDatabase.LoadAllAssetsAtPath(tilesetPath).OfType<Sprite>().ToArray();

        // Try to load ruletile before overwriting
        RuleTile ruleTile;
        ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(dstPath + "/" + filePrefix + ".asset");
        if (ruleTile == null)
        {
            AssetDatabase.CopyAsset(GetScriptPath() + "/EmptyRuleTile.asset", ruleTilePath);
            ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(ruleTilePath);
            Debug.Log($"<color=magenta>RPGMakerToRuleTile</color>: <color=cyan>created RuleTile at </color><color=orange>'{ruleTilePath}'</color>");
        }
        else
        {
            Debug.Log($"<color=magenta>RPGMakerToRuleTile</color>: <color=cyan>used existing RuleTile at </color><color=orange>'{ruleTilePath}'</color>");
        }

        // Map sprites to ruletile
        for (int i = 0; i < sprites.Length; i++)
            ruleTile.m_TilingRules[i].m_Sprites[0] = sprites[i];
    }
}
#endif
