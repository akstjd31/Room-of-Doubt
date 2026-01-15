using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Scriptable Objects/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<Item> items;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Refresh();
    }

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        items.Clear();

        // Item 타입 에셋 전부 검색
        string[] guids = AssetDatabase.FindAssets("t:Item");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // 스크립터블 폴더 경로에서만 찾기
            if (!path.StartsWith("Assets/ScriptableObjects/"))
                continue;

            var item = AssetDatabase.LoadAssetAtPath<Item>(path);
            if (item != null) items.Add(item);
        }

        EditorUtility.SetDirty(this);
    }
#endif
}
