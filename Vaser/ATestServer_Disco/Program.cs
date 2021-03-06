﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaser;
using System.Threading;

namespace ATestServer_Disco
{
    class Program
    {
        // Build your data container
        public class TestContainer : Container
        {
            //only public, nonstatic and standard datatypes can be transmitted
            public int ID = 1;
            public string test = "test text!";

            //also 1D arrays are posible
            //public int[] array = new int[1000];
        }

        //static Portal system = null;
        // create new container
        //static TestContainer con1 = new TestContainer();

        static int pkt
        {
            get;
            set;
        }

        static Timer t = null;

        //create connection managing lists
        static object Livinglist_lock = new object();
        static List<Link> Livinglist = new List<Link>();

        static Portal system = null;
        static uint object_counter = 0;
        static Link[] linkarray = null;

        static void TimerCallback(object sender)
        {
            int x = pkt;
            pkt = 0;
            lock (Livinglist_lock)
            {
                Console.WriteLine("Packets in 10 Sek: " + x + " Connected Links: " + Livinglist.Count);

                /*if(linkarray != null)
                {
                    foreach(Link l in linkarray)
                    {
                        foreach(Link y in Livinglist)
                        {
                            if (l == y) throw new Exception("Fond stale link!");
                        }
                    }
                }*/

                linkarray = Livinglist.ToArray();
            }

        }

        static void Main(string[] args)
        {


            //Client initalisieren
            system = new Portal(100);
            PortalCollection PC = new PortalCollection();
            PC.RegisterPortal(system);

            system.IncomingPacket += OnSystemPacket;
            Vaser.ConnectionSettings.VaserKerberosServer k = new Vaser.ConnectionSettings.VaserKerberosServer();
            //start the server
            VaserServer Server1 = new VaserServer(System.Net.IPAddress.Any, 3500, PC, k);
            Server1.NewLink += OnNewLinkServer1;
            Server1.DisconnectingLink += OnDisconnectingLinkServer1;
            Server1.Start();
            t = new Timer(TimerCallback, null, 10000, 10000);
            //TestContainer con2 = new TestContainer();




            //run the server
            Console.ReadKey();
            t.Dispose();
            //close the server
            Server1.Stop();
        }

        static TestContainer con3 = new TestContainer();
        static void OnNewLinkServer1(object p, LinkEventArgs e)
        {

            Console.WriteLine("CL1 CON " + object_counter);
            lock (Livinglist_lock)
            {
                Livinglist.Add(e.lnk);
            }
            e.lnk.Accept();

            //send data
            con3.ID = 0;
            con3.test = "You are connected to Server 1 via Vaser. Please send your Logindata.";
            // the last 2 digits are manually set [1]
            object_counter++;
            system.SendContainer(e.lnk, con3, 1, object_counter);

        }
        
        static void OnDisconnectingLinkServer1(object p, LinkEventArgs e)
        {
            Console.WriteLine("                        CL1 DIS");
            lock (Livinglist_lock)
            {
                Livinglist.Remove(e.lnk);
            }
        }

        static TestContainer con2 = new TestContainer();
        static void OnSystemPacket(object p, PacketEventArgs e)
        {
            //unpack the packet, true if the decode was successful
            if (con2.UnpackContainer(e.pak))
            {
                //Console.WriteLine(con1.test);
                Console.WriteLine("Pong!  CounterID" + con2.ID + " Object:" + e.pak.ObjectID);
                // the last 2 digits are manually set [1]
                e.portal.SendContainer(e.lnk, con2, 1, e.pak.ObjectID);
                pkt++;
            }
            else
            {
                Console.WriteLine("Decode error");
            }
        }

    }
}
