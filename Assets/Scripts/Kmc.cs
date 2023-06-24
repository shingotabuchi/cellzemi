using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Kmc : MonoBehaviour
{
    public enum PlotMode
    {
        Liquid,
        Solid,
    }
    public bool isRandom;
    public PlotMode plotMode;
    public ComputeShader compute;
    public Image plotImage;
    public int DIM;
    public float T;
    public float mu;
    public int loopCount;
    public float areaFraction;
    public int particleCount;
    Texture2D plotTexture;
    RenderTexture renderTexture;
    int stepkernel,plotkernel,stepParticle;
    ComputeBuffer lBuffer,nBuffer,nPosBuffer;
    int[] getLBuffer;
    int[] getNBuffer;
    int[] getNPosBuffer;
    Color[] plotPixels;
    private void OnDestroy() {
        lBuffer.Dispose();
        nBuffer.Dispose();
        nPosBuffer.Dispose();
    }
    // Start is called before the first frame update
    void Start()
    {
        plotTexture = new Texture2D(DIM,DIM);
        plotPixels = new Color[DIM*DIM];
        plotTexture.filterMode = FilterMode.Point;
        plotImage.sprite = Sprite.Create(plotTexture, new Rect(0,0,DIM,DIM),Vector2.zero);
        renderTexture = new RenderTexture(DIM,DIM,24);
        renderTexture.enableRandomWrite = true;
        particleCount = (int)((float)(DIM*DIM)*areaFraction*0.25f);
        lBuffer = new ComputeBuffer(DIM*DIM,sizeof(int));
        getLBuffer = new int[DIM*DIM];
        nBuffer = new ComputeBuffer(DIM*DIM,sizeof(int));
        getNBuffer = new int[DIM*DIM];
        nPosBuffer = new ComputeBuffer(particleCount*2,sizeof(int));
        getNPosBuffer = new int[particleCount*2];
        for (int i = 0; i < DIM*DIM; i++)
        {
            getLBuffer[i] = 1;
            getNBuffer[i] = 0;
        }

        int placedParticles = 0;
        while(placedParticles < particleCount)
        {
            int newX = Random.Range(0,DIM);
            int newY = Random.Range(0,DIM);
            if(
                getNBuffer[newX + newY*DIM] == 1
                ||getNBuffer[(newX+1)%DIM + newY*DIM] == 1
                ||getNBuffer[newX + ((newY+1)%DIM)*DIM] == 1
                ||getNBuffer[(newX+1)%DIM + ((newY+1)%DIM)*DIM] == 1
            ) continue;

            getNPosBuffer[placedParticles*2 + 0] = newX;
            getNPosBuffer[placedParticles*2 + 1] = newY;
            getNBuffer[newX + newY*DIM] = 1;
            getNBuffer[(newX+1)%DIM + newY*DIM] = 1;
            getNBuffer[newX + ((newY+1)%DIM)*DIM] = 1;
            getNBuffer[(newX+1)%DIM + ((newY+1)%DIM)*DIM] = 1;
            placedParticles++;
        }

        
        lBuffer.SetData(getLBuffer);
        nBuffer.SetData(getNBuffer);
        nPosBuffer.SetData(getNPosBuffer);

        stepkernel = compute.FindKernel("Step");
        plotkernel = compute.FindKernel("Plot");
        stepParticle = compute.FindKernel("StepParticle");
        compute.SetInt("DIM",DIM);
        compute.SetInt("particleCount",particleCount);
        OnValidate();
        compute.SetInt("offset",(int)Random.Range(0,int.MaxValue));
        compute.SetBuffer(stepkernel,"lBuffer",lBuffer);
        compute.SetBuffer(plotkernel,"lBuffer",lBuffer);
        compute.SetBuffer(stepParticle,"lBuffer",lBuffer);
        compute.SetBuffer(stepkernel,"nBuffer",nBuffer);
        compute.SetBuffer(plotkernel,"nBuffer",nBuffer);
        compute.SetBuffer(stepParticle,"nBuffer",nBuffer);
        compute.SetBuffer(stepkernel,"nPosBuffer",nPosBuffer);
        compute.SetBuffer(plotkernel,"nPosBuffer",nPosBuffer);
        compute.SetBuffer(stepParticle,"nPosBuffer",nPosBuffer);
        compute.SetTexture(plotkernel,"renderTexture",renderTexture);

        compute.Dispatch(plotkernel,(DIM+7)/8,(DIM+7)/8,1);

        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }
    private void OnValidate() {
        compute.SetInt("plotMode",(int)plotMode);
        compute.SetFloat("mu",mu);
        compute.SetFloat("T",T);
    }
    void Update()
    {
        for (int kk = 0; kk < loopCount; kk++)
        {
            compute.SetInt("offset",(int)Random.Range(0,int.MaxValue));
            compute.SetInt("offset1",(int)Random.Range(0,int.MaxValue));
            compute.Dispatch(stepkernel,(DIM+7)/8,(DIM+7)/8,1);
            compute.SetInt("offset",(int)Random.Range(0,int.MaxValue));
            compute.SetInt("offset1",(int)Random.Range(0,int.MaxValue));
            compute.Dispatch(stepParticle,(particleCount+63)/64,1,1);
        }
        compute.Dispatch(plotkernel,(DIM+7)/8,(DIM+7)/8,1);
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }
}
