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

namespace Kostassoid.Anodyne.Node.Configuration
{
    using System;
    using Anodyne.DataAccess;
    using Dependency;
    using Logging;
    using Wcf;

    public interface INodeInstance
    {
        RuntimeMode RuntimeMode { get; }
        IContainer Container { get; }
        ILoggerAdapter Logger { get; }
        IWcfAdapter WcfAdapter { get; }
        IDataAccessProvider DataAccess { get; }
        string SystemNamespace { get; }

        event Action OnContainerReady;
    }
}