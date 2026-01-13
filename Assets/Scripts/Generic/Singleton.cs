using UnityEngine;

// 제네릭 싱글톤
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                DontDestroyOnLoad(instance.gameObject);
            }

            return instance;
        }
    }
}
