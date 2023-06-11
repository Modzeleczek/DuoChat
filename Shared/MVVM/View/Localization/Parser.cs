using System.Text;

namespace Shared.MVVM.View.Localization
{
    public class Parser
    {
        private enum State { Normal, EscapeInNormal, Translate, EscapeInTranslate }
        private State _state = State.Normal;
        private StringBuilder _buffer = new StringBuilder();

        public enum Text { None, Normal, Translated }
        public Text TextReady { get; private set; } = Text.None;

        public void Write(char c)
        {
            switch (_state)
            {
                case State.Normal:
                    HandleNormal(c); break;
                case State.EscapeInNormal:
                    HandleEscape(c, State.Normal); break;
                case State.Translate:
                    HandleTranslate(c); break;
                case State.EscapeInTranslate:
                    HandleEscape(c, State.Translate); break;
            }
        }

        private void HandleNormal(char c)
        {
            if (c == '\\')
                _state = State.EscapeInNormal;
            else if (c == '|')
            {
                TextReady = Text.Normal;
                _state = State.Translate;
            }
            else
            {
                _buffer.Append(c);
                // _state = State.Normal;
            }
        }

        private void HandleEscape(char c, State next)
        {
            /* Jeżeli aktualny znak to |, to dopisujemy tylko | (zescapowany |).
            W przeciwnym przypadku, dopisujemy \<aktualny znak>. */
            if (c != '|')
                _buffer.Append('\\');
            _buffer.Append(c);
            _state = next;
        }

        private void HandleTranslate(char c)
        {
            if (c == '\\')
                _state = State.EscapeInTranslate;
            else if (c == '|')
            {
                TextReady = Text.Translated;
                _state = State.Normal;
            }
            else
            {
                _buffer.Append(c);
                // _state = State.Translate;
            }
        }

        public string FlushText()
        {
            TextReady = Text.None;
            var str = _buffer.ToString();
            _buffer.Clear();
            return str;
        }
    }
}
