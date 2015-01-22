// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Logging;
using System;
using JetBrains.Annotations;

namespace Microsoft.Data.Entity
{
    public class DbLogger : ILogger
    {
        public virtual  ILogger WrappedLogger { get; }
        public virtual bool LogAppDataDbLogger { get; set; }

        public virtual void Write(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            WrappedLogger.Write(logLevel, eventId, state, exception, formatter);
        }

        public virtual bool IsEnabled(LogLevel logLevel)
        {
            return WrappedLogger.IsEnabled(logLevel);
        }

        public virtual IDisposable BeginScope(object state)
        {
            return WrappedLogger.BeginScope(state);
        }

        public DbLogger([NotNull] ILogger logger, bool logAppData)
        {
            WrappedLogger = logger;
            LogAppDataDbLogger = logAppData;
        }
    }
}
