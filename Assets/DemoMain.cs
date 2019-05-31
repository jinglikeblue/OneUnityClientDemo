using One;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class DemoMain : MonoBehaviour
{
    public InputField inputIp;
    public InputField inputPort;
    public Button btnConnect;
    public Text text;

    List<string> _content = new List<string>();
    BaseTcpProtocolProcess _pp;
    TcpClient _client;
    ThreadSyncActions _tsa = new ThreadSyncActions();

    private void Awake()
    {
        _client = new TcpClient(new BaseTcpProtocolProcess());
        _client.onConnectSuccess += OnConnectSuccess;
        _client.onDisconnect += OnDisconnect;
        _client.onConnectFail += OnConnectFail;

        inputIp.text = PlayerPrefs.GetString("ip", "127.0.0.1");
        inputPort.text = PlayerPrefs.GetString("port", "1875");
    }

    private void Start()
    {
        StartCoroutine(NetCheck());
    }

    private void OnDestroy()
    {
        _client.Close();
    }

    private void Update()
    {
        _tsa.RunSyncActions();
    }

    IEnumerator NetCheck()
    {
        _pp = _client.protocolProcess as BaseTcpProtocolProcess;
        while (true)
        {
            if (_client.IsConnected)
            {
                _pp.ReceiveProtocols(OnReceiveProtocol);
            }
            yield return new WaitForSeconds(1f);
        }
    }

    public void ConnectServer()
    {
        var ip = inputIp.text.Trim();        
        var port = int.Parse(inputPort.text.Trim());

        PlayerPrefs.SetString("ip", ip);
        PlayerPrefs.SetString("port", port.ToString());

        _client.Connect(ip, port, 4096);
    }

    void Send()
    {
        BaseTcpProtocolBody obj = new BaseTcpProtocolBody();
        obj.value = DateTime.Now.ToFileTimeUtc().ToString();
        _client.Send(_pp.Pack(obj));
        var log = string.Format("[{0}] 发送消息:{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), obj.value);
        Log(log);
    }

    void Log(string content)
    {
        Debug.Log(content);
        _content.Insert(0, content);
        if (_content.Count > 20)
        {
            _content.RemoveRange(20, _content.Count - 20);
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < _content.Count; i++)
        {
            sb.AppendLine(_content[i]);
        }

        text.text = sb.ToString();
    }

    private void OnDisconnect(object sender, IRemoteProxy e)
    {
        _tsa.AddToSyncAction(() =>
        {
            var log = string.Format("连接断开：{0}", Thread.CurrentThread.ManagedThreadId);
            Log(log);
        });
    }

    private void OnReceiveProtocol(BaseTcpProtocolBody obj)
    {
        long last = long.Parse(obj.value);
        long now = DateTime.Now.ToFileTimeUtc();

        var log = string.Format("[{0}] 收到消息:{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), obj.value);
        Log(log);

        Send();
    }

    private void OnConnectSuccess(object sender, IRemoteProxy e)
    {
        _tsa.AddToSyncAction(() =>
        {
            var log = string.Format("连接成功：{0}", Thread.CurrentThread.ManagedThreadId);
            Log(log);

            Send();
        });
    }

    private void OnConnectFail(object sender, IRemoteProxy e)
    {
        _tsa.AddToSyncAction(() =>
        {
            var log = string.Format("连接失败：{0}", Thread.CurrentThread.ManagedThreadId);
            Log(log);
        });
    }


}
