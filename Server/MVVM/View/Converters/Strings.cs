using Shared.MVVM.View.Converters;
using System.Collections.Generic;

namespace Server.MVVM.View.Converters
{
    public class Strings : Translator
    {
        public static Strings Instance { get; } = new Strings();

        private Strings() : base(new Dictionary<string, string>[1]) { }

        protected override void FillDictionaries()
        {
            FillPolish();
        }

        private void FillPolish()
        {
            languages[0] = new Dictionary<string, string>();
            var d = languages[0];
            d["Server"] = "Serwer";
            d["Settings"] = "Ustawienia";
            d["Connected clients"] = "Połączone klienty";
            d["Accounts"] = "Konta";
        }
    }
}
