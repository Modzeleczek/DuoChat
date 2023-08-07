using System;

namespace Client.MVVM.ViewModel.Observables
{
    public class Attachment
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public byte[] PlainContent { get; set; }

        public static Attachment Random(Random rng)
        {
            var bytes = new byte[rng.Next(0, 20)];
            rng.NextBytes(bytes);
            return new Attachment
            {
                Name = rng.Next().ToString(),
                Type = rng.Next().ToString(),
                PlainContent = bytes
            };
        }
    }
}
