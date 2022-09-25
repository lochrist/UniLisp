using System.Collections;
using System.Collections.Generic;
using UniLisp;
using UnityEngine;

public static class UniLispGlobalContext
{
    private static LispContext m_GlobalContext;
    public static LispContext get
    {
        get
        {
            if (m_GlobalContext == null)
                m_GlobalContext = new LispContext();
            return m_GlobalContext;
        }
    }

    public static void Reset()
    {
        m_GlobalContext = null;
    }
}
