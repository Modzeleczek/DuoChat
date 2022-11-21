namespace Client.MVVM.ViewModel
{
    public class PasswordFormViewModel : FormViewModel
    {
        protected bool Validate(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            { Error(d["Specify a password."]); return false; }
            if (password.Length < 8)
            { Error(d["Password should be at least 8 characters long."]); return false; }
            bool hasDigit = false;
            foreach (var c in password)
                if (c >= '0' && c <= '9') { hasDigit = true; break; }
            if (!hasDigit)
            { Error(d["Password should contain at least one digit."]); return false; }
            bool hasSpecial = false;
            foreach (var c in password)
                if (!((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')))
                { hasSpecial = true; break; }
            if (!hasSpecial)
            {
                Error(d["Password should contain at least one special character (not a letter or a digit)."]);
                return false;
            }
            return true;
        }
    }
}
