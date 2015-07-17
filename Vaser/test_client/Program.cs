﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Vaser;
using System.Threading;

namespace test_client
{
    class Program
    {
        // Build your data container
        public class TestContainer : Container
        {
            //only public, nonstatic and standard datatypes can be transmitted
            public int ID = 1;
            public string test = "test text!";
            public byte[] by = new byte[1000];
        }

        static void Main(string[] args)
        {
            //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            // create new container
            TestContainer con1 = new TestContainer();

            bool online = true;

            //Client initalisieren
            Portal system = new Portal();

            //Create a TestCert in CMD: makecert -sr LocalMachine -ss root -r -n "CN=localhost" -sky exchange -sk 123456
            // Do not use in Production | do not use localhost -> use your machinename!

            //Import Test Cert from local store
            X509Certificate2 cert = new X509Certificate2();

            X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, "CN=localhost", false);
            store.Close();

            if (certificates.Count == 0)
            {
                Console.WriteLine("Server certificate not found...");

            }
            else
            {
                cert = certificates[0];
            }
            // Get the value.
            string resultsTrue = cert.ToString(true);
            // Display the value to the console.
            Console.WriteLine(resultsTrue);
            // Get the value.
            string resultsFalse = cert.ToString(false);
            // Display the value to the console.
            Console.WriteLine(resultsFalse);


            /*VaserClient Client1 = new VaserClient();

            Link lnk1 = Client1.ConnectClient("localhost", 3100, VaserOptions.ModeSSL, cert);*/


            Link lnk1 = VaserClient.ConnectClient("localhost", 3100, VaserOptions.ModeSSL, cert);

            if (lnk1 != null) Console.WriteLine("1: successfully established connection.");

            //working
            if (lnk1.Connect.StreamIsConnected) Console.WriteLine("Test. Con OK");
            while (online)
            {

                

                //proceed incoming data
                foreach (Packet_Recv pak in system.getPakets())
                {
                    // [1] now you can sort the packet to the right container and object

                    //unpack the packet, true if the decode was successful
                    if (con1.UnpackDataObject(pak, system))
                    {
                        
                        system.SendContainer(pak.link, con1, 1, 1);
                    }
                }

                Portal.Finialize();
                Thread.Sleep(1);

                //entfernen
                if (lnk1.Connect.StreamIsConnected == false) online = false;
            }
            //Client1.CloseClient();
            lnk1.Dispose();

            Console.WriteLine("Test ended... press any key...");
            Console.ReadKey();
        }
    }
}
