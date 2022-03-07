using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class WaveGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    static int LOCAL_WORK_GROUP_X = 16;
    static int LOCAL_WORK_GROUP_Y = 16;

    [Header("Phillips Spectrum")]
    public float windSpeed;
    public Vector2 windDirection;
    public float A;

    [Header("Other")]
    public Texture2D gaussianNoise;
    public ComputeShader fourierAmplitudeCompute; // h0(k) h0(-k)


    public RenderTexture h0k_RenderTexture;
    public RenderTexture h0minusk_RenderTexture;


    public Material matH0k;
    public Material matH0minusk;

    // MeshData meshData;

    // void OnRenderImage(RenderTexture src, RenderTexture dest)
    // {

    //     mat.SetTexture("_MainTex", h0k_RenderTexture);
    //     // Read pixels from the source RenderTexture, apply the material, copy the updated results to the destination RenderTexture
    //     Graphics.Blit(h0k_RenderTexture, dest);

    // }
    bool shouldUpdate;
    void Awake()
    {
        shouldUpdate = true;
    }
    void OnValidate()
    {
        shouldUpdate = true;
    }

    void Update()
    {
        if (shouldUpdate)
        {
            genFourierAmplitude();
            matH0k.SetTexture("_MainTex", h0k_RenderTexture);
            matH0minusk.SetTexture("_MainTex", h0minusk_RenderTexture);
            shouldUpdate = false;
        }
    }

    void genFourierAmplitude()
    {
        MeshGenerator meshGenerator = FindObjectOfType<MeshGenerator>();

        // if (h0k_RenderTexture == null)
        createTexture(ref h0k_RenderTexture, meshGenerator.N, meshGenerator.M);

        // if (h0minusk_RenderTexture == null)
        createTexture(ref h0minusk_RenderTexture, meshGenerator.N, meshGenerator.M);


        int initialSpectrumKernel = fourierAmplitudeCompute.FindKernel("CSInitialSpectrum");
        // int conjugateSpectrumKernel = fourierAmplitudeCompute.FindKernel("CSConjugateSpectrum");

        fourierAmplitudeCompute.SetInt("N", meshGenerator.N);
        fourierAmplitudeCompute.SetFloat("Lx", meshGenerator.Lx);

        fourierAmplitudeCompute.SetFloat("windSpeed", windSpeed);

        fourierAmplitudeCompute.SetFloats("windDirection", new float[] { windDirection.normalized.x, windDirection.normalized.y });
        fourierAmplitudeCompute.SetFloat("A", A);

        fourierAmplitudeCompute.SetTexture(initialSpectrumKernel, "GaussianNoise", gaussianNoise);
        fourierAmplitudeCompute.SetTexture(initialSpectrumKernel, "H0k", h0k_RenderTexture);
        fourierAmplitudeCompute.SetTexture(initialSpectrumKernel, "H0minusk", h0minusk_RenderTexture);

        fourierAmplitudeCompute.Dispatch(initialSpectrumKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);

        // fourierAmplitudeCompute.SetTexture(conjugateSpectrumKernel, "H0k", h0k_RenderTexture);
        // fourierAmplitudeCompute.SetTexture(conjugateSpectrumKernel, "H0minusk", h0minusk_RenderTexture);
        // fourierAmplitudeCompute.Dispatch(conjugateSpectrumKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);
    }

    void createTexture(ref RenderTexture renderTexture, int xResolution, int yResolution)
    {
        if (renderTexture == null || !renderTexture.IsCreated() || renderTexture.width != xResolution || renderTexture.height != yResolution)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
            }
            renderTexture = new RenderTexture(xResolution, yResolution, 0, RenderTextureFormat.ARGB32);
            renderTexture.enableRandomWrite = true;
            // renderTexture.volumeDepth = resolution;
            renderTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            renderTexture.wrapMode = TextureWrapMode.Mirror;
            renderTexture.filterMode = FilterMode.Bilinear;
            renderTexture.Create();
        }

    }

}
