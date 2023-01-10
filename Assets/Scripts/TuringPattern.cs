using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TuringPattern : MonoBehaviour
{
    public enum LapalacianWeightType
    {
        FivePoint,
        NinePoint,
        KarlSim,
    }
    public bool loadData;
    public string dataName;
    public LapalacianWeightType weightType;
    public ComputeShader compute;
    public Image plotImage;
    public int DIM;
    Texture2D plotTexture;
    RenderTexture renderTexture;
    int init,step,plot;
    public float dx,dt,DA,DB;
    [Range(0.01f,0.1f)]
    public float feed,kill;
    float setFeed,setKill;
    public int loopCount;
    ComputeBuffer A,B,laplacianWeights;
    public bool initButton;
    public bool saveButton;
    float[] fivePointWeights = new float[9]
    {
        0f, 1f, 0f,
        1f,-4f, 1f,
        0f, 1f, 0f
    };
    float[] ninePointWeights = new float[9]
    {
        0.25f,  0.5f,  0.25f,
        0.5f,  -3.0f,  0.5f,
        0.25f,  0.5f,  0.25f
    };
    float[] karlSimWeights = new float[9]
    {
        0.05f,  0.2f,  0.05f,
        0.2f,  -1.0f,  0.2f,
        0.05f,  0.2f,  0.05f
    };
    private void Start() 
    {
        plotTexture = new Texture2D(DIM,DIM);
        plotTexture.filterMode = FilterMode.Point;
        plotImage.sprite = Sprite.Create(plotTexture, new Rect(0,0,DIM,DIM),Vector2.zero);
        renderTexture = new RenderTexture(DIM,DIM,24);
        renderTexture.enableRandomWrite = true;

        A = new ComputeBuffer(DIM*DIM*2,sizeof(float));
        B = new ComputeBuffer(DIM*DIM*2,sizeof(float));
        if(loadData)
        {
            float[] savedData = new float[DIM*DIM*2];
            string[] savedDataStr = File.ReadAllLines(dataName + "A.txt");
            for (int i = 0; i < DIM*DIM*2; i++)
            {
                savedData[i] = float.Parse(savedDataStr[i]);
            }
            A.SetData(savedData);
            savedDataStr = File.ReadAllLines(dataName + "B.txt");
            for (int i = 0; i < DIM*DIM*2; i++)
            {
                savedData[i] = float.Parse(savedDataStr[i]);
            }
            B.SetData(savedData);
        }
        laplacianWeights = new ComputeBuffer(9,sizeof(float));
        if(weightType == LapalacianWeightType.FivePoint) laplacianWeights.SetData(fivePointWeights);
        if(weightType == LapalacianWeightType.NinePoint) laplacianWeights.SetData(ninePointWeights);
        if(weightType == LapalacianWeightType.KarlSim) laplacianWeights.SetData(karlSimWeights);
            

        compute.SetFloat("feed",feed);
        setFeed = feed;
        compute.SetFloat("kill",kill);
        setKill = kill;
        compute.SetFloat("DA",DA);
        compute.SetFloat("DB",DB);
        compute.SetFloat("dx",dx);
        compute.SetFloat("dt",dt);
        compute.SetInt("DIM",DIM);
        compute.SetInt("offsetA",(int)Random.Range(0,int.MaxValue));
        compute.SetInt("offsetB",(int)Random.Range(0,int.MaxValue));
        
        init = compute.FindKernel("Init");
        compute.SetBuffer(init,"A",A);
        compute.SetBuffer(init,"B",B);
        compute.SetTexture(init,"renderTexture",renderTexture);

        step = compute.FindKernel("Step");
        compute.SetBuffer(step,"A",A);
        compute.SetBuffer(step,"B",B);
        compute.SetBuffer(step,"laplacianWeights",laplacianWeights);

        plot = compute.FindKernel("Plot");
        compute.SetBuffer(plot,"A",A);
        compute.SetBuffer(plot,"B",B);
        compute.SetTexture(plot,"renderTexture",renderTexture);

        if(!loadData) compute.Dispatch(init,(DIM+7)/8,(DIM+7)/8,1);
        else compute.Dispatch(plot,(DIM+7)/8,(DIM+7)/8,1);
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }

    private void Update() 
    {
        if(initButton)
        {
            compute.Dispatch(init,(DIM+7)/8,(DIM+7)/8,1);
            initButton = false;
        }
        if(saveButton)
        {
            SaveData();
            saveButton = false;
        }
        if(setFeed != feed)
        {
            compute.SetFloat("feed",feed);
            setFeed = feed;
        }
        if(setKill != kill)
        {
            compute.SetFloat("kill",kill);
            setKill = kill;
        }
        for (int i = 0; i < loopCount; i++)
        {
            compute.Dispatch(step,(DIM+7)/8,(DIM+7)/8,1);
            compute.Dispatch(plot,(DIM+7)/8,(DIM+7)/8,1);
        }
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }

    void SaveData()
    {
        float[] saveData = new float[DIM*DIM*2];
        A.GetData(saveData);
        string metaStr = "DIM" + DIM.ToString() + "_Feed" + feed.ToString() + "_Kill" + kill.ToString(); 
        File.WriteAllLines(metaStr + "A.txt", System.Array.ConvertAll(saveData, x => x.ToString()));
        B.GetData(saveData);
        File.WriteAllLines(metaStr + "B.txt", System.Array.ConvertAll(saveData, x => x.ToString()));
    }
}
