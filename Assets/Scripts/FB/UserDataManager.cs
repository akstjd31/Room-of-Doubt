using System;
using UnityEngine;

[System.Serializable]
public class UserData
{
    public string nickname;
    public int gold;
    public int exp;
    public int level;
}

public class UserDataManager : Singleton<UserDataManager>
{
    public UserData Data { get; private set; } = new UserData();

    public void SetData(UserData data)
    {
        Data = data ?? new UserData();
    }
    
    public void SetNickname(string nickname)
    {
        Data.nickname = nickname;
    }

    public void AddGold(int amount)
    {
        Data.gold += amount;
    }

    public void AddExp(int amount)
    {
        Data.exp += amount;
    }

    public void SetLevel(int level)
    {
        Data.level = level;
    }
}
