namespace Client.MVVM.Model
{
    public class Attachment
    {
        public byte[] Content_ { get; set; }
        public AttachmentType Type { get; set; }

        public enum AttachmentType
        {
            // TODO: klasy (lub jedna klasa), które będą tworzyły podglądy znanych typów załączników
            ZIP, PNG, BMP, JPG, MP3, WAV
        }
    }
}
