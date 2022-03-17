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
    public Texture2D gaussianNoise2;
    public ComputeShader initialSpectrumCompute; // h0(k) h0(-k)
    public ComputeShader fourierAmplitudeCompute;


    public RenderTexture h0k_RenderTexture;
    public RenderTexture h0minusk_RenderTexture;


    public RenderTexture hktDy; // height change, this is the h0 from the paper
    public RenderTexture hktDx; // directional change
    public RenderTexture hktDz; // directional change


    public Material matVis;
    // public Material matH0minusk;


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
        GaussianNoiseTexture gnt = new GaussianNoiseTexture();
        gaussianNoise2 = gnt.generateGaussianTexture(256, 256);
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
            genInitialSpectrum();
            matVis.SetTexture("_MainTex", hktDy);
            // matH0minusk.SetTexture("_MainTex", h0minusk_RenderTexture);
            shouldUpdate = false;
        }
        calcFourierAmplitude();
        matVis.SetTexture("_MainTex", hktDy);

        // if (h0k_RenderTexture != null && h0minusk_RenderTexture != null)
        // {
        //     Texture2D tex1 = new Texture2D(h0k_RenderTexture.width, h0k_RenderTexture.height, TextureFormat.RGB24, false);
        //     RenderTexture.active = h0k_RenderTexture;
        //     tex1.ReadPixels(new Rect(0, 0, h0k_RenderTexture.width, h0k_RenderTexture.height), 0, 0);
        //     Color[] pixels1 = tex1.GetPixels();

        //     Texture2D tex2 = new Texture2D(h0minusk_RenderTexture.width, h0minusk_RenderTexture.height, TextureFormat.RGB24, false);
        //     RenderTexture.active = h0minusk_RenderTexture;
        //     tex2.ReadPixels(new Rect(0, 0, h0minusk_RenderTexture.width, h0minusk_RenderTexture.height), 0, 0);
        //     Color[] pixels2 = tex2.GetPixels();
        //     for (int i = 0; i < 256; i++)
        //     {
        //         for (int j = 0; j < 256; j++)
        //         {
        //             int index = j + i * 256;
        //             if (!pixels1[index].Equals(pixels2[index]))
        //             {
        //                 print("textures are different at" + index);
        //             }

        //         }
        //     }
        // }
    }

    void genInitialSpectrum()
    {

        MeshGenerator meshGenerator = FindObjectOfType<MeshGenerator>();

        // if (h0k_RenderTexture == null)
        createTexture(ref h0k_RenderTexture, meshGenerator.N, meshGenerator.M);
        createTexture(ref h0minusk_RenderTexture, meshGenerator.N, meshGenerator.M);


        int initialSpectrumKernel = initialSpectrumCompute.FindKernel("CSInitialSpectrum");
        // int conjugateSpectrumKernel = fourierAmplitudeCompute.FindKernel("CSConjugateSpectrum");

        initialSpectrumCompute.SetInt("N", meshGenerator.N);
        initialSpectrumCompute.SetFloat("Lx", meshGenerator.Lx);

        initialSpectrumCompute.SetFloat("windSpeed", windSpeed);

        initialSpectrumCompute.SetFloats("windDirection", new float[] { windDirection.normalized.x, windDirection.normalized.y });
        initialSpectrumCompute.SetFloat("A", A);

        initialSpectrumCompute.SetTexture(initialSpectrumKernel, "GaussianNoise", gaussianNoise);
        initialSpectrumCompute.SetTexture(initialSpectrumKernel, "GaussianNoise2", gaussianNoise2);

        initialSpectrumCompute.SetTexture(initialSpectrumKernel, "H0k", h0k_RenderTexture);
        initialSpectrumCompute.SetTexture(initialSpectrumKernel, "H0minusk", h0minusk_RenderTexture);

        initialSpectrumCompute.Dispatch(initialSpectrumKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);
    }

    void calcFourierAmplitude()
    {
        MeshGenerator meshGenerator = FindObjectOfType<MeshGenerator>();

        createTexture(ref hktDy, meshGenerator.N, meshGenerator.M);
        createTexture(ref hktDx, meshGenerator.N, meshGenerator.M);
        createTexture(ref hktDz, meshGenerator.N, meshGenerator.M);

        int fourierAmplitudeKernel = fourierAmplitudeCompute.FindKernel("CSFourierAmplitude");

        fourierAmplitudeCompute.SetInt("N", meshGenerator.N);
        fourierAmplitudeCompute.SetFloat("Lx", meshGenerator.Lx);
        fourierAmplitudeCompute.SetFloat("t", Time.time);



        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "H0k", h0k_RenderTexture);
        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "H0minusk", h0minusk_RenderTexture);

        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "Hkt_dy", hktDy);
        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "Hkt_dx", hktDx);
        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "Hkt_dz", hktDz);

        fourierAmplitudeCompute.Dispatch(fourierAmplitudeKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);
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
