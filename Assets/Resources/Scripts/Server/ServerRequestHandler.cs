using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Serialization;

[RequireComponent(typeof(Snapshot))]
public class ServerRequestHandler : MonoBehaviour
{
    [SerializeField]
    private string serverName;

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
                bool fromSeedFound = jsonObject.TryGetValue("FromSeed", out JToken fromSeed);

                if (!promptFound || !keyFound || !fromSeedFound)
                {
                    Debug.LogError("Invalid json received! Are you using the right version? Disconnecting...");
                    HandlerFailure(jsonObject, stream);
                    continue;
                }

                Debug.Log("Read request. Processing renders...");

                //Process client's request.
                this.snapshot.RequestCapture(prompt.Value<string>(), key.Value<string>(), fromSeed.Value<bool>());
                while (this.snapshot.CapturedImages == null && !this.snapshot.IsFailed) { }

                if (this.snapshot.IsFailed)
                {
                    Debug.LogError("Generation failed, disconnecting.");
                    HandlerFailure(jsonObject, stream);
                    continue;
                }

                byte[][] imagesJpg = this.snapshot.CapturedImages;
                int number = 1;

                foreach (byte[] jpg in imagesJpg)
                {
                    jsonObject.Add($"Image{number++}", JToken.FromObject(Convert.ToBase64String(jpg)));
                }

                jsonObject.Add("ServerName", JToken.FromObject(this.serverName));
                jsonObject.Add("eSeed", JToken.FromObject(this.snapshot.ExtendedSeed));
                jsonObject.Add("ToD", JToken.FromObject($"{Mathf.FloorToInt(this.snapshot.TimeOfDay).ToString().PadLeft(2, '0')}:00"));
                jsonObject.Add("Success", JToken.FromObject(true));

                //Send back processed data.
                WriteString(stream, jsonObject.ToString());
                Debug.Log("Sent images. Waiting for connection-close...");
                WaitForShutdown(stream);
                Debug.Log("Connection closed.");
            }
        }
    }

    private static void HandlerFailure(JObject jsonObject, NetworkStream stream)
    {
        jsonObject.Add("Success", JToken.FromObject(false));
        WriteString(stream, jsonObject.ToString());
        WaitForShutdown(stream);
    }

    private static void WaitForShutdown(NetworkStream stream)
    {
        byte[] buffer = new byte[100];
        while (stream.Read(buffer, 0, buffer.Length) > 0) { }
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
