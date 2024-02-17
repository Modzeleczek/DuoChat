using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shared.MVVM.Core;
using Shared.MVVM.Model.Cryptography;
using Shared.MVVM.Model.Networking.Transfer;
using Shared.MVVM.Model.Networking.Transfer.Reception;
using Shared.MVVM.Model.Networking.Transfer.Transmission;
using System.Net;
using System.Text;
using UnitTests.Mocks;

namespace UnitTests
{
    [TestClass]
    public class NetworkingTests
    {
        [TestMethod]
        public void IPAddress_HostToNetworkOrder_Ushort_ShouldBeRestorable()
        {
            // Arrange
            ushort expected = 0x9B40;

            // Act
            short inNetworkOrder = IPAddress.HostToNetworkOrder((short)expected);
            ushort actual = (ushort)IPAddress.NetworkToHostOrder(inNetworkOrder);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void PacketBuilder_Build_UnencryptedUnauthenticatedPacket_ShouldReturnExpectedPacket()
        {
            // Arrange
            PacketBuilder pb = new PacketBuilder();
            byte[] expPacket = new byte[] {
                0x12, 0x34,
                0x00, 0x0A, 0xBC, 0xDE
            };

            // Act
            pb.Append(0x1234, 2);
            pb.Append(0xABCDE, 4);
            byte[] actPacket = pb.Build();

            // Assert
            Console.WriteLine($"expected: {expPacket.ToHexString()}");
            Console.WriteLine($"actual: {actPacket.ToHexString()}");

            expPacket.BytesEqual(actPacket);
        }

        [TestMethod]
        public void PacketBuilder_Build_UnencryptedUnauthenticatedPacketAfterOnly1Append_ShouldReturnExpectedPacket()
        {
            // Arrange
            PacketBuilder pb = new PacketBuilder();
            byte[] expPacket = new byte[]
            {
                0x12, 0x34,
                0x00, 0x0A, 0xBC, 0xDE
            };

            // Act
            pb.Append(0x1234000ABCDE, 6);
            byte[] actPacket = pb.Build();

            // Assert
            expPacket.BytesEqual(actPacket);
        }

        [TestMethod]
        public void PacketBuilder_Build_UnencryptedAuthenticatedPacket_ShouldReturnExpectedPacket()
        {
            // Arrange
            PrivateKey privateKey = PrivateKey.Random();
            PublicKey publicKey = privateKey.ToPublicKey();
            PacketBuilder pb = new PacketBuilder();

            // Act
            pb.Append(0x1234, 2);
            pb.Append(0xABCDE, 4);
            pb.Sign(privateKey);
            pb.Prepend(0x03, 1);
            byte[] built = pb.Build();

            // Assert
            byte[] expOpCode = new byte[] { 0x03 };
            byte[] actOpCode = built.Slice(0, 1);
            expOpCode.BytesEqual(actOpCode);

            byte[] actSignatureLength = built.Slice(1, 2);
            ushort actSignatureLengthValue = (ushort)IPAddress.NetworkToHostOrder(
                BitConverter.ToInt16(actSignatureLength, 0));

            byte[] expPayload = new byte[] { 0x12, 0x34, 0x00, 0x0A, 0xBC, 0xDE };
            byte[] actPayload = built.Slice(3 + actSignatureLengthValue, 6);
            expPayload.BytesEqual(actPayload);

            byte[] actSignature = built.Slice(3, actSignatureLengthValue);
            Assert.IsTrue(Rsa.Verify(publicKey, actPayload, actSignature));

            Console.WriteLine($"op code: size {actOpCode.Length}; {actOpCode.ToHexString()}");
            Console.WriteLine($"signature length: size {actSignatureLength.Length}; " +
                $"{actSignatureLength.ToHexString()}");
            Console.WriteLine($"signature: size {actSignature.Length}; {actSignature.ToHexString()}");
            Console.WriteLine($"payload: size {actPayload.Length}; {actPayload.ToHexString()}");
        }

        [TestMethod]
        public void PacketBuilder_Build_UnencryptedAuthenticatedPacket_ShouldReturnExpectedPacket_AssertUsingPacketReader()
        {
            // Arrange
            PrivateKey privateKey = PrivateKey.Random();
            PublicKey publicKey = privateKey.ToPublicKey();
            PacketBuilder pb = new PacketBuilder();

            // Act
            pb.Append(0x1234, 2);
            pb.Append(0xABCDE, 4);
            pb.Sign(privateKey);
            pb.Prepend(0x03, 1);
            byte[] built = pb.Build();

            // Assert
            PacketReader pr = new PacketReader(built);

            int actOpCode = pr.ReadUInt8();
            Assert.AreEqual(0x03, actOpCode);

            Assert.IsTrue(pr.VerifySignature(publicKey));

            byte[] actPayload = pr.ReadBytes(6);
            new byte[] { 0x12, 0x34, 0x00, 0x0A, 0xBC, 0xDE }.BytesEqual(actPayload);

            Console.WriteLine($"op code: {actOpCode}");
            Console.WriteLine($"payload: {actPayload.ToHexString()}");
        }

        [TestMethod]
        public void PacketBuilder_Build_EncryptedUnauthenticatedPacket_ShouldReturnExpectedPacket()
        {
            // Arrange
            PrivateKey privateKey = PrivateKey.Random();
            PublicKey publicKey = privateKey.ToPublicKey();
            PacketBuilder pb = new PacketBuilder();

            // Act
            pb.Append(0x1234, 2);
            pb.Append(0xABCDE, 4);
            pb.Encrypt(publicKey);
            pb.Prepend(0x03, 1);
            byte[] built = pb.Build();

            // Assert
            PacketReader pr = new PacketReader(built);

            int actOpCode = pr.ReadUInt8();
            Assert.AreEqual(0x03, actOpCode);

            pr.Decrypt(privateKey);

            byte[] actPayload = pr.ReadBytes(6);
            new byte[] { 0x12, 0x34, 0x00, 0x0A, 0xBC, 0xDE }.BytesEqual(actPayload);

            Console.WriteLine($"op code: {actOpCode}");
            Console.WriteLine($"payload: {actPayload.ToHexString()}");
        }

        [TestMethod]
        public void PacketBuilder_Build_EncryptedAuthenticatedPacket_ShouldReturnExpectedPacket()
        {
            // Arrange
            PrivateKey receiverPrivateKey = PrivateKey.Random();
            PublicKey receiverPublicKey = receiverPrivateKey.ToPublicKey();

            PrivateKey senderPrivateKey = PrivateKey.Random();
            PublicKey senderPublicKey = senderPrivateKey.ToPublicKey();
            PacketBuilder pb = new PacketBuilder();

            // Act
            pb.Append(0x1234, 2);
            pb.Append(0xABCDE, 4);
            pb.Sign(senderPrivateKey);
            pb.Encrypt(receiverPublicKey);
            pb.Prepend(0x03, 1);
            byte[] built = pb.Build();

            // Assert
            PacketReader pr = new PacketReader(built);

            int actOpCode = pr.ReadUInt8();
            Assert.AreEqual(0x03, actOpCode);

            pr.Decrypt(receiverPrivateKey);

            Assert.IsTrue(pr.VerifySignature(senderPublicKey));

            byte[] actPayload = pr.ReadBytes(6);
            new byte[] { 0x12, 0x34, 0x00, 0x0A, 0xBC, 0xDE }.BytesEqual(actPayload);

            Console.WriteLine($"op code: {actOpCode}");
            Console.WriteLine($"payload: {actPayload.ToHexString()}");
        }

        [TestMethod]
        public void PacketBuilder_Sign_WithoutData_ShouldOnlyPrependSignature()
        {
            // Arrange
            PrivateKey privateKey = PrivateKey.Random();
            PublicKey publicKey = privateKey.ToPublicKey();
            PacketBuilder pb = new PacketBuilder();

            // Act
            pb.Sign(privateKey);
            byte[] built = pb.Build();

            // Assert
            PacketReader pr = new PacketReader(built);

            Assert.IsTrue(pr.VerifySignature(publicKey));

            pr = new PacketReader(built);
            int actSignatureLength = pr.ReadUInt16();
            byte[] actSignatureValue = pr.ReadBytes(actSignatureLength);

            Console.WriteLine($"signature length: {actSignatureLength}");
            Console.WriteLine($"signature value: {actSignatureValue.ToHexString()}");
        }

        [TestMethod]
        public void PacketReceiveBuffer_ReceiveUntilCompletedOrInterrupted_WhenReceived2OrdinaryPackets_ShouldReturnThem()
        {
            // Arrange
            var packetReceiveBuffer = new PacketReceiveBuffer();

            int[] returnedByteCounts = new int[] { 4, 3, 5, 2 };
            byte[] byteStream = new byte[]
            {
                0, 0, 0, 4, 1, 2, 3, 4, // Pierwszy pakiet
                0, 0, 0, 2, 5, 6 // Drugi pakiet
            };
            var socketMock = new ReceiveSocketMock(returnedByteCounts, byteStream);

            // Act
            byte[]? actualPacket0 = packetReceiveBuffer.ReceiveUntilCompletedOrInterrupted(
                socketMock, CancellationToken.None);
            byte[]? actualPacket1 = packetReceiveBuffer.ReceiveUntilCompletedOrInterrupted(
                socketMock, CancellationToken.None);

            // Assert
            Console.WriteLine(actualPacket0!.ToHexString());
            Console.WriteLine(actualPacket1!.ToHexString());

            byteStream.Slice(4, 4).BytesEqual(actualPacket0!); // Pierwszy pakiet
            byteStream.Slice(12, 2).BytesEqual(actualPacket1!); // Drugi pakiet
        }

        [TestMethod]
        public void PacketReceiveBuffer_ReceiveUntilCompletedOrInterrupted_WhenReceived0Bytes_ShouldReturnNull()
        {
            // Arrange
            var packetReceiveBuffer = new PacketReceiveBuffer();

            int[] returnedByteCounts = new int[] { };
            byte[] byteStream = new byte[] { };
            var socketMock = new ReceiveSocketMock(returnedByteCounts, byteStream);

            // Act
            byte[]? actualPacket0 = packetReceiveBuffer.ReceiveUntilCompletedOrInterrupted(
                socketMock, CancellationToken.None);
            byte[]? actualPacket1 = packetReceiveBuffer.ReceiveUntilCompletedOrInterrupted(
                socketMock, CancellationToken.None);

            // Assert
            Assert.IsNull(actualPacket0);
            Assert.IsNull(actualPacket1);
        }

        [TestMethod]
        public void PacketReceiveBuffer_ReceiveUntilCompletedOrInterrupted_WhenReceivedKeepAlive_ShouldReturn0LengthPacket()
        {
            // Arrange
            var packetReceiveBuffer = new PacketReceiveBuffer();

            int[] returnedByteCounts = new int[] { 1, 2, 1 };
            byte[] byteStream = new byte[] { 0, 0, 0, 0 }; // Keep alive
            var socketMock = new ReceiveSocketMock(returnedByteCounts, byteStream);

            // Act
            byte[]? actualPacket = packetReceiveBuffer.ReceiveUntilCompletedOrInterrupted(
                socketMock, CancellationToken.None);

            // Assert
            Assert.AreEqual(0, actualPacket!.Length);
        }

        [TestMethod]
        public void PacketReceiveBuffer_ReceiveUntilCompletedOrInterrupted_WhenReceivedIncompletePacket_ShouldReturnNull()
        {
            // Arrange
            var packetReceiveBuffer = new PacketReceiveBuffer();

            int[] returnedByteCounts = new int[] { 1, 1, 1 };
            byte[] byteStream = new byte[] { 1, 0, 0 }; // Niepełny pakiet
            var socketMock = new ReceiveSocketMock(returnedByteCounts, byteStream);

            // Act
            byte[]? actualPacket = packetReceiveBuffer.ReceiveUntilCompletedOrInterrupted(
                socketMock, CancellationToken.None);

            // Assert
            Assert.IsNull(actualPacket);
        }

        [TestMethod]
        public void PacketReceiveBuffer_ReceiveUntilCompletedOrInterrupted_WhenBufferFull_ShouldReturnToBufferStart()
        {
            // Arrange
            var packetReceiveBuffer = new PacketReceiveBuffer(1 << 16);

            const int numberOfKeepAlives = 1 << 18;
            int[] returnedByteCounts = new int[numberOfKeepAlives];
            byte[] byteStream = new byte[SocketWrapper.PACKET_PREFIX_SIZE * numberOfKeepAlives];
            for (int i = 0; i < numberOfKeepAlives; ++i)
            {
                returnedByteCounts[i] = SocketWrapper.PACKET_PREFIX_SIZE;
                for (int j = i * 4; j < i * 4 + 4; ++j)
                    byteStream[j] = 0; // Keep alive
            }
            var socketMock = new ReceiveSocketMock(returnedByteCounts, byteStream);

            for (int i = 0; i < numberOfKeepAlives; ++i)
            {
                // Act
                byte[]? actualPacket = packetReceiveBuffer.ReceiveUntilCompletedOrInterrupted(
                    socketMock, CancellationToken.None);

                // Assert
                Assert.AreEqual(0, actualPacket!.Length);
            }
        }

        [TestMethod]
        public void PacketReceiveBuffer_ReceiveUntilCompletedOrInterrupted_WhenSocketReturnsOnlyPacketPrefixOfValue0_ShouldCreatePacketOfLength0()
        {
            // Arrange
            var packetReceiveBuffer = new PacketReceiveBuffer();

            int[] returnedByteCounts = new int[] { 2, 1, 1 }; // 4 - rozmiar prefiksu
            byte[] byteStream = new byte[] { 0, 0, 0, 0 }; // Sam prefiks
            var socketMock = new ReceiveSocketMock(returnedByteCounts, byteStream);

            // Act
            byte[]? actualPacket = packetReceiveBuffer.ReceiveUntilCompletedOrInterrupted(
                socketMock, CancellationToken.None);

            // Assert
            Assert.AreEqual(0, actualPacket?.Length);
        }

        [TestMethod]
        public void PacketReceiveBuffer_ReceiveUntilCompletedOrInterrupted_WhenSocketReturnsTooBigPacketPrefix_ShouldThrowReceived_packet_with_prefix_value_greater_than_max_packet_size()
        {
            // Arrange
            var packetReceiveBuffer = new PacketReceiveBuffer();

            int[] returnedByteCounts = new int[] { 2, 1, 1 }; // 4 - rozmiar prefiksu
            byte[] byteStream = new byte[] { 1, 3, 2, 1 }; // Za duża wartość prefiksu
            var socketMock = new ReceiveSocketMock(returnedByteCounts, byteStream);

            // Act
            var testDelegate = () => packetReceiveBuffer.ReceiveUntilCompletedOrInterrupted(
                socketMock, CancellationToken.None);

            // Assert
            var ex = Assert.ThrowsException<Error>(testDelegate);
            Assert.AreEqual("|Received packet with prefix value greater than max packet size|.",
                ex.Message);
        }

        [TestMethod]
        public void PacketReceiveBuffer_ReceiveUntilCompletedOrInterrupted_WhenPacketStartsAtEndOfBufferAndEndsAtStartOfBuffer_ShouldCorrectlyReturnIt()
        {
            // Arrange
            var packetReceiveBuffer = new PacketReceiveBuffer(9);

            // Socket zwraca: keep alive, 3 bajty do końca bufora, 2 bajty na początku bufora
            int[] returnedByteCounts = new int[] { 4, 5, 2 };
            byte[] byteStream = new byte[] { 0, 0, 0, 0 /* pakiet 1 - keep alive. */,
                0, 0, 0, 3, 0xAB, 0xCD, 0xEF /* pakiet 2 */ };
            var socketMock = new ReceiveSocketMock(returnedByteCounts, byteStream);

            // Act
            byte[]? actualKeepAlive = packetReceiveBuffer.ReceiveUntilCompletedOrInterrupted(
                socketMock, CancellationToken.None);
            byte[]? actualOverlappingPacket = packetReceiveBuffer.ReceiveUntilCompletedOrInterrupted(
                socketMock, CancellationToken.None);

            // Assert
            Assert.AreEqual(0, actualKeepAlive!.Length);
            byteStream.Slice(8, 3).BytesEqual(actualOverlappingPacket!);
        }

        [TestMethod]
        public void PacketSendBuffer_SendUntilCompletedOrInterrupted_WhenGivenPackets_ShouldConsumeThem()
        {
            // Arrange
            var packetSendBuffer = new PacketSendBuffer();

            /* PacketSendBuffer dodaje prefiksy do pakietów.
                                                   ^     ^     ^     ^     ^ */
            int[] returnedByteCounts = new int[] { 4, 4, 4, 2, 4, 0, 4, 0, 4, 0 };
            byte[][] expectedPackets = new byte[][]
            {
                new byte[] { 1, 2, 3, 4 }, // Pierwszy pakiet
                new byte[] { 5, 6 }, // Drugi pakiet
                new byte[] { }, // Keep alive
                new byte[] { }, // Keep alive
                new byte[] { } // Keep alive
            };
            var socketMock = new SendSocketMock(returnedByteCounts);

            // Act
            foreach (var expectedPacket in expectedPackets)
                packetSendBuffer.SendUntilCompletedOrInterrupted(
                    socketMock, CancellationToken.None, expectedPacket);

            // Assert
            int packetStartIndex = 0;
            foreach (var expectedPacket in expectedPackets)
            {
                packetStartIndex += SocketWrapper.PACKET_PREFIX_SIZE;

                byte[] actualPacket = socketMock.ByteStream.Slice(packetStartIndex,
                    expectedPacket.Length);

                Console.WriteLine(actualPacket.ToHexString());
                expectedPacket.BytesEqual(actualPacket);

                packetStartIndex += expectedPacket.Length;
            }
        }

        [TestMethod]
        public void PacketSendBuffer_SendUntilCompletedOrInterrupted_WhenSocketReturnsPacketInFragments_ShouldMergeThem()
        {
            // Arrange
            var packetSendBuffer = new PacketSendBuffer();

            // Muszą sumować się do długości prefiksu + długości pakietu.
            int[] returnedByteCounts = new int[] { 1, 2, 1, 3, 1, 2 };
            byte[] expectedPacket = new byte[] { 1, 2, 3, 4, 5, 6 };
            var socketMock = new SendSocketMock(returnedByteCounts);

            // Act
            packetSendBuffer.SendUntilCompletedOrInterrupted(
                socketMock, CancellationToken.None, expectedPacket);

            // Assert
            byte[] actualPacket = socketMock.ByteStream.Slice(SocketWrapper.PACKET_PREFIX_SIZE,
                expectedPacket.Length);

            Console.WriteLine(actualPacket.ToHexString());
            expectedPacket.BytesEqual(actualPacket);
        }

        [TestMethod]
        public void PacketReader_ReadUtf8String_ShouldReturnString()
        {
            // Arrange
            string expString = "text";
            var pb = new PacketBuilder();
            pb.Append((ulong)expString.Length, sizeof(int));
            pb.Append(Encoding.UTF8.GetBytes(expString));
            var pr = new PacketReader(pb.Build());

            // Act
            string actString = pr.ReadUtf8String((int)pr.ReadUInt32());

            // Assert
            Assert.AreEqual(expString, actString);
        }

        [TestMethod]
        public void PacketReader_ReadBytesToEnd_ShouldReturnAllPacketBytesBeginningFromTheCurrentPosition()
        {
            // Arrange
            byte[] packet = new byte[] { 0, 1, 2, 3 };
            var pr = new PacketReader(packet);

            // Act
            pr.ReadUInt16();
            byte[] actReadToEnd = pr.ReadBytesToEnd();

            // Assert
            packet.Slice(2, 2).BytesEqual(actReadToEnd);
        }

        [TestMethod]
        public void PacketReader_ReadGuid_ShouldReturnGuid()
        {
            // Arrange
            Guid expGuid = Guid.NewGuid();
            var pr = new PacketReader(expGuid.ToByteArray());

            // Act
            Guid actGuid = pr.ReadGuid();

            // Assert
            Assert.AreEqual(expGuid, actGuid);
        }

        [TestMethod]
        public void PacketReader_ReadUInt64_ShouldReturnUInt64()
        {
            // Arrange
            ulong expUInt64 = 15;
            var pb = new PacketBuilder();
            pb.Append(expUInt64, sizeof(ulong));
            var pr = new PacketReader(pb.Build());

            // Act
            ulong actUInt64 = pr.ReadUInt64();

            // Assert
            Assert.AreEqual(expUInt64, actUInt64);
        }

        [TestMethod]
        public void PacketSendBuffer_SendUntilCompletedOrInterrupted_WhenTriedToSendNewPacketBeforeCompletelySendingCurrentOne_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var packetSendBuffer = new PacketSendBuffer();

            // Wysyłamy tylko prefiks i pierwsze 3 z 4 bajtów pakietu packet0.
            int[] returnedByteCounts = new int[] { 4, 3 };
            byte[] packet0 = new byte[] { 1, 2, 3, 4 };
            byte[] packet1 = new byte[] { 1 };
            var socketMock = new SendSocketMock(returnedByteCounts);

            // Act
            var testDelegate = () =>
            {
                var cts = new CancellationTokenSource();
                /* SendUntilCompletedOrInterrupted dla packet0 kręci się w nieskończonej pętli,
                bo mock socketa "wysłał" tylko 7/8 bajtów i potem zwraca 0 wysłanych bajtów. */
                cts.CancelAfter(100);
                try
                {
                    /* Po zcancelowaniu tokenu, SendUntilCompletedOrInterrupted rzuca wyjątek
                    OperationCanceledException i zwraca sterowanie. */
                    packetSendBuffer.SendUntilCompletedOrInterrupted(
                        socketMock, cts.Token, packet0);
                }
                catch (OperationCanceledException) { }
                // Nie skończyliśmy wysyłać packet0, ale zmieniamy na packet1 i próbujemy go wysyłać.
                packetSendBuffer.SendUntilCompletedOrInterrupted(
                    socketMock, CancellationToken.None, packet1);
            };

            // Assert
            var ex = Assert.ThrowsException<InvalidOperationException>(testDelegate);
            Assert.AreEqual("Tried to send a new packet before completely sending the previous one.",
                ex.Message);
        }

        [TestMethod]
        public void PacketSendBuffer_SendUntilCompletedOrInterrupted_WhenCanceledBeforeCalling_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var packetSendBuffer = new PacketSendBuffer();

            int[] returnedByteCounts = new int[] { 4, 3 };
            byte[] packet0 = new byte[] { 1, 2, 3, 4 };
            byte[] packet1 = new byte[] { 1 };
            var socketMock = new SendSocketMock(returnedByteCounts);

            // Act
            var testDelegate = () =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                try
                {
                    packetSendBuffer.SendUntilCompletedOrInterrupted(
                        socketMock, cts.Token, packet0);
                }
                catch (OperationCanceledException) { }
                packetSendBuffer.SendUntilCompletedOrInterrupted(
                    socketMock, CancellationToken.None, packet1);
            };

            // Assert
            var ex = Assert.ThrowsException<InvalidOperationException>(testDelegate);
            Assert.AreEqual("Tried to send a new packet before completely sending the previous one.",
                ex.Message);
        }

        [TestMethod]
        public void ByteArrayExtensions_BytesEqual_WhenParametersAreOfTypeIEnumerableAndHaveDifferentLengths_ShouldThrowAssertFailedException()
        {
            // Arrange
            var a = (IEnumerable<byte>)new byte[] { 1, 2, 3 };
            var b = (IEnumerable<byte>)new byte[] { 1, 2 };

            // Act
            var testDelegate = () =>
            {
                a.BytesEqual(b);
            };

            // Assert
            var ex = Assert.ThrowsException<AssertFailedException>(testDelegate);
            Assert.IsTrue(ex.Message.Contains("expected and actual have different lengths"));
        }

        [TestMethod]
        public void PacketSendBuffer_Reset_WhenCalledAfterCancellingSendingDueToSocketRepeatedlyReturning0BytesSent_ShouldResetPacketSendBufferState()
        {
            // Arrange
            var packetSendBuffer = new PacketSendBuffer();

            byte[] packet = new byte[] { 1, 2, 3, 4 };
            // Do socketu socketMock0 wysyłamy tylko prefiks i pierwsze 3 z 4 bajtów pakietu packet.
            int[] returnedByteCounts0 = new int[] { 4, 3 };
            var socketMock0 = new SendSocketMock(returnedByteCounts0);
            // Do socketu socketMock1 wysyłamy prefiks i cały pakiet packet.
            int[] returnedByteCounts1 = new int[] { 4, 4 };
            var socketMock1 = new SendSocketMock(returnedByteCounts1);

            // Act
            var cts = new CancellationTokenSource();
            cts.CancelAfter(100);
            try
            {
                packetSendBuffer.SendUntilCompletedOrInterrupted(
                    socketMock0, cts.Token, packet);
            }
            catch (OperationCanceledException) { }
            packetSendBuffer.Reset();
            packetSendBuffer.SendUntilCompletedOrInterrupted(
                socketMock1, CancellationToken.None, packet);

            // Assert
            byte[] prefixBytes = new byte[] { 0, 0, 0, 4 };
            prefixBytes.Concat(packet.Slice(0, 3)).BytesEqual(socketMock0.GetSentBytes());
            prefixBytes.Concat(packet).BytesEqual(socketMock1.GetSentBytes());
        }

        [TestMethod]
        public void PacketSendBuffer_Reset_WhenCalledAfterCancellingBeforeEvenCallingSend_ShouldResetPacketSendBufferState()
        {
            // Arrange
            var packetSendBuffer = new PacketSendBuffer();

            byte[] packet = new byte[] { 1, 2, 3, 4 };
            int[] returnedByteCounts0 = new int[] { 4, 3 };
            var socketMock0 = new SendSocketMock(returnedByteCounts0);
            int[] returnedByteCounts1 = new int[] { 4, 4 };
            var socketMock1 = new SendSocketMock(returnedByteCounts1);

            // Act
            var cts = new CancellationTokenSource();
            cts.Cancel();
            try
            {
                packetSendBuffer.SendUntilCompletedOrInterrupted(
                    socketMock0, cts.Token, packet);
            }
            catch (OperationCanceledException) { }
            packetSendBuffer.Reset();
            packetSendBuffer.SendUntilCompletedOrInterrupted(
                socketMock1, CancellationToken.None, packet);

            // Assert
            /* Nic się nie wyłało do socketMock0, bo zcancelowaliśmy
            token jeszcze przed rozpoczęciem wysyłania. */
            new byte[] { }.BytesEqual(socketMock0.GetSentBytes());
            byte[] prefixBytes = new byte[] { 0, 0, 0, 4 };
            prefixBytes.Concat(packet).BytesEqual(socketMock1.GetSentBytes());
        }
    }
}
