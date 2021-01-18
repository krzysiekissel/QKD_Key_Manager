//Program.cs
using QKD_Key_Manager.Auth;
using QKD_Key_Manager.Destilation;
using QKD_Key_Manager.Entities;
using QKD_Key_Manager.Exceptions;
using QKD_Key_Manager.KeyManagment;
using QKD_Key_Manager.Messaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QKD_Key_Manager
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //INICJALIZACJA SYSTEMU
            var alreadyAgreedKeys = GetKeyStream(32768, 2);
            var initiator = GetInitiator(alreadyAgreedKeys);
            var service = GetService(CopyKeys(alreadyAgreedKeys));
            var bus = new ClientServerMessageBus() { Initiator = initiator, Service = service };
            var keyLength = 32768;
            var aliceBits = GetBits(keyLength);
            var bobBits = Distortion(aliceBits, 0.05);
            var aliceBytes = new byte[aliceBits.Length / 4];
            var bobBytes = new byte[aliceBits.Length / 4];
            new BitArray(aliceBits).CopyTo(aliceBytes, 0);
            new BitArray(bobBits).CopyTo(bobBytes, 0);

            bus.Send(initiator, new MoreUndestiledBitsArrived(aliceBytes));
            bus.Send(service, new MoreUndestiledBitsArrived(bobBytes));

            //INICJACJA PROCEDURY DESTYLACJI

            bus.Send(initiator, new InitiateDestilation());

            //FAZA ESTYMACJI BŁĘDÓW

            bus.Send(initiator, new EstimateQBER(0, 4048 / 2 / 2 / 2));

            //FAZA KOREKCJI BŁĘDÓW (3 iteracje)

            for (int i = 0; i < 3; i++)
            {
                bus.Send(initiator, new InitiateCascade() { Service = service, Bus = bus });

                initiator.DestilationBuffer.Permute(initiator.CommonKeyStore.ServiceKeyBuffer.GetKey().GetBytes());
                service.DestilationBuffer.Permute(service.CommonKeyStore.ServiceKeyBuffer.GetKey().GetBytes());
                initiator.DestilationBuffer.SetEstimatedQBER(initiator.DestilationBuffer.GetEstimatedQBER() / 2);
            }

            //FAZA POTWIERDZENIA 

            bus.Send(initiator, new EstimateQBER(0, 256));
            if (Cascade.EstimateQBER(initiator.DestilationBuffer._bits, service.DestilationBuffer._bits) != 0)
            {
                throw new ErrorCorrectionFailException();
            }

            //FAZA AMPLIFIKACJA PRYWATNOŚCI

            var privacyLevel = 0.1;

            bus.Send(initiator, new AmplifiePrivacy() { PrivacyLevel = privacyLevel });
            bus.Send(service, new AmplifiePrivacy() { PrivacyLevel = privacyLevel });

            //DODANIE KLUCZA DO SYSTEMU
            var klucz = initiator.DestilationBuffer.FlushBuffer();
            bus.Send(initiator, new MoreBitsArrived(new List<Key>() { new Key(klucz) }));
            bus.Send(service, new MoreBitsArrived(new List<Key>() { new Key(service.DestilationBuffer.FlushBuffer()) }));

            
        }
        static bool[] GetBits(int length)
        {
            var random = new Random();

            bool[] array = new bool[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = random.NextDouble() > 0.5;
            }
            return array;
        }
        static bool[] Distortion(bool[] array, double QBER)
        {
            var random = new Random();
            var newArray = new bool[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                if (random.NextDouble() < QBER)
                {
                    newArray[i] = !array[i];
                }
                else
                {
                    newArray[i] = array[i];
                }
            }
            return newArray;
        }

        private static void BufferUsageExample(Initiator initiator, Service service, QualityOfService qualityOfService, ClientServerMessageBus bus, List<Key> newKeys)
        {
            bus.Send(initiator, new MoreBitsArrived(newKeys));
            bus.Send(service, new MoreBitsArrived(newKeys));

            bus.Send(initiator, new Initiate());

            bus.Send(initiator, new CreateKeyBuffer("app1.in", qualityOfService));
            bus.Send(initiator, new CreateKeyBuffer("app1.out", qualityOfService));

            bus.Send(service, new CreateKeyBuffer("app1.in", qualityOfService));
            bus.Send(service, new CreateKeyBuffer("app1.out", qualityOfService));

            bus.Send(initiator, new MoreBitsArrived(newKeys));
            bus.Send(service, new MoreBitsArrived(newKeys));

            bus.Send(initiator, new Initiate());

            bus.Send(initiator, new CloseKeyBuffer("app1.in"));
            bus.Send(service, new CloseKeyBuffer("app1.in"));

            bus.Send(initiator, new GetOutBufferKey("app1.out", 128));


            bus.Send(service, new GetOutBufferKey("app1.out", 128));
        }

        private static Initiator GetInitiator(List<Key> commonKeys)
        {
            var initiator_local_keyStore = new ServiceKeyBuffer(512);
            var destilationBuffer = new DestilationBuffer();

            var initiator_common_service = new ServiceKeyBuffer(2);
            foreach(var key in commonKeys)
            {
                initiator_common_service.AddKey(key);
            }

            var initiator_common_keyStores = new CommonKeyStoreSystem(initiator_common_service);

            var initiator_authenticator = new WegmanCarterAuthenticator(initiator_common_service, 16);

            return new Initiator() { LocalKeyStore = initiator_local_keyStore, CommonKeyStore = initiator_common_keyStores, MessageAuthenticator = initiator_authenticator,DestilationBuffer=destilationBuffer };
        }

        private static Service GetService(List<Key> commonKeys)
        {
            var service_local_keyStore = new ServiceKeyBuffer(512);
            var destilationBuffer = new DestilationBuffer();

            var service_common_service = new ServiceKeyBuffer(2);
            foreach (var key in commonKeys)
            {
                service_common_service.AddKey(key);
            }

            var service_common_keyStores = new CommonKeyStoreSystem(service_common_service);

            var service_authenticator = new WegmanCarterAuthenticator(service_common_service, 16);

            return new Service() { LocalKeyStore = service_local_keyStore, CommonKeyStore = service_common_keyStores, MessageAuthenticator = service_authenticator, DestilationBuffer = destilationBuffer };
        }

        private static List<Key> GetKeyStream(int bytesPerKey, int streamSize)
        {
            var keys = new List<Key>();
            var random = new Random();
            for(int i = 0; i < streamSize; i++)
            {
                var keyBuffer = new byte[bytesPerKey];
                random.NextBytes(keyBuffer);
                keys.Add(new Key(keyBuffer));
            }
            return keys;
        }
        private static List<Key> CopyKeys(List<Key> keysToCopy)
        {
            var keys = new List<Key>();
            for(int i = 0; i < keysToCopy.Count; i++)
            {
                var bytes = new byte[keysToCopy[i].GetLength()];
                Array.Copy(keysToCopy[i].GetBytes(), bytes, bytes.Length);
                var key = new Key(bytes);
                keys.Add(key);
            }
            return keys;
        }

    }
}
