using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;

[RequireComponent(typeof(Snapshot))]
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

        while(true)
        {
            using (TcpClient client = listener.AcceptTcpClient())
            using (NetworkStream stream = client.GetStream())
            {
                Debug.Log("Connection established.");

                //Read request from client.
                string jsonData = ReadString(stream, client.ReceiveBufferSize);
                JObject jsonObject = JObject.Parse(jsonData);

                bool promptFound = jsonObject.TryGetValue("Prompt", out JToken prompt);
                bool keyFound = jsonObject.TryGetValue("Key", out JToken key);

                if (!promptFound || !keyFound)
                {
                    Debug.LogError("No prompt or key given, disconnecting.");
                    continue;
                }

                Debug.Log("Read request. Processing renders...");

                //Process client's request.
                this.snapshot.RequestCapture(prompt.Value<string>(), key.Value<string>());
                while (this.snapshot.CapturedImages == null) { }
                byte[][] imagesJpg = this.snapshot.CapturedImages;
                int number = 1;

                foreach (byte[] jpg in imagesJpg)
                {
                    jsonObject.Add($"Image{number++}", JToken.FromObject(Convert.ToBase64String(jpg)));
                }

                //Send back processed data.
                WriteString(stream, jsonObject.ToString());
                Thread.Sleep(3000);

                Debug.Log("Sent images.");
            }
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
        stream.Flush();
    }

    private static byte[] ReadBytes(NetworkStream stream, int bufferSize, int bytesToRead)
    {
        byte[] data = new byte[bytesToRead];

        int bytesRead;
        int bytesReadTotal = 0;

        do
        {
            bytesRead = stream.Read(data, bytesReadTotal, Math.Min(bufferSize, bytesToRead - bytesReadTotal));
            bytesReadTotal += bytesRead;
        }
        while (bytesRead != 0 && bytesReadTotal != bytesToRead);

        return data;
    }
}
