using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;

[System.Serializable]
public class UserData
{
    public int gold;
    public int exp;
    public int level;
}
public class FirebaseDBManager : MonoBehaviour
{
    DatabaseReference dbRef;
    FirebaseUser user;

    public int gold;
    public int exp;

    private void Start()
    {
        this.dbRef = FirebaseAuthManager.dbRef;
        this.user = FirebaseAuthManager.user;
    }

}
