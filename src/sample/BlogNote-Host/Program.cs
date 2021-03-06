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

namespace Kostassoid.BlogNote.Host
{
    using System.Collections.Specialized;
    using Common.Logging;
    using Common.Logging.Log4Net;
    using Topshelf;

    class Program
    {
        static void Main(string[] args)
        {
            LogManager.Adapter = new Log4NetLoggerFactoryAdapter(
                new NameValueCollection { { "configType", "FILE-WATCH" }, { "configFile", "log4net.config" } });

            var logger = LogManager.GetLogger(typeof(Program));

            const string serviceName = Const.ProjectName + "-Host";

            var h = HostFactory.New(x =>
            {
                x.UseLog4Net();

                x.Service<BlogNoteHost>(s =>
                {
                    s.BeforeStartingService(c => logger.InfoFormat("Staring service {0}...", serviceName));
                    s.AfterStartingService(c => logger.InfoFormat("Service {0} started.", serviceName));
                    s.AfterStoppingService(c => logger.InfoFormat("Service {0} stopped.", serviceName));

                    s.ConstructUsing(name => new BlogNoteHost());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Shutdown());
                });

                x.SetServiceName(serviceName);
                x.RunAsNetworkService();

                x.SetDescription(Const.ProjectName + " Host Service");
                x.SetDisplayName(serviceName);
                x.SetServiceName(serviceName);
            });

            h.Run();

        }
    }
}
