using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoRelation.Common
{
    /// <summary>
    /// Defines available status of levels.
    /// </summary>
    public enum LevelStatus
    {
        /// <summary>The Off level's status.</summary>
        Off = -1,
        /// <summary>The Trace level's status.</summary>
        Trace = 0,
        /// <summary>The Debug level's status.</summary>
        Debug = 1,
        /// <summary>The Info level's status.</summary>
        Info = 2,
        /// <summary>The Warn level's status.</summary>
        Warn = 3,
        /// <summary>The Error level's status.</summary>
        Error = 4,
        /// <summary>The Fatal level's status.</summary>
        Fatal = 5,
    }

    /// <summary>
    /// Defines available log levels.
    /// </summary>
    public class LogLevel : IComparable
    {
        private string _name;
        private LevelStatus _levelStatus;

        /// <summary>
        /// The Trace level.
        /// </summary>
        public static readonly LogLevel Trace;

        /// <summary>
        /// The Debug level.
        /// </summary>
        public static readonly LogLevel Debug;

        /// <summary>
        /// The Info level.
        /// </summary>
        public static readonly LogLevel Info;

        /// <summary>
        /// The Warn level.
        /// </summary>
        public static readonly LogLevel Warn;

        /// <summary>
        /// The Error level.
        /// </summary>
        public static readonly LogLevel Error;

        /// <summary>
        /// The Fatal level.
        /// </summary>
        public static readonly LogLevel Fatal;

        /// <summary>
        /// The Off level.
        /// </summary>
        public static readonly LogLevel Off;

        /// <summary>
        /// The largest possible value of an <see cref="LogLevel"/>.
        /// </summary>
        public static readonly LogLevel MaxLevel;

        /// <summary>
        /// The smallest possible value of an <see cref="LogLevel"/>.
        /// </summary>
        public static readonly LogLevel MinLevel;

        static LogLevel()
        {
            Trace = new LogLevel(LevelStatus.Trace);
            Debug = new LogLevel(LevelStatus.Debug);
            Info = new LogLevel(LevelStatus.Info);
            Warn = new LogLevel(LevelStatus.Warn);
            Error = new LogLevel(LevelStatus.Error);
            Fatal = new LogLevel(LevelStatus.Fatal);
            Off = new LogLevel(LevelStatus.Off);

            MinLevel = Trace;
            MaxLevel = Fatal;
        }

        private LogLevel(LevelStatus levelStatus)
        {
            _levelStatus = levelStatus;
            _name = levelStatus.ToString();
        }

        /// <summary>
        /// Gets the name of the log level.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Gets the name of the logger in upper case.
        /// </summary>
        public string UppercaseName
        {
            get
            {
                return _name.ToUpper();
            }
        }

        /// <summary>
        /// Gets the name of the logger in lower case.
        /// </summary>
        public string LowercaseName
        {
            get
            {
                return _name.ToLower();
            }
        }

        internal LevelStatus LevelStatus
        {
            get
            {
                return _levelStatus;
            }
        }

        /// <summary>
        /// Returns the LogLevel that corresponds to the specified <see cref="T:ReliableNet.RAI5.ReliableNet.RAI5.Common.Log.ReliableNet.RAI5.Common.LevelStatus"/>.
        /// </summary>
        /// <returns>The LogLevel instance. For LevelStatus.Debug it returns LogLevel.Debug, LevelStatus.Info gives LogLevel.Info and so on.</returns>
        /// <param name="levelStatus">The <see cref="T:ReliableNet.RAI5.ReliableNet.RAI5.Common.Log.ReliableNet.RAI5.Common.LevelStatus"/>.</param>
        public static LogLevel FromLevelStatus(LevelStatus levelStatus)
        {
            return new LogLevel(levelStatus);
        }

        /// <summary>
        /// Returns the LogLevel that corresponds to the supplied <see langword="string" />.
        /// </summary>
        /// <param name="levelStatusName">The texual representation of the log level status.</param>
        /// <returns>The LogLevel instance. For "Error" it return LogLevel.Error</returns>
        public static LogLevel FromString(string levelStatusName)
        {
            try
            {
                LevelStatus levelStatus = (LevelStatus)Enum.Parse(typeof(LevelStatus), levelStatusName);
                return new LogLevel(levelStatus);
            }
            catch
            {
                return Off;
            }
        }

        /// <summary>
        /// Compares two <see cref="LogLevel"/> objects and returns a value indicating whether the first one is equal to the second one.
        /// </summary>
        /// <param name="l1">The first level.</param>
        /// <param name="l2">The second level.</param>
        /// <returns>The value of <c>l1.LevelStatus == l2.LevelStatus</c></returns>
        public static bool operator ==(LogLevel l1, LogLevel l2)
        {
            return l1.Equals(l2);
        }

        /// <summary>
        /// Compares two <see cref="LogLevel"/> objects and returns a value indicating whether the first one is not equal to the second one.
        /// </summary>
        /// <param name="l1">The first level.</param>
        /// <param name="l2">The second level.</param>
        /// <returns>The value of <c>l1.LevelStatus != l2.LevelStatus</c></returns>
        public static bool operator !=(LogLevel l1, LogLevel l2)
        {
            return !l1.Equals(l2);
        }

        /// <summary>
        /// Compares two <see cref="LogLevel"/> objects and returns a value indicating whether the first one is less than or equal to the second one.
        /// </summary>
        /// <param name="l1">The first level.</param>
        /// <param name="l2">The second level.</param>
        /// <returns>The value of <c>l1.LevelStatus &lt;= l2.LevelStatus</c></returns>
        public static bool operator <=(LogLevel l1, LogLevel l2)
        {
            return l1.LevelStatus <= l2.LevelStatus;
        }

        /// <summary>
        /// Compares two <see cref="LogLevel"/> objects and returns a value indicating whether the first one is greater than or equal to the second one.
        /// </summary>
        /// <param name="l1">The first level.</param>
        /// <param name="l2">The second level.</param>
        /// <returns>The value of <c>l1.LevelStatus &gt;= l2.LevelStatus</c></returns>
        public static bool operator >=(LogLevel l1, LogLevel l2)
        {
            return l1.LevelStatus >= l2.LevelStatus;
        }

        /// <summary>
        /// Compares two <see cref="LogLevel"/> objects and returns a value indicating whether the first one is less than the second one.
        /// </summary>
        /// <param name="l1">The first level.</param>
        /// <param name="l2">The second level.</param>
        /// <returns>The value of <c>l1.LevelStatus &lt; l2.LevelStatus</c></returns>
        public static bool operator <(LogLevel l1, LogLevel l2)
        {
            return l1.LevelStatus < l2.LevelStatus;
        }

        /// <summary>
        /// Compares two <see cref="LogLevel"/> objects and returns a value indicating whether the first one is greater than the second one.
        /// </summary>
        /// <param name="l1">The first level.</param>
        /// <param name="l2">The second level.</param>
        /// <returns>The value of <c>l1.LevelStatus &gt; l2.LevelStatus</c></returns>
        public static bool operator >(LogLevel l1, LogLevel l2)
        {
            return l1.LevelStatus > l2.LevelStatus;
        }

        /// <summary>
        /// Returns a string representation of the log level.
        /// </summary>
        /// <returns>Log level name. If the instance of  LogLevel equal LogLevel.Fatal then it return "Fatal"</returns>        
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Compares the level to the other <see cref="LogLevel"/> object.
        /// </summary>
        /// <param name="obj">the object object</param>
        /// <returns>a value less than zero when this logger's <see cref="LevelStatus"/> is 
        /// less than the other logger's <see cref="LevelStatus"/>, 0 when they are equal and 
        /// greater than zero when this <see cref="LevelStatus"/> is greater than the
        /// other <see cref="LevelStatus"/>.</returns>
        public int CompareTo(object obj)
        {
            LogLevel l = (LogLevel)obj;
            return LevelStatus - l.LevelStatus;
        }

        ///<summary>
        /// Determines whether the specified object is equal to the current instace.
        /// </summary>
        ///<param name="obj">The object to compare with the current instace.</param>
        ///<returns>true if the specified object is equal to the current instace; otherwise, false. </returns>
        public override bool Equals(object obj)
        {
            if (this == obj) return true;

            if (obj == null || !(obj is LogLevel)) return false;
            LogLevel logLevel = (LogLevel)obj;

            return Equals(_levelStatus, logLevel._levelStatus);
        }

        ///<summary>
        /// Serves as a hash function for a particular type. GetHashCode is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        ///<returns>A hash code for the current instace. </returns>
        public override int GetHashCode()
        {
            return _levelStatus.GetHashCode();
        }
    }
}
