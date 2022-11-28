using System.Windows.Controls;

namespace Client.MVVM.ViewModel
{
    public abstract class PasswordFormViewModel : FormViewModel
    {
        protected override void CancelHandler(object e)
        {
            DisposePasswords((Control[])e);
            base.CancelHandler(e);
            /* alternatywa z RTTI
            var inpCtrls = (Control[])e;
            foreach (var c in inpCtrls)
            {
                var p = c as PasswordBox;
                if (p != null)
                    p.SecurePassword.Dispose();
            }
            base.CancelHandler(e); */
        }

        protected abstract void DisposePasswords(Control[] controls);
    }
}
