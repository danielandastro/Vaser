﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Vaser.global;

namespace Vaser
{
    public class TCPServer
    {
        private SemaphoreSlim _ThreadLock = new SemaphoreSlim(1);
        private TcpListener _TCPListener;
        private Thread _ListenThread;
        private bool _ServerOnline = true;

        private SemaphoreSlim _ConnectionList_ThreadLock = new SemaphoreSlim(1);
        private List<Connection> _ConnectionList = new List<Connection>();
        private List<Link> NewLinkList = new List<Link>();

       

        public List<Connection> ConnectionList
        {
            get
            {
                _ThreadLock.Wait();
                List<Connection> ret = _ConnectionList;
                _ThreadLock.Release();
                return ret;
            }
            set
            {
                _ThreadLock.Wait();
                _ConnectionList = value;
                _ThreadLock.Release();
            }
        }


        private bool ServerOnline
        {
            get
            {
                _ThreadLock.Wait();
                bool ret = _ServerOnline;
                _ThreadLock.Release();
                return ret;
            }
            set
            {
                _ThreadLock.Wait();
                _ServerOnline = value;
                _ThreadLock.Release();
            }
        }

        /// <summary>
        /// Stops the TCP Server
        /// </summary>
        public void Stop()
        {
            _ThreadLock.Wait();
            _ServerOnline = false;
            _ThreadLock.Release();
        }

        /// <summary>
        /// Stops Vaser
        /// </summary>
        public void StopEngine()
        {
            Options.Operating = false;
        }

        /// <summary>
        /// Creates a new TCP Server and listen for clients
        /// </summary>
        /// <param name="LocalAddress">IPAddress.Any</param>
        /// <param name="Port">3000</param>
        public TCPServer(IPAddress LocalAddress, int Port)
        {
            _ThreadLock.Wait();
            _TCPListener = new TcpListener(LocalAddress, Port);
            _ListenThread = new Thread(new ThreadStart(ListenForClients));
            _ListenThread.Start();
            _ThreadLock.Release();
        }


        private void ListenForClients()
        {
            _TCPListener.Start();

            while (ServerOnline && Options.Operating)
            {
                if (_TCPListener.Pending())
                {

                    TcpClient Client = this._TCPListener.AcceptTcpClient();

                    Connection con = new Connection(Client, true, this);

                    _ConnectionList_ThreadLock.Wait();
                    _ConnectionList.Add(con);
                    _ConnectionList_ThreadLock.Release();

                    _ThreadLock.Wait();
                    NewLinkList.Add(con.link);
                    _ThreadLock.Release();
                }

                Thread.Sleep(1);
            }

            _ConnectionList_ThreadLock.Wait();
            foreach (Connection Con in _ConnectionList)
            {
                Con.Stop();
            }
            _ConnectionList_ThreadLock.Release();

            _TCPListener.Stop();
        }

        internal void RemoveFromConnectionList(Connection con)
        {
            _ConnectionList_ThreadLock.Wait();
            _ConnectionList.Remove(con);
            _ConnectionList_ThreadLock.Release();
        }

        /// <summary>
        /// Get a new Connected Client.
        /// </summary>
        /// <returns>Returns null if no new client is connected</returns>
        public Link GetNewLink()
        {
            _ThreadLock.Wait();
            Link lnk = null;

            if (NewLinkList.Count > 0)
            {
                lnk = NewLinkList[0];
                NewLinkList.Remove(lnk);
            }
            _ThreadLock.Release();

            return lnk;
        }
    }
}
