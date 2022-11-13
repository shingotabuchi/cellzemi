using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Potts : MonoBehaviour
{
    public enum PlotMode
    {
        Energy,
        CellType,
        DLM,
    }
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
    public float[] Jdd_dl_ll_m = new float[4]{
        2f,
        11f,
        14f,
        16f
    };
    Texture2D plotTexture;
    RenderTexture renderTexture;
    int initkernel,stepkernel,plotkernel;
    ComputeBuffer sigma;
    int[] getSigmaBuffer;
    ComputeBuffer area;
    int[] getAreaBuffer;
    int[] dlmBuffer;
    public bool getArea;
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
        dlmBuffer = new int[N];
        area = new ComputeBuffer(N,sizeof(int));
        getAreaBuffer = new int[N];
        for (int i = 0; i < N; i++)
        {
            getAreaBuffer[i] = 0;
            if(i!=N-1)dlmBuffer[i] = Random.Range(0,2);
            else dlmBuffer[i] = 2;
        }
        for (int i = 0; i < DIM*DIM; i++)
        {
            getSigmaBuffer[i] = Random.Range(0,N);
            getAreaBuffer[getSigmaBuffer[i]]++;
        }
        
        int maxArea = 0;
        int minArea = (int)(DIM*DIM);
        int aveArea = 0;
        for (int i = 0; i < getAreaBuffer.Length; i++)
        {
            maxArea = (int)Mathf.Max(maxArea,getAreaBuffer[i]);
            minArea = (int)Mathf.Min(minArea,getAreaBuffer[i]);
            aveArea += getAreaBuffer[i];
        }
        print("hello");
        print(maxArea);
        print(minArea);
        print(aveArea);
        print(aveArea/getAreaBuffer.Length);

        sigma.SetData(getSigmaBuffer);
        area.SetData(getAreaBuffer);

        initkernel = compute.FindKernel("Init");
        stepkernel = compute.FindKernel("Step");
        plotkernel = compute.FindKernel("Plot");
        compute.SetInt("DIM",DIM);
        compute.SetInt("plotMode",(int)plotMode);
        setPlotMode = plotMode;
        compute.SetFloat("T",T);
        compute.SetFloat("lambda",lambda);
        // A = (DIM*DIM)/N;
        compute.SetInt("A",A);
        setA = A;
        setT = T;
        setLambda = lambda;
        compute.SetInt("N",N);
        compute.SetInt("sqrtN",(int)Mathf.Sqrt(N));
        compute.SetInt("offset",(int)Random.Range(0,int.MaxValue));

        compute.SetBuffer(initkernel,"sigma",sigma);
        compute.SetBuffer(stepkernel,"sigma",sigma);
        compute.SetBuffer(plotkernel,"sigma",sigma);
        compute.SetBuffer(initkernel,"area",area);
        compute.SetBuffer(stepkernel,"area",area);
        compute.SetBuffer(plotkernel,"area",area);
        compute.SetTexture(plotkernel,"renderTexture",renderTexture);

        // compute.Dispatch(initkernel,(DIM+7)/8,(DIM+7)/8,1);


        compute.Dispatch(plotkernel,(DIM+7)/8,(DIM+7)/8,1);

        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }

    void Update()
    {
        if(getArea)
        {
            getArea = false;
            int maxArea = 0;
            int minArea = (int)(DIM*DIM);
            int aveArea = 0;
            for (int i = 0; i < getAreaBuffer.Length; i++)
            {
                maxArea = (int)Mathf.Max(maxArea,getAreaBuffer[i]);
                minArea = (int)Mathf.Min(minArea,getAreaBuffer[i]);
                aveArea += getAreaBuffer[i];
            }
            print(maxArea);
            print(minArea);
            print(aveArea);
            print(aveArea/getAreaBuffer.Length);
        }
        for (int kk = 0; kk < loopCount; kk++)
        {
            // int i = Random.Range(0,DIM);
            // int j = Random.Range(0,DIM);
            for (int i = 0; i < DIM; i++)
            {
                for (int j = 0; j < DIM; j++)
                {
            int[] nearby = new int[4]{
                getSigmaBuffer[(i+1)%DIM + j*DIM],
                getSigmaBuffer[(i-1+DIM)%DIM + j*DIM],
                getSigmaBuffer[i + ((j+1)%DIM)*DIM],
                getSigmaBuffer[i + ((j-1+DIM)%DIM)*DIM]
            };
            
            int[] nearbydlm = new int[4]{
                dlmBuffer[nearby[0]],
                dlmBuffer[nearby[1]],
                dlmBuffer[nearby[2]],
                dlmBuffer[nearby[3]]
            };

            if(i+1>=DIM)
            {
                nearby[0] = N-1;
                nearbydlm[0] = 2;
            }
            if(i-1<0)
            {
                nearby[1] = N-1;
                nearbydlm[1] = 2;
            }
            if(j+1>=DIM)
            {
                nearby[2] = N-1;
                nearbydlm[2] = 2;
            }
            if(j-1<0)
            {
                nearby[3] = N-1;
                nearbydlm[3] = 2;
            }

            int index = Random.Range(0,4);
            int s = nearby[index];
            int dlm = nearbydlm[index];

            float[] nearbyJbefore = new float[4]{0,0,0,0};
            float[] nearbyJafter = new float[4]{0,0,0,0};
            
            for (int l = 0; l < 4; l++)
            {
                if(dlmBuffer[getSigmaBuffer[i + j*DIM]]==nearbydlm[l])
                {
                    if(nearbydlm[l]==0) nearbyJbefore[l] = Jdd_dl_ll_m[0];
                    if(nearbydlm[l]==1) nearbyJbefore[l] = Jdd_dl_ll_m[2];
                }
                else
                {
                    if(nearbydlm[l]==0&&dlmBuffer[getSigmaBuffer[i + j*DIM]]==1 || nearbydlm[l]==1&&dlmBuffer[getSigmaBuffer[i + j*DIM]]==0) 
                    nearbyJbefore[l] = Jdd_dl_ll_m[1];
                    else nearbyJbefore[l] = Jdd_dl_ll_m[3];
                }

                if(dlm==nearbydlm[l])
                {
                    if(nearbydlm[l]==0) nearbyJafter[l] = Jdd_dl_ll_m[0];
                    if(nearbydlm[l]==1) nearbyJafter[l] = Jdd_dl_ll_m[2];
                }
                else
                {
                    if(nearbydlm[l]==0&&dlm==1 || nearbydlm[l]==1&&dlm==0) 
                    nearbyJafter[l] = Jdd_dl_ll_m[1];
                    else nearbyJafter[l] = Jdd_dl_ll_m[3];
                }
            }
            
            float dH = 0;
            if(s!=nearby[0]) dH += nearbyJafter[0];
            if(s!=nearby[1]) dH += nearbyJafter[1];
            if(s!=nearby[2]) dH += nearbyJafter[2];
            if(s!=nearby[3]) dH += nearbyJafter[3];
            if(getSigmaBuffer[i + j*DIM]!=nearby[0]) dH -= nearbyJbefore[0];
            if(getSigmaBuffer[i + j*DIM]!=nearby[1]) dH -= nearbyJbefore[1];
            if(getSigmaBuffer[i + j*DIM]!=nearby[2]) dH -= nearbyJbefore[2];
            if(getSigmaBuffer[i + j*DIM]!=nearby[3]) dH -= nearbyJbefore[3];

            // if(s!=getSigmaBuffer[(i+1)%DIM + j*DIM]) dH += 1f;
            // if(s!=getSigmaBuffer[(i-1+DIM)%DIM + j*DIM]) dH += 1f;
            // if(s!=getSigmaBuffer[i + ((j+1)%DIM)*DIM]) dH += 1f;
            // if(s!=getSigmaBuffer[i + ((j-1+DIM)%DIM)*DIM]) dH += 1f;
            // if(getSigmaBuffer[i + j*DIM]!=getSigmaBuffer[ (i+1)%DIM + j*DIM]) dH -= 1f;
            // if(getSigmaBuffer[i + j*DIM]!=getSigmaBuffer[ (i-1+DIM)%DIM + j*DIM]) dH -= 1f;
            // if(getSigmaBuffer[i + j*DIM]!=getSigmaBuffer[ i + ((j+1)%DIM)*DIM]) dH -= 1f;
            // if(getSigmaBuffer[i + j*DIM]!=getSigmaBuffer[ i + ((j-1+DIM)%DIM)*DIM]) dH -= 1f;
            

            // float dH = 0;
            if(s!=getSigmaBuffer[i + j*DIM])
            {
                if(s!=N-1)
                {
                    dH += lambda*(float)(
                        (getAreaBuffer[s] + 1 - A)*(getAreaBuffer[s] + 1 - A)
                        -(getAreaBuffer[s] - A)*(getAreaBuffer[s] - A)
                    );
                }
                if(getSigmaBuffer[i + j*DIM]!=N-1)
                {
                    dH += lambda*(float)(
                        +(getAreaBuffer[getSigmaBuffer[i + j*DIM]] - 1 - A)*(getAreaBuffer[getSigmaBuffer[i + j*DIM]] - 1 - A)
                        -(getAreaBuffer[getSigmaBuffer[i + j*DIM]] - A)*(getAreaBuffer[getSigmaBuffer[i + j*DIM]] - A)
                    );
                }
                
                
            }

            float r = Random.Range(0f,1f);

            if(T!=0)
            {
                if(Mathf.Exp(-dH/T)>=r)
                {
                    getAreaBuffer[s]++;
                    getAreaBuffer[getSigmaBuffer[i + j*DIM]]--;
                    getSigmaBuffer[i + j*DIM] = s;
                }
            }
            else
            {
                if(dH<0){
                    getAreaBuffer[s]++;
                    getAreaBuffer[getSigmaBuffer[i + j*DIM]]--;
                    getSigmaBuffer[i + j*DIM] = s;
                }
                if(dH==0 && r <= 0.5){
                    getAreaBuffer[s]++;
                    getAreaBuffer[getSigmaBuffer[i + j*DIM]]--;
                    getSigmaBuffer[i + j*DIM] = s;
                }
            }
            float c = 1;
            if(plotMode==PlotMode.Energy)
            {
                c = 4f;
                if(getSigmaBuffer[i + j*DIM]!=nearby[0]) c -= 1f;
                if(getSigmaBuffer[i + j*DIM]!=nearby[1]) c -= 1f;
                if(getSigmaBuffer[i + j*DIM]!=nearby[2]) c -= 1f;
                if(getSigmaBuffer[i + j*DIM]!=nearby[3]) c -= 1f;
                c/=4f;
            }
            else if(plotMode==PlotMode.CellType)
            {
                c = (float)(getSigmaBuffer[i + j*DIM])/(float)(N-1);
            }
            else
            {
                c = (float)(dlmBuffer[getSigmaBuffer[i + j*DIM]])/2f;
            }
            plotPixels[i + j*DIM] = new Color(c,c,c,1);
            }}
        }
        plotTexture.SetPixels(plotPixels);
        plotTexture.Apply();
    }

    // void Update()
    // {
    //     if(setA != A)
    //     {
    //         compute.SetFloat("A",A);
    //         setA = A;
    //     }
    //     if(setLambda!=lambda)
    //     {
    //         compute.SetFloat("lambda",lambda);
    //         setLambda = lambda;
    //     }
    //     if(setPlotMode!=plotMode)
    //     {
    //         compute.SetInt("plotMode",(int)plotMode);
    //         setPlotMode = plotMode;
    //     }
    //     if(setT!=T)
    //     {
    //         compute.SetFloat("T",T);
    //         setT = T;
    //     }
    //     if(getArea)
    //     {
    //         getArea = false;
    //         area.GetData(getAreaBuffer);
    //         int maxArea = 0;
    //         int minArea = (int)(DIM*DIM);
    //         int aveArea = 0;
    //         for (int i = 0; i < getAreaBuffer.Length; i++)
    //         {
    //             maxArea = (int)Mathf.Max(maxArea,getAreaBuffer[i]);
    //             minArea = (int)Mathf.Min(minArea,getAreaBuffer[i]);
    //             aveArea += getAreaBuffer[i];
    //         }
    //         print(maxArea);
    //         print(minArea);
    //         print(aveArea);
    //         print(aveArea/getAreaBuffer.Length);
    //     }
    //     for (int kk = 0; kk < loopCount; kk++)
    //     {
    //         compute.SetInt("offset",(int)Random.Range(0,int.MaxValue));
    //         compute.SetInt("offset1",(int)Random.Range(0,int.MaxValue));
    //         compute.Dispatch(stepkernel,(DIM+7)/8,(DIM+7)/8,1);
    //     }
    //     compute.Dispatch(plotkernel,(DIM+7)/8,(DIM+7)/8,1);
    //     RenderTexture.active = renderTexture;
    //     plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
    //     plotTexture.Apply();
    // }
}
