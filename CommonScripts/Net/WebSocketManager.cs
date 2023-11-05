using WebSocketSharp;
using System.Collections.Generic;
using System;
using UnityEngine;

public class WebSocketManager : MonoSingleton<WebSocketManager>
{
    string url { get; set; }
    string hostIP { get; set; } = string.Empty;
    WebSocket webSocket { get; set; }
    float connectingTimeout { get; set; } = 0f;

    Queue<byte[]> packetQueue = new Queue<byte[]>();

    Action<byte[]> onData;
    Action onClose;
    Action onConnecting;
    Action onOpen;

    bool webSocketIsConnecting
    {
        get
        {
            return webSocket != null && (webSocket.ReadyState == WebSocketState.Open || webSocket.ReadyState == WebSocketState.Closing);
        }
    }

    bool isConnecting
    {
        get
        {
            return null != webSocket && (webSocket.ReadyState == WebSocketState.Open || webSocket.ReadyState == WebSocketState.Connecting);
        }
    }

    void initData()
    {
        webSocket = null;
        connectingTimeout = 0f;

        packetQueue.Clear();
    }

    public void connect(string url, Action<byte[]> onData, Action onClose, Action onConnecting, Action onOpen)
    {
        if (null != webSocket)
        {
            throw new InvalidOperationException("WebSocket != null , must call close() first");
        }

        this.url = url;

        this.onData = onData;
        this.onClose = onClose;
        this.onConnecting = onConnecting;
        this.onOpen = onOpen;

        connect();
    }
    void connect()
    {
        webSocket = new WebSocket(url);

        webSocket.OnOpen += (sender, e) =>
        {
            Debug.Log($"Websocket onOpen:{url}");
            if (null != onOpen)
            {
                onOpen();
            }
        };
        webSocket.OnMessage += (sender, e) =>
        {
            packetQueue.Enqueue(e.RawData);
        };
        webSocket.OnError += (sender, e) =>
        {
            Debug.LogError($"Socket Error:{e.Message}");
        };
        webSocket.OnClose += (sender, e) =>
        {
            Debug.Log($"socket close: {webSocket.ReadyState}");
            closeAction(e);
        };
    }

    public void sendData(byte[] data, Action errorCallback = null)
    {
        if (!webSocketIsConnecting)
        {
            Debug.LogError("Send Data error : websocket is not connected");
            if (null != errorCallback)
            {
                errorCallback();
            }
            return;
        }

        webSocket.Send(data);
    }

    public void closeSocket()
    {
        if (null != webSocket)
        {
            webSocket.Close();
        }

        initData();
    }

    void closeAction(CloseEventArgs close)
    {
        closeSocket();

        if (null != onClose)
        {
            onClose();
        }
    }

    void resetConnectingTimeout()
    {
        connectingTimeout = Time.time + 5f;
    }

    void reConnectSocket()
    {
        Debug.Log("Re Connect");
        connect();
        resetConnectingTimeout();

        webSocket.Connect();
    }

    protected override void OnDestroy()
    {
        closeSocket();
        base.OnDestroy();
    }

    private void Update()
    {
        dispatch();
        checkWebSocketState();
    }

    void dispatch()
    {
        while (packetQueue.Count > 0)
        {
            onData(packetQueue.Dequeue());
        }
    }

    void checkWebSocketState()
    {
        if (null == webSocket)
        {
            return;
        }
        switch (webSocket.ReadyState)
        {
            case WebSocketState.Connecting:
                onConnecting?.Invoke();
                if (Time.time > connectingTimeout)
                {
                    closeSocket();
                    reConnectSocket();
                }
                break;
        }
    }

    public byte[] testMsgPack(byte[] data)
    {
        return data;
    }
}
