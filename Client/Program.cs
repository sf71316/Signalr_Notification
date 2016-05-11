using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    public class ClientInfo
    {
        public string ConnId { get; set; }

        public string ClientName { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            //輸入使用者稱謂
            Console.Write("Please input client name: ");
            string clientName = Console.ReadLine();

            //與SignalR Hub Server 連線
            var connection = new HubConnection("http://localhost:5051/notification/");
            IHubProxy myHub = connection.CreateHubProxy("MyHub");//其名稱必須與 Server Hub 類別名稱一樣

            //---實作(定義) Client 端方法----------------------------------------------------------------

            //實作(定義) ReceiveMsg 方法
            //功能:接收並顯示 Server Hub 傳入的文字訊息
            myHub.On<string, string>("ReceiveMsg", (name, message) =>
                {
                    var data = JsonConvert.DeserializeObject<List<DataCollection>>(message);
                    foreach (var item in data)
                    {
                        Console.WriteLine(string.Format("[{0}] tot:{1}->{2} sbi {3}->{4} ",
                            item.Old.sna,
                            item.Old.tot,
                            item.New.tot,
                            item.Old.sbi,
                            item.New.sbi));
                    }
                });

            //實作(定義) NowUser 方法
            //功能:接收並顯示目前所有使用 Server Hub 的使用者
            //myHub.On<Dictionary<string, ClientInfo>>("NowUser", (currClients) => currClients.Values.ToList().ForEach(row => Console.WriteLine("目前已連線使用者: {0}", row.ClientName)));

            //-------------------------------------------------------------------------------------------

            //建立連線，連線建立完成後向 Server Hub 註冊使用者稱謂
            connection.Start().ContinueWith(task =>
            {
                if (!task.IsFaulted)
                {
                    //連線成功時呼叫 Server 端 Register 方法
                    myHub.Invoke("Register", clientName);//必須與 MyHub 的 Register 方法名稱一樣
                }
                else
                {
                    throw new Exception("連線失敗!");
                }
            }).Wait();

            //定時發送訊息給所有使用者()
            //new Task(() =>
            //{
            //    while (true)
            //    {
            //        Thread.Sleep(1000);
            //        myHub.Invoke("SendMsg", DateTime.Now.ToString());//必須與 MyHub 的 SendMsg 方法名稱一樣
            //    }
            //}).Start();

            //維持程式執行迴圈，直止輸入 colse 文字串
            while (Console.ReadLine() != "colse") { }
            Console.WriteLine("close...");

            //結束與 SignalR Hub Server 之間的連線
            connection.Stop();
        }
    }
    public class DataCollection
    {
        public Data Old { get; set; }
        public Data New { get; set; }
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
}
