using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimNemMan : MonoBehaviour
{
    public GameObject RodParticlePrefab,BG;
    public Transform ParticleParent; 
    public float RodLength = 20f;
    public float particleSpeed;
    public float particleRotateSpeed;
    public int SpawnCount;
    public int SpawnedCount = 0;
    public int chunkResolutionX;
    public int chunkResolutionY;
    public int attemptlimit = 10000;
    public HashSet<RodParticle>[,] chunks;
    float C,S;
    public float avTheta = 0;
    public float chitujo = 0;
    private void Awake() {
        C = S  =0;
        RodParticle.simulationManager = this;
    }
    private void Start() {
        float diameter = RodParticlePrefab.transform.localScale.y*RodLength;
        chunkResolutionX = (int)(BG.transform.localScale.x/diameter);
        chunkResolutionY = (int)(BG.transform.localScale.y/diameter);
        chunks = new HashSet<RodParticle>[chunkResolutionX,chunkResolutionY];
        for (int i = 0; i < chunkResolutionX; i++)
        {
            for (int j = 0; j < chunkResolutionY; j++)
            {
                chunks[i,j] = new HashSet<RodParticle>();
            }
        }
        int spawnedCount = 0;
        while(spawnedCount<SpawnCount)
        {
            SpawnParticleAtRandom(0);
            spawnedCount++;
        }
        print(C/SpawnedCount);
        print(S/SpawnedCount);
        avTheta /= SpawnedCount;
        foreach (Transform child in ParticleParent)
        {
            float particleTheta = ((child.rotation.eulerAngles.z%180f)*Mathf.PI)/180f;
            chitujo += (3*Mathf.Cos(particleTheta)*Mathf.Cos(particleTheta)-1)/2;
        }
        chitujo /= ParticleParent.childCount;
    }
    public void MoveParticles()
    {
        // SpawnParticleAtRandom(0);
        SpawnParticleFast();
        avTheta = 0;
        foreach (Transform child in ParticleParent)
        {
            avTheta += child.transform.rotation.eulerAngles.z%180f;
        }
        avTheta /=  ParticleParent.childCount;
        foreach (Transform child in ParticleParent)
        {
            float particleTheta = ((child.rotation.eulerAngles.z%180f)*Mathf.PI)/180f - avTheta;
            chitujo += (3*Mathf.Cos(particleTheta)*Mathf.Cos(particleTheta)-1)/2;
        }
        chitujo /= ParticleParent.childCount;
    }
    public void SpawnParticleFast()
    {
        for (int m = 0; m < 10000; m++)
        {
            Vector3 spawnPos = new Vector3(
                Random.Range(-BG.transform.localScale.x/2,BG.transform.localScale.x/2),
                Random.Range(-BG.transform.localScale.y/2,BG.transform.localScale.y/2),0) + BG.transform.position;
            GameObject newParticle = Instantiate(RodParticlePrefab,spawnPos,Quaternion.Euler(0,0,Random.Range(0f,360f)),ParticleParent);
            RodParticle newParticleScript = newParticle.GetComponent<RodParticle>();
            Vector3 newPosition = spawnPos;
            Quaternion newRotation = newParticle.transform.rotation;
            float newposX = newPosition.x - BG.transform.position.x + BG.transform.localScale.x/2;
            float newposY = newPosition.y - BG.transform.position.y + BG.transform.localScale.y/2;
            int newChunkIndexX = (int)((newposX*chunkResolutionX)/BG.transform.localScale.x);
            int newChunkIndexY = (int)((newposY*chunkResolutionY)/BG.transform.localScale.y);
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    foreach (RodParticle particle in 
                    chunks[
                        (newChunkIndexX+i+chunkResolutionX)%chunkResolutionX,
                        (newChunkIndexY+j+chunkResolutionY)%chunkResolutionY])
                    {
                        if(particle==newParticleScript) continue;
                        float othertheta = ((particle.transform.rotation.eulerAngles.z%180f)*Mathf.PI)/180f;
                        float thistheta = ((newRotation.eulerAngles.z%180f)*Mathf.PI)/180f;
                        if(othertheta==thistheta)continue;
                        Vector2 parraleltoother = new Vector2(-Mathf.Sin(othertheta),Mathf.Cos(othertheta));
                        Vector2 parraleltothis = new Vector2(-Mathf.Sin(thistheta),Mathf.Cos(thistheta));
                        float diameter = RodParticlePrefab.transform.localScale.y*RodLength;
                        Vector2 edgepoint1 = new Vector2(particle.transform.position.x,particle.transform.position.y) + diameter/2 * parraleltoother;
                        Vector2 edgepoint2 = new Vector2(particle.transform.position.x,particle.transform.position.y) - diameter/2 * parraleltoother;
                        Vector2 edgepoint3 = edgepoint1 + diameter/2 * parraleltothis;
                        Vector2 edgepoint4 = edgepoint1 - diameter/2 * parraleltothis;
                        Line line1 = new Line(Mathf.Tan(thistheta-Mathf.PI/2),edgepoint1.y-Mathf.Tan(thistheta-Mathf.PI/2)*edgepoint1.x);
                        Line line2 = new Line(Mathf.Tan(thistheta-Mathf.PI/2),edgepoint2.y-Mathf.Tan(thistheta-Mathf.PI/2)*edgepoint2.x);
                        Line line3 = new Line(Mathf.Tan(othertheta-Mathf.PI/2),edgepoint3.y-Mathf.Tan(othertheta-Mathf.PI/2)*edgepoint3.x);
                        Line line4 = new Line(Mathf.Tan(othertheta-Mathf.PI/2),edgepoint4.y-Mathf.Tan(othertheta-Mathf.PI/2)*edgepoint4.x);

                        float zure1 = newPosition.x*line1.katamuki + line1.seppen - newPosition.y;
                        float zure2 = newPosition.x*line2.katamuki + line2.seppen - newPosition.y;
                        float zure3 = newPosition.x*line3.katamuki + line3.seppen - newPosition.y;
                        float zure4 = newPosition.x*line4.katamuki + line4.seppen - newPosition.y;
                        if(zure1*zure2 < 0 && zure3*zure4 < 0){
                            Destroy(newParticle);
                            return;
                        }
                    }
                }
            }
            SpawnedCount++;
            if(newParticleScript.chunkIndexX!=newChunkIndexX||newParticleScript.chunkIndexY!=newChunkIndexY){
                if(newParticleScript.chunkIndexX!=-1&&newParticleScript.chunkIndexY!=-1)
                {
                    if(chunks[newParticleScript.chunkIndexX,newParticleScript.chunkIndexY].Contains(newParticleScript)){
                        chunks[newParticleScript.chunkIndexX,newParticleScript.chunkIndexY].Remove(newParticleScript);
                    }
                }
                if(newChunkIndexX==chunkResolutionX) newChunkIndexX = chunkResolutionX-1;
                if(newChunkIndexY==chunkResolutionY) newChunkIndexY = chunkResolutionY-1;
                chunks[newChunkIndexX,newChunkIndexY].Add(newParticleScript);
            }
            newParticleScript.chunkIndexX = newChunkIndexX;
            newParticleScript.chunkIndexY = newChunkIndexY;
        }
    }
    public void SpawnParticleAtRandom(int attemptcount)
    {
        if(attemptcount>=attemptlimit){
            // SpawnCount--;
            return;
        }
        Vector3 spawnPos = new Vector3(
            Random.Range(-BG.transform.localScale.x/2,BG.transform.localScale.x/2),
            Random.Range(-BG.transform.localScale.y/2,BG.transform.localScale.y/2),0) + BG.transform.position;
        GameObject newParticle = Instantiate(RodParticlePrefab,spawnPos,Quaternion.Euler(0,0,Random.Range(0f,360f)),ParticleParent);
        RodParticle newParticleScript = newParticle.GetComponent<RodParticle>();
        // Collider2D coll = newParticle.GetComponent<Collider2D>();
        // ContactFilter2D filter = new ContactFilter2D().NoFilter();
        // List<Collider2D> results = new List<Collider2D>();
        // if (coll.OverlapCollider(filter,results)>0) {
        //     Destroy(newParticle);
        //     SpawnParticleAtRandom(attemptcount+1);
        // }
        // else{
        //     SpawnedCount++;
        //     avTheta +=  newParticle.transform.rotation.eulerAngles.z%180f;
        //     float newParticleTheta = ((newParticle.transform.rotation.eulerAngles.z%180f)*Mathf.PI)/180f ;
        //     C += Mathf.Cos(newParticleTheta);
        //     S += Mathf.Sin(newParticleTheta);
        // }
        Vector3 newPosition = spawnPos;
        Quaternion newRotation = newParticle.transform.rotation;
        float newposX = newPosition.x - BG.transform.position.x + BG.transform.localScale.x/2;
        float newposY = newPosition.y - BG.transform.position.y + BG.transform.localScale.y/2;
        int newChunkIndexX = (int)((newposX*chunkResolutionX)/BG.transform.localScale.x);
        int newChunkIndexY = (int)((newposY*chunkResolutionY)/BG.transform.localScale.y);
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                foreach (RodParticle particle in 
                chunks[
                    (newChunkIndexX+i+chunkResolutionX)%chunkResolutionX,
                    (newChunkIndexY+j+chunkResolutionY)%chunkResolutionY])
                {
                    if(particle==newParticleScript) continue;
                    float othertheta = ((particle.transform.rotation.eulerAngles.z%180f)*Mathf.PI)/180f;
                    float thistheta = ((newRotation.eulerAngles.z%180f)*Mathf.PI)/180f;
                    if(othertheta==thistheta)continue;
                    Vector2 parraleltoother = new Vector2(-Mathf.Sin(othertheta),Mathf.Cos(othertheta));
                    Vector2 parraleltothis = new Vector2(-Mathf.Sin(thistheta),Mathf.Cos(thistheta));
                    float diameter = RodParticlePrefab.transform.localScale.y*RodLength;
                    Vector2 edgepoint1 = new Vector2(particle.transform.position.x,particle.transform.position.y) + diameter/2 * parraleltoother;
                    Vector2 edgepoint2 = new Vector2(particle.transform.position.x,particle.transform.position.y) - diameter/2 * parraleltoother;
                    Vector2 edgepoint3 = edgepoint1 + diameter/2 * parraleltothis;
                    Vector2 edgepoint4 = edgepoint1 - diameter/2 * parraleltothis;
                    Line line1 = new Line(Mathf.Tan(thistheta-Mathf.PI/2),edgepoint1.y-Mathf.Tan(thistheta-Mathf.PI/2)*edgepoint1.x);
                    Line line2 = new Line(Mathf.Tan(thistheta-Mathf.PI/2),edgepoint2.y-Mathf.Tan(thistheta-Mathf.PI/2)*edgepoint2.x);
                    Line line3 = new Line(Mathf.Tan(othertheta-Mathf.PI/2),edgepoint3.y-Mathf.Tan(othertheta-Mathf.PI/2)*edgepoint3.x);
                    Line line4 = new Line(Mathf.Tan(othertheta-Mathf.PI/2),edgepoint4.y-Mathf.Tan(othertheta-Mathf.PI/2)*edgepoint4.x);

                    float zure1 = newPosition.x*line1.katamuki + line1.seppen - newPosition.y;
                    float zure2 = newPosition.x*line2.katamuki + line2.seppen - newPosition.y;
                    float zure3 = newPosition.x*line3.katamuki + line3.seppen - newPosition.y;
                    float zure4 = newPosition.x*line4.katamuki + line4.seppen - newPosition.y;
                    if(zure1*zure2 < 0 && zure3*zure4 < 0){
                        Destroy(newParticle);
                        SpawnParticleAtRandom(attemptcount+1);
                        return;
                    }
                }
            }
        }
        SpawnedCount++;
        if(newParticleScript.chunkIndexX!=newChunkIndexX||newParticleScript.chunkIndexY!=newChunkIndexY){
            if(newParticleScript.chunkIndexX!=-1&&newParticleScript.chunkIndexY!=-1)
            {
                if(chunks[newParticleScript.chunkIndexX,newParticleScript.chunkIndexY].Contains(newParticleScript)){
                    chunks[newParticleScript.chunkIndexX,newParticleScript.chunkIndexY].Remove(newParticleScript);
                }
            }
            if(newChunkIndexX==chunkResolutionX) newChunkIndexX = chunkResolutionX-1;
            if(newChunkIndexY==chunkResolutionY) newChunkIndexY = chunkResolutionY-1;
            chunks[newChunkIndexX,newChunkIndexY].Add(newParticleScript);
        }
        newParticleScript.chunkIndexX = newChunkIndexX;
        newParticleScript.chunkIndexY = newChunkIndexY;
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
}