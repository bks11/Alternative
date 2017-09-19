using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;


namespace DoRelation.Common
{
    public class LogFormatter
    {
        private const string LOG_DATETIME_PATTERN = "[{0}]";
        private const string LOG_LEVEL_PATTERN = "[Log Level: {0}]";
        private const string LOG_MESSAGE_PATTERN = "[Message]\r\n{0}";
        private const string LOG_METHOD_PATTERN = "[Method: {0} {1}.{2}]";
        private const string LOG_EXCEPTION_PATTERN_WITH_STACK = "[Error]\r\n{0}\r\n[Stack Trace]\r\n{1}";
        private const string LOG_EXCEPTION_PATTERN = "[Error]\r\n{0}";
        private const string LOG_INNER_EXCEPTION_PATTERN_WITH_STACK = "{0}[InnerError]\r\n{1}\r\n{2}[Stack Trace]\r\n{3}";
        private const string LOG_INNER_EXCEPTION_PATTERN = "{0}[InnerError]\r\n{1}";
        private int indent = 0;


        private string getTabs(int count)
        {
            string tabs = string.Empty;

            for (int i = 0; i < count; i++)
            {
                tabs += "\t";
            }

            return tabs;
        }
        private string addTabsToString(string s, int tabCount)
        {
            return getTabs(tabCount) + s.Replace("\r\n", string.Format("\r\n{0}", getTabs(tabCount)));
        }

        private void getExceptionInfo(Exception ex, StringBuilder formattedexception, bool isInnerException)
        {
            if (isInnerException)
            {
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    formattedexception.AppendLine(string.Format(LOG_INNER_EXCEPTION_PATTERN_WITH_STACK, getTabs(indent - 1),
                        addTabsToString(ex.Message, indent), getTabs(indent), addTabsToString(ex.StackTrace, indent)));
                }
                else
                {
                    formattedexception.AppendLine(string.Format(LOG_INNER_EXCEPTION_PATTERN, getTabs(indent - 1),
                        addTabsToString(ex.Message, indent)));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    formattedexception.AppendLine(string.Format(LOG_EXCEPTION_PATTERN_WITH_STACK, ex.Message, ex.StackTrace));
                }
                else
                {
                    formattedexception.AppendLine(string.Format(LOG_EXCEPTION_PATTERN, ex.Message));
                }

            }

            if (ex.InnerException != null)
            {
                indent++;
                getExceptionInfo(ex.InnerException, formattedexception, true);
            }
        }

        #region ILogFormater Members

        public string ToFormattedString(params object[] objects)
        {
            StringBuilder formattedString = new StringBuilder("");

            if (objects == null || objects.Length == 0) return formattedString.ToString();

            formattedString.AppendLine(string.Format(LOG_DATETIME_PATTERN, DateTime.Now));

            if (objects.Length >= 1)
            {
                LogLevel logLevel = (LogLevel)objects[0];
                if (logLevel != null)
                {
                    formattedString.AppendLine(string.Format(LOG_LEVEL_PATTERN, logLevel));
                }
            }

            if (objects.Length >= 2)
            {
                string message = objects[1] as string;
                if (!string.IsNullOrEmpty(message))
                {
                    formattedString.AppendLine(string.Format(LOG_MESSAGE_PATTERN, message));
                }
            }

            if (objects.Length >= 3)
            {
                MethodBase methodBase = objects[2] as MethodBase;
                if (methodBase != null)
                {
                    formattedString.AppendLine(string.Format(LOG_METHOD_PATTERN,
                                                             Enum.GetName(typeof(MemberTypes), methodBase.MemberType),
                                                             methodBase.DeclaringType, methodBase.Name));
                }

            }

            if (objects.Length == 4)
            {
                Exception ex = objects[3] as Exception;
                if (ex != null)
                {
                    indent = 0;
                    getExceptionInfo(ex, formattedString, false);
                }
            }

            formattedString.AppendLine("---------------------------------------------------------------------");
            formattedString.AppendLine();

            return formattedString.ToString();
        }

        #endregion
    }
}
