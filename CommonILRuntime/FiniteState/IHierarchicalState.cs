using UnityEngine;
using System.Collections;

namespace CommonILRuntime.FiniteState
{
    public abstract class IHierarchicalState
    {
        private HierarchicalStateController m_StateController;
        protected HierarchicalStateController Controller
        {
            get
            {
                return m_StateController;
            }
        }
        private int m_iLevel = 0;
        protected int StateLevel
        {
            get
            {
                return m_iLevel;
            }
        }
        private bool m_bAtStateBegin = true;
        public bool AtStateBegin
        {
            get
            {
                return m_bAtStateBegin;
            }
        }

        protected int m_iCurrStateID;
        public int CurrStateID
        {
            get
            {
                return m_iCurrStateID;
            }
        }
        public void SetProperty(HierarchicalStateController oController, int iLevel)
        {
            m_StateController = oController;
            m_iLevel = iLevel;
        }

        public void TouchStateBegin()
        {
            m_bAtStateBegin = false;
        }

        protected void TransTo(IHierarchicalState oState)
        {
            m_StateController.TransTo(m_iLevel, oState);
        }

        protected void TransTo(int iLevel, IHierarchicalState oState)
        {
            m_StateController.TransTo(iLevel, oState);
        }

        protected void AddSubState(IHierarchicalState oState)
        {
            m_StateController.TransTo(m_iLevel + 1, oState);
        }

        /// <summary>
        /// 狀態起始
        /// </summary>
        public virtual void StateBegin() { }
        /// <summary>
        /// 狀態每frame更新
        /// </summary>
        public virtual void StateUpdate() { }
        /// <summary>
        /// 狀態結束
        /// </summary>
        public virtual void StateEnd() { }

        public override string ToString()
        {
            return string.Format("<IHState>" + this.GetType().Name);
        }
    }
}
