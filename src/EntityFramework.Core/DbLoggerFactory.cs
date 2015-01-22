// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Framework.Logging;

namespace Microsoft.Data.Entity
{
    public class DbLoggerFactory : ILoggerFactory
    {
        public virtual ILoggerFactory LoggerFactory { get; }
        private bool _logAppData { get; set; }
        private DbContextService<IDbContextOptions> _contextService;

        public virtual bool LogAppData()
        {
            var service = _contextService.Service;

            return service?.LogAppData() ?? false;
        }

        public virtual ILogger Create(string name)
        {
            return new DbLogger(LoggerFactory.Create(name), LogAppData());
        }

        public virtual void AddProvider(ILoggerProvider provider)
        {
            LoggerFactory.AddProvider(provider);
        }

        public DbLoggerFactory([NotNull] ILoggerFactory loggerFactory, [NotNull] DbContextService<IDbContextOptions> contextService)
        {
            LoggerFactory = loggerFactory;
            _contextService = contextService;
        }
    }
}
