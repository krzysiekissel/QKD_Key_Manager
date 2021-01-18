//Cascade.cs
using QKD_Key_Manager.Entities;
using QKD_Key_Manager.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QKD_Key_Manager.Destilation
{
    class Cascade
    {
        private Initiator _initiator;
        private Service _service;
        private MessageBus _messageBus;
        public Cascade(Initiator initiator,Service service,MessageBus messageBus)
        {
            _initiator = initiator;
            _service = service;
            _messageBus = messageBus;
        }
        public bool CheckParity(int index, int length)
        {
            if (length == 1)
            {
                _messageBus.PublicBits++;
            }
            var payload = new BlockIdentifiaction(index, length);
            var mac = _initiator.MessageAuthenticator.GetMAC(payload.GetBytes());

            var message = new CheckParity(mac,payload);
            var response = _messageBus.SendRequest(_service, message) as Messaging.Parity;
            var parityPayload = response.Payload as ParityPayload;
            var bobParity = parityPayload.Parity;
            var aliceParity = Parity(_initiator.DestilationBuffer.GetBits(index, length));
            return aliceParity == bobParity;
        }
        public struct Block
        {
            public int Index;
            public int Length;
            public int Parity;
        }
        public void CorrectErrors(List<Block> blocks)
        {
            var nextBlocks = new List<Block>();
            List<Block> blocksWithParity = SetBlocksParity(blocks);
            for(int i = 0; i < blocksWithParity.Count; i++)
            {
                var block = blocksWithParity[i];
                if (block.Parity != Parity(_initiator.DestilationBuffer.GetBits(block.Index, block.Length))){
                    if (block.Length == 1)
                    {
                        _messageBus.PublicBits++;
                        _initiator.DestilationBuffer.UpdateBit(block.Index, !_initiator.DestilationBuffer.GetBit(block.Index));
                    }
                    else
                    {
                        nextBlocks.AddRange(DivideBlock(block));
                    }
                }
                else
                {
                    if(block.Length == 1)
                    {
                        _messageBus.PublicBits++;
                    }
                }
            }
            if (nextBlocks.Count > 0)
            {
                CorrectErrors(nextBlocks);
            }

        }

        private List<Block> DivideBlock(Block block)
        {
            var blocks = new List<Block>();
            var blockLength = block.Length;
            int rem;
            int div = Math.DivRem(blockLength, blockLength / 2,out rem);

            for(int i = 0; i < div; i++)
            {
                blocks.Add(new Block() { Index = block.Index + i * blockLength / 2, Length = blockLength / 2 });
            }
            if (rem != 0)
            {
                blocks.Add(new Block() { Index = block.Index + div * blockLength / 2, Length = rem });
            }
            return blocks;
        }

        private List<Block> SetBlocksParity(List<Block> blocks)
        {
            var payload = new Blocks(blocks.ToArray());
            var mac = _initiator.MessageAuthenticator.GetMAC(payload.GetBytes());

            var message = new BlocksParityRequest(mac, payload);
            var response = _messageBus.SendRequest(_service, message) as Messaging.BlocksParityResponse;
            var responseBlocksPayload = response.Payload as Blocks;
            var bobBlocks = responseBlocksPayload.BlocksArray;
            return bobBlocks.ToList();
        }


        public static int Parity(bool[] array)
        {
            var convertedArray = new int[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                convertedArray[i] = array[i] ? 1 : 0;
            }
            var result = convertedArray.Sum() % 2;
            return result;
        }
        public static double EstimateQBER(BitArray aliceBits,BitArray bobBits)
        {
            var errors = 0;
            for(int i = 0; i < aliceBits.Length; i++)
            {
                if (aliceBits[i] != bobBits[i])
                    errors += 1;
            }
            return errors / (double)aliceBits.Length;
        }
        public int GetProperInitialBlockSize(int initialBlockSize)
        {
            if (initialBlockSize < 1)
            {
                return 1;
            }
            return (int)Math.Pow(2, (int)Math.Log(initialBlockSize, 2));
        }

    }
}
