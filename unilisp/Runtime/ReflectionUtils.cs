using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
}
