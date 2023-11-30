using System;
using System.Collections.Generic;

namespace Shared.MVVM.Core
{
    public class Error : Exception
    {
        #region Classes
        public enum ErrorType : byte
        {
            Generic = 0, SingleClient = 1
        }
        #endregion

        #region Properties
        private readonly LinkedList<string> Strings = new LinkedList<string>();

        public new string Message
        {
            get
            {
                // Rekurencyjnie wypisujemy łańcuch InnerExceptionów.
                Exception? temp = InnerException;
                while (!(temp is null))
                {
                    string message;
                    /* Sztuczny polimorfizm - jeżeli temp jest klasy Error,
                    to bierzemy jego Message, a nie read-only Message zdefiniowane
                    w klasie Exception. */
                    if (temp is Error error)
                        message = error.Message;
                    else
                        message = temp.Message;

                    if (!string.IsNullOrWhiteSpace(message))
                        Strings.AddLast(message);
                    temp = temp.InnerException;
                }
                return string.Join("\n", Strings);
            }
            // get { return string.Join("\n", Strings); }
        }

        public ErrorType SubType { get; set; } = ErrorType.Generic;
        #endregion

        public Error(Exception? inner, params string[] messageStrings) :
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
