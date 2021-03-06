﻿using System;
using Vaser;
using Vaser.OON;

namespace test_server
{
    public class TestChannel : cChannel
    {
        TestContainer con1 = new TestContainer();
        public void mySendStarter(string myMessage, Link lnk)
        {
            con1.test = myMessage;

            SendPacket(con1, lnk);
        }

        TestContainer con2 = new TestContainer();
        public override void IncomingPacket(object p, PacketEventArgs e)
        {
            if (con2.UnpackContainer(e.pak))
            {
                Console.WriteLine(con2.test);
                con2.test = "Hello Back channel!";

                SendPacket(con2);
            }
            else
            {
                Console.WriteLine("Decode error!");
            }
        }
    }
}
