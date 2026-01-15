#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ItemDatabaseAutoUpdater : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets,        // 새로 추가되거나 수정된 에셋
        string[] deletedAssets,         // 삭제된 에셋
        string[] movedAssets,           // 이동된 에셋 새 경로
        string[] movedFromAssetPaths)   // 이동되기 전 옛 경로
    {
        bool changed = false;           // 갱신할지, 말지

        foreach (var path in importedAssets)
            if (path.EndsWith(".asset")) changed = true;

        foreach (var path in deletedAssets)
            if (path.EndsWith(".asset")) changed = true;

        foreach (var path in movedAssets)
            if (path.EndsWith(".asset")) changed = true;

        if (!changed) return;

        // 프로젝트에서 ItemDatabase 찾기 (하나만 있다고 가정)
        string[] dbGuids = AssetDatabase.FindAssets("t:" + nameof(ItemDatabase));
        if (dbGuids.Length == 0) return;
        
        // 실제 파일 경로 반환하기
        string dbPath = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
        if (db == null) return;

        
        db.Refresh();
        Debug.Log("아이템 데이터베이스 갱신 완료!");
    }
}
#endif
