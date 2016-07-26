﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Vaser
{
    /// <summary>
    /// This class is the portalholder.
    /// </summary>
    public class PortalCollection
    {
        private object _ListLock = new object();
        internal bool _Active = false;

        internal bool Active
        {
            get
            {
                return _Active;
            }
            set
            {
                _Active = value;
            }
        }

        internal Portal[] PortalArray = new Portal[256];

        /// <summary>
        /// Register a Portal.
        /// </summary>
        /// <param name="p">the portal you want to register</param>
        /// <param name="ID">a channel ID you want to use</param>
        /// <returns></returns>
        public void RegisterPortal(Portal portal)
        {
            if (Active) throw new Exception("this PortalCollection is in use! please register portals before using");

            lock (_ListLock)
            {
                if (PortalArray[portal._PID] != null) throw new Exception("Portal ID is already used!");
                PortalArray[portal._PID] = portal;
            }
        }

        internal object _GivePacketToClass_lock = new object();

        internal void GivePacketToClass(Packet_Recv pak)
        {
            lock (_GivePacketToClass_lock)
            {
                Portal clas = PortalArray[pak.ClassID];
                if (clas != null)
                {
                    clas.AddPacket(pak);
                }
                else
                {
                    Debug.WriteLine("Vaser.Portal> Portal not found.");
                    pak.link.Dispose();
                }
            }
        }
    }
}