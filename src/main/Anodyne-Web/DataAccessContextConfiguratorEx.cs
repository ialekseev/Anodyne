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

namespace Kostassoid.Anodyne.Web
{
    using System;
    using Abstractions.DataAccess;

    public static class DataAccessContextConfiguratorEx
    {
        public static void BoundToWebRequest(this DataAccessContextConfigurator cc)
        {
            var node = Node.Node.Current;
            if (!(node is WebNode))
            {
                throw new InvalidOperationException(string.Format("Expected Node to be of WebNode type but was {0}", node.GetType().Name));
            }

            var container = node.Configuration.Container;
            ((WebNode)node).EndRequest += () =>
            {
                if (container.Has<IDataAccessContext>())
                    container.Get<IDataAccessContext>().CloseSession();
            };
        }
    }
}