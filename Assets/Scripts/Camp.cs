using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Camp : MonoBehaviour
{
    struct point_data
    {
        float C,E,time;
        int state;
    }
    public Image plotImage;
    public int DIM;
    public ComputeShader compute;
    public float initE,dt,D,gamma,a,dx,Cmin,Cmax,tau,tauar,taurr;
    // ComputeBuffer C,E,state,time;
    // ComputeBuffer CTmp,ETmp;
    ComputeBuffer pointDatas,pointDatasTmp;
    RenderTexture renderTexture;
    int initkernel,stepkernel,copykernel,minmaxkernel,plotkernel;
    Texture2D plotTexture;
    Color[] plotPixels;
    // Start is called before the first frame update
    void Start()
    {
        
        plotTexture = new Texture2D(DIM,DIM);
        plotTexture.filterMode = FilterMode.Point;
        plotPixels = plotTexture.GetPixels();
        plotImage.sprite = Sprite.Create(plotTexture, new Rect(0,0,DIM,DIM),Vector2.zero);
        // plotPixels[0] = new Color(1,2,3,4);
        // plotTexture.SetPixels(plotPixels);
        // plotTexture.Apply();
        initkernel = compute.FindKernel("Init");
        stepkernel = compute.FindKernel("Step");
        copykernel = compute.FindKernel("Copy");
        minmaxkernel = compute.FindKernel("MinMax");
        plotkernel = compute.FindKernel("Plot");
        compute.SetInt("DIM",DIM);
        compute.SetFloat("initE",initE);
        compute.SetFloat("dt",dt);
        compute.SetFloat("D",D);
        compute.SetFloat("a",a);
        compute.SetFloat("dx",dx);
        compute.SetFloat("dy",dx);
        compute.SetFloat("Cmin",Cmin);
        compute.SetFloat("Cmax",Cmax);
        compute.SetFloat("tau",tau);
        compute.SetFloat("tauar",tauar);
        compute.SetFloat("taurr",taurr);

        renderTexture = new RenderTexture(DIM,DIM,24);
        renderTexture.enableRandomWrite = true;
        // C = new ComputeBuffer(DIM*DIM,sizeof(float));
        // state = new ComputeBuffer(DIM*DIM,sizeof(int));
        // E = new ComputeBuffer(DIM*DIM,sizeof(float));
        // CTmp = new ComputeBuffer(DIM*DIM,sizeof(float));
        // ETmp = new ComputeBuffer(DIM*DIM,sizeof(float));
        // time = new ComputeBuffer(DIM*DIM,sizeof(float));

        pointDatas = new ComputeBuffer(DIM*DIM,sizeof(float)*3 + sizeof(int));
        pointDatasTmp = new ComputeBuffer(DIM*DIM,sizeof(float)*3 + sizeof(int));

        compute.SetBuffer(initkernel,"pointDatas",pointDatas);
        compute.SetBuffer(initkernel,"pointDatasTmp",pointDatasTmp);
        compute.SetBuffer(stepkernel,"pointDatas",pointDatas);
        compute.SetBuffer(stepkernel,"pointDatasTmp",pointDatasTmp);
        compute.SetBuffer(copykernel,"pointDatas",pointDatas);
        compute.SetBuffer(copykernel,"pointDatasTmp",pointDatasTmp);
        compute.SetBuffer(minmaxkernel,"pointDatas",pointDatas);
        compute.SetBuffer(plotkernel,"pointDatas",pointDatas);
        // compute.SetBuffer(initkernel,"C",C);
        // compute.SetBuffer(initkernel,"state",state);
        // compute.SetBuffer(initkernel,"E",E);
        // compute.SetBuffer(initkernel,"CTmp",CTmp);
        // compute.SetBuffer(initkernel,"ETmp",ETmp);
        // compute.SetBuffer(initkernel,"time",time);
        compute.SetTexture(initkernel,"renderTexture",renderTexture);
        compute.SetTexture(plotkernel,"renderTexture",renderTexture);

        // compute.SetBuffer(stepkernel,"C",C);
        // compute.SetBuffer(stepkernel,"state",state);
        // compute.SetBuffer(stepkernel,"E",E);
        // compute.SetBuffer(stepkernel,"CTmp",CTmp);
        // compute.SetBuffer(stepkernel,"ETmp",ETmp);
        // compute.SetBuffer(stepkernel,"time",time);

        compute.Dispatch(initkernel,(DIM+7)/8,(DIM+7)/8,1);
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }

    // Update is called once per frame
    void Update()
    {
        compute.Dispatch(stepkernel,(DIM+7)/8,(DIM+7)/8,1);
        compute.Dispatch(copykernel,(DIM+7)/8,(DIM+7)/8,1);
        compute.Dispatch(minmaxkernel,1,1,1);
        compute.Dispatch(plotkernel,(DIM+7)/8,(DIM+7)/8,1);
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }
}
