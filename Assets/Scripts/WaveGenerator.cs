using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [ExecuteInEditMode, ImageEffectAllowedInSceneView]
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
    public ComputeShader butterflyTextureCompute;
    public ComputeShader butterflyCompute;
    public ComputeShader inversePermutationCompute;

    public RenderTexture h0k_RenderTexture;
    public RenderTexture h0minusk_RenderTexture;

    public RenderTexture butterflyTexture;


    public RenderTexture hktDy; // height change, this is the h0 from the paper
    public RenderTexture hktDx; // directional change
    public RenderTexture hktDz; // directional change

    public RenderTexture displacement;
    public RenderTexture pingpong0;
    public RenderTexture pingpong1;

    public Material matVis;

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
            // matVis.SetTexture("_MainTex", hktDy);
            // matH0minusk.SetTexture("_MainTex", h0minusk_RenderTexture);
            // computeButterflyTexture();
            butterflyTexture = PrecomputeTwiddleFactorsAndInputIndices();

            shouldUpdate = false;
        }
        calcFourierAmplitude();
        fft(hktDy);

        matVis.SetTexture("_MainTex", displacement);

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

    void computeButterflyTexture()
    {
        MeshGenerator meshGenerator = FindObjectOfType<MeshGenerator>();
        createTexture(ref butterflyTexture, (int)Mathf.Log(meshGenerator.N, 2), meshGenerator.M);
        int butterflyTextureKernel = butterflyTextureCompute.FindKernel("CSButterflyTexture");

        butterflyTextureCompute.SetInt("N", meshGenerator.N);
        butterflyTextureCompute.SetTexture(butterflyTextureKernel, "butterflyTexture", butterflyTexture);

        butterflyTextureCompute.Dispatch(butterflyTextureKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);
        return;
    }

    RenderTexture PrecomputeTwiddleFactorsAndInputIndices()
    {
        MeshGenerator meshGenerator = FindObjectOfType<MeshGenerator>();
        int size = meshGenerator.N;
        int logSize = (int)Mathf.Log(size, 2);
        RenderTexture rt = new RenderTexture(logSize, size, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt.filterMode = FilterMode.Point;
        rt.wrapMode = TextureWrapMode.Repeat;
        rt.enableRandomWrite = true;
        rt.Create();

        int precomputeKernel = butterflyCompute.FindKernel("PrecomputeTwiddleFactorsAndInputIndices");

        butterflyCompute.SetInt("Size", size);
        butterflyCompute.SetTexture(precomputeKernel, "ButterflyBuffer", rt);
        butterflyCompute.Dispatch(precomputeKernel, logSize, size / LOCAL_WORK_GROUP_Y, 1);
        return rt;
    }

    void fft(RenderTexture fourierAmplitude)
    {
        MeshGenerator meshGenerator = FindObjectOfType<MeshGenerator>();

        pingpong0 = fourierAmplitude;
        createTexture(ref pingpong1, meshGenerator.N, meshGenerator.M);
        createTexture(ref displacement, meshGenerator.N, meshGenerator.M);

        int hButterflyKernel = butterflyCompute.FindKernel("CSHorizontalButterflies");
        int vButterflyKernel = butterflyCompute.FindKernel("CSVerticalButterflies");
        bool pingpong = false;

        butterflyCompute.SetTexture(hButterflyKernel, "pingpong0", pingpong0);
        butterflyCompute.SetTexture(hButterflyKernel, "pingpong1", pingpong1);
        butterflyCompute.SetTexture(hButterflyKernel, "ButterflyTexture", butterflyTexture);
        // horizontal fft
        for (int i = 0; i < (int)Mathf.Log(meshGenerator.N, 2); i++)
        {
            pingpong = !pingpong;
            butterflyCompute.SetInt("stage", i);
            butterflyCompute.SetInt("direction", 0);
            butterflyCompute.SetBool("pingpong", pingpong);
            butterflyCompute.Dispatch(hButterflyKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);

        }

        pingpong0 = fourierAmplitude;

        butterflyCompute.SetTexture(vButterflyKernel, "pingpong0", pingpong0);
        butterflyCompute.SetTexture(vButterflyKernel, "pingpong1", pingpong1);
        butterflyCompute.SetTexture(vButterflyKernel, "ButterflyTexture", butterflyTexture);
        // vertical fft
        // pingpong = 0;

        for (int i = 0; i < (int)Mathf.Log(meshGenerator.M, 2); i++)
        {
            pingpong = !pingpong;
            butterflyCompute.SetInt("stage", i);
            butterflyCompute.SetInt("direction", 1);
            butterflyCompute.SetBool("pingpong", pingpong);

            butterflyCompute.Dispatch(vButterflyKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);
        }


        int invPermKernel = inversePermutationCompute.FindKernel("CSMain");

        inversePermutationCompute.SetBool("pingpong", pingpong);
        inversePermutationCompute.SetInt("N", meshGenerator.N);
        inversePermutationCompute.SetTexture(invPermKernel, "displacement", displacement);
        inversePermutationCompute.SetTexture(invPermKernel, "pingpong0", pingpong0);
        inversePermutationCompute.SetTexture(invPermKernel, "pingpong1", pingpong1);
        inversePermutationCompute.Dispatch(invPermKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);

    }
    void createTexture(ref RenderTexture renderTexture, int xResolution, int yResolution)
    {
        if (renderTexture == null || !renderTexture.IsCreated() || renderTexture.width != xResolution || renderTexture.height != yResolution)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
            }
            renderTexture = new RenderTexture(xResolution, yResolution, 0, RenderTextureFormat.ARGBFloat);
            renderTexture.enableRandomWrite = true;
            renderTexture.wrapMode = TextureWrapMode.Repeat;
            renderTexture.filterMode = FilterMode.Trilinear;
            renderTexture.useMipMap = false;
            renderTexture.autoGenerateMips = false;
            renderTexture.anisoLevel = 6;

            renderTexture.Create();
        }

    }

}
