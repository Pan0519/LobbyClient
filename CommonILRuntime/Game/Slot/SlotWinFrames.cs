using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Slot
{
    public class SlotWinFrames
    {
        protected List<FrameData> frames = new List<FrameData>();

        public virtual string objPath { get; set; } = "prefab/get_frame_short";


        /// <summary>
        /// 生成贏線框
        /// </summary>
        /// <param name="root"></param>
        /// <param name="pos"></param>
        public virtual void makeFrameEffect(RectTransform root, List<Vector3> pos)
        {
            frames.Clear();
            for (int reelDataIdx = 0; reelDataIdx < pos.Count; reelDataIdx++)
            {
                var frameObj = genWinLineEffect(root, pos[reelDataIdx]);
                frames.Add(frameObj);
            }
        }

        protected FrameData genWinLineEffect(RectTransform root, Vector3 pos)
        {
            var effectObj = ResourceManager.instance.getObjectFromPool(objPath, root);
            effectObj.cachedRectTransform.position = pos;
            var frame = new FrameData(effectObj);
            frame.img.enabled = false;
            return frame;
        }
        /// <summary>
        /// 開啟特定贏線框
        /// </summary>
        /// <param name="symbolId"></param>
        public virtual void setFrameEffects(List<int> symbolId)
        {
            for (int i = 0; i < frames.Count; ++i)
            {
                frames[i].img.enabled = symbolId.Any(x => x == i);
            }
        }
        /// <summary>
        /// 取得特定贏線框座標
        /// </summary>
        /// <param name="symbolId"></param>
        /// <returns></returns>
        public virtual Vector3 getFramePos(int symbolId)
        {
            return frames[symbolId].pos;
        }
        /// <summary>
        /// 關閉所有贏線框
        /// </summary>
        public virtual void clearFrameEffects()
        {
            for (int i = 0; i < frames.Count; ++i)
            {
                frames[i].img.enabled = false;
            }
        }
    }
}
