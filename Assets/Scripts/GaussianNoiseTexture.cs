using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GaussianNoiseTexture
{
    // Start is called before the first frame update
    public Texture2D generateGaussianTexture(int width, int height)
    {
        Texture2D gaussianTexture = new Texture2D(width, height, TextureFormat.RGFloat, 0, false);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float r1 = boxMuller(0, 1);
                float r2 = boxMuller(0, 1);
                Color c = new Color(r1, r2, 0, 1);
                gaussianTexture.SetPixel(x, y, c);
            }
        }
        gaussianTexture.Apply();
        return gaussianTexture;
    }
    static float boxMuller(float mean, float stdDev)
    {
        double u1 = 1.0 - Random.value; //uniform(0,1] random doubles
        double u2 = 1.0 - Random.value;
        double randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log((float)u1)) *
                     Mathf.Sin(2.0f * Mathf.PI * (float)u2); //random normal(0,1)
        double randNormal =
                     mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
        return (float)randNormal;
    }
}
