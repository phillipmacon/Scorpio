﻿using System;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Scorpio.Modularity;

namespace Scorpio.AspNetCore.Mvc
{
    /// <summary>
    /// 
    /// </summary>
    [DependsOn(typeof(AspNetCoreModule))]
    public sealed class AspNetCoreMvcModule : ScorpioModule
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void PreConfigureServices(ConfigureServicesContext context)
        {
            context.Services.AddConventionalRegistrar<AspNetCoreConventionalRegistrar>();
            base.PreConfigureServices(context);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void ConfigureServices(ConfigureServicesContext context)
        {
            context.Services.Options<MvcOptions>().PreConfigure<IServiceProvider>(
                (options, serviceProvider) => options.AddScorpio());
            //context.Services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
            //Use DI to create controllers
            //context.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());
            //context.Services.Replace(ServiceDescriptor.Transient<IPageModelActivatorProvider, ServiceBasedPageModelActivatorProvider>());
            ////Use DI to create view components
            //context.Services.Replace(ServiceDescriptor.Singleton<IViewComponentActivator, ServiceBasedViewComponentActivator>());
            //context.Services.AddControllersWithViews();

        }
    }
}
