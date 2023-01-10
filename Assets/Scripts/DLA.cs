using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DLA : MonoBehaviour
{
    public enum PlotMode
    {
        C,
        N,
    }
    public ComputeShader compute;
    public Image plotImage;
    public int DIM;
    public PlotMode plotMode = PlotMode.N;
    public float valueMax,valueMin;
    public float dx,dt;
    public float c0,beta,sigma0;
    public int loopCount;
    public bool normalize = false;
    RenderTexture renderTexture;
    ComputeBuffer C,N;
    float[] getC,getN;
    public float normalizeFactor = -1;
    public bool isRadial = false;
    public float radiusFactor;
    public float gaussianSharpness = 6.25f;
    public float evapRate;
    public bool evap = false;
    public bool radial = false;
    // ComputeBuffer Ctmp,Ntmp;
    Texture2D plotTexture;
    Color[] plotPixels;
    // float[] C,N;
    int initkernel,stepkernel,plotkernel,initRadialKernel,stepEvapKernel;
    int stepRadialKernel;
    void Start()
    {
        plotTexture = new Texture2D(DIM,DIM);
        plotTexture.filterMode = FilterMode.Point;
        plotPixels = new Color[DIM*DIM];
        plotImage.sprite = Sprite.Create(plotTexture, new Rect(0,0,DIM,DIM),Vector2.zero);

        renderTexture = new RenderTexture(DIM,DIM,24);
        renderTexture.enableRandomWrite = true;
        C = new ComputeBuffer(DIM*DIM*2,sizeof(float));
        N = new ComputeBuffer(DIM*DIM*2,sizeof(float));
        getC = new float[DIM*DIM*2];
        getN = new float[DIM*DIM*2];
        // Ctmp = new ComputeBuffer(DIM*DIM,sizeof(float));
        // Ntmp = new ComputeBuffer(DIM*DIM,sizeof(float));
        initRadialKernel = compute.FindKernel("InitRadial");
        initkernel = compute.FindKernel("Init");
        stepkernel = compute.FindKernel("Step");
        stepEvapKernel = compute.FindKernel("StepEvap");
        stepRadialKernel = compute.FindKernel("StepRadial");
        plotkernel = compute.FindKernel("Plot");
        compute.SetInt("plotmode",(int)plotMode);
        compute.SetInt("DIM",DIM);
        compute.SetFloat("c0",c0);
        compute.SetFloat("dx",dx);
        compute.SetFloat("dt",dt);
        compute.SetFloat("beta",beta);
        compute.SetFloat("sigma0",sigma0);
        compute.SetFloat("normalizeFactor",normalizeFactor);
        compute.SetFloat("radiusFactor",radiusFactor);
        compute.SetFloat("gaussianSharpness",gaussianSharpness);

        // compute.SetInt("offset",(int)Random.Range(0,int.MaxValue));
        compute.SetBuffer(initkernel,"C",C);
        compute.SetBuffer(initkernel,"N",N);
        compute.SetBuffer(initRadialKernel,"C",C);
        compute.SetBuffer(initRadialKernel,"N",N);
        // compute.SetBuffer(initkernel,"Ctmp",Ctmp);
        // compute.SetBuffer(initkernel,"Ntmp",Ntmp);
        compute.SetBuffer(stepkernel,"C",C);
        compute.SetBuffer(stepkernel,"N",N);
        compute.SetBuffer(stepEvapKernel,"C",C);
        compute.SetBuffer(stepEvapKernel,"N",N);
        compute.SetBuffer(stepRadialKernel,"C",C);
        compute.SetBuffer(stepRadialKernel,"N",N);
        compute.SetBuffer(plotkernel,"C",C);
        compute.SetBuffer(plotkernel,"N",N);
        // compute.SetBuffer(stepkernel,"Ctmp",Ctmp);
        // compute.SetBuffer(stepkernel,"Ntmp",Ntmp);
        // compute.SetBuffer(plotkernel,"Ctmp",Ctmp);
        // compute.SetBuffer(plotkernel,"Ntmp",Ntmp);
        compute.SetTexture(initkernel,"renderTexture",renderTexture);
        compute.SetTexture(initRadialKernel,"renderTexture",renderTexture);
        compute.SetTexture(plotkernel,"renderTexture",renderTexture);

        if(!isRadial) compute.Dispatch(initkernel,(DIM+7)/8,(DIM+7)/8,1);
        else compute.Dispatch(initRadialKernel,(DIM+7)/8,(DIM+7)/8,1);

        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
        
        // C = new float[DIM*DIM];
        // N = new float[DIM*DIM];
        // valueMin = Mathf.Infinity;
        // valueMax = -Mathf.Infinity;
        // for (int i = 0; i < DIM; i++)
        // {
        //     for (int j = 0; j < DIM; j++)
        //     {
        //         float x = ((float)i-(float)DIM/2f)*dx;
        //         float y = ((float)j-(float)DIM/2f)*dx;
        //         C[i + j*DIM] = c0;
        //         N[i + j*DIM] = beta*Mathf.Exp(-(x*x + y*y)/6.25f);

        //         if(plotMode == PlotMode.C) UpdateMinMax(C[i + j*DIM]);
        //         if(plotMode == PlotMode.N) UpdateMinMax(N[i + j*DIM]);
        //     }
        // }
        // UpdatePlot();
    }

    void Update()
    {
        for (int i = 0; i < loopCount; i++)
        {
            compute.SetInt("offset",(int)Random.Range(0,int.MaxValue));
            if(!evap) compute.Dispatch(stepkernel,(DIM+7)/8,(DIM+7)/8,1);
            else if(!radial)
            {
                if(radiusFactor > 0)
                {
                    radiusFactor -= evapRate/(radiusFactor*radiusFactor);
                }
                else radiusFactor = 0;
                compute.SetFloat("radiusFactor",radiusFactor);
                compute.Dispatch(stepEvapKernel,(DIM+7)/8,(DIM+7)/8,1);
            }
            else
            {
                compute.Dispatch(stepRadialKernel,(DIM+7)/8,(DIM+7)/8,1);
            }
            compute.Dispatch(plotkernel,(DIM+7)/8,(DIM+7)/8,1);
        }
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        if(normalize)
        {
            C.GetData(getC);
            for (int i = 0; i < DIM; i++)
            {
                for (int j = 0; j < DIM; j++)
                {
                    normalizeFactor = Mathf.Max(normalizeFactor,getC[i + j*DIM]);
                }
            }
            normalize = false;
            compute.SetFloat("normalizeFactor",normalizeFactor);
        }
        plotTexture.Apply();
    }

    void UpdateMinMax(float value)
    {
        valueMin = Mathf.Min(valueMin,value);
        valueMax = Mathf.Max(valueMax,value);
    }

    void PlotAtPoint(int point,float value)
    {
        plotPixels[point] = GrayScale(value);
    }

    // void UpdatePlot()
    // {
    //     for (int i = 0; i < DIM; i++)
    //     {
    //         for (int j = 0; j < DIM; j++)
    //         {
    //             int index = i+j*DIM;
    //             float value;
    //             if(plotMode == PlotMode.C) value = (C[index]-valueMin)/(valueMax-valueMin);
    //             else value = (N[index]-valueMin)/(valueMax-valueMin);
    //             PlotAtPoint(index, value);
    //         }
    //     }
    //     plotTexture.SetPixels(plotPixels);
    //     plotTexture.Apply();
    // }

    Color GrayScale(float c)
    {
        return new Color(c,c,c,1);
    }
}
