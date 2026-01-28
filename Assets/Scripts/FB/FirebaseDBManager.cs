using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class FirebaseDBManager : MonoBehaviour
{
    DatabaseReference dbRef;
    FirebaseUser user;
    [SerializeField] private string userRoot = "users";

    private void Start()
    {
        this.dbRef = FirebaseAuthManager.dbRef;
        this.user = FirebaseAuthManager.user;
    }

    private bool Ready()
    {
        if (dbRef == null)
        {
            Debug.LogError("dfRef가 null 입니다.");
            return false;
        }

        if (user == null)
        {
            Debug.LogError("user가 null 입니다.");
            return false;
        }

        if (UserDataManager.Instance == null)
        {
            Debug.LogError("UserDataManager가 없습니다!");
            return false;
        }

        return true;
    }

    private DatabaseReference UserNode()
        => dbRef.Child(userRoot).Child(user.UserId);

    // 저장
    public async Task SaveUserDataAsync()
    {
        if (!Ready()) return;

        var data = UserDataManager.Instance.Data;

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            ["money"] = data.gold,
            ["exp"] = data.exp,
            ["level"] = data.level,
        };

        try
        {
            await UserNode().UpdateChildrenAsync(updates);
            Debug.Log("저장 성공!");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"저장 실패 : {e}");
        }
    }

    // 로드
    public async Task LoaduserDataAsync()
    {
        if (!Ready()) return;

        try
        {
            DataSnapshot snap = await UserNode().GetValueAsync();

            int gold = ReadInt(snap, "money", 0);
            int exp = ReadInt(snap, "exp", 0);
            int level = ReadInt(snap, "level", 1);

            var data = new UserData
            {
                gold = gold,
                exp = exp,
                level = level
            };

            UserDataManager.Instance.SetData(data);

            Debug.Log("로드 완료!");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"로드 실패 : {e}");
        }
    }

    // int 읽기
    private int ReadInt(DataSnapshot parent, string key, int defaultValue)
    {
        if (parent == null) return defaultValue;

        DataSnapshot child = parent.Child(key);
        if (child == null || !child.Exists || child.Value == null) return defaultValue;

        // 어떤 데이터 타입이 와도 대응하기
        if (child.Value is long l) return (int)l;
        if (child.Value is int i) return i;
        if (child.Value is double d) return (int)d;

        if (int.TryParse(child.Value.ToString(), out int parsed))
            return parsed;
        
        return defaultValue;
    }      
}
