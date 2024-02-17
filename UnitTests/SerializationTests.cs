using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.ViewModel.LongBlockingOperation;
using Shared.MVVM.ViewModel.Results;
using System.ComponentModel;
using System.Numerics;

namespace UnitTests
{
    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void Cast_UshortToShort_ShouldNotChangeBinaryRepresentation()
        {
            // Arrange
            ushort a = 0x9B40;
            byte[] expected = BitConverter.GetBytes(a);

            // Act
            byte[] actual = BitConverter.GetBytes((short)a);

            // Assert
            expected.BytesEqual(actual);
        }

        [TestMethod]
        public void BigInteger_ToByteArray_ShouldReturnBytesInLittleEndian()
        {
            // Arrange
            var expected = new byte[] { 0x12, 0x34, 0x56, 0x78 };

            // Act
            var actual = new BigInteger(0x78563412).ToByteArray();

            // Assert
            /* W tablicy otrzymanej poprzez BigInteger.ToByteArray
            na indeksie 0 znajduje się najmniej znaczący bajt liczby. */
            Console.WriteLine($"expected: {actual.ToHexString()}");
            Console.WriteLine($"actual: {actual.ToHexString()}");

            expected.BytesEqual(actual);
        }

        [TestMethod]
        public void PrivateKey_ToUnsignedBigEndian_12ABCD34_ShouldReturn12ABCD34()
        {
            // Arrange
            BigInteger bigInt = new BigInteger(0x12ABCD34);
            var expected = new byte[] { 0x12, 0xAB, 0xCD, 0x34 };

            // Act
            byte[] actual = typeof(PrivateKey).InvokeStatic<byte[]>(
                "BIToPaddedUBE", bigInt, bigInt.GetByteCount());

            // Assert
            Console.WriteLine(actual.ToHexString());

            expected.BytesEqual(actual);
        }

        [TestMethod]
        public void PrivateKey_TryParse_WhenGivenValidString_ShouldReturnTrue()
        {
            // Arrange
            const bool expected = true;
            const string privateKeyString = "Fw==;JQ=="; // <23 w base64>;<37 w base64>

            // Act
            var actual = PrivateKey.TryParse(privateKeyString, out PrivateKey? key);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void PrivateKey_TryParse_WhenGivenInvalidString_ShouldReturnFalse()
        {
            // Arrange
            const string privateKeyString = null!;

            // Act
            var actual = PrivateKey.TryParse(privateKeyString, out PrivateKey? key);

            // Assert
            Assert.AreEqual(false, actual);
        }

        [TestMethod]
        public void PrivateKey_Parse_WhenTextIsNull_ShouldReturnFailure()
        {
            // Arrange
            string text = null!;
            
            // Act
            var testDelegate = () => PrivateKey.Parse(text);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|String is null.|", ex.Message);
        }

        [TestMethod]
        public void PrivateKey_Parse_WhenTextDoesNotConsistOf2PartsSeparatedWithSemicolon_ShouldReturnFailure()
        {
            // Arrange
            string text = "";

            // Act
            var testDelegate = () => PrivateKey.Parse(text);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|String does not consist of two parts separated with semicolon.|", ex.Message);
        }

        [TestMethod]
        public void PrivateKey_Parse_WhenPIsEmpty_ShouldReturnFailure()
        {
            // Arrange
            string text = ";";

            // Act
            var testDelegate = () => PrivateKey.Parse(text);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|First| |number| (p) |is empty.|", ex.Message);
        }

        [TestMethod]
        public void PrivateKey_Parse_WhenQIsEmpty_ShouldReturnFailure()
        {
            // Arrange
            string text = "Fw==;"; // 23;

            // Act
            var testDelegate = () => PrivateKey.Parse(text);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|Second| |number| (q) |is empty.|", ex.Message);
        }

        [TestMethod]
        public void PrivateKey_Parse_WhenPIsNotValidBase64String_ShouldReturnFailure()
        {
            // Arrange
            string text = "abc);JQ=="; // ;37

            // Act
            var testDelegate = () => PrivateKey.Parse(text);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.IsTrue(ex.Message.Contains("|First| |number| (p) |is not valid Base64 string.|"));
        }

        [TestMethod]
        public void PrivateKey_Parse_WhenQIsNotValidBase64String_ShouldReturnFailure()
        {
            // Arrange
            string text = "Fw==;def("; // 23;

            // Act
            var testDelegate = () => PrivateKey.Parse(text);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.IsTrue(ex.Message.Contains("|Second| |number| (q) |is not valid Base64 string.|"));
        }

        [TestMethod]
        public void PrivateKey_Parse_WhenPIsNotPrime_ShouldReturnFailure()
        {
            // Arrange
            var p = Convert.ToBase64String(BitConverter.GetBytes(6));

            string text = p + ";JQ=="; // 6;37

            // Act
            var testDelegate = () => PrivateKey.Parse(text);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|First| |number| (p) |is not prime.|", ex.Message);
        }

        [TestMethod]
        public void PrivateKey_Parse_WhenQIsNotPrime_ShouldReturnFailure()
        {
            // Arrange
            var q = Convert.ToBase64String(BitConverter.GetBytes(8));

            string text = "Fw==;" + q; // 23;8

            // Act
            var testDelegate = () => PrivateKey.Parse(text);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|Second| |number| (q) |is not prime.|", ex.Message);
        }

        [TestMethod]
        public void PrivateKey_ToBytes_ShouldReturnSerializedBytes()
        {
            // Arrange
            byte[] expBytes = new byte[] { 1, 0 /* długość p */, 23, 1, 0 /* długość q */, 37 };
            var privateKey = PrivateKey.Parse("Fw==;JQ=="); // 23;37

            // Act
            var actBytes = privateKey.ToBytes();

            // Assert
            expBytes.BytesEqual(actBytes);
        }

        [TestMethod]
        public void PrivateKey_FromBytes_WhenGivenValidBytes_ShouldReturnDeserializedPrivateKey()
        {
            // Arrange
            var expPrivateKey = PrivateKey.Parse("Fw==;JQ=="); // 23;37
            byte[] bytes = new byte[] { 1, 0, 23, 1, 0, 37 };

            // Act
            var actPrivateKey = PrivateKey.FromBytes(bytes);

            // Assert
            /* PrivateKey nie ma przesłoniętego (override)
            Equals, więc porównujemy tekstowe reprezentacje. */
            Assert.AreEqual(expPrivateKey.ToString(), actPrivateKey.ToString());
        }

        [TestMethod]
        public void PrivateKey_Parse_WhenCancelledBeforeBeingCalled_ShouldReturnCancellationAfterParsingP()
        {
            // Arrange
            var expResultType = typeof(Cancellation);
            var worker = new BackgroundWorker
            { WorkerReportsProgress = true, WorkerSupportsCancellation = true };
            var doWorkEventArgs = new DoWorkEventArgs(null);
            var progressReporter = new ProgressReporter(worker, doWorkEventArgs);
            worker.CancelAsync();
            string privateKeyString = "Fw==;JQ=="; // 23;37

            // Act
            PrivateKey.Parse(progressReporter, privateKeyString);
            var actResult = doWorkEventArgs.Result;

            // Assert
            Assert.IsInstanceOfType(actResult, expResultType);
        }

        [TestMethod]
        public void PublicKey_Equals_WhenBIsNotPublicKeyObject_ShouldReturnFalse()
        {
            // Arrange
            var a = new PublicKey(new byte[] { 1, 2, 3 });
            var b = new object();
            PublicKey_Equals(a, b, false);
        }

        private void PublicKey_Equals(PublicKey a, object b, bool expectedResult)
        {
            // Act
            bool result = a.Equals(b);

            // Assert
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void PublicKey_Equals_WhenAIsPublicKeyWithNullModulus_ShouldReturnFalse()
        {
            // Arrange
            var a = new PublicKey(new byte[] { 1, 2, 3 });
            var b = new PublicKey(null!);
            PublicKey_Equals(a, b, false);
        }

        [TestMethod]
        public void PublicKey_Equals_WhenAIsNullAndBIsValidPublicKey_ShouldReturnFalse()
        {
            // Arrange
            var a = new PublicKey(null!);
            var b = new PublicKey(new byte[] { 1, 2, 3 });
            PublicKey_Equals(a, b, false);
        }

        [TestMethod]
        public void PublicKey_Equals_WhenAAndBHaveDifferentLengths_ShouldReturnFalse()
        {
            // Arrange
            var a = new PublicKey(new byte[] { 1, 2, 3 });
            var b = new PublicKey(new byte[] { 1, 2, 3, 4 });
            PublicKey_Equals(a, b, false);
        }

        [TestMethod]
        public void PublicKey_Equals_WhenAAndBHaveDifferentBytes_ShouldReturnFalse()
        {
            // Arrange
            var a = new PublicKey(new byte[] { 1, 2, 3 });
            var b = new PublicKey(new byte[] { 4, 5, 6 });
            PublicKey_Equals(a, b, false);
        }

        [TestMethod]
        public void PublicKey_Equals_WhenAAndBHaveTheSameBytes_ShouldReturnTrue()
        {
            // Arrange
            var a = new PublicKey(new byte[] { 1, 2, 3 });
            var b = new PublicKey(new byte[] { 1, 2, 3 });
            PublicKey_Equals(a, b, true);
        }

        [TestMethod]
        public void PublicKey_Parse_WhenTextIsNull_ShouldThrowError()
        {
            // Arrange
            string text = null!;

            // Act
            var testDelegate = () => PublicKey.Parse(text);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|String is null.|", ex.Message);
        }

        [TestMethod]
        public void PublicKey_Parse_WhenTextIsEmpty_ShouldThrowError()
        {
            // Arrange
            string text = "";

            // Act
            var testDelegate = () => PublicKey.Parse(text);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|String is empty.|", ex.Message);
        }

        [TestMethod]
        public void PublicKey_Parse_WhenTextIsNotValidBase64String_ShouldThrowError()
        {
            // Arrange
            string text = "abc)";

            // Act
            var testDelegate = () => PublicKey.Parse(text);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.IsTrue(ex.Message.Contains("|Number| |is not valid Base64 string.|"));
        }

        [TestMethod]
        public void PublicKey_Parse_WhenTextIsValid_ShouldReturnDeserializedPublicKey()
        {
            // Arrange
            var expPublicKey = new PublicKey(new byte[] { 1, 2, 3 });
            string text = expPublicKey.ToString();

            // Act
            var actPublicKey = PublicKey.Parse(text);

            // Assert
            Assert.AreEqual(expPublicKey, actPublicKey);
        }

        [TestMethod]
        public void PublicKey_FromPacketReader_WhenPublicKeyHasBeenSerializedWithToBytes_ShouldDeserializeIt()
        {
            // Arrange
            var expPublicKey = new PublicKey(new byte[] { 1, 2, 3 });
            byte[] bytes = expPublicKey.ToBytes();
            var pr = new PacketReader(bytes);

            // Act
            var actPublicKey = PublicKey.FromPacketReader(pr);

            // Assert
            Assert.AreEqual(expPublicKey, actPublicKey);
        }

        [TestMethod]
        public void PublicKey_FromPacketReader_WhenPublicKeyHasLengthGreaterThan256_ShouldThrowError()
        {
            // Arrange
            var privateKey = PrivateKey.Random(3000, 256 * 8 + 1);
            var publicKey = privateKey.ToPublicKey();
            byte[] bytes = publicKey.ToBytes();
            var pr = new PacketReader(bytes);

            // Act
            var testDelegate = () => PublicKey.FromPacketReader(pr);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|Public key can be at most 256 bytes long|.", ex.Message);
        }

        [TestMethod]
        public void PublicKey_FromBytesNoLength_WhenPublicKeyHasBeenSerializedWithToBytesNoLength_ShouldDeserializeIt()
        {
            // Arrange
            var expPublicKey = new PublicKey(new byte[] { 1, 2, 3 });
            byte[] bytes = expPublicKey.ToBytesNoLength();

            // Act
            var actPublicKey = PublicKey.FromBytesNoLength(bytes);

            // Assert
            Assert.AreEqual(expPublicKey, actPublicKey);
        }

        [TestMethod]
        public void IPv4Address_Parse_WhenTextIsNull_ShouldThrowError()
        {
            // Arrange
            string? text = null;

            // Act
            var testDelegate = () => IPv4Address.Parse(text!);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|String is null.|", ex.Message);
        }

        [TestMethod]
        public void IPv4Address_Parse_WhenTextDoesNotConsistOf4OctetsSeparatedWithPeriods_ShouldThrowError()
        {
            // Arrange
            string? text = "1.2";

            // Act
            var testDelegate = () => IPv4Address.Parse(text!);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|String does not consist of four octets separated with periods.|", ex.Message);
        }

        [TestMethod]
        public void IPv4Address_Parse_WhenGivenValidIPString_ShouldReturnParsedIPv4Address()
        {
            // Arrange
            var expIpAddress = new IPv4Address(0xFFFFFFFF);
            string? text = "255.255.255.255";

            // Act
            var actIpAddress = IPv4Address.Parse(text!);

            // Assert
            Assert.AreEqual(expIpAddress, actIpAddress);
        }
    }
}
