using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RodParticle : MonoBehaviour
{
    public static SimNemMan simulationManager;
    public int chunkIndexX = -1;
    public int chunkIndexY = -1;
    public float length;
    public Collider2D collider2d;
    Vector3 prevFreePos;
    Quaternion prevFreeRot;
    public void Move()
    {
        float theta = Random.Range(-0.5f,0.5f) * Mathf.PI;
        transform.position += new Vector3(Mathf.Cos(theta),Mathf.Sin(theta),0) * GaussianNumberGenerator() * simulationManager.particleSpeed * Time.deltaTime;
        transform.position = simulationManager.AdjustPosition(transform.position);
        theta = 360f* GaussianNumberGenerator()*simulationManager.particleRotateSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(0,0,theta + transform.rotation.eulerAngles.z);

        Collider2D coll = GetComponent<Collider2D>();
        coll.transform.position = transform.position;
        coll.transform.rotation = transform.rotation;
        ContactFilter2D filter = new ContactFilter2D().NoFilter();
        List<Collider2D> results = new List<Collider2D>();
        if (coll.OverlapCollider(filter,results)>0) {
            transform.position = prevFreePos;
            transform.rotation = prevFreeRot;
        }
        else{
            prevFreePos = transform.position;
            prevFreeRot = transform.rotation;
        }
    }
    private void Start() {
        length = simulationManager.RodLength * transform.localScale.y;
        collider2d = GetComponent<Collider2D>();

        prevFreePos = transform.position;
        prevFreeRot = transform.rotation;

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
    }
    void Update()
    {
        float theta = Random.Range(-0.5f,0.5f) * Mathf.PI;

        Vector3 newPosition = transform.position;

        newPosition += new Vector3(Mathf.Cos(theta),Mathf.Sin(theta),0) * GaussianNumberGenerator() * simulationManager.particleSpeed * Time.deltaTime;
        newPosition = simulationManager.AdjustPosition(newPosition);

        Quaternion newRotation = transform.rotation;
        theta = 360f* GaussianNumberGenerator()*simulationManager.particleRotateSpeed * Time.deltaTime;    
        newRotation = Quaternion.Euler(0,0,theta + newRotation.eulerAngles.z);

        // float newRotationtheta = (newRotation.eulerAngles.z*Mathf.PI)/180f;
        // newPosition += new Vector3(-Mathf.Sin(newRotationtheta),Mathf.Cos(newRotationtheta),0) * simulationManager.particleSpeed * Time.deltaTime;
        // newPosition = simulationManager.AdjustPosition(newPosition);

        float newposX = newPosition.x - simulationManager.BG.transform.position.x + simulationManager.BG.transform.localScale.x/2;
        float newposY = newPosition.y - simulationManager.BG.transform.position.y + simulationManager.BG.transform.localScale.y/2;
        int newChunkIndexX = (int)((newposX*simulationManager.chunkResolutionX)/simulationManager.BG.transform.localScale.x);
        int newChunkIndexY = (int)((newposY*simulationManager.chunkResolutionY)/simulationManager.BG.transform.localScale.y);
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                foreach (RodParticle particle in 
                simulationManager.chunks[
                    (newChunkIndexX+i+simulationManager.chunkResolutionX)%simulationManager.chunkResolutionX,
                    (newChunkIndexY+j+simulationManager.chunkResolutionY)%simulationManager.chunkResolutionY])
                {
                    if(particle==this) continue;
                    float othertheta = ((particle.transform.rotation.eulerAngles.z%180f)*Mathf.PI)/180f;
                    float thistheta = ((newRotation.eulerAngles.z%180f)*Mathf.PI)/180f;
                    if(othertheta==thistheta)continue;
                    Vector2 parraleltoother = new Vector2(-Mathf.Sin(othertheta),Mathf.Cos(othertheta));
                    Vector2 parraleltothis = new Vector2(-Mathf.Sin(thistheta),Mathf.Cos(thistheta));
                    Vector2 edgepoint1 = new Vector2(particle.transform.position.x,particle.transform.position.y) + particle.length/2 * parraleltoother;
                    Vector2 edgepoint2 = new Vector2(particle.transform.position.x,particle.transform.position.y) - particle.length/2 * parraleltoother;
                    Vector2 edgepoint3 = edgepoint1 + particle.length/2 * parraleltothis;
                    Vector2 edgepoint4 = edgepoint1 - particle.length/2 * parraleltothis;
                    Line line1 = new Line(Mathf.Tan(thistheta-Mathf.PI/2),edgepoint1.y-Mathf.Tan(thistheta-Mathf.PI/2)*edgepoint1.x);
                    Line line2 = new Line(Mathf.Tan(thistheta-Mathf.PI/2),edgepoint2.y-Mathf.Tan(thistheta-Mathf.PI/2)*edgepoint2.x);
                    Line line3 = new Line(Mathf.Tan(othertheta-Mathf.PI/2),edgepoint3.y-Mathf.Tan(othertheta-Mathf.PI/2)*edgepoint3.x);
                    Line line4 = new Line(Mathf.Tan(othertheta-Mathf.PI/2),edgepoint4.y-Mathf.Tan(othertheta-Mathf.PI/2)*edgepoint4.x);

                    float zure1 = newPosition.x*line1.katamuki + line1.seppen - newPosition.y;
                    float zure2 = newPosition.x*line2.katamuki + line2.seppen - newPosition.y;
                    float zure3 = newPosition.x*line3.katamuki + line3.seppen - newPosition.y;
                    float zure4 = newPosition.x*line4.katamuki + line4.seppen - newPosition.y;
                    if(zure1*zure2 < 0 && zure3*zure4 < 0){
                        return;
                    }
                }
            }
        }
        transform.position = newPosition;
        transform.rotation = newRotation;
        float posX = transform.position.x - simulationManager.BG.transform.position.x + simulationManager.BG.transform.localScale.x/2;
        float posY = transform.position.y - simulationManager.BG.transform.position.y + simulationManager.BG.transform.localScale.y/2;

        newChunkIndexX = (int)((posX*simulationManager.chunkResolutionX)/simulationManager.BG.transform.localScale.x);
        newChunkIndexY = (int)((posY*simulationManager.chunkResolutionY)/simulationManager.BG.transform.localScale.y);
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
    }
    
    float GaussianNumberGenerator()
    {
        return Mathf.Sqrt((-2f*Mathf.Log(Random.Range(float.Epsilon,1f))))*Mathf.Cos(2f*Mathf.PI*Random.Range(0f,1f));
    }
    public void DestroyParticle()
    {
        if(chunkIndexX!=-1&&chunkIndexY!=-1)simulationManager.chunks[chunkIndexX,chunkIndexY].Remove(this);
        Destroy(gameObject);
    }
}