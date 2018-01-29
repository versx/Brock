namespace BrockBot.Diagnostics
{
    using System;

    public class EventLogger: IEventLogger
    {
        #region Properties

        public Action<LogType, string> LogHandler { get; set; }

        #endregion

        #region Constructor(s)

        public EventLogger()
        {
            LogHandler = new Action<LogType, string>((logType, message) => Console.WriteLine($"{logType}: {message}"));
        }

        public EventLogger(Action<LogType, string> logHandler)
        {
            LogHandler = logHandler;
        }

        #endregion

        #region Public Methods

        public void Trace(string format, params object[] args)
        {
            LogHandler(LogType.Trace, args.Length > 0 ? string.Format(format, args) : format);
        }

        public void Debug(string format, params object[] args)
        {
            LogHandler(LogType.Debug, args.Length > 0 ? string.Format(format, args) : format);
        }

        public void Info(string format, params object[] args)
        {
            LogHandler(LogType.Info, args.Length > 0 ? string.Format(format, args) : format);
        }

        public void Warn(string format, params object[] args)
        {
            LogHandler(LogType.Warning, args.Length > 0 ? string.Format(format, args) : format);
        }

        public void Error(string format, params object[] args)
        {
            LogHandler(LogType.Error, args.Length > 0 ? string.Format(format, args) : format);
        }

        public void Error(Exception ex)
        {
            LogHandler(LogType.Error, ex.ToString());
        }

        #endregion
    }
}