using Shared.MVVM.View.Localization;

namespace Client.MVVM.View.Localization
{
    public class ClientTranslator : Translator
    {
        public static ClientTranslator Instance { get; } = new ClientTranslator();

        private ClientTranslator() { }

        protected override string TranslationsFilePath =>
            "/MVVM/View/Localization/Translations.xaml";
    }
}
