
namespace BruSoft.VS2P4
{
    using System;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Log output to the Output Window.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// The logging level
        /// </summary>
        public enum Level
        {
#if DEBUG
            Debug,
#endif
            Information,
            Warning,
            Error,
            None,
        }

        public static Level OptionsLevel { get; set; }

        private static bool isPaneCreated;

        /// <summary>
        /// Log message to the ActivityLog (if devenv was started with /log) and to the OutputWindow (VS2P4 pane).
        ///     This routine adds a newline to the message for the OutputWindow.
        /// Whether the message is actually logged, or not, depends on the Options setting for level logged;
        /// E.g, if the option level is Error, a message with level Warning or above will not be logged. 
        /// </summary>
        /// <param name="level">The logging level for this message.</param>
        /// <param name="message">The message to log.</param>
        private static void LogMessage(Level level, string message)
        {
            if (!IsLoggableForOptionsLevel(level))
            {
                return;
            }

            DateTime timeStamp = DateTime.Now;
            message = String.Format("{0}: {1}", timeStamp.ToString("hh:mm:ss.fff"), message);

            LogToActivityLog(level, message);
            LogToOutputWindow(message);
        }

        /// <summary>
        /// Log message to the ActivityLog (if devenv was started with /log) and to the OutputWindow (VS2P4 pane).
        ///     This routine adds a newline to the message for the OutputWindow.
        /// Whether the message is actually logged, or not, depends on the Options setting for level logged;
        /// That is, a message with level Warning or above will not be logged. 
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Error(string message)
        {
            message = Resources.Error + message;
            LogMessage(Level.Error, message);
        }

        /// <summary>
        /// Return true if we want to write to logs for level compared to OptionsLevel
        /// </summary>
        /// <param name="level">the particular level.</param>
        /// <returns>true if we want to write to logs for level compared to OptionsLevel</returns>
        private static bool IsLoggableForOptionsLevel(object level)
        {
            int levelInt = (int)level;
            int optionsLevelInt = (int)OptionsLevel;
            return levelInt >= optionsLevelInt;
        }

        /// <summary>
        /// Log message to the ActivityLog (if devenv was started with /log) and to the OutputWindow (VS2P4 pane).
        ///     This routine adds a newline to the message for the OutputWindow.
        /// Whether the message is actually logged, or not, depends on the Options setting for level logged;
        /// That is, a message with level Information or above will not be logged. 
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Warning(string message)
        {
            message = Resources.Warning + message;
            LogMessage(Level.Warning, message);
        }

        /// <summary>
        /// Log message to the ActivityLog (if devenv was started with /log) and to the OutputWindow (VS2P4 pane).
        ///     This routine adds a newline to the message for the OutputWindow.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void Information(string message)
        {
            message = Resources.Information + message;
            LogMessage(Level.Information, message);
        }

        public static void Debug(string message)
        {
#if DEBUG
            message = "Debug: " + message;
            LogMessage(Level.Debug, message);
#endif
        }

        private static void LogToOutputWindow(string message)
        {
            IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outWindow != null)
            {
                Guid generalWindowGuid = VSConstants.GUID_OutWindowGeneralPane;
                if (!isPaneCreated)
                {
                    const bool visible = true;
                    const bool clearWithSolution = true;
                    outWindow.CreatePane(ref generalWindowGuid, Resources.ProviderName, Convert.ToInt32(visible), Convert.ToInt32(clearWithSolution));
                    isPaneCreated = true;
                }
                IVsOutputWindowPane windowPane;
                outWindow.GetPane(ref generalWindowGuid, out windowPane);
                if (windowPane != null)
                {
                    windowPane.OutputStringThreadSafe(message + "\n");
                }
            }
        }

        /// <summary>
        /// Log to the Activity Log IFF /log is entered on the command line
        /// </summary>
        /// <param name="level">The log level.</param>
        /// <param name="message">The message to log.</param>
        private static void LogToActivityLog(Level level, string message)
        {
            IVsActivityLog log = Package.GetGlobalService(typeof(SVsActivityLog)) as IVsActivityLog;
            if (log != null)
            {
                uint actType;
                switch (level)
                {
#if DEBUG
                    case Level.Debug:
#endif
                    case Level.Information:
                        actType = (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION;
                        break;
                    case Level.Warning:
                        actType = (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_WARNING;
                        break;
                    case Level.Error:
                        actType = (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("level");
                }

                log.LogEntry(actType, Resources.ProviderName, message);
            }
        }
    }
}
