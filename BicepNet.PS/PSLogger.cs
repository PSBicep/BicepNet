using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace BicepNet.PS
{
    public class PSLogger : ILogger
    {
        private readonly PSCmdlet cmdlet;
        private readonly string name;

        private readonly List<LogLevel> logLevels = new List<LogLevel>() {
            LogLevel.Trace,
            LogLevel.Debug,
            LogLevel.Information,
            LogLevel.Warning,
            LogLevel.Error
        };

        public PSLogger(PSCmdlet cmdlet) {
            this.cmdlet = cmdlet;
            name = cmdlet.MyInvocation.InvocationName;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => logLevels.Contains(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            switch (logLevel)
            {
                case LogLevel.Trace:
                    cmdlet.WriteVerbose($"{name}: {formatter(state, exception)}");
                    break;
                case LogLevel.Debug:
                    cmdlet.WriteDebug(formatter(state, exception));
                    break;
                case LogLevel.Information:
                    cmdlet.WriteInformation(new InformationRecord(formatter(state, exception), name));
                    break;
                case LogLevel.Warning:
                    cmdlet.WriteWarning(formatter(state, exception));
                    break;
                case LogLevel.Error:
                    cmdlet.WriteError(new ErrorRecord(exception ?? new Exception(formatter(state, exception)), eventId.Name, ErrorCategory.WriteError, null));
                    break;
                default:
                    break;
            }
        }
    }
}
