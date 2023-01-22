using System.Collections.Generic;

namespace Shared.MVVM.Model
{
    public class Status
    {
        #region Properties
        public int Code { get; private set; } = 0;

        private LinkedList<string> Strings = new LinkedList<string>();

        public object Data { get; private set; } = null; // ewentualne dodatkowe dane

        public string Message
        {
            get => string.Join(" ", Strings);
        }
        #endregion

        public Status(int code = 0, object data = null, params string[] messageStrings)
        {
            Code = code;
            for (int i = 0; i < messageStrings.Length; ++i)
                Strings.AddLast(messageStrings[i]);
            Data = data;
        }

        public Status Prepend(params string[] messageStrings)
        {
            for (int i = messageStrings.Length - 1; i >= 0; --i)
                Strings.AddFirst(messageStrings[i]);
            return this;
        }

        public Status Prepend(int newCode, params string[] messageStrings)
        {
            Code = newCode;
            return Prepend(messageStrings);
        }

        public Status Append(params string[] messageStrings)
        {
            for (int i = 0; i < messageStrings.Length; ++i)
                Strings.AddLast(messageStrings[i]);
            return this;
        }

        public Status Append(int newCode, params string[] messageStrings)
        {
            Code = newCode;
            return Append(messageStrings);
        }
    }
}
