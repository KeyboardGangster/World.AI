using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class Snapshot : MonoBehaviour
{
    private Camera cam;

    private object threadLock = new object();

    private bool captureRequested = false;

    private byte[] capturedImage;

    public byte[] CapturedImage
    {
        get
        {
            lock (this.threadLock)
                return this.capturedImage;
        }
        private set
        {
            lock (this.threadLock)
                this.capturedImage = value;
        }
    }

    private void Awake()
    {
        this.cam = this.GetComponent<Camera>();
        StartCoroutine(CoroutineCapture());
    }

    private IEnumerator CoroutineCapture()
    {
        while(true)
        {
            lock (this.threadLock)
            {
                if (captureRequested)
                {
                    this.CapturedImage = this.Capture();
                    this.captureRequested = false;
                }
            }

            yield return new WaitForSeconds(1);
        }
    }

    public void RequestCapture()
    {
        lock(this.threadLock)
        {
            this.captureRequested = true;
            this.CapturedImage = null;
        }
    }

    public byte[] Capture()
    {
        RenderTexture activeRenderTexture = RenderTexture.active;
        RenderTexture.active = cam.targetTexture;

        cam.Render();

        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        image.Apply();
        RenderTexture.active = activeRenderTexture;

        byte[] bytes = image.EncodeToJPG();
        Destroy(image);

        if (!Directory.Exists(Application.dataPath + "/snapshots/"))
            Directory.CreateDirectory(Application.dataPath + "/snapshots/");

        File.WriteAllBytes(Application.dataPath + "/snapshots/" + Random.Range(0, 99999999) + ".jpg", bytes);
        return bytes;
    }
}