using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace RemoteNotificationCommon
{
    public class Client
    {
        private Dictionary<string, Action> Events = new Dictionary<string, Action>();
        private ServerTcp<SocketClient> sk = new ServerTcp<SocketClient>(1,1024);
        private SocketClient sc = null;
        public Action ConnectHeadl;
        public Client(IPEndPoint ip,string ApplicationName)
        {
            sk.Init();
            sk.Connect(ip, (SocketRect obj, bool isOk) => {
                if (isOk)
                {
                    obj.SendCmd(ActionEnum.Login, ApplicationName);
                    sc = obj as SocketClient;
                    sc.ReactData = (SocketRect s,byte[] data)=>{
                        ActionEnum actionName = (ActionEnum)(data[0]);
                        switch (actionName) {
                            case ActionEnum.PostEvent: {
                                    string Key = Encoding.UTF8.GetString(data, 1, data.Length - 1);
                                    if (Events.ContainsKey(Key)) {
                                        Events[Key].Invoke();
                                    }
                            }
                            break;
                        }
                    };
                    this.ConnectHeadl?.Invoke();
                }
                else {
                    throw new Exception("服务器连接失败");
                }
            });
        }
        /// <summary>
        /// 添加事件,如果有重复Key,会被替换
        /// </summary>
        /// <param name="key">事件Key</param>
        /// <param name="action">执行的方法</param>
        public void AddEvent(string key, Action action) {
            lock (Events) {
                Events[key] = action;
                this.sc?.SendCmd(ActionEnum.BindEvent, key);
            }
        }
        /// <summary>
        /// 移除事件
        /// </summary>
        /// <param name="key"></param>
        public void Romove(string key) {
            lock (Events)
            {
                Events.Remove(key);
                this.sc?.SendCmd(ActionEnum.RemoveEvent, key);
            }
        }
        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="key"></param>
        public void PostEvent(string key) {
            this.sc?.SendCmd(ActionEnum.PostEvent, key);
        }
    }
}
