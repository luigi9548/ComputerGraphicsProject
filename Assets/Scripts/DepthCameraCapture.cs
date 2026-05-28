using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Perception.GroundTruth;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Application = UnityEngine.Application;

[RequireComponent(typeof(Camera))]
public class DepthCameraCapture : MonoBehaviour
{
    private Camera depthCamera;
    private Material depthMaterial;
    public string outputFolder = "DepthOutput";
    private string fullPath;
    private int frameCount = 0;
    private PerceptionCamera perceptionCamera;

    void Start()
    {
        depthCamera = GetComponent<Camera>();
        depthCamera.depthTextureMode = DepthTextureMode.Depth;

        Shader depthShader = Shader.Find("Hidden/DepthGrayscale");
        depthMaterial = new Material(depthShader);

        fullPath = Path.Combine(Application.persistentDataPath, outputFolder);
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);

        perceptionCamera = Camera.main.GetComponent<PerceptionCamera>();
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDestroy()
    {
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
    }

    void OnEndCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        if (cam != depthCamera) return;
        if (perceptionCamera == null) return;

        Texture depthTex = Shader.GetGlobalTexture("_CameraDepthTexture");
        if (depthTex == null) return;

        depthMaterial.SetTexture("_CameraDepthTexture", depthTex);

        RenderTexture depthRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
        Graphics.Blit(null, depthRT, depthMaterial);

        RenderTexture.active = depthRT;
        Texture2D tex = new Texture2D(depthRT.width, depthRT.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, depthRT.width, depthRT.height), 0, 0);
        tex.Apply();

        string path = Path.Combine(fullPath, $"depth_{frameCount:D4}.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());

        Destroy(tex);
        RenderTexture.ReleaseTemporary(depthRT);
        frameCount++;
    }
}