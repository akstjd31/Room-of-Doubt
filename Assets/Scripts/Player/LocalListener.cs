using UnityEngine;

public static class LocalListener
{
    private static Transform cached;
    public static Transform Transform
    {
        get
        {
            if (cached == null)
            {
                var al = Object.FindFirstObjectByType<AudioListener>();
                if (al != null) cached = al.transform;
            }

            return cached;
        }
        
        set => cached = value;
    }
}
