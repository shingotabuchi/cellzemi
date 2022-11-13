using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour
{
    public static SimulationManager simulationManager;
    public int chunkIndexX = -1;
    public int chunkIndexY = -1;
    public ParticleType type;
    SpriteRenderer sRenderer;
    void Awake()
    {
        sRenderer = transform.GetComponent<SpriteRenderer>();
    }
    void Update()
    {
        float theta = Random.Range(-0.5f,0.5f) * Mathf.PI;
        transform.position += new Vector3(Mathf.Cos(theta),Mathf.Sin(theta),0) * GaussianNumberGenerator() * simulationManager.particleSpeed * Time.deltaTime;
        transform.position = simulationManager.AdjustPosition(transform.position);

        float posX = transform.position.x - simulationManager.BG.transform.position.x + simulationManager.BG.transform.localScale.x/2;
        float posY = transform.position.y - simulationManager.BG.transform.position.y + simulationManager.BG.transform.localScale.y/2;

        int newChunkIndexX = (int)((posX*simulationManager.chunkResolutionX)/simulationManager.BG.transform.localScale.x);
        int newChunkIndexY = (int)((posY*simulationManager.chunkResolutionY)/simulationManager.BG.transform.localScale.y);
        if(chunkIndexX!=newChunkIndexX||chunkIndexY!=newChunkIndexY){
            if(chunkIndexX!=-1&&chunkIndexY!=-1)
            {
                if(simulationManager.chunks[chunkIndexX,chunkIndexY].Contains(this)){
                    simulationManager.chunks[chunkIndexX,chunkIndexY].Remove(this);
                }
            }
            if(newChunkIndexX==simulationManager.chunkResolutionX) newChunkIndexX = simulationManager.chunkResolutionX-1;
            if(newChunkIndexY==simulationManager.chunkResolutionY) newChunkIndexY = simulationManager.chunkResolutionY-1;
            simulationManager.chunks[newChunkIndexX,newChunkIndexY].Add(this);
        }
        chunkIndexX = newChunkIndexX;
        chunkIndexY = newChunkIndexY;

        if(type==ParticleType.Y)
        {
            int nearbyXCount = 0;
            HashSet<Particle> nearbyXSet = new HashSet<Particle>();
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    foreach (Particle particle in 
                    simulationManager.chunks[
                        (chunkIndexX+i+simulationManager.chunkResolutionX)%simulationManager.chunkResolutionX,
                        (chunkIndexY+j+simulationManager.chunkResolutionY)%simulationManager.chunkResolutionY])
                    {
                        if(particle.type==ParticleType.X){
                            float distanceSqrd = (transform.position - particle.transform.position).sqrMagnitude;
                            if(distanceSqrd < transform.localScale.x*transform.localScale.x){
                                nearbyXCount++;
                                nearbyXSet.Add(particle);
                                if(nearbyXCount==simulationManager.HillCoeff) goto here;
                            }
                        }
                    }
                }
            }

            here:
            if(nearbyXCount==simulationManager.HillCoeff){
                if(Random.Range(0f,1f)<simulationManager.kOn)
                {
                    simulationManager.SpawnParticleAtPosition(ParticleType.XY,transform.position);
                    foreach (Particle particle in nearbyXSet)
                    {
                        particle.DestroyParticle();
                    }
                    DestroyParticle();
                    return;
                }
            }
        }
        if(type==ParticleType.XY)
        {
            if(Random.Range(0f,1f)<simulationManager.kOff)
            {
                for (int i = 0; i < simulationManager.HillCoeff; i++)
                {
                    simulationManager.SpawnParticleAtPosition(ParticleType.X,transform.position);
                }
                simulationManager.SpawnParticleAtPosition(ParticleType.Y,transform.position);
                DestroyParticle();
                return;
            }
        }
        if(sRenderer.color != simulationManager.TypeColor(type))sRenderer.color = simulationManager.TypeColor(type);
    }
    float GaussianNumberGenerator()
    {
        return Mathf.Sqrt((-2f*Mathf.Log(Random.Range(float.Epsilon,1f))))*Mathf.Cos(2f*Mathf.PI*Random.Range(0f,1f));
    }
    public void DestroyParticle()
    {
        if(chunkIndexX!=-1&&chunkIndexY!=-1)simulationManager.chunks[chunkIndexX,chunkIndexY].Remove(this);
        if(type==ParticleType.X||type==ParticleType.XY)
        {
            simulationManager.ParticleXSet.Remove(this);
        }
        else if(type==ParticleType.Y) simulationManager.YCount--;
        Destroy(gameObject);
    }
}