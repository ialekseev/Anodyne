﻿// Copyright 2011-2013 Anodyne.
//   
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
//  
//      http://www.apache.org/licenses/LICENSE-2.0 
//  
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.

using Kostassoid.Anodyne.Common.Reflection;

namespace Kostassoid.Anodyne.Wiring.Subscription
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Common.CodeContracts;
    using Internal;

    using Common.Extentions;

    internal static class SubscriptionPerformer
    {
        public static Action Perform<TEvent>(SubscriptionSpecification<TEvent> specification) where TEvent : class, IEvent
        {
            Requires.NotNull(specification, "specification");
            Requires.True(specification.IsValid, "specification", "Invalid specification");

            Action unsubscribeAction = () => { };

            foreach (var source in ResolveSourceTypes(specification))
            {
                if (specification.TargetDiscoveryFunction == null)
                {
                    unsubscribeAction += Subscribe(source, specification.HandlerAction, specification);
                }
                else
                {
                    unsubscribeAction = ResolveHandlers(source, specification)
                        .Aggregate(unsubscribeAction, (current, target) => current + Subscribe(source, target, specification));
                }
            }

            return unsubscribeAction;
        }

        private static Action Subscribe<TEvent>(Type source, Action<TEvent> target, SubscriptionSpecification<TEvent> specification) where TEvent : class, IEvent
        {
            return specification
                .EventAggregator
                .Subscribe(
                new InternalEventHandler<TEvent>(source, target, specification.EventPredicate, specification.Priority, specification.Async));
        }

        private static IEnumerable<Type> ResolveSourceTypes<TEvent>(SubscriptionSpecification<TEvent> specification) where TEvent : class, IEvent
        {
            return specification.IsPolymorphic
                              ? FindTypes(specification.BaseEventType, specification.SourceAssemblies, specification.TypePredicate)
                              : specification.BaseEventType.AsEnumerable();
        }

        private static IEnumerable<Action<TEvent>> ResolveHandlers<TEvent>(Type eventType, SubscriptionSpecification<TEvent> specification) where TEvent : class, IEvent
        {
            var methods = specification.TargetType
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi => IsTargetCompatibleWithSource(mi, eventType, specification.EventMatching))
                .ToList();

            if (methods.Count == 0) yield break;

            foreach (var method in methods)
            {
                var handlerDelegate = CreateHandlerDelegate(method, eventType);

                Action<TEvent> handler = ev =>
                {
                    var targetObject = specification.TargetDiscoveryFunction(ev);
                    if (targetObject.IsNone) return;

                    handlerDelegate(targetObject.Value, ev);
                };

                yield return handler;
            }
        }

        delegate void HandlerMethodDelegate(object instance, IEvent @event);

        private static HandlerMethodDelegate CreateHandlerDelegate(MethodInfo methodInfo, Type eventType)
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var ev = Expression.Parameter(typeof(IEvent), "event");

            var lambda = Expression.Lambda<HandlerMethodDelegate>(
                Expression.Call(
                    Expression.Convert(instance, methodInfo.DeclaringType),
                    methodInfo,
                    Expression.Convert(ev, eventType)
                    ),
                instance,
                ev
                );

            return lambda.Compile();
        }

        private static bool IsTargetCompatibleWithSource(MethodInfo methodInfo, Type eventType, EventMatching eventMatching)
        {
            if (methodInfo.ReturnType != typeof(void)) return false;

            var parameters = methodInfo.GetParameters();
            if (parameters.Length != 1) return false;

            switch (eventMatching)
            {
                case EventMatching.Strict:
                    return parameters[0].ParameterType == eventType;
                case EventMatching.All:
                    return parameters[0].ParameterType.IsAssignableFrom(eventType);
                default:
                    throw new NotSupportedException(string.Format("EventMatching.{0} is not supported", eventMatching));
            }
        }

        private static IEnumerable<Type> FindTypes(Type baseEventType, IEnumerable<Assembly> assemblies, Predicate<Type> typePredicate)
        {
            return AllTypes.BasedOn(baseEventType, assemblies).Where(t => typePredicate(t));
        }
    }
}