﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.IO.Pipes;
using Vaser.ConnectionSettings;

namespace Vaser
{
    /// <summary>
    /// This class is used to start servers.
    /// Use: VaserServer srv = new VaserServer(...);
    /// </summary>
    public class VaserServer
    {
        //private object _ThreadLock = new object();
        private TcpListener _TCPListener;
        NamedPipeServerStream _pipeServer;

        //private Thread _ListenThread;
        //private volatile bool _ServerOnline = true;
        //private System.Timers.Timer _aTimer;
        private static Timer _GCTimer;

        private object _ConnectionList_ThreadLock = new object();
        private List<Connection> _ConnectionList = new List<Connection>();

        private object _NewLinkList_ThreadLock = new object();
        private List<Link> _NewLinkList = new List<Link>();

        //private VaserOptions _ServerOption = null;
        private VaserKerberosServer _vKerberos = null;
        private VaserSSLServer _vSSL = null;

        //private PortalCollection _PCollection = null;

        /// <summary>
        /// EventHandler for new connected links.
        /// </summary>
        public event EventHandler<LinkEventArgs> NewLink;

        /// <summary>
        /// EventHandler for disconnecting links.
        /// </summary>
        public event EventHandler<LinkEventArgs> DisconnectingLink;

        /// <summary>
        /// The PortalCollection of this server.
        /// </summary>
        public PortalCollection PCollection
        {
            get; set;
        }

        /// <summary>
        /// The used options for this server, such as unencrypted, kerberos or SSL.
        /// </summary>
        internal VaserOptions ServerOption
        {
            get; set;
        }
        
        /// <summary>
        /// A copy of all active links in this server
        /// </summary>
        public List<Link> LinkList
        {
            get
            {
                List<Link> TempList = new List<Link>();
                lock (_ConnectionList_ThreadLock)
                {
                    foreach (Connection c in _ConnectionList)
                    {
                        TempList.Add(c.link);
                    }
                }
                return TempList;
            }
        }


        private bool ServerOnline
        {
            get; set;
        } = true;

        /// <summary>
        /// Stops the Vaser Server
        /// </summary>
        public void Stop()
        {
            ServerOnline = false;
            if (_TCPListener != null)
            {
                _TCPListener.Stop();
            }
            DoStop();
            //_aTimer.Enabled = false;
        }

        /// <summary>
        /// Stops the garbage collector Timer
        /// </summary>
        public static void StopEngine()
        {
            Options.Operating = false;
            _GCTimer.Dispose();
            _GCTimer = null;
        }


        /// <summary>
        /// Starts listening for clients on selected Mode.
        /// </summary>
        public void Start()
        {
            try
            {
                if (ServerOption == VaserOptions.ModeNamedPipeServerStream)
                {
                    throw new NotImplementedException();
                }else { 
                    _TCPListener.Start();
                }

                DoBeginAcceptTcpClient();

                if (_GCTimer == null)
                {
                    System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.LowLatency;
                    _GCTimer = new Timer(GC_Collect,null,15000,15000);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Creates a new unencrypted TCP server and listen for clients.
        /// </summary>
        /// <param name="LocalAddress">The local IP-Address for listening - IPAddress.Any</param>
        /// <param name="Port">The local port of the server.</param>
        /// <param name="PColl">The Portal Collection</param>
        public VaserServer(IPAddress LocalAddress, int Port, PortalCollection PColl)
        {
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {
                ServerOption = VaserOptions.ModeNotEncrypted;
                PColl.Active = true;
                PCollection = PColl;
                _TCPListener = new TcpListener(LocalAddress, Port);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Creates a new internal pipe server for a client.
        /// </summary>
        /// <param name="Pipename">The name of the pipe.</param>
        /// <param name="PColl">The PortalCollection.</param>
        public VaserServer(string Pipename, PortalCollection PColl)
        {
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {
                ServerOption = VaserOptions.ModeNamedPipeServerStream;
                PColl.Active = true;
                PCollection = PColl;

                _pipeServer = new NamedPipeServerStream(Pipename);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Creates a new Kerberos Server and listen for clients.
        /// </summary>
        /// <param name="LocalAddress">IPAddress.Any</param>
        /// <param name="Port">The local port of the server.</param>
        /// <param name="PColl">The PortalCollection.</param>
        /// <param name="Kerberos">Kerberos connection settings.</param>
        public VaserServer(IPAddress LocalAddress, int Port, PortalCollection PColl, VaserKerberosServer Kerberos)
        {
            if (Kerberos == null) throw new Exception("Missing Kerberos options in VaserServer(...)");
            if (PColl == null) throw new Exception("PortalCollection is needed!");

            try
            {

                _vKerberos = Kerberos;
                ServerOption = VaserOptions.ModeKerberos;
                PColl.Active = true;
                PCollection = PColl;
                _TCPListener = new TcpListener(LocalAddress, Port);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Creates a new SSL Server and listen for clients.
        /// </summary>
        /// <param name="LocalAddress">IPAddress.Any</param>
        /// <param name="Port">The local port of the server.</param>
        /// <param name="PColl">The PortalCollection.</param>
        /// <param name="SSL">SSL connection settings.</param>
        public VaserServer(IPAddress LocalAddress, int Port, PortalCollection PColl, VaserSSLServer SSL)
        {
            if (SSL == null) throw new Exception("Missing SSL options in VaserServer(...)");
            if (PColl == null) throw new Exception("PortalCollection is needed!");
            try
            {
                _vSSL = SSL;
                ServerOption = VaserOptions.ModeSSL;
                PColl.Active = true;
                PCollection = PColl;
                _TCPListener = new TcpListener(LocalAddress, Port);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void GC_Collect(object source)
        {
            GC.Collect();
        }

        private void DoBeginAcceptTcpClient()
        {
            _TCPListener.BeginAcceptSocket(
                new AsyncCallback(DoAcceptTcpClientCallback),
                null);
        }

        private void DoAcceptTcpClientCallback(IAsyncResult ar)
        {
            try
            {
                if (ServerOnline && Options.Operating)
                {
                    Socket Client = _TCPListener.EndAcceptSocket(ar);
                    if (Client != null)
                    {
                        QueueNewConnection(Client);

                        DoBeginAcceptTcpClient();
                    }
                    else
                    {
                        Debug.WriteLine("Client was null");
                        DoStop();
                    }
                }
            }catch(Exception ex)
            {
                Debug.WriteLine("exeption "+ ex.ToString());
                DoStop();
            }
        }

        void DoStop()
        {
            if (!ServerOnline || !Options.Operating)
            {
                //_aTimer.Enabled = false;
                Connection[] cArray = null;

                lock (_ConnectionList_ThreadLock)
                {
                    cArray = _ConnectionList.ToArray();
                }

                foreach (Connection Con in cArray)
                {
                    Con.Stop();
                }


                _TCPListener.Stop();
            }
        }

        internal void QueueNewConnection(object Client)
        {
            try
            {
                Connection con = new Connection((Socket)Client, true, ServerOption, PCollection, _vKerberos, _vSSL, null, null, this);

                lock (_ConnectionList_ThreadLock)
                {
                    _ConnectionList.Add(con);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("ERROR in VaserServer.QueueNewConnection(object Client) > " + e.ToString());
            }
        }

        internal void AddNewLink(Link lnk)
        {
            lock (_NewLinkList_ThreadLock)
            {
                _NewLinkList.Add(lnk);

                if (!NewQueueLock)
                {
                    NewQueueLock = true;
                    ThreadPool.QueueUserWorkItem(NewEventWorker);
                }
            }
            

        }
        volatile bool NewQueueLock = false;
        private void NewEventWorker(object threadContext)
        {
            List<Link> LinkListTEMP = GetNewLinkList();
            while (LinkListTEMP.Count != 0)
            {
                foreach (Link lnk in LinkListTEMP)
                {
                    LinkEventArgs args = new LinkEventArgs()
                    {
                        lnk = lnk
                    };
                    OnNewLink(args);
                }
                LinkListTEMP = GetNewLinkList();
            }

        }

        private List<Link> GetNewLinkList()
        {
            List<Link> LinkListTEMP = null;

            lock (_NewLinkList_ThreadLock)
            {

                LinkListTEMP = _NewLinkList;
                _NewLinkList = new List<Link>();
                if (LinkListTEMP.Count == 0) NewQueueLock = false;
            }

            return LinkListTEMP;
        }

        /// <summary>
        /// Raises an event when a new client is connected.
        /// </summary>
        /// <param name="e">Contains the connection link.</param>
        protected virtual void OnNewLink(LinkEventArgs e)
        {
            lock (e.lnk._OnEventLink_ThreadLock)
            {
                NewLink?.Invoke(this, e);
            }
        }

        internal void RemoveFromConnectionList(Connection con)
        {
            if (con == null) return;
            LinkEventArgs args = null;
            lock (_ConnectionList_ThreadLock)
            {
                _ConnectionList.Remove(con);
                args = new LinkEventArgs()
                {
                    lnk = con.link
                };
            }
            OnDisconnectingLink(args);
        }

        /// <summary>
        /// Raises an event when a client is disconnected.
        /// </summary>
        /// <param name="e">Contains the connection link.</param>
        protected virtual void OnDisconnectingLink(LinkEventArgs e)
        {
            lock (e.lnk._OnEventLink_ThreadLock)
            {
                DisconnectingLink?.Invoke(this, e);
            }
        }

    }

    /// <summary>
    /// Holds the connection link.
    /// </summary>
    public class LinkEventArgs : EventArgs
    {
        /// <summary>
        /// The link.
        /// </summary>
        public Link lnk { get; set; }
    }
}
