using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum ParticleType
{  
    X,
    Y,
    XY,
}
public class SimulationManager : MonoBehaviour
{
    public GameObject ParticlePrefab,BG;
    public Color ColorX,ColorY,ColorXY;
    public int SpawnCountX,SpawnCountY;
    public int HillCoeff;
    public Transform ParticleParent; 
    public HashSet<Particle>[,] chunks;
    public HashSet<Particle> ParticleXSet = new HashSet<Particle>();
    public float particleSpeed;
    public float kOn,kOff;
    public int chunkResolutionX;
    public int chunkResolutionY;
    public int XtCount;
    public int YtCount;
    public int YCount = 0;
    public Slider XtSlider;
    public LineRenderer GraphLine;
    Vector3[] GraphValues = new Vector3[11];
    public Text XtText;
    void Start()
    {
        for (int i = 0; i < 11; i++)
        {
            GraphValues[i] = new Vector3((BG.transform.localScale.x*i)/10f,0,0);
        }
        XtCount = SpawnCountX;
        YtCount = SpawnCountY;
        Particle.simulationManager = this;
        float diameter = ParticlePrefab.transform.localScale.x;
        chunkResolutionX = (int)(BG.transform.localScale.x/diameter);
        chunkResolutionY = (int)(BG.transform.localScale.y/diameter);
        chunks = new HashSet<Particle>[chunkResolutionX,chunkResolutionY];
        for (int i = 0; i < chunkResolutionX; i++)
        {
            for (int j = 0; j < chunkResolutionY; j++)
            {
                chunks[i,j] = new HashSet<Particle>();
            }
        }
        int spawnedCount = 0;
        while(spawnedCount<SpawnCountX)
        {
            SpawnParticleAtRandom(ParticleType.X);
            spawnedCount++;
        }
        spawnedCount = 0;
        while(spawnedCount<SpawnCountY)
        {
            SpawnParticleAtRandom(ParticleType.Y);
            spawnedCount++;
        }
    }
    void Update()
    {
        GraphValues[(int)XtSlider.value] = new Vector3(GraphValues[(int)XtSlider.value].x,(float)(BG.transform.localScale.y*(YtCount-YCount))/(float)YtCount,0);
        GraphLine.SetPositions(GraphValues);
        XtText.text = "XT = " + XtCount.ToString();
    }
    public Vector3 AdjustPosition(Vector3 position)
    {
        float[] adjustments = new float[2];
        if(position.x - BG.transform.position.x>=BG.transform.localScale.x/2){
            adjustments[0] += -((int)((position.x - BG.transform.position.x - BG.transform.localScale.x/2))/BG.transform.localScale.x)*BG.transform.localScale.x-BG.transform.localScale.x;
        }
        else if(position.x - BG.transform.position.x<-BG.transform.localScale.x/2){
            adjustments[0] += ((int)((-1f)*(position.x - BG.transform.position.x + BG.transform.localScale.x/2))/BG.transform.localScale.x)*BG.transform.localScale.x+BG.transform.localScale.x;
        }
        if(position.y - BG.transform.position.y>=BG.transform.localScale.x/2){
            adjustments[1] += -((int)((position.y - BG.transform.position.y - BG.transform.localScale.x/2))/BG.transform.localScale.x)*BG.transform.localScale.x-BG.transform.localScale.x;
        }
        else if(position.y - BG.transform.position.y<-BG.transform.localScale.x/2){
            adjustments[1] += ((int)((-1f)*(position.y - BG.transform.position.y + BG.transform.localScale.x/2))/BG.transform.localScale.x)*BG.transform.localScale.x+BG.transform.localScale.x;
        }
        position += new Vector3(adjustments[0],adjustments[1],0);
        return position;
    }
    public Color TypeColor(ParticleType type)
    {
        switch (type)
        {
            case ParticleType.X:
                return ColorX;
            case ParticleType.Y:
                return ColorY;
            case ParticleType.XY:
                return ColorXY;
            default: return new Color(1,1,1,1);
        }
    }
    public void ChangeXtWithSlider()
    {  
        int SliderValue = (int)(SpawnCountX*((float)(XtSlider.value)/5f));
        if(SliderValue>XtCount)
        {
            while(XtCount<SliderValue)
            {
                SpawnParticleAtRandom(ParticleType.X);
                XtCount++;
            }
        }
        else if(SliderValue<XtCount)
        {
            while(XtCount>SliderValue)
            {
                foreach (Particle xparticle in ParticleXSet)
                {
                    if(xparticle.type==ParticleType.X)
                    {
                        xparticle.DestroyParticle();
                    }
                    else if(xparticle.type==ParticleType.XY)
                    {
                        for (int i = 0; i < HillCoeff-1; i++)
                        {
                            SpawnParticleAtPosition(ParticleType.X,xparticle.transform.position);
                        }
                        SpawnParticleAtPosition(ParticleType.Y,xparticle.transform.position);
                        xparticle.DestroyParticle();
                    }
                    XtCount--;
                    break;
                }
            }
        }
    }
    public void SpawnParticleAtRandom(ParticleType type)
    {
        Vector3 spawnPos = new Vector3(
            Random.Range(-BG.transform.localScale.x/2,BG.transform.localScale.x/2),
            Random.Range(-BG.transform.localScale.y/2,BG.transform.localScale.y/2),0) + BG.transform.position;
        SpawnParticleAtPosition(type,spawnPos);
    }
    public void SpawnParticleAtPosition(ParticleType type,Vector3 position)
    {
        GameObject newparticle = Instantiate(ParticlePrefab,position,Quaternion.identity,ParticleParent);
        newparticle.GetComponent<Particle>().type = type;
        if(type==ParticleType.X||type==ParticleType.XY)
        {
            ParticleXSet.Add(newparticle.GetComponent<Particle>());
        }
        else if(type==ParticleType.Y) YCount++;
    }
}