namespace MidSpace.ShipScan.SeModCore
{
    using Sandbox.ModAPI;
    using System;
    using System.IO;
    using VRage;

    /// <summary>
    /// Generic text file logger devloped by Midspace for Space Engineers mods.
    /// </summary>
    public class TextLogger
    {
        #region fields

        private string _logFileName;
        private LogEventType _loggingLevel;
        private TextWriter _logWriter;
        private bool _isInitialized;
        private int _delayedWrite;
        private int _writeCounter;
        private readonly FastResourceLock _executionLock = new FastResourceLock();

        #endregion

        #region properties

        public string LogFileName => _logFileName;

        public string LogFile => Path.Combine(MyAPIGateway.Utilities.GamePaths.UserDataPath, "Storage", _logFileName);

        public bool IsActive => _isInitialized;

        #endregion

        #region ctor

        /// <summary>
        /// Initialize the TextLogger with a default filename.
        /// The TextLogger must be Initialized before it can write log entries.
        /// This allows a TextLogger to be created and the Write(...) methods invoked without the TextLogger initialized so you don't have to wrap the TextLogger variable with if statements.
        /// </summary>
        public void Init()
        {
            _isInitialized = true;
            _logFileName = $"TextLog_{(MyAPIGateway.Session != null ? Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath) : "0")}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
            _loggingLevel = LogEventType.All;
        }

        /// <summary>
        /// Initialize the TextLogger with a custom filename.
        /// The TextLogger must be Initialized before it can write log entries.
        /// This allows a TextLogger to be created and the Write(...) methods invoked without the TextLogger initialized so you don't have to wrap the TextLogger variable with if statements.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="loggingLevel"></param>
        /// <param name="addTimestamp"></param>
        /// <param name="delayedWrite">This will specify how many record to delay writing to the log file, to reduce I/O. Setting 0 will write instantly.</param>
        public void Init(string filename, LogEventType loggingLevel, bool addTimestamp = false, int delayedWrite = 0)
        {
            _isInitialized = true;
            if (addTimestamp)
                _logFileName = $"TextLog_{Path.GetFileNameWithoutExtension(filename)}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{Path.GetExtension(filename)}";
            else
                _logFileName = Path.GetFileName(filename);

            _loggingLevel = loggingLevel;
            _delayedWrite = delayedWrite;
        }

        // This isn't whitelisted, because Keen hate Finalizers?
        //~TextLogger()
        //{
        //    Terminate();
        //}

        #endregion

        #region Write methods

        public void WriteStart(string text, params object[] args)
        {
            if (LogEventType.Start <= _loggingLevel)
                Write(LogEventType.Start, false, text, args);
        }

        public void WriteStop(string text, params object[] args)
        {
            if (LogEventType.Stop <= _loggingLevel)
                Write(LogEventType.Stop, false, text, args);
        }

        public void WriteVerbose(string text, params object[] args)
        {
            if (LogEventType.Verbose <= _loggingLevel)
                Write(LogEventType.Verbose, false, text, args);
        }

        public void WriteInfo(string text, params object[] args)
        {
            if (LogEventType.Information <= _loggingLevel)
                Write(LogEventType.Information, false, text, args);
        }

        public void WriteWarning(string text, params object[] args)
        {
            if (LogEventType.Warning <= _loggingLevel)
                Write(LogEventType.Warning, false, text, args);
        }

        public void WriteError(string text, params object[] args)
        {
            if (LogEventType.Error <= _loggingLevel)
                Write(LogEventType.Error, false, text, args);
        }

        public void WriteRaw(LogEventType eventType, string text, params object[] args)
        {
            if (eventType <= _loggingLevel)
                Write(eventType, true, text, args);
        }

        public void WriteException(Exception ex, string additionalInformation = null)
        {
            if (LogEventType.Critical <= _loggingLevel)
            {
                string msg = ex + "\r\n";

                if (!string.IsNullOrEmpty(additionalInformation))
                {
                    msg += $"Additional information on {ex.Message}:\r\n";
                    msg += additionalInformation + "\r\n";
                }

                Write(LogEventType.Error, false, msg);
            }
        }

        private void Write(LogEventType eventType, bool writeRaw, string text, params object[] args)
        {
            if (!_isInitialized)
                return;

            // we create the writer only when it is needed to prevent the creation of empty files.
            if (_logWriter == null)
            {
                try
                {
                    _logWriter = MyAPIGateway.Utilities.WriteFileInGlobalStorage(_logFileName);
                }
                catch (Exception ex)
                {
                    Terminate();
                    WriteGameLog($"## TextLogger Exception caught in mod. Message: {ex}");
                    return;
                }
            }

            string message;
            if (args == null || args.Length == 0)
                message = text;
            else
                message = string.Format(text, args);

            if (writeRaw)
                _logWriter.Write(message);
            else
                _logWriter.WriteLine("{0:yyyy-MM-dd HH:mm:ss.fff} - {1}", DateTime.Now, message);
            _writeCounter++;
            if (_delayedWrite == 0 || _writeCounter > _delayedWrite || eventType <= LogEventType.Error)
            {
                _logWriter.Flush();
                _writeCounter = 0;
            }
        }

        public static void WriteGameLog(string text, params object[] args)
        {
            string message = text;
            if (args != null && args.Length != 0)
                message = string.Format(text, args);

            if (MyAPIGateway.Utilities != null && MyAPIGateway.Utilities.IsDedicated)
                VRage.Utils.MyLog.Default.WriteLineAndConsole(message);
            else
                VRage.Utils.MyLog.Default.WriteLine(message);
        }

        #endregion

        public void Flush()
        {
            if (!_isInitialized)
                return;

            _logWriter?.Flush();
        }

        public void Terminate()
        {
            using (_executionLock.AcquireExclusiveUsing())
            {
                _isInitialized = false;
                if (_logWriter != null)
                {
                    try
                    {
                        _logWriter.Flush();
                        _logWriter.Dispose();
                    }
                    catch
                    {
                        // catch exception caused by SE Server Extender Essential plugin during auto restart
                        // which causes file stream to be already closed during flush.
                    }
                    _logWriter = null;
                }
            }
        }
    }
}
