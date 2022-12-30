using Shared.MVVM.View.Localization;

namespace Server.MVVM.View.Localization
{
    public class ServerTranslator : Translator
    {
        public static ServerTranslator Instance { get; } = new ServerTranslator();

        private ServerTranslator() { }

        protected override string TranslationsFilePath =>
            "/MVVM/View/Localization/Translations.xaml";
    }
}
