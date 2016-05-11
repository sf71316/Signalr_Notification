using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Server
{
    class Program
    {
        static Dictionary<string, Data> _Data;
        static string url = "http://localhost:5051/notification/";//設定 SignalR Hub Server 對外的接口
        static bool ISRUN = false;

        static void Main(string[] args)
        {
            ExcuteNotificationService();
            Timer t = new Timer();
            t.Enabled = true;
            t.Interval = 3 * 1000;
            t.Elapsed += t_Elapsed;
            Console.ReadLine();
        }

        private static void ExcuteNotificationService()
        {
            using (WebApp.Start(url))//啟動 SignalR Hub Server
            {
                Console.WriteLine("Server running on {0}", url);
            }
        }
        static void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!ISRUN)
            {
                ISRUN = true;
                Console.WriteLine("Sync data.......");
                GetiBikeData();
                ISRUN = false;
            }
        }

        private static void GetiBikeData()
        {
            List<DataCollection> _change = new List<DataCollection>();
            JsonSerializer json = new JsonSerializer();
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://ybjson01.youbike.com.tw:1002/gwjs.json");
            HttpResponseMessage resp = client.GetAsync("").Result;
            DataContainer data = null;
            if (resp.IsSuccessStatusCode)
            {
                data = JsonConvert.DeserializeObject<DataContainer>(resp.Content.ReadAsStringAsync().Result);
                if (_Data != null)
                {
                    foreach (KeyValuePair<string, Data> item in data.retVal)
                    {
                        var _source = _Data[item.Key];
                        if (_source.tot != item.Value.tot ||
                           _source.sbi != item.Value.sbi)
                        {
                            DataCollection c = new DataCollection();
                            c.Old = _source;
                            c.New = item.Value;
                            _change.Add(c);
                        }
                    }
                    if (_change.Count > 0)
                    {
                        _Data = data.retVal;
                        Console.WriteLine("follow location value changed.....");
                        foreach (var item in _change)
                        {
                            Console.WriteLine(string.Format("[{0}] tot:{1}->{2} sbi {3}->{4} ",
                                item.Old.sna,
                                item.Old.tot,
                                item.New.tot,
                                item.Old.sbi,
                                item.New.sbi));
                        }
                        NotificationClient(_change);
                    }
                    else
                    {
                        Console.WriteLine("no data changed....");
                    }
                }
                else
                {
                    _Data = data.retVal;
                    Console.WriteLine("has new data...");
                }
            }
        }

        private static void NotificationClient(List<DataCollection> _change)
        {
            var connection = new HubConnection(url);
            IHubProxy myHub = connection.CreateHubProxy("MyHub");//其名稱必須與 Server Hub 類別名稱一樣
            string data = JsonConvert.SerializeObject(_change);
            myHub.Invoke("SendMsg", data);
        }
    }
    #region SignalR

    /// <summary>
    /// 啟動 SignalR Hub 所需
    /// </summary>
    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            //app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }

    /// <summary>
    /// 保存Client識別資料的物件
    /// </summary>
    public class ClientInfo
    {
        public string ConnId { get; set; }

        public string ClientName { get; set; }
    }

    /// <summary>
    /// Server Hub
    /// </summary>
    public class MyHub : Hub
    {
        /// <summary>
        /// 紀錄目前已連結的 Client 識別資料
        /// </summary>
        public static Dictionary<string, ClientInfo> CurrClients = new Dictionary<string, ClientInfo>();

        /// <summary>
        /// 提供Client 端呼叫
        /// 功能:對全體 Client 發送訊息
        /// </summary>
        /// <param name="message">發送訊息內容</param>
        public void SendMsg(string message)
        {
            string connId = Context.ConnectionId;
            lock (CurrClients)
            {
                if (CurrClients.ContainsKey(connId))
                {
                    Clients.All.ReceiveMsg(CurrClients[connId].ClientName, message);//呼叫 Client 端所提供 ReceiveMsg方法(ReceiveMsg 方法由 Client 端實作)
                }
            }
        }

        /// <summary>
        /// 提供 Client 端呼叫
        /// 功能:對 Server 進行身分註冊
        /// </summary>
        /// <param name="clientName">使用者稱謂</param>
        public void Register(string clientName)
        {
            string connId = Context.ConnectionId;
            lock (CurrClients)
            {
                if (!CurrClients.ContainsKey(connId))
                {
                    CurrClients.Add(connId, new ClientInfo { ConnId = connId, ClientName = clientName });
                }
            }
            Clients.All.NowUser(CurrClients);
        }

        /// <summary>
        /// Client 端離線時的動作
        /// </summary>
        /// <param name="stopCalled">true:為使用者正常關閉(離線); false: 使用者不正常關閉(離線)，如連線狀態逾時</param>
        /// <returns></returns>
        public override Task OnDisconnected(bool stopCalled)
        {
            string connId = Context.ConnectionId;
            lock (CurrClients)
            {
                if (CurrClients.ContainsKey(connId))
                {
                    CurrClients.Remove(connId);
                }
            }
            Clients.All.NowUser(CurrClients);//呼叫 Client 所提供 NowUser 方法(ReceiveMsg 方法由Client 端實作)

            stopCalled = true;
            return base.OnDisconnected(stopCalled);
        }
    }
    #endregion

    class DataContainer
    {
        public int retCode { get; set; }

        public Dictionary<string, Data> retVal { get; set; }
    }
    public class Data
    {
        public string sno { get; set; }
        public string sna { get; set; }
        public string tot { get; set; }
        public string sbi { get; set; }
        public string sarea { get; set; }
        public string mday { get; set; }
        public string lat { get; set; }
        public string lng { get; set; }
        public string ar { get; set; }
        public int bemp { get; set; }
        public int act { get; set; }
    }
    public class DataCollection
    {
        public Data Old { get; set; }
        public Data New { get; set; }
    }
}
