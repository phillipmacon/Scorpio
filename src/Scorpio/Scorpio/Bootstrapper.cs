﻿using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Scorpio.DependencyInjection;
using Scorpio.Localization;
using Scorpio.Modularity;

namespace Scorpio
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class Bootstrapper : IBootstrapper, IModuleContainer, IServiceProviderAccessor
    {
        private bool _isShutdown = false;
        private readonly Lazy<IServiceFactoryAdapter> _serviceFactory;

        private readonly BootstrapperCreationOptions _options;

        /// <summary>
        /// 
        /// </summary>
        public Type StartupModuleType { get; }


        /// <summary>
        /// 
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// The <see cref="IConfiguration" /> containing the merged configuration of the application.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// A central location for sharing state between components during the host building process.
        /// </summary>
        public IDictionary<string, object> Properties { get; }


        /// <summary>
        /// 
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<IModuleDescriptor> Modules { get; }

        /// <summary>
        /// 
        /// </summary>
        protected internal IModuleLoader ModuleLoader { get; }

        /// <summary>
        /// 
        /// </summary>
        internal IServiceFactoryAdapter ServiceFactoryAdapter => _serviceFactory.Value;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        protected IServiceProvider CreateServiceProvider(IServiceCollection services)
        {
            var containerBuilder = ServiceFactoryAdapter.CreateBuilder(services);

            return ServiceFactoryAdapter.CreateServiceProvider(containerBuilder);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startupModuleType"></param>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="optionsAction"></param>
        protected Bootstrapper(Type startupModuleType, IServiceCollection services, IConfiguration configuration, Action<BootstrapperCreationOptions> optionsAction)
        {
            Services = services;
            StartupModuleType = startupModuleType;
            Properties = new Dictionary<string, object>();
            _options = new BootstrapperCreationOptions();
            ModuleLoader = new ModuleLoader();
            optionsAction(_options);
            _serviceFactory = new Lazy<IServiceFactoryAdapter>(() => _options.ServiceFactory());
            var configBuilder = new ConfigurationBuilder();
            if (configuration != null)
            {
                configBuilder.AddConfiguration(configuration);
            }
            _options.ConfigureConfiguration(configBuilder);
            Configuration = configBuilder.Build();
            ConfigureCoreService(services);
            Modules = LoadModules();
            ConfigureServices();
        }

        private void ConfigureCoreService(IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton<IBootstrapper>(this);
            services.AddSingleton<IModuleContainer>(this);
            services.AddSingleton(ModuleLoader);
            services.AddSingleton<IModuleManager, ModuleManager>();
        }

        private void ConfigureServices()
        {
            var context = new ConfigureServicesContext(this, Services, Configuration);
            Services.AddSingleton(context);
            Modules.ForEach(m => m.Instance.PreConfigureServices(context));
            _options.PreConfigureServices(context);
            Modules.ForEach(m =>
            {
                if (m.Instance is ScorpioModule module && !module.SkipAutoServiceRegistration)
                {
                    Services.RegisterAssemblyByConvention(m.Type.Assembly);
                }
                m.Instance.ConfigureServices(context);
            });
            _options.ConfigureServices(context);
            Modules.ForEach(m => m.Instance.PostConfigureServices(context));
            _options.PostConfigureServices(context);
        }

        private IReadOnlyList<IModuleDescriptor> LoadModules() => ModuleLoader.LoadModules(Services, StartupModuleType, _options.PlugInSources);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        protected internal void SetServiceProvider(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;



        /// <summary>
        /// 
        /// </summary>
        public virtual void Initialize(params object[] initializeParams) => InitializeModules(initializeParams);

        /// <summary>
        /// 
        /// </summary>
        protected virtual void InitializeModules(params object[] initializeParams)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                scope.ServiceProvider
                    .GetRequiredService<IModuleManager>()
                    .InitializeModules(new ApplicationInitializationContext(scope.ServiceProvider, initializeParams));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void Shutdown()
        {
            if (_isShutdown)
            {
                return;
            }
            _isShutdown = true;
            using (var scope = ServiceProvider.CreateScope())
            {
                scope.ServiceProvider
                    .GetRequiredService<IModuleManager>()
                    .ShutdownModules(new ApplicationShutdownContext(scope.ServiceProvider));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TStartupModule"></typeparam>
        /// <param name="optionsAction"></param>
        /// <returns></returns>
        public static IBootstrapper Create<TStartupModule>(Action<BootstrapperCreationOptions> optionsAction) where TStartupModule : IScorpioModule => Create(typeof(TStartupModule), optionsAction);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="optionsAction"></param>
        /// <param name="startupModuleType"></param>
        /// <returns></returns>
        public static IBootstrapper Create(Type startupModuleType, Action<BootstrapperCreationOptions> optionsAction)
        {
            if (!startupModuleType.IsAssignableTo<IScorpioModule>())
            {
                throw new ArgumentException($"{nameof(startupModuleType)} should be derived from {typeof(IScorpioModule)}");
            }
            var services = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            var config = configBuilder.Build();
            var bootstrapper = new InternalBootstrapper(startupModuleType, services, config, optionsAction);
            bootstrapper.SetServiceProvider(bootstrapper.CreateServiceProvider(services));
            return bootstrapper;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startupModuleType"></param>
        /// <returns></returns>
        public static IBootstrapper Create(Type startupModuleType) => Create(startupModuleType, o => { });

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TStartupModule"></typeparam>
        /// <returns></returns>
        public static IBootstrapper Create<TStartupModule>() where TStartupModule : IScorpioModule => Create(typeof(TStartupModule));


        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Shutdown();
                }

                _disposedValue = true;
            }
        }


        // This code added to correctly implement the disposable pattern.
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);

            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
