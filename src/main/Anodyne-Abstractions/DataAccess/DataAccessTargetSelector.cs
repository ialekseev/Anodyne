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

namespace Kostassoid.Anodyne.Abstractions.DataAccess
{
    using System;
    using Dependency;
    using Dependency.Registration;

    public class DataAccessTargetSelector
    {
        public DataAccessProviderSelector Selector { get; private set; }
        public IDataAccessProvider DataProvider { get; private set; }

        public DataAccessTargetSelector(DataAccessProviderSelector selector, IDataAccessProvider dataProvider)
        {
            Selector = selector;
            DataProvider = dataProvider;
        }

        /// <summary>
        /// Use injectable low-level DataAccessContext to work with persistable objects.
        /// </summary>
        /// <param name="cc">Optional configurator.</param>
        public void AsInjectedContext(Action<DataAccessContextConfigurator> cc = null)
        {
            if (Selector.Container.Has<IDataAccessContext>())
                throw new InvalidOperationException("Only one DataAccessContext is allowed.");

            Selector.Container.Put(
                Binding.For<IDataAccessContext>()
                .Use(() => new DefaultDataAccessContext(DataProvider))
                .With(Lifestyle.Singleton));

            if (cc != null)
                cc(new DataAccessContextConfigurator());
        }
    }
}