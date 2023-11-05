using UnityEngine;
using UnityEngine.UI;

namespace Game.Slot
{
    public class FrameData
    {
        public PoolObject obj;
        public Vector3 pos;
        public Image img;
        public FrameData(PoolObject _obj)
        {
            obj = _obj;
            pos = _obj.cachedRectTransform.position;
            img = _obj.GetComponent<Image>();
            if (null == img) img = _obj.transform.GetChild(0).GetComponent<Image>();
        }
    }
}
