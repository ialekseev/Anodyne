// Copyright 2011-2013 Anodyne.
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

namespace Kostassoid.BlogNote.Web.Code
{
    using Anodyne.Common.Reflection;
    using Anodyne.Node.Configuration;
    using Anodyne.Web.Mvc;
    using Anodyne.Windsor;
    using Anodyne.MongoDb;
    using Castle.Windsor.Installer;

    public class BlogNoteWebNode : MvcNode
    {
        public override void OnConfigure(IConfiguration c)
        {
            c.UseWindsorContainer();
            c.UseWindsorWcfServicePublisher();
            c.UseMongoDataAccess(Configured.From.AppSettings("DatabaseServer", "DatabaseName"));
            c.UseDataAccessPolicy(p => p.ReadOnlyAccess());
            c.ResolveControllersFromContainer();
            c.RegisterControllers(From.ThisAssembly);

            c.ConfigureUsing(n => n.Container.OnNative(container => container.Install(FromAssembly.This())));

            c.OnStartupPerform<WcfClientsRegistration>();
        }
    }
}