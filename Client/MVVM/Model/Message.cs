using System;
using System.Collections.Generic;

namespace Client.MVVM.Model
{
    public class Message
    {
        public Participant Sender { get; set; }
        public string PlainContent { get; set; }
        public DateTime SendTime { get; set; }
        public DateTime ReceiveTime { get; set; }
        public DateTime DisplayTime { get; set; }
        // TODO: czas edycji i edytowanie wiadomości
        public bool IsDeleted { get; set; }
        public List<Attachment> Attachments { get; set; }

        public static Message Random(Random rng)
        {
            var ret = new Message
            {
                Sender = Participant.Random(rng),
                PlainContent = rng.Next().ToString(),
                SendTime = DateTime.Now.AddTicks(rng.Next()),
                ReceiveTime = DateTime.Now.AddTicks(rng.Next()),
                DisplayTime = DateTime.Now.AddTicks(rng.Next()),
                IsDeleted = rng.Next(0, 2) != 0
            };
            int attCnt = rng.Next(0, 10);
            ret.Attachments = new List<Attachment>(attCnt);
            for (var i = 0; i < attCnt; i++)
                ret.Attachments.Add(Attachment.Random(rng));
            return ret;
        }
    }
}
