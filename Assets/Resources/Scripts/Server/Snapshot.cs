using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(CamTargetPicker))]
public class Snapshot : MonoBehaviour
{


    [SerializeField]
    private Camera cam;
    [SerializeField]
    private WorldGenerator worldGenerator;
    [SerializeField]
    private WorldGeneratorInterface_AI worldGeneratorInterface;
    [SerializeField]
    private AthmosphereControl athmosphereControl;
    private CamTargetPicker targetPicker;

    private object threadLock = new object();

    private bool captureRequested = false;
    private string prompt;
    private string key;

    private byte[][] capturedImages;
    private bool isFailed;

    public byte[][] CapturedImages
    {
        get
        {
            lock (this.threadLock)
                return this.capturedImages;
        }
        private set
        {
            lock (this.threadLock)
                this.capturedImages = value;
        }
    }

    public bool IsFailed
    {
        get
        {
            lock(this.threadLock)
                return this.isFailed;
        }
        private set
        {
            lock(this.threadLock)
                this.isFailed = value;
        }
    }

    private void Awake()
    {
        this.targetPicker = this.GetComponent<CamTargetPicker>();
        StartCoroutine(CoroutineCapture());
    }

    private IEnumerator CoroutineCapture()
    {
        yield return new WaitForSeconds(3);

        while (true)
        {
            lock (this.threadLock)
            {
                if (this.captureRequested)
                {
                    this.worldGeneratorInterface.GenerateWorld(this.prompt, this.key);

                    Debug.Log("Waiting for generation...");
                    yield return new WaitUntil(() => this.worldGeneratorInterface.isGenerated || this.worldGeneratorInterface.isFailed);

                    if (this.worldGeneratorInterface.isFailed)
                    {
                        this.CapturedImages = null;
                        this.captureRequested = false;
                        this.isFailed = true;
                        continue;
                    }

                    Debug.Log("complete!");
                    yield return new WaitForSeconds(1f);

                    Transform[] targets = this.targetPicker.getTargets(this.worldGenerator.Args, this.athmosphereControl.GetTimeOfDay());
                    byte[][] imagesJpg = new byte[targets.Length][];

                    for (int i = 0; i < targets.Length; i++)
                    {
                        this.cam.transform.position = targets[i].position;
                        this.cam.transform.rotation = targets[i].rotation;
                        yield return new WaitForSeconds(1);
                        imagesJpg[i] = this.Capture(targets[i]);

                    }

                    this.CapturedImages = imagesJpg;
                    this.captureRequested = false;
                }
            }

            yield return new WaitForSeconds(1);
        }
    }

    public void RequestCapture(string prompt, string key)
    {
        lock(this.threadLock)
        {
            this.prompt = prompt;
            this.key = key;
            this.captureRequested = true;
            this.CapturedImages = null;
            this.isFailed = false;
        }
    }

    public byte[] Capture(Transform target)
    {
        RenderTexture activeRenderTexture = RenderTexture.active;
        RenderTexture.active = cam.targetTexture;

        cam.Render();

        Texture2D image = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
        image.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        FixColorSpace(image);
        image.Apply();
        RenderTexture.active = activeRenderTexture;

        byte[] bytes = image.EncodeToJPG();
        Destroy(image);

        /*if (!Directory.Exists(Application.dataPath + "/snapshots/"))
            Directory.CreateDirectory(Application.dataPath + "/snapshots/");

        File.WriteAllBytes(Application.dataPath + "/snapshots/__" + Random.Range(0, 99999999) + ".jpg", bytes);*/

        return bytes;

    }

    private void FixColorSpace(Texture2D image)
    {
        for (int y = 0; y < image.height; y++)
        {
            for (int x = 0; x < image.width; x++)
            {
                image.SetPixel(x, y, new Color(
                    Mathf.Pow(image.GetPixel(x, y).r, 1f / 2.2f),
                    Mathf.Pow(image.GetPixel(x, y).g, 1f / 2.2f),
                    Mathf.Pow(image.GetPixel(x, y).b, 1f / 2.2f),
                    Mathf.Pow(image.GetPixel(x, y).a, 1f / 2.2f)
                ));
            }
        }
    }
}