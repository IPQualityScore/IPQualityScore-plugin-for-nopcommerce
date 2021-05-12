﻿using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;

namespace Nop.Plugin.Misc.IPQualityScore.Areas.Admin.Infrastructure
{
    /// <summary>
    /// Represents a plugin dependency registrar for admin area
    /// </summary>
    public class DependencyRegistrar : IDependencyRegistrar
    {
        #region Methods

        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Container builder</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="config">Config</param>
        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order => 1;

        #endregion
    }
}
