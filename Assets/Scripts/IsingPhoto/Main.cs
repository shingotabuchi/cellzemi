using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    public ComputeShader compute;
    public Image plotImage;
    public Texture2D photo;
    public int DIM;
    public int cellCount;
    public int maxIterations;
    Texture2D plotTexture;
    RenderTexture renderTexture;
    Color[] plotPixels;
    public int DIM_X,DIM_Y;
    KMeansResults kMeansResult;
    int threadCount = 32;
    public float T;
    float setT;
    public int loopCount;
    int initkernel,stepkernel,plotkernel;
    ComputeBuffer sigma,cellColors;
    int[] getSigmaBuffer;
    Color[] getCellColors;

    // Start is called before the first frame update
    void Start()
    {
        plotImage.transform.localScale = new Vector2(photo.width,photo.height)*6f/Mathf.Max(photo.width,photo.height);
        if(DIM >= Mathf.Max(photo.width,photo.height))
        {
            DIM_X = photo.width;
            DIM_Y = photo.height;
        }
        else
        {
            if(photo.width>photo.height)
            {
                DIM_X = DIM;
                DIM_Y = (DIM*photo.height)/photo.width;
            }
            else
            {
                DIM_Y = DIM;
                DIM_X = (DIM*photo.width)/photo.height;
            }
        }
        print(DIM_X);
        print(DIM_Y);
        RenderTexture rt=new RenderTexture(DIM_X,DIM_Y,24);
        RenderTexture.active = rt;
        Graphics.Blit(photo,rt);
        plotTexture=new Texture2D(DIM_X,DIM_Y);
        plotTexture.filterMode = FilterMode.Point;
        plotTexture.ReadPixels(new Rect(0,0,DIM_X,DIM_Y),0,0);
        plotTexture.Apply();
        plotPixels = new Color[DIM_X*DIM_Y];
        plotPixels = plotTexture.GetPixels();
        plotImage.sprite = Sprite.Create(plotTexture, new Rect(0,0,DIM_X,DIM_Y),Vector2.zero);
        
        renderTexture = new RenderTexture(DIM_X,DIM_Y,24);
        renderTexture.enableRandomWrite = true;

        sigma = new ComputeBuffer(DIM_X*DIM_Y,sizeof(int));
        cellColors = new ComputeBuffer(cellCount,sizeof(float)*4);
        getSigmaBuffer = new int[DIM_X*DIM_Y];
        getCellColors = new Color[cellCount];

        Vector3[] data = new Vector3[DIM_X*DIM_Y];

        for (int i = 0; i < data.Length; i++) {
            data[i] = new Vector3(plotPixels[i].r, plotPixels[i].g, plotPixels[i].b);
        }

        kMeansResult = KMeans.Cluster(data, cellCount, maxIterations, 0);
        print(kMeansResult.itersTaken);
        for (int i = 0; i < kMeansResult.clusters.Length; i++) {
            getCellColors[i] = plotPixels[kMeansResult.centroids[i]];
            for (int j = 0; j < kMeansResult.clusters[i].Length; j++) {
                getSigmaBuffer[kMeansResult.clusters[i][j]] = i;
                // plotPixels[kMeansResult.clusters[i][j]] = plotPixels[kMeansResult.centroids[i]];
            }
        }

        // plotTexture.SetPixels(plotPixels);
        // plotTexture.Apply();

        sigma.SetData(getSigmaBuffer);
        cellColors.SetData(getCellColors);

        initkernel = compute.FindKernel("Init");
        stepkernel = compute.FindKernel("Step");
        plotkernel = compute.FindKernel("Plot");
        compute.SetInt("DIM_X",DIM_X);
        compute.SetInt("DIM_Y",DIM_Y);
        compute.SetFloat("T",T);
        setT = T;
        compute.SetInt("cellCount",cellCount);
        compute.SetInt("offset",(int)Random.Range(0,int.MaxValue));

        compute.SetBuffer(initkernel,"sigma",sigma);
        compute.SetBuffer(stepkernel,"sigma",sigma);
        compute.SetBuffer(plotkernel,"sigma",sigma);
        compute.SetBuffer(initkernel,"cellColors",cellColors);
        compute.SetBuffer(stepkernel,"cellColors",cellColors);
        compute.SetBuffer(plotkernel,"cellColors",cellColors);
        compute.SetTexture(plotkernel,"renderTexture",renderTexture);

        compute.Dispatch(plotkernel,(DIM_X+threadCount-1)/threadCount,(DIM_Y+threadCount-1)/threadCount,1);

        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }

    void Update()
    {
        if(setT!=T)
        {
            compute.SetFloat("T",T);
            setT = T;
        }
        for (int kk = 0; kk < loopCount; kk++)
        {
            compute.SetInt("offset",(int)Random.Range(0,int.MaxValue));
            compute.SetInt("offset1",(int)Random.Range(0,int.MaxValue));
            compute.Dispatch(stepkernel,(DIM_X+threadCount-1)/threadCount,(DIM_Y+threadCount-1)/threadCount,1);
        }
        compute.Dispatch(plotkernel,(DIM_X+threadCount-1)/threadCount,(DIM_Y+threadCount-1)/threadCount,1);
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }
}
