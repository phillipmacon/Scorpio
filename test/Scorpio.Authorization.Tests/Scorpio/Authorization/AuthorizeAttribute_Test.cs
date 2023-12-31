﻿using Microsoft.Extensions.DependencyInjection;

using Scorpio.TestBase;

using Shouldly;

using Xunit;

namespace Scorpio.Authorization
{
    public class AuthorizeAttribute_Test : IntegratedTest<AuthorizationTestModule>
    {

        protected override void SetBootstrapperCreationOptions(BootstrapperCreationOptions options)
        {
            options.UseAspectCore();
            options.ConfigureServices(c => c.Services.AddTransient<IAuthorizeAttributeTestService, AuthorizeAttributeTestService>());
            base.SetBootstrapperCreationOptions(options);
        }

        [Fact]
        public void AuthorizeService()
        {
            var service = ServiceProvider.GetService<IAuthorizeAttributeTestService>();
            service.AuthorizeByServcieAsync().ShouldThrow<AuthorizationException>();
        }

        [Fact]
        public void AuthorizeByNotAllAttributeAsync()
        {
            var service = ServiceProvider.GetService<IAuthorizeAttributeTestService>();
            service.AuthorizeByNotAllAttributeAsync().ShouldThrow<AuthorizationException>();
        }

        [Fact]
        public void AuthorizeByApplyAttributeAsync()
        {
            var service = ServiceProvider.GetService<IAuthorizeAttributeTestService>();
            service.AuthorizeByNotAllAttributeAsync().ShouldThrow<AuthorizationException>();
            using (Aspects.CrossCuttingConcerns.Applying(service, AuthorizationInterceptor.Concern))
            {
                service.AuthorizeByAllAttributeAsync().ShouldNotThrow();
            }
            service.AuthorizeByNotAllAttributeAsync().ShouldThrow<AuthorizationException>();
        }

        [Fact]
        public void AuthorizeByAllAttributeAsync()
        {
            var service = ServiceProvider.GetService<IAuthorizeAttributeTestService>();
            service.AuthorizeByAllAttributeAsync().ShouldNotThrow();
        }

        [Fact]
        public void AuthorizeNotPermission()
        {
            var service = ServiceProvider.GetService<IAuthorizeAttributeTestService>();
            service.AuthorizeByAttributeAsync().ShouldNotThrow();
        }
        [Fact]
        public void AuthorizeAnonymous()
        {
            var service = ServiceProvider.GetService<IAuthorizeAttributeTestService>();
            service.AuthorizeAnonymousAsync().ShouldNotThrow();
        }

    }
}
