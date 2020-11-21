﻿
namespace Scorpio.BackgroundJobs
{
    /// <summary>
    /// Some extension methods for <see cref="IBackgroundJobManager"/>.
    /// </summary>
    public static class BackgroundJobManagerExtensions
    {
        /// <summary>
        /// Checks if background job system has a real implementation.
        /// It returns false if the current implementation is <see cref="NullBackgroundJobManager"/>.
        /// </summary>
        /// <param name="backgroundJobManager"></param>
        /// <returns></returns>
        public static bool IsAvailable(this IBackgroundJobManager backgroundJobManager) => backgroundJobManager is not NullBackgroundJobManager;
    }
}