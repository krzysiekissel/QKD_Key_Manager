//Message.cs
using QKD_Key_Manager.Auth;
using QKD_Key_Manager.Entities;
using QKD_Key_Manager.KeyManagment;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using static QKD_Key_Manager.Destilation.Cascade;

namespace QKD_Key_Manager.Messaging
{
    abstract class Message
    {


    }
    abstract class Payload
    {
        protected byte[] _bytes { get; set; }

        public virtual byte[] GetBytes()
        {
            return _bytes;
        }

    }
    class BitsAsBytes : Payload
    {
        public int Index { get; set; }
        public int Length { get; set; }
        public BitsAsBytes(int index, int length,byte[] bytes)
        {
            _bytes = bytes;
            Index = index;
            Length = length;
        }
        public override byte[] GetBytes()
        {
            var result = new byte[8 + _bytes.Length];
            var index = BitConverter.GetBytes(Index);
            Array.Copy(index, result, 4);
            Array.Copy(BitConverter.GetBytes(Length), 0, result, 4, 4);
            Array.Copy(_bytes, 0, result, 8, _bytes.Length);
            return result;

        }
    }
    class Blocks : Payload
    {
        public Block[] BlocksArray { get; set; }

        public Blocks(Block[] blocks)
        {
            BlocksArray = blocks;
        }

        public override byte[] GetBytes()
        {
            var result = new byte[9 * BlocksArray.Length];
            for(int i = 0; i < BlocksArray.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(BlocksArray[i].Index),0, result,i*9, 4);
                Array.Copy(BitConverter.GetBytes(BlocksArray[i].Length), 0, result,i*9+ 4, 4);
                result[i*9+8] = (byte)(BlocksArray[i].Parity);
            }
            return result;
        }
    }
    class ParityPayload : Payload
    {
        public int Index { get; set; }
        public int Length { get; set; }
        public int Parity { get; set; }
        public ParityPayload(int index, int length,int parity)
        {
            Index = index;
            Length = length;
            Parity = parity;
        }

        public override byte[] GetBytes()
        {
            var result = new byte[9];

            Array.Copy(BitConverter.GetBytes(Index), result, 4);
            Array.Copy(BitConverter.GetBytes(Length), 0, result, 4, 4);
            result[8] = (byte)(Parity);

            return result;
        }
    }
    class BlockIdentifiaction : Payload
    {
        public int Index { get; set; }
        public int Length { get; set; }
        public BlockIdentifiaction(int index,int length)
        {
            Index = index;
            Length = length;
        }

        public override byte[] GetBytes()
        {
            var result = new byte[8];

            Array.Copy(BitConverter.GetBytes(Index), result, 4);
            Array.Copy(BitConverter.GetBytes(Length),0, result,4, 4);

            return result;
        }

    }
    class BlockIdAndKeyAllocation : Payload
    {
        public int BlockId { get; set; }
        public KeyAllocation KeyAllocation { get; set; }
        public BlockIdAndKeyAllocation(int blockId, KeyAllocation keyAllocation)
        {
            BlockId = blockId;
            KeyAllocation = keyAllocation;
        }


        public override byte[] GetBytes()
        {

            var formater = new BinaryFormatter();
            byte[] serializedKeyAllocation;
            using (var stream = new MemoryStream())
            {
                formater.Serialize(stream, KeyAllocation.Allocation);
                serializedKeyAllocation = stream.ToArray();
            }
            byte[] serializedBlockId = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(serializedBlockId, BlockId);

            byte[] finialBytes = new byte[4 + serializedKeyAllocation.Length];
            Array.Copy(serializedBlockId, 0, finialBytes, 0, 4);
            Array.Copy(serializedKeyAllocation, 0, finialBytes, 4, serializedKeyAllocation.Length);
            return finialBytes;

        }

    }
    abstract class AuthenticatedMessage : Message
    {
        public MAC MAC { get; set; }
        public Payload Payload { get; set; }
        public AuthenticatedMessage(MAC mac, Payload payload)
        {
            MAC = mac;
            Payload = payload;
        }
    }
    abstract class OfflineMessage : Message
    {

    }
    class AddKey : AuthenticatedMessage
    {
        public AddKey(MAC mac, BlockIdAndKeyAllocation chunkId) : base(mac, chunkId) { }
    }
    class AddingKeyAck : AuthenticatedMessage
    {
        public AddingKeyAck(MAC mac, BlockIdAndKeyAllocation chunkId) : base(mac, chunkId) { }
    }
    class MoreUndestiledBitsArrived : OfflineMessage
    {
        public byte[] Bytes { get; set; }
        public MoreUndestiledBitsArrived(byte[] bytes)
        {
            Bytes = bytes;
        }
    }

    class MoreBitsArrived : OfflineMessage
    {
        public List<Key> Keys { get; set; }
        public MoreBitsArrived(List<Key> keys)
        {
            Keys = keys;
        }
    }
    class Initiate : OfflineMessage { }
    class InitiateDestilation : OfflineMessage { }
    class InitiateCascade : OfflineMessage 
    {
        public Service Service { get; set; }
        public ClientServerMessageBus Bus { get; set; }
    }
    class AmplifiePrivacy : OfflineMessage
    {
        public double PrivacyLevel { get; set; }
    }
    class EstimateQBER : OfflineMessage
    {
        public int Index { get; set; }
        public int Length { get; set; }
        public EstimateQBER(int index,int length)
        {
            Index = index;
            Length = length;
        }
    }
    class Parity : AuthenticatedMessage
    {
        public Parity(MAC mac, ParityPayload parityPayload) : base(mac, parityPayload) { }
    }
    class BlocksParityRequest : AuthenticatedMessage
    {
        public BlocksParityRequest(MAC mac, Blocks blocks) : base(mac, blocks) { }
    }
    class BlocksParityResponse : AuthenticatedMessage
    {
        public BlocksParityResponse(MAC mac, Blocks blocks) : base(mac, blocks) { }
    }
    class CheckParity : AuthenticatedMessage
    {
        public CheckParity(MAC mac,BlockIdentifiaction blockIdentifiaction) : base(mac, blockIdentifiaction) { }
    }
    public enum Demand
    {
        Estimation,
        Confirmation
    }
    class RequestBits : AuthenticatedMessage
    {
        public Demand Demand;
        public RequestBits(MAC mac, Payload payload,Demand demand) : base(mac, payload)
        {
            Demand = demand;
        }
    }

    class Bytes : AuthenticatedMessage
    {
        public Demand Demand;
        public Bytes(MAC mac, Payload payload, Demand demand) : base(mac, payload)
        {
            Demand = demand;
        }
    }

    class GetOutBufferKey : OfflineMessage
    {
        public string Handle { get; set; }
        public int Count { get; set; }
        public GetOutBufferKey(string handle, int count)
        {
            Handle = handle;
            Count = count;
        }
    }
    class OutKey : OfflineMessage
    {
        public Key Key { get; set; }
        public OutKey(Key key)
        {
            Key = key;
            Console.WriteLine(Key.ToString());
        }
    }
    class CreateKeyBuffer : OfflineMessage
    {
        public string Handle { get; set; }
        public QualityOfService QualityOfService { get; set; }
        public CreateKeyBuffer(string handle,QualityOfService qualityOfService)
        {
            Handle = handle;
            QualityOfService = qualityOfService;
        }
    }
    class CloseKeyBuffer : OfflineMessage
    {
        public string Handle { get; set; }
        public CloseKeyBuffer(string handle)
        {
            Handle = handle;
        }
    }
    class NOP : Message
    {

    }
}
