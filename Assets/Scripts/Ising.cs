using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Ising : MonoBehaviour
{
    public enum PlotMode
    {
        Energy,
        CellType,
        DLM,
    }
    public bool isRandom;
    public PlotMode plotMode;
    PlotMode setPlotMode;
    public ComputeShader compute;
    public Image plotImage;
    public int DIM;
    public int A;
    public float T,lambda;
    float setT,setLambda,setA;
    public int N;
    public int loopCount;
    Texture2D plotTexture;
    RenderTexture renderTexture;
    int initkernel,stepkernel,plotkernel;
    ComputeBuffer sigma;
    int[] getSigmaBuffer;
    Color[] plotPixels;
    // Start is called before the first frame update
    void Start()
    {
        plotTexture = new Texture2D(DIM,DIM);
        plotPixels = new Color[DIM*DIM];
        plotTexture.filterMode = FilterMode.Point;
        plotImage.sprite = Sprite.Create(plotTexture, new Rect(0,0,DIM,DIM),Vector2.zero);
        renderTexture = new RenderTexture(DIM,DIM,24);
        renderTexture.enableRandomWrite = true;
        sigma = new ComputeBuffer(DIM*DIM,sizeof(int));
        getSigmaBuffer = new int[DIM*DIM];
        for (int i = 0; i < DIM*DIM; i++)
        {
            getSigmaBuffer[i] = Random.Range(0,N);
        }
        
        sigma.SetData(getSigmaBuffer);

        initkernel = compute.FindKernel("Init");
        stepkernel = compute.FindKernel("Step");
        plotkernel = compute.FindKernel("Plot");
        compute.SetInt("DIM",DIM);
        compute.SetInt("plotMode",(int)plotMode);
        setPlotMode = plotMode;
        compute.SetFloat("T",T);
        setT = T;
        compute.SetInt("N",N);
        compute.SetInt("sqrtN",(int)Mathf.Sqrt(N));
        compute.SetInt("offset",(int)Random.Range(0,int.MaxValue));

        compute.SetBuffer(initkernel,"sigma",sigma);
        compute.SetBuffer(stepkernel,"sigma",sigma);
        compute.SetBuffer(plotkernel,"sigma",sigma);
        compute.SetTexture(plotkernel,"renderTexture",renderTexture);

        compute.Dispatch(plotkernel,(DIM+7)/8,(DIM+7)/8,1);

        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }
    void Update()
    {
        if(setPlotMode!=plotMode)
        {
            compute.SetInt("plotMode",(int)plotMode);
            setPlotMode = plotMode;
        }
        if(setT!=T)
        {
            compute.SetFloat("T",T);
            setT = T;
        }
        for (int kk = 0; kk < loopCount; kk++)
        {
            compute.SetInt("offset",(int)Random.Range(0,int.MaxValue));
            compute.SetInt("offset1",(int)Random.Range(0,int.MaxValue));
            compute.Dispatch(stepkernel,(DIM+7)/8,(DIM+7)/8,1);
        }
        compute.Dispatch(plotkernel,(DIM+7)/8,(DIM+7)/8,1);
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }
}
