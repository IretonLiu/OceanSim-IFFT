using UnityEngine;

public class OceanGeometry : MonoBehaviour {
    [Header("Mesh Settings")]
    public bool isSquare;
    // number of points on each side
    public int N;
    public int M;
    // width and length of the mesh
    public float Lx;
    public float Lz;
    // Start is called before the first frame update
    [Header("Phillips Spectrum")]
    public float windSpeed;
    public Vector2 windDirection;
    public float A;

    [Header("Choppy Factor")]
    public float lambda;

    [Header("Lighting")]
    public Light lighting;

    [Header("Ocean Material")]
    public Color diffuseColor;
    public Color specularColor;
    public float specularExponent;


    [Header("Compute Shaders")]
    public ComputeShader initialSpectrumCompute; // h0(k) h0(-k)
    public ComputeShader fourierAmplitudeCompute;
    public ComputeShader butterflyTextureCompute;
    public ComputeShader butterflyCompute;
    public ComputeShader inversePermutationCompute;
    public ComputeShader combineCompute;

    [Header("Other")]
    public Material matVis1;
    public Material matVis2;
    public Material waveSurface;
    
    
    
    Texture2D gaussianNoiseTexture1;
    Texture2D gaussianNoiseTexture2;

    bool shouldUpdateStatic = false;
    MeshGenerator meshGenerator;
    WaveGenerator waveGenerator;

    [Header("Wave Plane")]
    public GameObject wavePlane;

    void Start() {
        updateMeshGenerator();

        updateWaveGenerator();


    }

    // Update is called once per frame
    void Update() {

        if (shouldUpdateStatic) {
            updateMeshGenerator();
            updateWaveGenerator();

            shouldUpdateStatic = false;
        }
        waveGenerator.CalcFourierAmplitude();
        waveGenerator.CalcDisplacement();
        waveGenerator.CalcSlopeVector();
        waveGenerator.CombineDisplacementAndSlope(lambda);

        matVis1.SetTexture("_MainTex", waveGenerator.displacement);
        matVis2.SetTexture("_MainTex", waveGenerator.slope);
        waveSurface.SetTexture("_Displacement", waveGenerator.displacement);
        waveSurface.SetTexture("_Slope", waveGenerator.slope);

        waveSurface.SetVector("_LightPos", new Vector4(lighting.transform.position.x, lighting.transform.position.y, lighting.transform.position.z, 0));
        waveSurface.SetVector("_LightColor", lighting.color);

        waveSurface.SetVector("_MatDiffuseColor", diffuseColor);
        waveSurface.SetVector("_MatSpecularColor", specularColor);
        waveSurface.SetFloat("_MatSpecularExponent", specularExponent);

        //     Texture2D tex2 = new Texture2D(h0minusk_RenderTexture.width, h0minusk_RenderTexture.height, TextureFormat.RGB24, false);
        //     RenderTexture.active = h0minusk_RenderTexture;
        //     tex2.ReadPixels(new Rect(0, 0, h0minusk_RenderTexture.width, h0minusk_RenderTexture.height), 0, 0);
        //     Color[] pixels2 = tex2.GetPixels();

    }

    void OnValidate() {
        // update mesh settings
        if (N < 256) N = 256;
        if (M < 256) M = 256;
        if (Lx < 1) Lx = 1;
        if (Lz < 1) Lz = 1;

        if (isSquare) {
            M = N;
            Lz = Lx;
        }
        shouldUpdateStatic = true;
    }

    void updateMeshGenerator() {
        // Transform transform = GetComponent<Transform>();
        // transform.localScale = new Vector3(Lx, 1, Lz);

        // initialize mesh
        MeshFilter meshFilter = wavePlane.GetComponent<MeshFilter>();
        meshGenerator = new MeshGenerator(N, M, Lx, Lz);

        meshFilter.mesh = meshGenerator.oceanMesh;

        meshGenerator.genVertexAndIndexArray();
        meshGenerator.updateMesh();

    }
    void updateWaveGenerator() {
        GaussianNoiseTexture gnt = new GaussianNoiseTexture();
        gaussianNoiseTexture1 = gnt.generateGaussianTexture(256, 256);
        gaussianNoiseTexture2 = gnt.generateGaussianTexture(256, 256);
        waveGenerator = new WaveGenerator(gaussianNoiseTexture1, gaussianNoiseTexture2, meshGenerator);
        waveGenerator.SetComputeShader(initialSpectrumCompute, fourierAmplitudeCompute,
                                        butterflyCompute, inversePermutationCompute, combineCompute);
        waveGenerator.SetPhillipsParams(windSpeed, windDirection, A);
        waveGenerator.InitialSpectrum();
        waveGenerator.PrecomputeTwiddleFactorsAndInputIndices();

    }
}
