using System;
using UnityEngine;

[System.Serializable]
public class UserData
{
    public int gold;
    public int exp;
    public int level;
}
public class UserDataManager : Singleton<UserDataManager>
{
    public UserData Data { get; private set; } = new UserData();
    public event Action<UserData> OnDataChanged;

    public void SetData(UserData data)
    {
        Data = data ?? new UserData();
        OnDataChanged?.Invoke(Data);
    }

    public void AddGold(int amount)
    {
        Data.gold += amount;
        OnDataChanged?.Invoke(Data);
    }

    public void AddExp(int amount)
    {
        Data.exp += amount;
        OnDataChanged?.Invoke(Data);
    }

    public void SetLevel(int level)
    {
        Data.level = level;
        OnDataChanged?.Invoke(Data);
    }
}
