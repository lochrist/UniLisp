using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class ReflectionUtilsTests
{
    public class DelegateTestEntry
    {
        public string methodName;
        public int methodArity;

        public DelegateTestEntry(string name, int arity)
        {
            methodName = name;
            methodArity = arity;
        }
    }

    static DelegateTestEntry[] s_CreateDelegateFromNameTests =
    {
        new DelegateTestEntry("UnityEngine.Debug.Log", 1),
        new DelegateTestEntry("UnityEngine.GameObject.Find", 1),
    };

    [Test]
    public void CreateDelegateFromMethodNameTests([ValueSource(nameof(s_CreateDelegateFromNameTests))] DelegateTestEntry t)
    {
        var result = ReflectionUtils.ExtractTypeFromFunctionName(t.methodName, out var typeName, out var functionName);
        Assert.IsTrue(result);
        var mi = ReflectionUtils.GetFunctionFromAssemblies(typeName, functionName, t.methodArity);
        Assert.NotNull(mi);
        var d = ReflectionUtils.CreateDelegate(mi);
        Assert.NotNull(d);
    }

    static MethodInfo[] s_CreateDelegateMethodTests =
    {
        ReflectionUtils.GetMethodInfo((Func<string, GameObject>)UnityEngine.GameObject.Find),
        ReflectionUtils.GetMethodInfo((Action<object>)UnityEngine.Debug.Log),
        ReflectionUtils.GetMethodInfo((Func<SelectionMode, Transform[]>)UnityEditor.Selection.GetTransforms)
    };

    [Test]
    public void CreateDelegateTests([ValueSource(nameof(s_CreateDelegateMethodTests))] MethodInfo mi)
    {
        var d = ReflectionUtils.CreateDelegate(mi);
        Assert.NotNull(d);
    }
}
