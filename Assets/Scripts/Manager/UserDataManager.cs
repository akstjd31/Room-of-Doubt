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

    public void AddGold(int amount) => Data.gold += amount;
    public void AddExp(int amount) => Data.exp += amount;
}
