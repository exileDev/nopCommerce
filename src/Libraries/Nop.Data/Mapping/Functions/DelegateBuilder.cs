using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Nop.Data.Mapping.Functions
{
    public class DelegateBuilder
{
    public static Expression<Func<TResult>> BuildDelegate<TResult>(MethodInfo method, params object[] missingParamValues)
    {
        var queueMissingParams = new Queue<object>(missingParamValues);

        var dgtRet = method.ReturnType;
        var dgtParams = method.GetParameters();

        var paramsOfDelegate = dgtParams
            .Select(tp => Expression.Parameter(tp.ParameterType, tp.Name))
            .ToArray();

        var methodParams = method.GetParameters();

        if (method.IsStatic)
        {
            var paramsToPass = methodParams
                .Select((p, i) => CreateParam(paramsOfDelegate, i, p, queueMissingParams))
                .ToArray();

            return Expression.Lambda<Func<TResult>>(Expression.Call(method, paramsToPass));
        }
        else
        {
            var paramThis = Expression.Convert(paramsOfDelegate[0], method.DeclaringType);

            var paramsToPass = methodParams
                .Select((p, i) => CreateParam(paramsOfDelegate, i + 1, p, queueMissingParams))
                .ToArray();

            return Expression.Lambda<Func<TResult>>(Expression.Call(paramThis, method, paramsToPass));
        }
    }

    private static Expression CreateParam(ParameterExpression[] paramsOfDelegate, int i, ParameterInfo callParamType, Queue<object> queueMissingParams)
    {
        if (i < paramsOfDelegate.Length)
            return Expression.Convert(paramsOfDelegate[i], callParamType.ParameterType);

        if (queueMissingParams.Count > 0)
            return Expression.Constant(queueMissingParams.Dequeue());

        if (callParamType.ParameterType.IsValueType)
            return Expression.Constant(Activator.CreateInstance(callParamType.ParameterType));

        return Expression.Constant(null);
    }
}
}