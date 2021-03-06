﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vaser;
using System.Threading;
using Vaser.ConnectionSettings;

namespace test_CoreClient
{
    public class Program
    {
        // Build your data container
        public class TestContainer : Container
        {
            //only public, nonstatic and standard datatypes can be transmitted
            public int ID = 1;
            public string test = "test text!";
            public byte[] by = new byte[10000];
        }
        // create new container

        static TestContainer con2 = new TestContainer();
        public static void Main(string[] args)
        {
            Run();
        }

        public static void Run()
        {
            bool online = true;

            //Client initalisieren
            Portal system = new Portal(100);
            PortalCollection PC = new PortalCollection();
            PC.RegisterPortal(system);
            system.IncomingPacket += OnSystemPacket;
            

            // ###########################################################
            // Create a TestCert in CMD: makecert -sr LocalMachine -ss root -r -n "CN=localhost" -sky exchange -sk 123456
            // Do not use in Production | do not use localhost -> use your machinename!
            // ###########################################################


            /*//Import Test Cert from local store
            X509Certificate2Collection cCollection = new X509Certificate2Collection();

            X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, "CN=localhost", false);
            store.Close();

            if (certificates.Count == 0)
            {
                Console.WriteLine("Server certificate not found...");
                Console.ReadKey();
                return;
            }
            else
            {
                cCollection.Add(certificates[0]); 
            }
            // Get the value.
            string resultsTrue = cCollection[0].ToString(true);
            // Display the value to the console.
            Console.WriteLine(resultsTrue);
            // Get the value.
            string resultsFalse = cCollection[0].ToString(false);
            // Display the value to the console.
            Console.WriteLine(resultsFalse);*/



            /*System.Timers.Timer _aTimer = new System.Timers.Timer(1);
            _aTimer.Elapsed += DoPackets;
            _aTimer.AutoReset = true;
            _aTimer.Enabled = true;*/
            Thread.Sleep(100);

            VaserSSLClient ssl = new VaserSSLClient("localhost");
            VaserKerberosClient kerberos = new VaserKerberosClient();
            Link lnk1 = VaserClient.ConnectClient("localhost", 3100, PC);
            lnk1.EmptyBuffer += OnEmptyBuffer;

            if (lnk1 != null) Console.WriteLine("1: successfully established connection.");

            //working
            if (lnk1.IsConnected) Console.WriteLine("Test. Con OK");
            
            while (online)
            {
                
                Thread.Sleep(100);

                //entfernen
                if (lnk1.IsConnected == false) online = false;
            }
            //Client1.CloseClient();
            lnk1.Dispose();

            Console.WriteLine("Test ended... press any key...");
            Console.ReadKey();
        }


        static void OnEmptyBuffer(object p, LinkEventArgs e)
        {
            //Console.WriteLine("OnEmptyBuffer called!");
        }

        static TestContainer con1 = new TestContainer();
        static void OnSystemPacket(object p, PacketEventArgs e)
        {
            //unpack the packet, true if the decode was successful
            if (con1.UnpackContainer(e.pak))
            {
                //Console.WriteLine("PACK");
                e.portal.SendContainer(e.lnk, con1, 1, 1);
                //Portal.Finialize();
            }
        }
    }
}
