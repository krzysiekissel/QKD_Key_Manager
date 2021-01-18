//Initiator.cs
using QKD_Key_Manager.Destilation;
using QKD_Key_Manager.Exceptions;
using QKD_Key_Manager.KeyManagment;
using QKD_Key_Manager.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static QKD_Key_Manager.Destilation.Cascade;

namespace QKD_Key_Manager.Entities
{
    class Initiator : Entity
    {
        private int _lastSendKeyIndex = 0;
        private int _lastReceivedKeyIndexAck = 0;
        private bool DoesAckCorrespondsToLastSentKeyIndex(BlockIdAndKeyAllocation chunkId)
        {
            return chunkId.BlockId == _lastSendKeyIndex;
        }
        private bool DoesLasReceivedAckCorrespondsToLastSentKeyIndex()
        {
            return _lastSendKeyIndex == _lastReceivedKeyIndexAck;
        }
        public override void PrintStatus()
        {
            Console.WriteLine(this);
            Console.WriteLine("Local key buffer");
            Console.WriteLine($"\t* size: \t {LocalKeyStore.GetLength()}");
            Console.WriteLine($"\t* last send key index: \t {_lastSendKeyIndex}");
            Console.WriteLine($"\t* last received key index ack: \t {_lastReceivedKeyIndexAck}");
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
            
            if (message is Bytes)
            {
                var bytesMessage = message as Bytes;
                var bitsAsBytesPayload = bytesMessage.Payload as BitsAsBytes;
                
                if (bytesMessage.Demand == Demand.Estimation)
                {
                    var bobBits = new BitArray(bitsAsBytesPayload.GetBytes().ToList().Skip(8).ToArray());
                    var aliceBits = new BitArray(DestilationBuffer.GetBytes(bitsAsBytesPayload.Index, bitsAsBytesPayload.Length));
                    var estimatedQBER = Cascade.EstimateQBER(aliceBits, bobBits);
                    DestilationBuffer.SetEstimatedQBER(estimatedQBER);
                }
                if(bytesMessage.Demand == Demand.Confirmation)
                {

                }
            }
            if(message is AddingKeyAck)
            {
                var addingKeyAckMessage = message as AddingKeyAck;
               
                if(DoesAckCorrespondsToLastSentKeyIndex(addingKeyAckMessage.Payload as BlockIdAndKeyAllocation))
                {
                    _lastReceivedKeyIndexAck = _lastSendKeyIndex;

                    var key = LocalKeyStore.GetKey();
                    CommonKeyStore.AddNewKey(key, (addingKeyAckMessage.Payload as BlockIdAndKeyAllocation).KeyAllocation);

                    if (!LocalKeyStore.IsEmpty())
                    {
                        _lastSendKeyIndex += 1;
                        return PrepareMessageForNewKeyChunk(_lastSendKeyIndex);
                    }
                    else
                    {
                        return new NOP();
                    }
                }
                else
                {
                    throw new UnexpectedMessageException();
                }
            }
            return new NOP();
        }
        protected override Message ReceiveOfflineMessage(OfflineMessage message)
        {
            if (message is AmplifiePrivacy)
            {
                var privacyLevel = (message as AmplifiePrivacy).PrivacyLevel;

                DestilationBuffer.Permute(CommonKeyStore.ServiceKeyBuffer.GetKey().GetBytes());
                DestilationBuffer.ShiftedXOr(CommonKeyStore.ServiceKeyBuffer.GetKey().GetBytes());
                DestilationBuffer.Slice(1 - privacyLevel);

            }
            if (message is InitiateCascade)
            {
                var initMesssage = message as InitiateCascade;
                var alpha = 0.6;
                var initialBlockSize =  (int)(alpha/DestilationBuffer.GetEstimatedQBER());
                var cascade = new Cascade(this, initMesssage.Service, initMesssage.Bus);
                var properInitialBlockSize = initialBlockSize;
                int rem;
                int div = Math.DivRem(DestilationBuffer.GetSize(), properInitialBlockSize, out rem);
                var initialBlocks = new List<Block>();
                for(int i = 0; i < div; i++)
                {
                    initialBlocks.Add(new Block() { Index = i * properInitialBlockSize, Length = properInitialBlockSize });
                }
                if (rem != 0)
                {
                    initialBlocks.Add(new Block() { Index = properInitialBlockSize * div, Length = rem });
                }
                cascade.CorrectErrors(initialBlocks);

            }
            if(message is MoreUndestiledBitsArrived)
            {
                var moreUndestiledBits = message as MoreUndestiledBitsArrived;
                DestilationBuffer.AddBytes(moreUndestiledBits.Bytes);
            }
            if(message is InitiateDestilation)
            {
                if (!DestilationBuffer.IsEmpty())
                {
                    return new NOP();
                }
                throw new DestilationBufferIsEmptyExeption();
            }
            if( message is EstimateQBER)
            {
                var estimateQBERMessage = message as EstimateQBER;
                var payload = new BlockIdentifiaction(estimateQBERMessage.Index, estimateQBERMessage.Length);
                var mac = MessageAuthenticator.GetMAC(payload.GetBytes());
                return new RequestBits(mac, payload,Demand.Estimation);
            }
            if(message is MoreBitsArrived)
            {
                var moreBitArrivedMessage = message as MoreBitsArrived;
                foreach(var key in moreBitArrivedMessage.Keys)
                {
                    LocalKeyStore.AddKey(key);
                }
                return new NOP();
            }
            if(message is Initiate)
            {
                if (!LocalKeyStore.IsEmpty())
                {
                    if (DoesLasReceivedAckCorrespondsToLastSentKeyIndex())
                    {
                        _lastSendKeyIndex += 1;
                        return PrepareMessageForNewKeyChunk(_lastSendKeyIndex);
                    }
                    else
                    {
                        return PrepareMessageForNewKeyChunk(_lastSendKeyIndex);
                    }
                }
            }
            if(message is GetOutBufferKey)
            {
                var getOutBufferKeyMessage = message as GetOutBufferKey;
                ApplicationKeyBuffer keyStore;
                CommonKeyStore.ApplicationKeyBuffers.TryGetValue(getOutBufferKeyMessage.Handle,out keyStore);
                if (keyStore != null)
                {
                    if (keyStore.GetLength() >= getOutBufferKeyMessage.Count)
                    {
                        return new OutKey(keyStore.GetKey(getOutBufferKeyMessage.Count));
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
            if(message is OutKey)
            {
                Console.WriteLine((message as OutKey).Key.ToString());
                return new NOP();
            }
            if(message is CreateKeyBuffer)
            {
                var createKeyBufferMessage = message as CreateKeyBuffer;
                CommonKeyStore.AddKeyBuffer(createKeyBufferMessage.Handle, createKeyBufferMessage.QualityOfService);
                return new NOP();
            }
            if(message is CloseKeyBuffer)
            {
                var closeKeyBuffer = message as CloseKeyBuffer;
                CommonKeyStore.RemoveKeyBuffer(closeKeyBuffer.Handle);
                return new NOP();
            }
            return new NOP();
        }

        private Message PrepareMessageForNewKeyChunk(int lastSendKeyIndex)
        {
            var keyChunk = new BlockIdAndKeyAllocation(lastSendKeyIndex, CommonKeyStore.GetKeyAllocation());
            var mac = MessageAuthenticator.GetMAC(keyChunk.GetBytes());
            return new AddKey(mac, keyChunk);
            
        }

        
    }
}
