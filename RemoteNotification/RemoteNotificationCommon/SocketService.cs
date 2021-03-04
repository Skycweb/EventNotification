using System;
using System.Collections.Generic;
using System.Text;

namespace RemoteNotificationCommon
{
    public class SocketService:SocketRect
    {
        /// <summary>
        /// 所有客户端
        /// </summary>
        public static Dictionary<string, SocketService> clients = new Dictionary<string, SocketService>();
        /// <summary>
        /// 所有事件
        /// </summary>
        public static Dictionary<string, List<SocketService>> events = new Dictionary<string, List<SocketService>>();
        /// <summary>
        /// self绑定的事件
        /// </summary>
        private Dictionary<string, List<SocketService>> myEventList = new Dictionary<string, List<SocketService>>();
        public SocketService()
        {
            this.ReactData = (SocketRect obj, byte[] data) => {
                ActionEnum actionName = (ActionEnum)data[0];
                string str = Encoding.UTF8.GetString(data,1,data.Length-1);
                if (str != null && str.Length > 0) {
                    string model = str;//Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(str);
                    if (model != null) {
                        switch (actionName) {
                            case ActionEnum.Login:
                                lock (clients) {
                                    clients[model] = obj as SocketService;
                                }
                                break;
                            case ActionEnum.BindEvent:
                                {
                                    string eventName = model;
                                    lock (events) {
                                        List<SocketService> myEvents ;
                                        if (events.ContainsKey(eventName))
                                        {
                                            myEvents = events[eventName];
                                        }
                                        else
                                        {
                                            myEvents = new List<SocketService>();
                                            events.Add(eventName, myEvents);
                                        }
                                        if (!myEvents.Contains(this))
                                            myEvents.Add(this);
                                        if (!this.myEventList.ContainsValue(myEvents)) {
                                            this.myEventList.Add(eventName,myEvents);
                                        }
                                    }
                                }
                                break;
                            case ActionEnum.Exit:
                                {
                                    lock (events)
                                    {
                                        string eventName = model;
                                        this.myEventList[eventName].Remove(this);
                                        this.myEventList.Remove(eventName);
                                    }
                                }
                                break;
                            case ActionEnum.PostEvent: {
                                    lock (events) {
                                        string eventName = model;
                                        var lst = events[eventName];
                                        byte[] sendData = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(new {action = "event_post",data="" }));
                                        foreach (SocketService sk in lst) {
                                            sk.SendCmd(ActionEnum.PostEvent,eventName);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            };
        }
    }
}
