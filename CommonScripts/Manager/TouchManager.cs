using UnityEngine;

public class TouchManager
{
    public static bool anyTouch
    {
        get
        {
            return Input.anyKeyDown;
        }
    }
}
