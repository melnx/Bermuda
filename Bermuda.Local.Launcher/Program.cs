using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net;
using Bermuda.Core;
using System.Collections.Concurrent;
using System.Threading;
using System.Reflection;
using System.Linq.Expressions;

namespace Bermuda.Local.Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Listeners.Add(new ConsoleTraceListener());      

            Console.WriteLine("Loading configuration information...");
            var config = new LocalHostEnvironmentConfiguration();
            Console.WriteLine();
            Console.WriteLine(config.ToString());
            Console.WriteLine();

            HostEnvironment.Instance.Initialize(config);

            Console.WriteLine();
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }

        //class MyObject
        //{
        //    public Int64 id;
        //}

        //static ConcurrentDictionary<Int64, MyObject> dic = new ConcurrentDictionary<Int64, MyObject>();
        //static ConcurrentDictionary<Int64, MyObject> dic2 = new ConcurrentDictionary<Int64, MyObject>();

        //static void SomeThing()
        //{
            
        //    for (Int64 x = 0; x < 1000; x++)
        //    {
        //        MyObject o = new MyObject() { id = x };
        //        //dic.AddOrUpdate(x, o, (i, j) => j);
        //        AddOrUpdate(dic, x, o);
        //        AddOrUpdate(dic2, x, o);
        //        //o = null;
        //    }
        //    for (Int64 x = 0; x < 1000; x++)
        //    {
        //        object o;
        //        TryRemove(dic, x, typeof(MyObject), out o);
        //        TryRemove(dic2, x, typeof(MyObject), out o);
        //        //o = null;
        //    }
        //    GC.Collect(GC.MaxGeneration);
        //    GC.WaitForPendingFinalizers();
        //    Console.WriteLine("Done");
        //    Thread.Sleep(1000000000);
        //}

        ///// <summary>
        ///// add or update reflection wrapper
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="item"></param>
        ///// <returns></returns>
        //static object AddOrUpdate(object dictionary, object key, object item)
        //{
        //    //get the types
        //    var itemType = item.GetType();
        //    var keyType = key.GetType();

        //    //call through reflection
        //    MethodInfo mi = GetAddOrUpdateMethod(keyType, itemType);
        //    Delegate d = GetAddOrUpdateDelegate(keyType, itemType);
        //    object[] args = new object[] { key, item, d };
        //    object ret = mi.Invoke(dictionary, args);
        //    return ret;
        //}

        //static bool TryRemove(object dictionary, object key, Type itemType, out object item)
        //{
        //    //init out variable
        //    item = null;

        //    //get the types
        //    var keyType = key.GetType();

        //    //call through reflection
        //    MethodInfo mi = GetTryRemoveMethod(keyType, itemType);
        //    object[] args = new object[] { key, item };
        //    var ret = mi.Invoke(dictionary, args);
        //    item = args[1];
        //    return (bool)ret;
        //}

        //static Delegate GetAddOrUpdateDelegate(Type keyType, Type itemType)
        //{
        //    Delegate exp;
        //    //if (!AddOrUpdateDelegateCache.TryGetValue(itemType, out exp))
        //    {
        //        var xParam = Expression.Parameter(keyType, "x");
        //        var yParam = Expression.Parameter(itemType, "y");

        //        var funcType = typeof(Func<,,>).MakeGenericType(keyType, itemType, itemType);

        //        var updateValueFactoryExpr = Expression.Lambda
        //        (
        //            delegateType: funcType,
        //            parameters: new ParameterExpression[] { xParam, yParam },
        //            body: yParam
        //        );

        //        exp = updateValueFactoryExpr.Compile();

        //        //AddOrUpdateDelegateCache.AddOrUpdate(itemType, exp, (x, y) => y);
        //    }
        //    return exp;
        //}

        ///// <summary>
        ///// get the method info for add or update
        ///// </summary>
        ///// <param name="keyType"></param>
        ///// <param name="itemType"></param>
        ///// <returns></returns>
        //static MethodInfo GetAddOrUpdateMethod(Type keyType, Type itemType)
        //{
        //    MethodInfo mi;
        //    //if (!AddOrUpdateMethodCache.TryGetValue(itemType, out mi))
        //    {
        //        mi = dic.GetType().GetMethods().Where(x =>
        //            x.Name == "AddOrUpdate" &&
        //            x.GetParameters().Length == 3 &&
        //            x.GetParameters()[0].ParameterType == keyType &&
        //            x.GetParameters()[1].ParameterType == itemType
        //        ).FirstOrDefault();

        //        //AddOrUpdateMethodCache.AddOrUpdate(itemType, mi, (x, y) => y);
        //    }
        //    return mi;
        //}

        //static MethodInfo GetTryRemoveMethod(Type keyType, Type itemType)
        //{
        //    MethodInfo mi;
        //    //if (!TryRemoveMethodCache.TryGetValue(itemType, out mi))
        //    {
        //        mi = dic.GetType().GetMethods().Where(x =>
        //            x.Name == "TryRemove" &&
        //            x.GetParameters().Length == 2 &&
        //            x.GetParameters()[0].ParameterType == keyType &&
        //            x.GetParameters()[1].IsOut &&
        //            x.GetParameters()[1].ParameterType.FullName.Replace("&", "") == itemType.FullName
        //        ).FirstOrDefault();

        //        //TryRemoveMethodCache.AddOrUpdate(itemType, mi, (x, y) => y);
        //    }
        //    return mi;
        //}
    }
}
