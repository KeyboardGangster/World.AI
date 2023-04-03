using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class BasicP2pServer : MonoBehaviour
{
    public Snapshot snapshot;

    private void Start()
    {
        //byte[] bytes = this.snapshot.Capture();
        new Thread(() => { ServerTest(); }).Start();
    }

    private void ServerTest()
    {
        TcpListener server = null;
        try
        {
            server = new TcpListener(IPAddress.Parse("127.0.0.1"), 9999);
            server.Start();

            // Buffer for reading data
            byte[] bytes = new byte[256];
            //string data = null;

            // Enter the listening loop.
            while (true)
            {
                Debug.Log("Waiting for a connection... ");
                using TcpClient client = server.AcceptTcpClient();
                Debug.Log("Connected!");

                //data = null;

                // Get a stream object for reading and writing
                NetworkStream stream = client.GetStream();


                /*
                int i;

                // Loop to receive all the data sent by the client.
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    Debug.Log($"Received: {data}");
                }*/

                this.snapshot.RequestCapture();

                while (this.snapshot.CapturedImage == null) { }
                byte[] jpg = this.snapshot.CapturedImage;

                Debug.Log($"Send length: {jpg.Length}");
                stream.Write(BitConverter.GetBytes(jpg.Length));

                Debug.Log("Sending image...");
                stream.Write(jpg, 0, jpg.Length);
                client.Close();

                Debug.Log("All done!");
                break;
            }
        }
        catch (SocketException e)
        {
            Debug.Log($"SocketException: {e}");
        }
        finally
        {
            server.Stop();
        }

        Debug.Log("Server stopped");
    }
}
