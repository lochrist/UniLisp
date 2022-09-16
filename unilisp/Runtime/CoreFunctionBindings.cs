using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UniLisp;
using UnityEngine;

public static class CoreFunctionBindings
{
    static void ValidateArgsCount(List<LispValue> args, int minArgs)
    {
        if (args.Count < minArgs)
            throw new LispRuntimeException($"Function needs at least {minArgs} arguments");
    }

    static void ValidateNumberArgs(List<LispValue> args, int minArgs)
    {
        ValidateArgsCount(args, minArgs);
        if (args.Any(v => v.type != LispType.Number))
            throw new LispRuntimeException("Not all arguments are number");
    }

    public static LispValue Add(LispContext ctx, List<LispValue> args)
    {
        ValidateNumberArgs(args, 2);
        var total = args.Select(v => v.floatValue).Aggregate((x, y) => x + y);
        return LispValue.Create(total);
    }

    public static LispValue Sub(LispContext ctx, List<LispValue> args)
    {
        ValidateNumberArgs(args, 2);
        var total = args.Select(v => v.floatValue).Aggregate((x, y) => x - y);
        return LispValue.Create(total);
    }

    public static LispValue Mult(LispContext ctx, List<LispValue> args)
    {
        ValidateNumberArgs(args, 2);
        var total = args.Select(v => v.floatValue).Aggregate((x, y) => x * y);
        return LispValue.Create(total);
    }

    public static LispValue Div(LispContext ctx, List<LispValue> args)
    {
        ValidateNumberArgs(args, 2);
        var total = args.Select(v => v.floatValue).Aggregate((x, y) => x / y);
        return LispValue.Create(total);
    }

    public static LispValue Greater(LispContext ctx, List<LispValue> args)
    {
        ValidateNumberArgs(args, 2);
        return LispValue.Create(args[0].floatValue > args[1].floatValue);
    }

    public static LispValue GreaterOrEqual(LispContext ctx, List<LispValue> args)
    {
        ValidateNumberArgs(args, 2);
        return LispValue.Create(args[0].floatValue >= args[1].floatValue);
    }

    public static LispValue Lower(LispContext ctx, List<LispValue> args)
    {
        ValidateNumberArgs(args, 2);
        return LispValue.Create(args[0].floatValue < args[1].floatValue);
    }

    public static LispValue LowerOrEqual(LispContext ctx, List<LispValue> args)
    {
        ValidateNumberArgs(args, 2);
        return LispValue.Create(args[0].floatValue <= args[1].floatValue);
    }

    public static LispValue Equal(LispContext ctx, List<LispValue> args)
    {
        ValidateNumberArgs(args, 2);
        return LispValue.Create(args.All(v => v.floatValue == args[0].floatValue));
    }

    public static Func<LispContext, List<LispValue>, LispValue> UnaryMathFunction(Func<float, float> func)
    {
        return (cxt, args) =>
        {
            ValidateNumberArgs(args, 1);
            return LispValue.Create(func(args[0].floatValue));
        };
    }

    public static LispValue IsNumber(LispContext ctx, List<LispValue> args)
    {
        if (args.Count < 1)
            throw new LispRuntimeException($"Needs 1 argument");
        return LispValue.Create(args[0].type == LispType.Number);
    }

    public static LispValue List(LispContext ctx, List<LispValue> args)
    {
        return LispValue.Create(args);
    }

    public static LispValue Car(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 1);
        if (args[0].type != LispType.List)
            throw new LispRuntimeException($"Non list argument");
        if (args[0].listValue.Count == 0)
            throw new LispRuntimeException($"Empty list");
        return args[0].listValue[0];
    }

    public static LispValue Cdr(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 1);
        if (args[0].type != LispType.List)
            throw new LispRuntimeException($"Non list argument");
        if (args[0].listValue.Count < 1)
            throw new LispRuntimeException($"No rest");
        return LispValue.Create(args[0].listValue.Skip(1).ToList());
    }

    public static LispValue Append(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 1);
        if (args.Any(v => v.type != LispType.List))
            throw new LispRuntimeException("Not all arguments are list");

        IEnumerable<LispValue> result = args[0].listValue;
        for (var i = 1; i < args.Count; ++i)
            result = result.Concat(args[i].listValue);
        return LispValue.Create(result.ToList());
    }

    public static LispValue Cons(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 2);
        if (args[1].type != LispType.List)
            throw new LispRuntimeException($"Non list argument");
        return LispValue.CreateList(args[0], args[1].listValue);
    }

    public static LispValue Length(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 1);
        if (args[0].type != LispType.List)
            throw new LispRuntimeException($"Non list argument");
        return LispValue.Create(args[0].listValue.Count);
    }

    public static LispValue IsList(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 1);
        return LispValue.Create(args[0].type == LispType.List);
    }

    public static LispValue IsSymbol(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 1);
        return LispValue.Create(args[0].type == LispType.Symbol);
    }

    public static LispValue IsString(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 1);
        return LispValue.Create(args[0].type == LispType.String);
    }

    public static LispValue IsBoolean(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 1);
        return LispValue.Create(args[0].type == LispType.Boolean);
    }

    public static LispValue IsNull(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 1);
        return LispValue.Create(args[0].type == LispType.Nil || (args[0].type == LispType.List && args[0].listValue.Count == 0));
    }

    public static LispValue Eval(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 1);
        var expanded = ctx.Expand(args[0]);
        var evalValue = ctx.Eval(expanded);
        return evalValue;
    }

    public static LispValue Eq(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 2);
        var v1 = args[0];
        var v2 = args[1];
        if (v1.type != v2.type)
            return LispValue.False;
        switch(v1.type)
        {
            case LispType.Number:
                return LispValue.Create(v1.floatValue == v2.floatValue);
            case LispType.Symbol:
                return LispValue.Create(v1.intValue == v2.intValue);
            case LispType.Boolean:
                return LispValue.Create(v1.intValue == v2.intValue);
            case LispType.List:
                return LispValue.Create(v1.listValue == v2.listValue);
        }
        
        return LispValue.Create(v1.objValue == v2.objValue);
    }

    public static LispValue Map(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 2);
        if (args[0].type != LispType.Procedure)
            throw new LispRuntimeException($"First argument to Map must be a procedure {args[0]}");
        if (args[1].type != LispType.List)
            throw new LispRuntimeException($"Second argument to Map must be a list {args[1]}");
        var proc = (Procedure)args[0].objValue;
        var result = LispValue.Create(args[1].listValue.Select(v => proc.Invoke(ctx, new List<LispValue>() { v })).ToList());
        return result;
    }

    public static LispValue Apply(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 2);
        if (args[0].type != LispType.Procedure)
            throw new LispRuntimeException($"First argument to Apply must be a procedure {args[0]}");
        if (args[1].type != LispType.List)
            throw new LispRuntimeException($"Second argument to Apply must be a list {args[1]}");
        var proc = (Procedure)args[0].objValue;
        return proc.Invoke(ctx, args[1].listValue);
    }

    public static LispValue While(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 2);

        var cond = args[0];
        var statements = args.Skip(1);
        var result = LispValue.Nil;
        while (LispContext.IsTruish(ctx.Eval(cond)))
        {
            foreach(var s in statements)
            {
                result = ctx.Eval(s);
            }
        }
        return result;
    }

    struct DelegateEntry
    {
        public Delegate d;
        public MethodInfo mi;
        public bool IsValid => d != null;
    }

    static Dictionary<string, DelegateEntry> s_DelegateCache = new Dictionary<string, DelegateEntry>();
    static Dictionary<string, LispValue> s_NativeProcedures = new Dictionary<string, LispValue>();

    private static DelegateEntry TryCreateEntry(string functionFullName, int functionArity)
    {
        if (!ReflectionUtils.ExtractTypeFromFunctionName(functionFullName, out var typeName, out var functionName))
        {
            throw new LispRuntimeException($"Cannot extract type from full function name {functionFullName}");
        }

        var assemblies = ReflectionUtils.GetValidAssemblies();
        foreach(var assembly in assemblies)
        {
            
            var mi = ReflectionUtils.GetFunctionFromAssembly(assembly, typeName, functionName, functionArity);
            if (mi != null)
            {
                var d = ReflectionUtils.CreateDelegate(mi);
                return new DelegateEntry()
                {
                    d = d,
                    mi = mi
                };
            }
        }
        return new DelegateEntry();
    }

    static DelegateEntry GetNativeFunction(string functionName, int arity = -1)
    {
        if (!s_DelegateCache.TryGetValue(functionName, out var entry))
        {
            entry = TryCreateEntry(functionName, arity);
            if (entry.IsValid)
                s_DelegateCache[functionName] = entry;
        }
        return entry;
    }

    static LispValue InvokeNativeFunction(DelegateEntry entry, List<LispValue> args)
    {
        try
        {
            var objList = args.Select(a => LispValue.ToObject(a)).ToArray();
            var result = entry.d.DynamicInvoke(objList);
            return LispValue.FromObject(result);
        }
        catch (Exception e)
        {
            var functionName = args[0].objValue.ToString();
            throw new LispRuntimeException($"Error while executing native function: {entry.mi.Name} => {e.Message}");
        }
    }

    public static bool ResolveNativeFunctionSymbol(LispContext ctx, string symbolName, out LispValue value)
    {
        if (!symbolName.StartsWith("#"))
        {
            value = new LispValue();
            return false;
        }
        symbolName = symbolName.Substring(1);
        return ResolveNativeFunctionSymbol(ctx, symbolName, -1, out value);
    }

    public static bool ResolveNativeFunctionSymbol(LispContext ctx, string symbolName, int arity, out LispValue value)
    {
        if (s_NativeProcedures.TryGetValue(symbolName, out value))
        {
            return true;
        }

        var entry = GetNativeFunction(symbolName, arity);
        if (entry.IsValid)
        {
            var proc = new Procedure((ctx, procArgs) =>
            {
                return InvokeNativeFunction(entry, procArgs);
            });
            value = LispValue.Create(proc);
            s_NativeProcedures[symbolName] = value;
            return true;
        }
        return false;
    }

    public static LispValue GetNativeFunction(LispContext ctx, List<LispValue> args)
    {
        ValidateArgsCount(args, 1);
        if (args[0].type != LispType.String && args[0].type != LispType.Symbol)
            throw new LispRuntimeException($"Function name must be a string or symbol: {args[0]}");
        var functionName = args[0].objValue.ToString();
        var arity = -1;
        if (args.Count > 1 && args[1].type == LispType.Number)
            arity = (int)args[1].floatValue;

        if (ResolveNativeFunctionSymbol(ctx, functionName, arity, out var value))
        {
            return value;
        }

        throw new LispRuntimeException($"Cannot find native function: {functionName}");
    }
}
