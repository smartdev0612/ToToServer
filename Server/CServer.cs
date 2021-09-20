using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
            m_bThread = false;
            base.OnClose(e);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            CGlobal.ShowConsole("Browser Error!");
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

        private void ReturnPacket(int nPacketCode, string strPacket, int nError)
        {
            CPacket clsPacket = new CPacket(nPacketCode);
            clsPacket.m_nRetCode = nError;
            clsPacket.m_strPacket = strPacket;
            SendPacket(clsPacket);
        }

    }

    public static class CServer
    {
        public static void Start()
        {
            WebSocketServer wssv = new WebSocketServer(CGlobal.ParseInt(CDefine.SERVER_PORT));
            wssv.AddWebSocketService<CGameServer>("/");
            wssv.Start();
            CGlobal.ShowConsole("Socket Server Start!");
            WebSocket ws = new WebSocket($"ws://127.0.0.1:{CDefine.SERVER_PORT}");
            ws.OnOpen += Ws_OnOpen;
            ws.OnError += Ws_OnError;
            ws.OnClose += Ws_OnClose;
            ws.Connect();
        }

        private static void Ws_OnClose(object sender, CloseEventArgs e)
        {
            CGlobal.ShowConsole("Socket Server Closed!");
            (sender as WebSocket).Connect();
        }

        private static void Ws_OnError(object sender, ErrorEventArgs e)
        {
            CGlobal.ShowConsole("Socket Server Error!");
            (sender as WebSocket).Close();
        }

        private static void Ws_OnOpen(object sender, EventArgs e)
        {
            (sender as WebSocket).Send("Server");
        }
    }

    
}
