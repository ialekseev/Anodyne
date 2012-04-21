using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Kostassoid.Anodyne.Common
{
    public static class ObjectFactory
    {
        private readonly static object Locker = new object();

        private delegate object ConstructionInvoker();

        private static readonly IDictionary<Type, ConstructionInvoker> ConstructionInvokers = new ConcurrentDictionary<Type, ConstructionInvoker>();

        public static object Build(Type type)
        {
            //lock (Locker)
            {
                ConstructionInvoker invoker;

                if (ConstructionInvokers.TryGetValue(type, out invoker)) return invoker();

                var constructorInfo = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new Type[] {}, null);
                if (constructorInfo == null)
                    throw new InvalidOperationException(String.Format("Parameter-less constructor needed for {0}", type.FullName));

                invoker = CreateMethod(constructorInfo);
                ConstructionInvokers[type] = invoker;

                return invoker();

            }
        }

        public static T Build<T>() where T : class
        {
            return (T) Build(typeof(T));
        }

        private static ConstructionInvoker CreateMethod(ConstructorInfo target)
        {
            var dynamic = new DynamicMethod(string.Empty,
                        typeof(object),
                        new Type[0],
                        target.DeclaringType);
            var il = dynamic.GetILGenerator();
            il.DeclareLocal(target.DeclaringType);
            il.Emit(OpCodes.Newobj, target);
            il.Emit(OpCodes.Stloc_0); // without this code may crash at runtime in release mode
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            return (ConstructionInvoker)dynamic.CreateDelegate(typeof(ConstructionInvoker));
        }
    }
}