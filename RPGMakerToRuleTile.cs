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
    const int frameCount = 3;

    [MenuItem("Assets/Create/RPGMaker/Animated Ground RuleTile", priority = 0)]
    public static void CreateAnimatedGroundRuleTile()
    {
        foreach (var obj in Selection.objects)
            Generate(obj, true, false);
    }

    [MenuItem("Assets/Create/RPGMaker/Ground RuleTile", priority = 0)]
    public static void CreateGroundRuleTile()
    {
        foreach (var obj in Selection.objects)
            Generate(obj, false, false);
    }

    [MenuItem("Assets/Create/RPGMaker/Wall RuleTile", priority = 0)]
    public static void CreateWallRuleTile()
    {
        foreach (var obj in Selection.objects)
            Generate(obj, false, true);
    }

    static string GetScriptPath([System.Runtime.CompilerServices.CallerFilePath] string filename = null)
    {
        return Path.GetDirectoryName(filename[(Application.dataPath.Length - "Assets".Length)..]);
    }

    public static void SavePNG(Texture2D texture, string path)
    {
        // Encode texture into PNG
        byte[] bytes = texture.EncodeToPNG();

        // For testing purposes, also write to a file in the project folder
        File.WriteAllBytes(path, bytes);

        AssetDatabase.Refresh();
    }

    public static void SaveTileset(Texture2D texture, string path, bool isAnimated, bool isWall)
    {
        SavePNG(texture, path);
        var preset = AssetDatabase.LoadAssetAtPath<Preset>(GetScriptPath() + "/" + (isAnimated ? "Animated" : "") + (isWall ? "Wall" : "Ground") + "TextureImporter.preset");
        var sprite = AssetImporter.GetAtPath(path);
        preset.ApplyTo(sprite);
        AssetDatabase.ImportAsset(path);
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

    public static Texture2D StichTile(Texture2D topLeft, Texture2D topRight, Texture2D bottomLeft, Texture2D bottomRight, Texture2D wall = null)
    {
        var texture = new Texture2D(topLeft.width * 2, topLeft.height * 2 + (wall == null ? 0 : topLeft.height * 4));
        SetPixels(texture, topLeft,      0,             0);
        SetPixels(texture, topRight,     topLeft.width, 0);
        SetPixels(texture, bottomLeft,   0,             topLeft.height);
        SetPixels(texture, bottomRight,  topLeft.width, topLeft.height);

        if (wall != null) SetPixels(texture, wall, 0, topLeft.height * 2);
        return texture;
    }

    public static Texture2D StichTilesetHorizontal(params Texture2D[] textures)
    {
        int totalWidth = 0;
        foreach (var texture in textures)
            totalWidth += texture.width;

        var tileset = new Texture2D(totalWidth, textures[0].height);

        int i = 0;
        foreach (Texture2D texture in textures)
        {
            SetPixels(tileset, texture, i * textures[0].width, 0);
            i++;
        }

        return tileset;
    }

    public static Texture2D StichTilesetVertical(params Texture2D[] textures)
    {
        int totalHeight = 0;
        foreach (var texture in textures)
            totalHeight += texture.height;

        var tileset = new Texture2D(textures[0].width, totalHeight);

        int i = 0;
        foreach (Texture2D texture in textures)
        {
            SetPixels(tileset, texture, 0, i * textures[0].height);
            i++;
        }

        return tileset;
    }

    public static Texture2D BuildTileset(Texture2D texture, bool isWall)
    {
        // Invert Y access for easy coordinate mapping (textures index bottom-to-top, we want top-to-bottom)
        var input = FlipTextureVertically(texture);

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
        var empty = StichTile(tempCeilBTRL,            tempCeilBTLR,     tempCeilTBRL,     tempCeilTBLR);

        // Four corners
        var cornerTBLR = StichTile(tempCeilCornerTL,   tempCeilCornerTR, tempCeilCornerBL, tempCeilCornerBR);

        // Wall top-bottom edge, left-right edge
        var wallTBE = StichTile(tempCeilTTLR,          tempCeilTTRL,     tempCeilBBLR,     tempCeilBBRL, tempWallM);
        var wallLRE = StichTile(tempCeilTBLL,          tempCeilTBRR,     tempCeilBTLL,     tempCeilBTRR);

        // Wall top-left-right edge, bottom-left-right edge, left-top-bottom edge, right-top-bottom edge
        var wallTLRE = StichTile(tempCeilTTLL,         tempCeilTTRR,     tempCeilTBLL,     tempCeilTBRR);
        var wallBLRE = StichTile(tempCeilBTLL,         tempCeilBTRR,     tempCeilBBLL,     tempCeilBBRR, tempWallS);
        var wallLTBE = StichTile(tempCeilTTLL,         tempCeilTTLR,     tempCeilBBLL,     tempCeilBBLR, tempWallL);
        var wallRTBE = StichTile(tempCeilTTRL,         tempCeilTTRR,     tempCeilBBRL,     tempCeilBBRR, tempWallR);

        // Wall top edge, bottom edge, left edge, right edge
        var wallTE = StichTile(tempCeilTTRL,           tempCeilTTLR,     tempCeilTBRL,     tempCeilTBLR);
        var wallBE = StichTile(tempCeilBTRL,           tempCeilBTLR,     tempCeilBBRL,     tempCeilBBLR, tempWallM);
        var wallLE = StichTile(tempCeilBTLL,           tempCeilBTLR,     tempCeilTBLL,     tempCeilTBLR);
        var wallRE = StichTile(tempCeilBTRL,           tempCeilBTRR,     tempCeilTBRL,     tempCeilTBRR);

        // Wall top edge left-right corners, bottom edge left-right corners, left edge top bottom corners, right edge top bottom corners
        var wallTELRC = StichTile(tempCeilTTLR,        tempCeilTTRL,     tempCeilCornerBL, tempCeilCornerBR);
        var wallBELRC = StichTile(tempCeilCornerTL,    tempCeilCornerTR, tempCeilBBLR,     tempCeilBBRL, tempWallM);
        var wallLETBC = StichTile(tempCeilTBLL,        tempCeilCornerTR, tempCeilBTLL,     tempCeilCornerBR);
        var wallRETBC = StichTile(tempCeilCornerTL,    tempCeilTBRR,     tempCeilCornerBL, tempCeilBTRR);

        // Wall top corners, bottom corners, left corners, right corners 
        var wallTC = StichTile(tempCeilCornerTL,       tempCeilCornerTR, tempCeilTBRL,     tempCeilTBLR);
        var wallBC = StichTile(tempCeilBTRL,           tempCeilBTLR,     tempCeilCornerBL, tempCeilCornerBR);
        var wallLC = StichTile(tempCeilCornerTL,       tempCeilBTLR,     tempCeilCornerBL, tempCeilTBLR);
        var wallRC = StichTile(tempCeilBTRL,           tempCeilCornerTR, tempCeilTBRL,     tempCeilCornerBR);

        // Wall top-left edge, top-right edge, bottom-left edge, bottom-right edge
        var wallTLE = StichTile(tempCeilTTLL,          tempCeilTTLR,     tempCeilTBLL,     tempCeilTBLR);
        var wallTRE = StichTile(tempCeilTTRL,          tempCeilTTRR,     tempCeilTBRL,     tempCeilTBRR);
        var wallBLE = StichTile(tempCeilBTLL,          tempCeilBTLR,     tempCeilBBLL,     tempCeilBBLR, tempWallL);
        var wallBRE = StichTile(tempCeilBTRL,          tempCeilBTRR,     tempCeilBBRL,     tempCeilBBRR, tempWallR);

        // Wall top-left edge w/ corner, top-right edge w/ corner, bottom-left edge /w corner, bottom-right edge w/ corner
        var wallTLEC = StichTile(tempCeilTTLL,         tempCeilTTLR,     tempCeilTBLL,     tempCeilCornerBR);
        var wallTREC = StichTile(tempCeilTTRL,         tempCeilTTRR,     tempCeilCornerBL, tempCeilTBRR);
        var wallBLEC = StichTile(tempCeilBTLL,         tempCeilCornerTR, tempCeilBBLL,     tempCeilBBLR, tempWallL);
        var wallBREC = StichTile(tempCeilCornerTL,     tempCeilBTRR,     tempCeilBBRL,     tempCeilBBRR, tempWallR);

        // Wall top edge left corner, top edge right corner, bottom edge left corner, bottom edge right corner
        var wallTELC = StichTile(tempCeilTTLR,         tempCeilTTRL,     tempCeilCornerBL, tempCeilTBLR);
        var wallTERC = StichTile(tempCeilTTLR,         tempCeilTTRL,     tempCeilTBRL,     tempCeilCornerBR);
        var wallBELC = StichTile(tempCeilCornerTL,     tempCeilBTLR,     tempCeilBBLR,     tempCeilBBRL, tempWallM);
        var wallBERC = StichTile(tempCeilBTRL,         tempCeilCornerTR, tempCeilBBLR,     tempCeilBBRL, tempWallM);

        // Wall left edge top corner, right edge top corner, left edge bottom corner, right edge bottom corner
        var wallLETC = StichTile(tempCeilTBLL,         tempCeilCornerTR, tempCeilBTLL,     tempCeilTBLR);
        var wallRETC = StichTile(tempCeilCornerTL,     tempCeilTBRR,     tempCeilTBRL,     tempCeilBTRR);
        var wallLEBC = StichTile(tempCeilTBLL,         tempCeilBTLR,     tempCeilBTLL,     tempCeilCornerBR);
        var wallREBC = StichTile(tempCeilBTRL,         tempCeilTBRR,     tempCeilCornerBL, tempCeilBTRR);


        // Corner top-left, top-right, bottom-left, bottom-right 
        var cornerTL = StichTile(tempCeilCornerTL,     tempCeilBTLR,     tempCeilTBRL,     tempCeilTBLR);
        var cornerTR = StichTile(tempCeilBTRL,         tempCeilCornerTR, tempCeilTBRL,     tempCeilTBLR);
        var cornerBL = StichTile(tempCeilBTRL,         tempCeilBTLR,     tempCeilCornerBL, tempCeilTBLR);
        var cornerBR = StichTile(tempCeilBTRL,         tempCeilBTLR,     tempCeilTBRL,     tempCeilCornerBR);

        // Corner top-left bottom-right, bottom-left top-right
        var cornerTLBR = StichTile(tempCeilCornerTL,   tempCeilBTLR,     tempCeilTBRL,     tempCeilCornerBR);
        var cornerBLTR = StichTile(tempCeilBTRL,       tempCeilCornerTR, tempCeilCornerBL, tempCeilTBLR);

        // Corner top
        var cornerTLTRBL = StichTile(tempCeilCornerTL, tempCeilCornerTR, tempCeilCornerBL, tempCeilTBLR);
        var cornerTLTRBR = StichTile(tempCeilCornerTL, tempCeilCornerTR, tempCeilTBRL,     tempCeilCornerBR);
        var cornerTLBLBR = StichTile(tempCeilCornerTL, tempCeilBTLR,     tempCeilCornerBL, tempCeilCornerBR);
        var cornerTRBLBR = StichTile(tempCeilBTRL,     tempCeilCornerTR, tempCeilCornerBL, tempCeilCornerBR);


        // Build tileset
        var tileset = StichTilesetHorizontal(
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

        // Invert Y access again to restore the original texture order (textures index bottom-to-top)
        tileset = FlipTextureVertically(tileset);

        return tileset;
    }

    public static void Generate(Object obj, bool isAnimated, bool isWall)
    {
        var input = obj as Texture2D;
        if (input == null)
        {
            Debug.Log("<color=red>RPGMakerToRuleTile: Must select a Texture!</color>");
            return;
        }

        // Get texture path
        var filePath = AssetDatabase.GetAssetPath(input);

        // Grab filename without path or suffix
        var filePrefix = Path.GetFileNameWithoutExtension(filePath);

        // Get texture folder
        var dstPath = Path.GetDirectoryName(filePath);

        // Create folder to hold rultile and tileset
        if (!AssetDatabase.IsValidFolder(dstPath + "/" + filePrefix + ".RuleTile"))
            AssetDatabase.CreateFolder(dstPath, filePrefix + ".RuleTile");
        dstPath = dstPath + "/" + filePrefix + ".RuleTile";

        // Setup tileset and ruletile paths
        var tilesetPath = dstPath + "/" + filePrefix + ".png";
        var ruleTilePath = dstPath + "/" + filePrefix + ".asset";

        // Copy texture (to get around read issues)
        var rawData = File.ReadAllBytes(filePath);
        input = new(2, 2);
        input.LoadImage(rawData);

        Texture2D tileset;
        if (!isAnimated)
        {
            // Build tileset
            tileset = BuildTileset(input, isWall);
        }
        else
        {
            // Build frames
            Texture2D[] frames = new Texture2D[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                var frame = GetTexture2D(input, i * (input.width / 3), 0, input.width / 3, input.height);
                frames[i] = BuildTileset(frame, isWall);
            }

            tileset = StichTilesetVertical(frames);
        }

        // Save tileset asset
        SaveTileset(tileset, tilesetPath, isAnimated, isWall);
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

        // Map sprites to ruletile (47 sprites per ruletile)
        for (int i = 0; i < 47; i++)
        {
            if (!isAnimated)
            {
                // Setup tiling rule for single sprites
                ruleTile.m_TilingRules[i].m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single;
                ruleTile.m_TilingRules[i].m_Sprites[0] = sprites[i];
            }
            else
            {
                // Setup tiling rule for animations
                ruleTile.m_TilingRules[i].m_Output = RuleTile.TilingRuleOutput.OutputSprite.Animation;
                ruleTile.m_TilingRules[i].m_MinAnimationSpeed = 2;
                ruleTile.m_TilingRules[i].m_MaxAnimationSpeed = 2;
                ruleTile.m_TilingRules[i].m_Sprites = new Sprite[frameCount];

                // Set sprites
                for (int j = 0; j < frameCount; j++)
                    ruleTile.m_TilingRules[i].m_Sprites[j] = sprites[i + j * 47];
            }
        }

        EditorUtility.SetDirty(ruleTile);
        AssetDatabase.SaveAssetIfDirty(ruleTile);
    }
}
#endif
