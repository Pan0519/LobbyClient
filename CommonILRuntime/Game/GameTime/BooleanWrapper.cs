using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniRx;
public class BooleanWrapper : IYieldWrapper
{
    Func<bool> m_isFinished;
    public bool finished
    {
        get
        {
            return m_isFinished();
        }
    }

    public BooleanWrapper(Func<bool> predicate)
    {
        m_isFinished = predicate;
    }
}

