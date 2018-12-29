﻿#if NETSTANDARD1_0 || NETSTANDARD1_1 || NETSTANDARD1_2

using System.Text;

namespace System
{
    public static class Console
    {
        private static readonly StringBuilder _stringBuilder = new StringBuilder();

        public static void ReadKey()
        {
            throw new Exception(_stringBuilder.ToString());
        }

        public static void Write(string value)
        {
            _stringBuilder.Append(value);
        }

        [CLSCompliant(false)]
        public static void Write(ulong value)
        {
            _stringBuilder.Append(value.ToString());
        }

        [CLSCompliant(false)]
        public static void Write(uint value)
        {
            _stringBuilder.Append(value.ToString());
        }

        public static void Write(float value)
        {
            _stringBuilder.Append(value.ToString());
        }

        public static void Write(decimal value)
        {
            _stringBuilder.Append(value.ToString());
        }

        public static void Write(long value)
        {
            _stringBuilder.Append(value.ToString());
        }

        public static void Write(int value)
        {
            _stringBuilder.Append(value.ToString());
        }

        public static void Write(char value)
        {
            _stringBuilder.Append(value.ToString());
        }

        public static void Write(bool value)
        {
            _stringBuilder.Append(value.ToString());
        }

        public static void Write(char[] value)
        {
            var str = new string(value);
            _stringBuilder.Append(str);
        }

        public static void Write(char[] buffer, int index, int count)
        {
            var str = new string(buffer, index, count);
            _stringBuilder.Append(str);
        }

        public static void Write(object value)
        {
            _stringBuilder.Append(value);
        }

        public static void Write()
        {
            _stringBuilder.Append(string.Empty);
        }

        public static void Write(double value)
        {
            _stringBuilder.Append(value.ToString());
        }

        public static void Write(string format, object arg0)
        {
            _stringBuilder.Append(string.Format(format, arg0));
        }

        public static void Write(string format, object arg0, object arg1)
        {
            _stringBuilder.Append(string.Format(format, arg0, arg1));
        }

        public static void Write(string format, object arg0, object arg1, object arg2)
        {
            _stringBuilder.Append(string.Format(format, arg0, arg1, arg2));
        }

        public static void Write(string format, object arg0, object arg1, object arg2, object arg3)
        {
            _stringBuilder.Append(string.Format(format, arg0, arg1, arg2, arg3));
        }

        public static void Write(string format, params object[] arg)
        {
            _stringBuilder.AppendLine(string.Format(format, arg));
        }

        public static void WriteLine(string value)
        {
            _stringBuilder.AppendLine(value);
        }

        [CLSCompliant(false)]
        public static void WriteLine(ulong value)
        {
            _stringBuilder.AppendLine(value.ToString());
        }

        [CLSCompliant(false)]
        public static void WriteLine(uint value)
        {
            _stringBuilder.AppendLine(value.ToString());
        }

        public static void WriteLine(float value)
        {
            _stringBuilder.AppendLine(value.ToString());
        }

        public static void WriteLine(decimal value)
        {
            _stringBuilder.AppendLine(value.ToString());
        }

        public static void WriteLine(long value)
        {
            _stringBuilder.AppendLine(value.ToString());
        }

        public static void WriteLine(int value)
        {
            _stringBuilder.AppendLine(value.ToString());
        }

        public static void WriteLine(char value)
        {
            _stringBuilder.AppendLine(value.ToString());
        }

        public static void WriteLine(bool value)
        {
            _stringBuilder.AppendLine(value.ToString());
        }

        public static void WriteLine(char[] value)
        {
            var str = new string(value);
            _stringBuilder.AppendLine(str);
        }

        public static void WriteLine(char[] buffer, int index, int count)
        {
            var str = new string(buffer, index, count);
            _stringBuilder.AppendLine(str);
        }

        public static void WriteLine(object value)
        {
            _stringBuilder.AppendLine(value.ToString());
        }

        public static void WriteLine()
        {
            _stringBuilder.AppendLine(string.Empty);
        }

        public static void WriteLine(double value)
        {
            _stringBuilder.AppendLine(value.ToString());
        }

        public static void WriteLine(string format, object arg0)
        {
            _stringBuilder.AppendLine(string.Format(format, arg0));
        }

        public static void WriteLine(string format, object arg0, object arg1)
        {
            _stringBuilder.AppendLine(string.Format(format, arg0, arg1));
        }

        public static void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            _stringBuilder.AppendLine(string.Format(format, arg0, arg1, arg2));
        }

        public static void WriteLine(string format, object arg0, object arg1, object arg2, object arg3)
        {
            _stringBuilder.AppendLine(string.Format(format, arg0, arg1, arg2, arg3));
        }

        public static void WriteLine(string format, params object[] arg)
        {
            _stringBuilder.AppendLine(string.Format(format, arg));
        }
    }
}

#endif