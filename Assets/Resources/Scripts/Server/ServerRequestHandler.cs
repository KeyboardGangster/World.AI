using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;

public class ServerRequestHandler : MonoBehaviour
{
    private Snapshot snapshot;

    private void Start()
    {
        this.snapshot = this.GetComponent<Snapshot>();
        new Thread(StartListening).Start();
    }

    private void StartListening()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 9999);
        listener.Start();

        using (TcpClient client = listener.AcceptTcpClient())
        using (NetworkStream stream = client.GetStream())
        {
            //Read request from client.
            string jsonData = ReadString(stream, client.ReceiveBufferSize);
            JObject jsonObject = JObject.Parse(jsonData);

            //Process client's request.
            this.snapshot.RequestCapture();
            while (this.snapshot.CapturedImage == null) { }
            byte[] jpg = this.snapshot.CapturedImage;
            jsonObject.Add("Image", JToken.FromObject(Convert.ToBase64String(jpg)));

            //Send back processed data.
            WriteString(stream, jsonObject.ToString());
        }
    }

    private static string ReadString(NetworkStream stream, int bufferSize)
    {
        int size = BitConverter.ToInt32(ReadBytes(stream, bufferSize, 4));
        return Encoding.ASCII.GetString(ReadBytes(stream, bufferSize, size));
    }

    private static void WriteString(NetworkStream stream, string toWrite)
    {
        byte[] msgSize = BitConverter.GetBytes(Convert.ToInt32(toWrite.Length));
        stream.Write(msgSize, 0, msgSize.Length);

        byte[] bytes = Encoding.ASCII.GetBytes(toWrite);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static byte[] ReadBytes(NetworkStream stream, int bufferSize, int bytesToRead)
    {
        byte[] data = new byte[bytesToRead];
        int pointer = 0;

        int bytesRead;
        int bytesReadTotal = 0;

        do
        {
            bytesRead = stream.Read(data, pointer, Math.Min(bufferSize, bytesToRead - bytesReadTotal));
            bytesReadTotal += bytesRead;
        }
        while (bytesRead != 0 && data.Length != bytesToRead);

        return data;
    }
}
