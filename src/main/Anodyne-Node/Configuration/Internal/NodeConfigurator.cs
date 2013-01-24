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

namespace Kostassoid.Anodyne.Node.Configuration.Internal
{
    using Abstractions.DataAccess;
    using Abstractions.Dependency;
    using Abstractions.Dependency.Registration;
    using System.Reflection;
    using Common.Tools;
    using Subsystem;
    using System;

    internal class NodeConfigurator : INodeConfigurator, INodeConfiguratorEx
    {
        private readonly INode _node;
        private readonly NodeConfiguration _configuration;

        public INode Node { get { return _node; } }
        public NodeConfiguration Configuration { get { return _configuration; } }

        public NodeConfigurator(INode node)
        {
            _node = node;
            _configuration = _node.Configuration;

            RunIn(RuntimeMode.Production);
            DefineSystemNamespaceAs(DetectSystemNamespace());
        }

        private static string DetectSystemNamespace()
        {
            var assembly = Assembly.GetEntryAssembly(); // could be null during tests run
            return assembly != null ? assembly.FullName.Substring(0, assembly.FullName.IndexOfAny(new[] { '.', '-', '_' })) : "";
        }

        void INodeConfiguratorEx.SetContainerAdapter(IContainer container)
        {
            if (_configuration.Container != null)
                throw new InvalidOperationException("Container adapter is already set to " + _configuration.Container.GetType().Name);

            _configuration.Container = container;
        }

        public void RunIn(RuntimeMode runtimeMode)
        {
            _configuration.RuntimeMode = runtimeMode;
        }

        public void DefineSystemNamespaceAs(string systemNamespace)
        {
            _configuration.SystemNamespace = systemNamespace;

            //AssemblyPreloader.Preload(From.AllFilesIn(".").Where(f => f.Extension == ".dll" && f.Name.StartsWith(systemNamespace)));
        }

        private bool CanContinue(ConfigurationPredicate when)
        {
            return when == null || when(_configuration);
        }

        public void ConfigureUsing<TConfiguration>(ConfigurationPredicate when) where TConfiguration : IConfigurationAction
        {
            if (!CanContinue(when)) return;
            EnsureContainerIsSet();

            _configuration.Container.Put(
                Binding.For<IConfigurationAction>()
                .Use<TConfiguration>().With(Lifestyle.Unmanaged)
                .Named(GetTypeUniqueName<TConfiguration>("Configuration")));
        }

        public void ConfigureUsing(Action<NodeConfiguration> configurationAction, ConfigurationPredicate when)
        {
            if (!CanContinue(when)) return;
            EnsureContainerIsSet();

            _configuration.Container.Put(
                Binding.For<IConfigurationAction>()
                .Use(() => new ConfigurationActionWrapper(configurationAction))
                .With(Lifestyle.Unmanaged)
                .Named("Configuration-" + SeqGuid.NewGuid()));
        }

        private static string GetTypeUniqueName<T>(string prefix)
        {
            return prefix + "-" + typeof (T).Name;
        }

        public void OnStartupPerform<TStartup>(ConfigurationPredicate when) where TStartup : IStartupAction
        {
            if (!CanContinue(when)) return;
            EnsureContainerIsSet();

            _configuration.Container.Put(
                Binding.For<IStartupAction>()
                .Use<TStartup>()
                .With(Lifestyle.Singleton)
                .Named(GetTypeUniqueName<TStartup>("Startup")));
        }

        public void OnStartupPerform(Action<NodeConfiguration> startupAction, ConfigurationPredicate when)
        {
            if (!CanContinue(when)) return;
            EnsureContainerIsSet();

            _configuration.Container.Put(
                Binding.For<IStartupAction>()
                .Use(() => new StartupActionWrapper(startupAction))
                .With(Lifestyle.Unmanaged)
                .Named("Startup-" + SeqGuid.NewGuid()));
        }

        public void OnShutdownPerform<TShutdown>(ConfigurationPredicate when) where TShutdown : IShutdownAction
        {
            if (!CanContinue(when)) return;
            EnsureContainerIsSet();

            _configuration.Container.Put(
                Binding.For<IShutdownAction>()
                .Use<TShutdown>()
                .With(Lifestyle.Unmanaged)
                .Named(GetTypeUniqueName<TShutdown>("Shutdown")));
        }

        public void OnShutdownPerform(Action<NodeConfiguration> shutdownAction, ConfigurationPredicate when)
        {
            if (!CanContinue(when)) return;
            _configuration.Container.Put(
                Binding.For<IShutdownAction>()
                .Use(() => new ShutdownActionWrapper(shutdownAction))
                .With(Lifestyle.Unmanaged)
                .Named("Shutdown-" + SeqGuid.NewGuid()));
        }

        public void RegisterSubsystem<TSubsystem>() where TSubsystem : ISubsystem
        {
            _configuration.Container.Put(
                Binding.For<ISubsystem>()
                .Use<TSubsystem>()
                .With(Lifestyle.Unmanaged));
        }

        public DataAccessProviderSelector UseDataAccess(string name = "default")
        {
            EnsureContainerIsSet();
            return new DataAccessProviderSelector(name, Configuration.Container);
        }

        public void EnsureConfigurationIsValid()
        {
            EnsureContainerIsSet();

            //TODO: add more sophisticated checks
        }

        private void EnsureContainerIsSet()
        {
            if (_configuration.Container == null)
                throw new InvalidOperationException("Node container should be configured first");
        }
    }
}