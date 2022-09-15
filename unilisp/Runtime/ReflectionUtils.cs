using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public static class ReflectionUtils
{
    // No cast necessary
    public static MethodInfo GetMethodInfo(Action action) => action.Method;
    public static MethodInfo GetMethodInfo<T>(Action<T> action) => action.Method;
    public static MethodInfo GetMethodInfo<T, U>(Action<T, U> action) => action.Method;
    public static MethodInfo GetMethodInfo<T, U, V>(Action<T, U, V> action) => action.Method;
    public static MethodInfo GetMethodInfo<TResult>(Func<TResult> fun) => fun.Method;
    public static MethodInfo GetMethodInfo<T, TResult>(Func<T, TResult> fun) => fun.Method;
    public static MethodInfo GetMethodInfo<T, U, TResult>(Func<T, U, TResult> fun) => fun.Method;
    public static MethodInfo GetMethodInfo<T, U, V, TResult>(Func<T, U, V, TResult> fun) => fun.Method;

    // Cast necessary
    public static MethodInfo GetMethodInfo(Delegate del) => del.Method;

    public static Delegate CreateDelegate(Action a)
    {
        var mi = GetMethodInfo(a);
        var d = Delegate.CreateDelegate(typeof(Action), mi);
        return d;
    }

    public static Delegate CreateDelegate<T>(Action<T> a)
    {
        var mi = GetMethodInfo<T>(a);
        var d = Delegate.CreateDelegate(typeof(Action<T>), mi);
        return d;
    }

    public static Delegate CreateDelegate<T, U>(Action<T, U> a)
    {
        var mi = GetMethodInfo<T, U>(a);
        var d = Delegate.CreateDelegate(typeof(Action<T, U>), mi);
        return d;
    }

    public static Delegate CreateDelegate<T, U, V>(Action<T, U, V> a)
    {
        var mi = GetMethodInfo<T, U, V>(a);
        var d = Delegate.CreateDelegate(typeof(Action<T, U, V>), mi);
        return d;
    }

    public static Delegate CreateDelegate<TResult>(Func<TResult> a)
    {
        var mi = GetMethodInfo(a);
        var d = Delegate.CreateDelegate(typeof(Func<TResult>), mi);
        return d;
    }

    public static Delegate CreateDelegate<T, TResult>(Func<T, TResult> a)
    {
        var mi = GetMethodInfo<T, TResult>(a);
        var d = Delegate.CreateDelegate(typeof(Func<T, TResult>), mi);
        return d;
    }

    public static Delegate CreateDelegate<T, U, TResult>(Func<T, U, TResult> a)
    {
        var mi = GetMethodInfo<T, U, TResult>(a);
        var d = Delegate.CreateDelegate(typeof(Func<T, U, TResult>), mi);
        return d;
    }

    public static Delegate CreateDelegate<T, U, V, TResult>(Func<T, U, V, TResult> a)
    {
        var mi = GetMethodInfo<T, U, V, TResult>(a);
        var d = Delegate.CreateDelegate(typeof(Func<T, U, V, TResult>), mi);
        return d;
    }

    public static Delegate CreateDelegate(MethodInfo mi)
    {
        Type delegateType = null;
        
        if (mi.ReturnType == typeof(void))
        {
            Type genericAction = null;
            switch(mi.GetParameters().Length)
            {
                case 0:
                    genericAction = typeof(Action);
                    break;
                case 1:
                    genericAction = typeof(Action<>);
                    break;
                case 2:
                    genericAction = typeof(Action<,>);
                    break;
                case 3:
                    genericAction = typeof(Action<,,>);
                    break;
                case 4:
                    genericAction = typeof(Action<,,,>);
                    break;
                default:
                    throw new UniLisp.LispRuntimeException($"Arity unsupported for {mi.Name}");
            }

            var paramTypes = mi.GetParameters().Select(p => p.ParameterType).ToArray();
            delegateType = genericAction.MakeGenericType(paramTypes);
        }
        else
        {
            Type genericFunc = null;
            switch (mi.GetParameters().Length)
            {
                case 0:
                    genericFunc = typeof(Func<>);
                    break;
                case 1:
                    genericFunc = typeof(Func<,>);
                    break;
                case 2:
                    genericFunc = typeof(Func<,,>);
                    break;
                case 3:
                    genericFunc = typeof(Func<,,,>);
                    break;
                case 4:
                    genericFunc = typeof(Func<,,,,>);
                    break;
                default:
                    throw new UniLisp.LispRuntimeException($"Arity unsupported for {mi.Name}");
            }

            var paramTypes = mi.GetParameters().Select(p => p.ParameterType).Concat(new[] { mi.ReturnType }).ToArray();
            delegateType = genericFunc.MakeGenericType(paramTypes);
        }

        try
        {
            return Delegate.CreateDelegate(delegateType, mi);
        }
        catch(System.Exception e)
        {
            throw new UniLisp.LispRuntimeException($"Cannot create Delegate for {mi.Name}");
        }
    }

    public static bool ExtractTypeFromFunctionName(string functionFullName, out string typeName, out string functionName)
    {
        typeName = null;
        functionName = null;
        var functionNameTokenIndex = functionFullName.LastIndexOf(".");
        if (functionNameTokenIndex == -1)
            return false;
        typeName = functionFullName.Substring(0, functionNameTokenIndex);
        functionName = functionFullName.Substring(functionNameTokenIndex + 1);
        return true;
    }

    private static readonly string[] s_IgnoredAssemblies =
    {
        "^UnityScript$", "^System$", "^mscorlib$", "^netstandard$",
        "^System\\..*", "^nunit\\..*", "^Microsoft\\..*", "^Mono\\..*", "^SyntaxTree\\..*"
    };

    public static IEnumerable<Assembly> GetValidAssemblies()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (IsIgnoredAssembly(assembly.GetName()))
                continue;
            yield return assembly;
        }
    }

    private static bool IsIgnoredAssembly(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        return s_IgnoredAssemblies.Any(candidate => Regex.IsMatch(name, candidate));
    }

    public static MethodInfo GetFunctionFromAssemblies(string typeName, string functionName, int arity)
    {
        var assemblies = GetValidAssemblies();
        foreach (var assembly in assemblies)
        {
            var mi = GetFunctionFromAssembly(assembly, typeName, functionName, arity);
            if (mi != null)
            {
                return mi;
            }
        }

        return null;
    }

    public static MethodInfo GetFunctionFromAssembly(Assembly assembly, string typeName, string functionName, int arity)
    {
        var bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var type = assembly.GetType(typeName, false, true);
        if (type == null)
            return null;
        var methods = type.GetMethods(bindingFlags);
        foreach (var m in methods)
        {
            if (m.IsGenericMethod)
                continue;

            if (m.GetCustomAttribute<ObsoleteAttribute>() != null)
                continue;

            if (m.Name.Contains("Begin") || m.Name.Contains("End"))
                continue;

            if (m.Name == functionName && m.GetParameters().Length == arity)
                return m;
        }
        return null;
    }
}
