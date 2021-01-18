//Service.cs
using QKD_Key_Manager.Destilation;
using QKD_Key_Manager.KeyManagment;
using QKD_Key_Manager.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QKD_Key_Manager.Entities
{
    class Service : Entity
    {
        private int _currentKeyChunk = 0;

        public override void PrintStatus()
        {
            Console.WriteLine(this);
            Console.WriteLine("Local key buffer");
            Console.WriteLine($"\t* size: \t {LocalKeyStore.GetLength()}");
            Console.WriteLine($"\t* last received key index: \t {_currentKeyChunk}");
            Console.WriteLine();
            Console.WriteLine("Common service buffer");
            Console.WriteLine($"\t* size: \t {CommonKeyStore.ServiceKeyBuffer.GetLength()}");
            Console.WriteLine();
            Console.WriteLine("Common undecided buffer");
            Console.WriteLine($"\t* size: \t {CommonKeyStore.UndecidedKeyBuffer.GetLength()}");
            Console.WriteLine();
            Console.WriteLine("Common application buffers");
            CommonKeyStore.ApplicationKeyBuffers.ToList().ForEach(s => Console.WriteLine($"\t* {s.Key} size: \t {s.Value.GetLength()}"));
            Console.WriteLine();
        }

        protected override Message ReceiveAuthenticatedMessage(AuthenticatedMessage message)
        {
            if(message is BlocksParityRequest)
            {
                var blockParityRequestMessage = message as BlocksParityRequest;
                var blocksPayload = message.Payload as Blocks;
                for(int i = 0; i < blocksPayload.BlocksArray.Length; i++)
                {
                    var block = blocksPayload.BlocksArray[i];
                    var parity = Cascade.Parity(DestilationBuffer.GetBits(block.Index, block.Length));
                    blocksPayload.BlocksArray[i].Parity = parity;
                }
                var mac = MessageAuthenticator.GetMAC(blocksPayload.GetBytes());
                return new BlocksParityResponse(mac, blocksPayload);
            }
            if(message is CheckParity)
            {
                var checkParityMessage = message as CheckParity;
                var blockIdentification = checkParityMessage.Payload as BlockIdentifiaction;
                var parity = Cascade.Parity(DestilationBuffer.GetBits(blockIdentification.Index, blockIdentification.Length));
                var payload = new ParityPayload(blockIdentification.Index, blockIdentification.Length, parity);
                var mac = MessageAuthenticator.GetMAC(payload.GetBytes());
                return new Parity(mac, payload);

            }
            if(message is RequestBits)
            {
                var requestBitsMessage = message as RequestBits;
                var blockIdentification = requestBitsMessage.Payload as BlockIdentifiaction;
                var bytes = DestilationBuffer.GetBytes(blockIdentification.Index, blockIdentification.Length);
                var payload = new BitsAsBytes(blockIdentification.Index, blockIdentification.Length, bytes);
                var mac = MessageAuthenticator.GetMAC(payload.GetBytes());
                return new Bytes(mac,payload,requestBitsMessage.Demand);
            }
            if(message is AddKey)
            {
                var addKeyMessage = message as AddKey;
                var messagPayload = (addKeyMessage.Payload as BlockIdAndKeyAllocation);
                if(messagPayload.BlockId==_currentKeyChunk + 1)
                {
                    var key = LocalKeyStore.GetKey();
                    CommonKeyStore.AddNewKey(key, messagPayload.KeyAllocation);
                    _currentKeyChunk += 1;
                    return PrepareAckForCommonKeyProposition(_currentKeyChunk);
                }
                else
                {
                    if (messagPayload.BlockId == _currentKeyChunk)
                    {
                        return PrepareAckForCommonKeyProposition(_currentKeyChunk);
                    }
                    else
                    {
                        return new NOP();
                    }
                }
            }
            return new NOP();
        }

        protected override Message ReceiveOfflineMessage(OfflineMessage message)
        {
            if( message is AmplifiePrivacy)
            {
                var privacyLevel = (message as AmplifiePrivacy).PrivacyLevel;

                DestilationBuffer.Permute(CommonKeyStore.ServiceKeyBuffer.GetKey().GetBytes());
                DestilationBuffer.ShiftedXOr(CommonKeyStore.ServiceKeyBuffer.GetKey().GetBytes());
                DestilationBuffer.Slice(1 - privacyLevel);

            }
            if (message is MoreUndestiledBitsArrived)
            {
                var moreUndestiledBits = message as MoreUndestiledBitsArrived;
                DestilationBuffer.AddBytes(moreUndestiledBits.Bytes);
            }
            if (message is MoreBitsArrived)
            {
                var moreBitsArrivedMessage = message as MoreBitsArrived;
                foreach(var key in moreBitsArrivedMessage.Keys)
                {
                    LocalKeyStore.AddKey(key);
                    
                }
                return new NOP();
            }
            if(message is GetOutBufferKey)
            {
                var getOutBufferKeyMessage = message as GetOutBufferKey;
                ApplicationKeyBuffer correspondingKeyStore;
                CommonKeyStore.ApplicationKeyBuffers.TryGetValue(getOutBufferKeyMessage.Handle, out correspondingKeyStore);
                if (correspondingKeyStore != null)
                {
                    if (correspondingKeyStore.GetLength() >= getOutBufferKeyMessage.Count)
                    {
                        return new OutKey(correspondingKeyStore.GetKey(getOutBufferKeyMessage.Count));
                    }
                    else
                    {
                        return new NOP();
                    }
                }
                else
                {
                    return new NOP();
                }
            }
            if (message is OutKey)
            {
                Console.WriteLine((message as OutKey).Key.ToString());
                return new NOP();
            }
            if (message is CreateKeyBuffer)
            {
                var createKeyBufferMessage = message as CreateKeyBuffer;
                CommonKeyStore.AddKeyBuffer(createKeyBufferMessage.Handle, createKeyBufferMessage.QualityOfService);
                return new NOP();
            }
            if (message is CloseKeyBuffer)
            {
                var closeKeyBuffer = message as CloseKeyBuffer;
                CommonKeyStore.RemoveKeyBuffer(closeKeyBuffer.Handle);
                return new NOP();
            }
            return new NOP();
        }

        private Message PrepareAckForCommonKeyProposition(int propositionKeyChunkId)
        {
            var keyChunk = new BlockIdAndKeyAllocation(propositionKeyChunkId, CommonKeyStore.GetKeyAllocation());
            var mac = MessageAuthenticator.GetMAC(keyChunk.GetBytes());
            return new AddingKeyAck(mac, keyChunk);
        }
    }
}
