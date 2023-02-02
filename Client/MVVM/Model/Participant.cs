using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Client.MVVM.Model
{
    public class Participant : User
    {
        public DateTime JoinTime { get; set; }
        public bool IsAdministrator { get; set; }

        new public static Participant Random(Random rng) =>
            new Participant
            {
                PublicKey = null,
                Nickname = rng.Next().ToString(),
                Image = new WriteableBitmap(100, 100, 96, 96, PixelFormats.Bgra32, null),
                JoinTime = DateTime.Now.AddTicks(rng.Next()),
                IsAdministrator = rng.Next(0, 2) != 0
            };
    }
}
