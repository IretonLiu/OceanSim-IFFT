using UnityEngine;

// [ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class WaveGenerator {
    // Start is called before the first frame update
    static int LOCAL_WORK_GROUP_X = 16;
    static int LOCAL_WORK_GROUP_Y = 16;

    //phillips
    float windSpeed;
    Vector2 windDirection;
    float A;
    [Header("Other")]
    public Texture2D gaussianNoise1;
    public Texture2D gaussianNoise2;
    ComputeShader initialSpectrumCompute; // h0(k) h0(-k)
    ComputeShader fourierAmplitudeCompute;
    //ComputeShader butterflyTextureCompute;
    ComputeShader butterflyCompute;
    ComputeShader inversePermutationCompute;
    ComputeShader combineCompute;


    RenderTexture h0k_RenderTexture;
    RenderTexture h0minusk_RenderTexture;

    RenderTexture butterflyTexture;

    RenderTexture hktDy; // height change, this is the h0 from the paper
    RenderTexture hktDx; // directional change
    RenderTexture hktDz; // directional change
    RenderTexture dhktDx; // derivative
    RenderTexture dhktDz; // derivative

    RenderTexture pingpong0;
    RenderTexture pingpong1;

    RenderTexture displacementX;
    RenderTexture displacementY;
    RenderTexture displacementZ;
    RenderTexture slopeX;
    RenderTexture slopeZ;

    public RenderTexture displacement;
    public RenderTexture slope;


    bool shouldUpdate;

    MeshGenerator meshGenerator;

    public WaveGenerator(Texture2D guassianTex1, Texture2D guassianTex2, MeshGenerator meshGenerator) {
        gaussianNoise1 = guassianTex1;
        gaussianNoise2 = guassianTex2;
        this.meshGenerator = meshGenerator;
    }

    public void SetComputeShader(ComputeShader ISCompute, ComputeShader FACompute,
                                      ComputeShader butterflyCompute, ComputeShader IPCompute, ComputeShader combineCompute) {
        initialSpectrumCompute = ISCompute;
        fourierAmplitudeCompute = FACompute;
        //this.butterflyTextureCompute = butterflyTextureCompute;
        this.butterflyCompute = butterflyCompute;
        inversePermutationCompute = IPCompute;
        this.combineCompute = combineCompute;
    }

    public void SetPhillipsParams(float windSpeed, Vector2 windDirection, float A) {
        this.A = A;
        this.windDirection = windDirection;
        this.windSpeed = windSpeed;
    }
    public void InitialSpectrum() {


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

        initialSpectrumCompute.SetTexture(initialSpectrumKernel, "GaussianNoise", gaussianNoise1);
        initialSpectrumCompute.SetTexture(initialSpectrumKernel, "GaussianNoise2", gaussianNoise2);

        initialSpectrumCompute.SetTexture(initialSpectrumKernel, "H0k", h0k_RenderTexture);
        initialSpectrumCompute.SetTexture(initialSpectrumKernel, "H0minusk", h0minusk_RenderTexture);

        initialSpectrumCompute.Dispatch(initialSpectrumKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);



    }

    public void CalcFourierAmplitude() {

        createTexture(ref hktDy, meshGenerator.N, meshGenerator.M);
        createTexture(ref hktDx, meshGenerator.N, meshGenerator.M);
        createTexture(ref hktDz, meshGenerator.N, meshGenerator.M);
        createTexture(ref dhktDx, meshGenerator.N, meshGenerator.M);
        createTexture(ref dhktDz, meshGenerator.N, meshGenerator.M);

        int fourierAmplitudeKernel = fourierAmplitudeCompute.FindKernel("CSFourierAmplitude");

        fourierAmplitudeCompute.SetInt("N", meshGenerator.N);
        fourierAmplitudeCompute.SetFloat("Lx", meshGenerator.Lx);
        fourierAmplitudeCompute.SetFloat("t", Time.time);



        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "H0k", h0k_RenderTexture);
        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "H0minusk", h0minusk_RenderTexture);

        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "Hkt_dy", hktDy);
        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "Hkt_dx", hktDx);
        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "Hkt_dz", hktDz);
        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "Dhkt_dx", dhktDx);
        fourierAmplitudeCompute.SetTexture(fourierAmplitudeKernel, "Dhkt_dz", dhktDz);

        fourierAmplitudeCompute.Dispatch(fourierAmplitudeKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);
    }

    //public void ComputeButterflyTexture()
    //{
    //    createTexture(ref butterflyTexture, (int)Mathf.Log(meshGenerator.N, 2), meshGenerator.M);
    //    int butterflyTextureKernel = butterflyTextureCompute.FindKernel("CSButterflyTexture");

    //    butterflyTextureCompute.SetInt("N", meshGenerator.N);
    //    butterflyTextureCompute.SetTexture(butterflyTextureKernel, "butterflyTexture", butterflyTexture);

    //    butterflyTextureCompute.Dispatch(butterflyTextureKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);
    //    return;
    //}

    public void PrecomputeTwiddleFactorsAndInputIndices() {
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
        butterflyTexture = rt;
    }

    public void CalcDisplacement() {
        createTexture(ref displacementY, meshGenerator.N, meshGenerator.M);
        createTexture(ref displacementX, meshGenerator.N, meshGenerator.M);
        createTexture(ref displacementZ, meshGenerator.N, meshGenerator.M);

        IFFT(hktDy, displacementY);
        IFFT(hktDx, displacementX);
        IFFT(hktDz, displacementZ);

    }

    public void CalcSlopeVector() {
        createTexture(ref slopeX, meshGenerator.N, meshGenerator.M);
        createTexture(ref slopeZ, meshGenerator.N, meshGenerator.M);

        IFFT(dhktDx, slopeX);
        IFFT(dhktDz, slopeZ);
    }

    public void CombineDisplacementAndSlope(float lambda) {

        createTexture(ref displacement, meshGenerator.N, meshGenerator.M);
        createTexture(ref slope, meshGenerator.N, meshGenerator.M);

        int combineKernel = combineCompute.FindKernel("CSCombine");

        combineCompute.SetTexture(combineKernel, "displacementX", displacementX);
        combineCompute.SetTexture(combineKernel, "displacementY", displacementY);
        combineCompute.SetTexture(combineKernel, "displacementZ", displacementZ);

        combineCompute.SetTexture(combineKernel, "slopeX", slopeX);
        combineCompute.SetTexture(combineKernel, "slopeZ", slopeZ);

        combineCompute.SetTexture(combineKernel, "displacement", displacement);
        combineCompute.SetTexture(combineKernel, "slope", slope);

        combineCompute.SetFloat("lambda", lambda);
        combineCompute.Dispatch(combineKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);

    }



    private void IFFT(RenderTexture input, RenderTexture output) {

        pingpong0 = input;
        createTexture(ref pingpong1, meshGenerator.N, meshGenerator.M);


        int hButterflyKernel = butterflyCompute.FindKernel("CSHorizontalButterflies");
        int vButterflyKernel = butterflyCompute.FindKernel("CSVerticalButterflies");
        bool pingpong = false;

        butterflyCompute.SetTexture(hButterflyKernel, "pingpong0", pingpong0);
        butterflyCompute.SetTexture(hButterflyKernel, "pingpong1", pingpong1);
        butterflyCompute.SetTexture(hButterflyKernel, "ButterflyTexture", butterflyTexture);
        // horizontal fft
        for (int i = 0; i < (int)Mathf.Log(meshGenerator.N, 2); i++) {
            pingpong = !pingpong;
            butterflyCompute.SetInt("stage", i);
            butterflyCompute.SetInt("direction", 0);
            butterflyCompute.SetBool("pingpong", pingpong);
            butterflyCompute.Dispatch(hButterflyKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);

        }

        butterflyCompute.SetTexture(vButterflyKernel, "pingpong0", pingpong0);
        butterflyCompute.SetTexture(vButterflyKernel, "pingpong1", pingpong1);
        butterflyCompute.SetTexture(vButterflyKernel, "ButterflyTexture", butterflyTexture);

        // vertical fft
        for (int i = 0; i < (int)Mathf.Log(meshGenerator.M, 2); i++) {
            pingpong = !pingpong;
            butterflyCompute.SetInt("stage", i);
            butterflyCompute.SetInt("direction", 1);
            butterflyCompute.SetBool("pingpong", pingpong);

            butterflyCompute.Dispatch(vButterflyKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);
        }


        int invPermKernel = inversePermutationCompute.FindKernel("CSMain");

        inversePermutationCompute.SetBool("pingpong", pingpong);
        inversePermutationCompute.SetInt("N", meshGenerator.N);
        inversePermutationCompute.SetTexture(invPermKernel, "displacement", output);
        inversePermutationCompute.SetTexture(invPermKernel, "pingpong0", pingpong0);
        inversePermutationCompute.SetTexture(invPermKernel, "pingpong1", pingpong1);
        inversePermutationCompute.Dispatch(invPermKernel, meshGenerator.N / LOCAL_WORK_GROUP_X, meshGenerator.M / LOCAL_WORK_GROUP_Y, 1);

    }
    void createTexture(ref RenderTexture renderTexture, int xResolution, int yResolution) {
        if (renderTexture != null) {
            renderTexture.Release();
        }
        else {

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
