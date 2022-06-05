﻿using System.IO;
using System.Linq;

using Microsoft.Extensions.Hosting;

namespace Scorpio.AspNetCore.Mvc
{
    public abstract class AspNetCoreMvcTestBase : AspNetCoreTestBase<TestModule, Startup>
    {
        protected override IHostBuilder CreateHostBuilder()
        {


            return base.CreateHostBuilder();
        }

        private static string CalculateContentRootPath(string projectFileName, string contentPath)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            while (!ContainsFile(currentDirectory, projectFileName))
            {
                currentDirectory = new DirectoryInfo(currentDirectory).Parent.FullName;
            }

            return Path.Combine(currentDirectory, contentPath);
        }

        private static bool ContainsFile(string currentDirectory, string projectFileName)
        {
            return Directory
                .GetFiles(currentDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Any(f => Path.GetFileName(f) == projectFileName);
        }
    }
}
