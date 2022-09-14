using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
}
