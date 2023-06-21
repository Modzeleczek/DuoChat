using System;
using System.Collections.Generic;

namespace Shared.MVVM.Core
{
    public class Error : Exception
    {
        #region Properties
        private LinkedList<string> Strings = new LinkedList<string>();

        public new string Message
        {
            get
            {
                if (!(InnerException is null) && !InnerException.Message.Equals(""))
                    Strings.AddLast(InnerException.Message);
                return string.Join(" ", Strings);
            }
        }
        #endregion

        public Error(Exception inner, params string[] messageStrings) :
            base(null, inner)
        {
            for (int i = 0; i < messageStrings.Length; ++i)
                Strings.AddLast(messageStrings[i]);
        }

        public Error(params string[] messageStrings) :
            this(null, messageStrings) { }

        public Error Prepend(params string[] messageStrings)
        {
            for (int i = messageStrings.Length - 1; i >= 0; --i)
                Strings.AddFirst(messageStrings[i]);
            return this;
        }

        public Error Append(params string[] messageStrings)
        {
            for (int i = 0; i < messageStrings.Length; ++i)
                Strings.AddLast(messageStrings[i]);
            return this;
        }
    }
}
