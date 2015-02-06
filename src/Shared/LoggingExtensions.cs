// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Utilities;
using System;

// ReSharper disable once CheckNamespace

namespace Microsoft.Framework.Logging
{
    internal static class LoggingExtensions
    {
        public static void WriteInformation(this ILogger logger, Func<string> formatter)
        { // not known
            logger.WriteInformation(0, default(object), _ => formatter());
        }

        public static void WriteInformation<TState>(this ILogger logger, TState state, Func<TState, string> formatter)
        { // not known
            logger.WriteInformation(0, state, formatter);
        }

        public static void WriteInformation(this ILogger logger, int eventId, Func<string> formatter)
        { // not known
            logger.WriteInformation(eventId, default(object), _ => formatter());
        }

        public static void WriteInformation<TState>(
            this ILogger logger, int eventId, TState state, Func<TState, string> formatter)
        {
            if (logger.IsEnabled(LogLevel.Information))
            { // not known
                logger.Write(LogLevel.Information, eventId, state, null, (s, _) => formatter((TState)s));
            }
        }

        public static void WriteError<TState>(
            this ILogger logger, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logger.IsEnabled(LogLevel.Error))
            { // not known
                logger.Write(LogLevel.Error, 0, null, exception, (s, e) => formatter((TState)s, e));
            }
        }

        public static void WriteError<TState>(
            this ILogger logger, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logger.IsEnabled(LogLevel.Error))
            { // not known
                logger.Write(LogLevel.Error, 0, state, exception, (s, e) => formatter((TState)s, e));
            }
        }

        public static void WriteVerbose<TState>(this ILogger logger, int eventId, TState state)
        {
            logger.WriteVerbose(eventId, state, s => s != null ? s.ToString() : null);
        }

        public static void WriteVerbose<TState>(
            this ILogger logger, TState state, Func<TState, string> formatter)
        {
            if (logger.IsEnabled(LogLevel.Verbose))
            { // not known
                logger.Write(LogLevel.Verbose, 0, state, null, (s, _) => formatter((TState)s));
            }
        }

        public static void WriteVerbose<TState>(
            this ILogger logger, int eventId, TState state, Func<TState, string> formatter)
        {
            if (logger.IsEnabled(LogLevel.Verbose))
            { // not known
                logger.Write(LogLevel.Verbose, eventId, state, null, (s, _) => formatter((TState)s));
            }
        }

        public static ILogger AppData(this ILogger logger)
        {
            var dbLogger = logger as DbLogger;
            Check.NotNull(dbLogger, "logger");

            if (dbLogger.LogAppDataDbLogger)
            {
                return logger;
            }
            else
            {
                return NullLogger.Instance;
            }
        }

        public static bool SensitiveLoggingEnabled(this ILogger logger)
        {
            var dbLogger = logger as DbLogger;
            Check.NotNull(dbLogger, "logger");

            return dbLogger.LogAppDataDbLogger;
        }
    }
}
