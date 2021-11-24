using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace LSportsServer
{
    public partial class CGameServer : WebSocketBehavior
    {
        public bool m_bThread;


        protected override void OnOpen()
        {
            base.OnOpen();
            CGlobal.ShowConsole("Browser connect!");
            m_bThread = true;
            // new Thread(() => OnAjaxRequestList(this)).Start();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            CGlobal.ShowConsole("Browser Close!");
            CGlobal.ShowConsole(e.Reason);
            m_bThread = false;
            base.OnClose(e);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            CGlobal.ShowConsole("Browser Error!");
            CGlobal.ShowConsole(e.Message);
            m_bThread = false;
            base.OnError(e);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            string strPacket = string.Empty;
            if (e.IsBinary)
            {
                strPacket = Encoding.UTF8.GetString(e.RawData);
            }
            else
            {
                strPacket = e.Data.ToString();
            }
            CGlobal.ShowConsole(strPacket);
            if (strPacket == "Server")
            {
                CGlobal.SetBroadcastSocket(this);
                return;
            }

            try
            {
                CPacket clsPacket = JsonConvert.DeserializeObject<CPacket>(strPacket);
                OnLSportsPacket(clsPacket);
            }
            catch(Exception err)
            {
                CGlobal.ShowConsole(err.Message);
            }

        }

        public void BroadCastPacket(string strPacket)
        {
            if (this.State == WebSocketState.Open)
                this.Sessions.Broadcast(strPacket);
        }

        public void BroadCastPacket(CPacket packet)
        {
            string strPacket = JsonConvert.SerializeObject(packet);
            BroadCastPacket(strPacket);
        }

        public void SendPacket(string strPacket)
        {
            if (this.State == WebSocketState.Open)
                this.Send(strPacket);
        }

        public void SendPacket(CPacket packet)
        {
            string strPacket = JsonConvert.SerializeObject(packet);
            SendPacket(strPacket);
        }

        private void ReturnPacket(int nPacketCode, string strPacket, int nError, int nSentType = 1)
        {
            CPacket clsPacket = new CPacket(nPacketCode);
            clsPacket.m_nRetCode = nError;
            if(nSentType == 1)
            {
                clsPacket.m_nEnd = 1;
                clsPacket.m_strPacket = strPacket;
                SendPacket(clsPacket);
            }
            else
            {
                List<string> lstPacket = new List<string>();
                int nCnt = strPacket.Length / 2500;
                for (int i = 0; i < nCnt; i++)
                {
                    lstPacket.Add(strPacket.Substring(i * 2500, 2500));
                }

                if (strPacket.Length > nCnt * 2500)
                {
                    int nOffset = strPacket.Length - nCnt * 2500;
                    lstPacket.Add(strPacket.Substring(nCnt * 2500, nOffset));
                }

                int nIndex = 0;
                foreach (string strItem in lstPacket)
                {
                    if (nIndex < (lstPacket.Count() - 1))
                    {
                        clsPacket.m_nEnd = 0;
                    }
                    else if (nIndex == (lstPacket.Count() - 1))
                    {
                        clsPacket.m_nEnd = 1;
                    }
                    nIndex++;
                    clsPacket.m_strPacket = strItem;
                    SendPacket(clsPacket);
                }
            }
        }

    }

    public static class Extensions
    {
        public static IEnumerable<string> Split(this string str, int n)
        {
            if (String.IsNullOrEmpty(str) || n < 1)
            {
                throw new ArgumentException();
            }

            return Enumerable.Range(0, str.Length / n).Select(i => str.Substring(i * n, n));
        }
    }

    public static class CServer
    {
        public static void Start()
        {
            if (CDefine.USE_WSS == "yes")
            {
                WebSocketServer wssv = new WebSocketServer(CGlobal.ParseInt(CDefine.SERVER_PORT), true);
                wssv.SslConfiguration.ServerCertificate = new X509Certificate2("ssl.pfx", "1111", X509KeyStorageFlags.MachineKeySet);
                wssv.AddWebSocketService<CGameServer>("/");
                wssv.Start();
                CGlobal.ShowConsole("Socket Server Start!");

                WebSocket ws = new WebSocket($"wss://{CDefine.SERVER_ADDR}:{CDefine.SERVER_PORT}");
                ws.OnOpen += Ws_OnOpen;
                ws.OnError += Ws_OnError;
                ws.OnClose += Ws_OnClose;
                ws.Connect();
            }
            else
            {
                WebSocketServer wssv = new WebSocketServer(CGlobal.ParseInt(CDefine.SERVER_PORT));
                wssv.AddWebSocketService<CGameServer>("/");
                wssv.Start();

                WebSocket ws = new WebSocket($"ws://127.0.0.1:{CDefine.SERVER_PORT}");
                ws.OnOpen += Ws_OnOpen;
                ws.OnError += Ws_OnError;
                ws.OnClose += Ws_OnClose;
                ws.Connect();
            }
        }

        private static void Ws_OnClose(object sender, CloseEventArgs e)
        {
            CGlobal.ShowConsole("Socket Server Closed!");
            CGlobal.ShowConsole(e.Reason);
            (sender as WebSocket).Connect();
        }

        private static void Ws_OnError(object sender, ErrorEventArgs e)
        {
            CGlobal.ShowConsole("Socket Server Error!");
            CGlobal.ShowConsole(e.Message);
            (sender as WebSocket).Close();
        }

        private static void Ws_OnOpen(object sender, EventArgs e)
        {
            (sender as WebSocket).Send("Server");
        }
    }

    
}
