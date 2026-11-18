using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Card art, the easy way: drop a picture into one of these folders, named after the card, and it is set up as
// a sprite and put on that card by itself.
//
//   Assets/Art/Cards/事件卡/大黄狗妖.png     → the event card titled 大黄狗妖
//   Assets/Art/Cards/主线卡/要问山川.png     → the main story card titled 要问山川
//   Assets/Art/Cards/战斗卡/好狗不挡道.png   → the combat card titled 好狗不挡道
//   Assets/Art/Cards/社交卡/嘬嘬嘬.png       → the social card titled 嘬嘬嘬
//
// The file name is the card's title (or the card asset's own file name, e.g. Event_DaHuangGouYao).
// Menu 山海秘闻 → 重新匹配卡牌插图 matches every picture again (e.g. after renaming a card).
public class CardArtImporter : AssetPostprocessor
{
    public const string Root = "Assets/Art/Cards";
    static readonly Dictionary<string, System.Type> Folders = new Dictionary<string, System.Type>
    {
        { Root + "/事件卡", typeof(EventData) },
        { Root + "/主线卡", typeof(StoryData) },
        { Root + "/战斗卡", typeof(CombatData) },
        { Root + "/社交卡", typeof(SocialData) },
    };

    static System.Type TypeFor(string path)
    {
        string dir = Path.GetDirectoryName(path).Replace('\\', '/');
        return Folders.TryGetValue(dir, out var type) ? type : null;
    }

    // Species portraits (Assets/Art/Species/人.png, 九尾.png, 饕裔.png, 白泽.png): replace a file with a new
    // picture of the same name and it shows up on the species page and the character card.
    public const string SpeciesFolder = "Assets/Art/Species";

    // Pictures in these folders come in as sprites, keeping their transparent background.
    void OnPreprocessTexture()
    {
        bool species = Path.GetDirectoryName(assetPath).Replace('\\', '/') == SpeciesFolder;
        if (TypeFor(assetPath) == null && !species) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.alphaIsTransparency = true;
    }

    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        bool changed = false;
        foreach (var path in imported) changed |= Match(path);
        foreach (var path in moved) changed |= Match(path);
        if (changed) AssetDatabase.SaveAssets();
    }

    [MenuItem("山海秘闻/重新匹配卡牌插图")]
    static void MatchAll()
    {
        foreach (var dir in Folders.Keys) Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Sprite", new[] { Root }))
            if (Match(AssetDatabase.GUIDToAssetPath(guid))) n++;
        AssetDatabase.SaveAssets();
        Debug.Log("卡牌插图：匹配了 " + n + " 张");
    }

    // Puts the picture at `path` on the card it is named after. True if a card got it.
    static bool Match(string path)
    {
        var type = TypeFor(path);
        if (type == null) return false;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (!sprite) return false;
        string name = Clean(Path.GetFileNameWithoutExtension(path));
        bool found = false;
        foreach (var guid in AssetDatabase.FindAssets("t:" + type.Name))
        {
            var cardPath = AssetDatabase.GUIDToAssetPath(guid);
            var card = AssetDatabase.LoadAssetAtPath(cardPath, type);
            if (!card) continue;
            string title = Clean(TitleOf(card));
            if (name != title && name != Clean(Path.GetFileNameWithoutExtension(cardPath))) continue;
            var so = new SerializedObject(card);
            var image = so.FindProperty("image");
            if (image == null) continue;
            if (image.objectReferenceValue != sprite)
            {
                image.objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(card);
                Debug.Log("卡牌插图：" + Path.GetFileName(path) + " → " + cardPath);
            }
            found = true;
        }
        if (!found) Debug.LogWarning("卡牌插图：没有找到名为「" + Path.GetFileNameWithoutExtension(path) + "」的卡（" + path + "）");
        return found;
    }

    static string TitleOf(Object card)
    {
        if (card is EventData e) return e.title;
        if (card is StoryData s) return s.title;
        if (card is CombatData c) return c.title;
        if (card is SocialData so) return so.title;
        return "";
    }

    static string Clean(string s) => (s ?? "").Replace(" ", "").Replace("　", "").Trim();
}
