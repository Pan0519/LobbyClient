using UnityEngine;

namespace CommonILRuntime.SpriteProvider
{
    public interface ISpriteProvider
    {
        Sprite getSprite(string name);
    }
}
