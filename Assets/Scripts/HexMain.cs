using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HexMain : MonoBehaviour
{
    public Tilemap tileMap;
    public Sprite tileSprite;
    public int DIM_X;
    public int DIM_Y;
    public int loopCount;
    public float K;
    public float ky,kr,gammay,gammar;
    public float n;
    public float dt = 0.01f;
    public bool Rmode = true;
    public bool far = false;
    int N;
    float[] Y;
    float[] R;
    float[] Ytmp;
    float[] Rtmp;
    public float miny;
    public float minr;
    public float maxy;
    public float maxr;
    int[,,] nearpos = new int[2,18,2]
    {
        {
            {1, 0},
            {1, 1},
            {0, 1},
            {-1, 0},
            {0,-1},
            {1,-1},

            { 2, 0},
            {-2, 0},
            {-1, 1},
            { 2, 1},
            { 0, 2},
            {-1, 2},
            { 1, 2},
            {-1, -1},
            { 2, -1},
            { 0,-2},
            {-1,-2},
            {-1,-2}
        },
        {
            { 1, 0},
            { 0, 1},
            {-1, 1},
            {-1, 0},
            {-1,-1},
            { 0,-1},

            { 2, 0},
            { -2, 0},
            { -2, 1},
            { 1, 1},
            { 0, 2},
            {-1, 2},
            { 1, 2},
            {-2, -1},
            { 1, -1},
            { 0,-2},
            {-1,-2},
            {-1,-2}
        }
    };
    Tile[] tiles;
    // Start is called before the first frame update
    void Start()
    {
        N = DIM_X * DIM_X;
        DIM_Y = DIM_X;
        tileMap.transform.localScale = (new Vector3(1f,1f,1f))*(10f/DIM_X);
        Y = new float[N];
        R = new float[N];
        tiles = new Tile[N];
        for (int i = 0; i < DIM_X; i++)
        {
            for (int j = 0; j < DIM_Y; j++)
            {
                Y[i + j*DIM_X] = 0.5f + Random.Range(-0.5f,0.5f); 
                R[i + j*DIM_X] = 0.5f + Random.Range(-0.5f,0.5f); 
                tiles[i + j*DIM_X] = ScriptableObject.CreateInstance<Tile>();
                tiles[i + j*DIM_X].sprite = tileSprite;
                tileMap.SetTile(new Vector3Int(DIM_X-1-i, j, 0), tiles[i + j*DIM_X]);
            }
        }
        tileMap.RefreshAllTiles();
    }

    // Update is called once per frame
    void Update()
    {
        Ytmp = (float[])Y.Clone();
        Rtmp = (float[])R.Clone();
        miny = Mathf.Infinity;
        minr = Mathf.Infinity;
        maxy = -Mathf.Infinity;
        maxr = -Mathf.Infinity;
        for (int i = 0; i < DIM_X; i++)
        {
            for (int j = 0; j < DIM_Y; j++)
            {
                Y[i + j*DIM_X] += (ky*K/(K+Rtmp[i + j*DIM_X]) - gammay * Ytmp[i + j*DIM_X])*dt;
                
                float nearYAve = 0f;
                if(!far)
                {
                    for (int k = 0; k < 6; k++)
                    {
                        int ii = (i + nearpos[j%2,k,0] + DIM_X)%DIM_X;
                        int ji = (j + nearpos[j%2,k,1] + DIM_Y)%DIM_Y;
                        nearYAve += Ytmp[ii + ji*DIM_X];
                    }
                    nearYAve /= 6f;
                }
                else
                {
                    for (int k = 0; k < 18; k++)
                    {
                        int ii = (i + nearpos[j%2,k,0] + DIM_X)%DIM_X;
                        int ji = (j + nearpos[j%2,k,1] + DIM_Y)%DIM_Y;
                        nearYAve += Ytmp[ii + ji*DIM_X];
                    }
                    nearYAve /= 18f;
                }
                
                nearYAve = Mathf.Pow(nearYAve,n);
                R[i + j*DIM_X] += (kr*nearYAve/(Mathf.Pow(K,n)+nearYAve) - gammar * Rtmp[i + j*DIM_X])*dt;

                miny = Mathf.Min(miny,Y[i + j*DIM_X]);
                minr = Mathf.Min(minr,R[i + j*DIM_X]);
                maxy = Mathf.Max(maxy,Y[i + j*DIM_X]);
                maxr = Mathf.Max(maxr,R[i + j*DIM_X]);
            }
        }
        for (int i = 0; i < DIM_X; i++)
        {
            for (int j = 0; j < DIM_Y; j++)
            {
                if(Rmode)
                {
                    float c = (R[i + j*DIM_X]-minr)/(maxr-minr);
                    tiles[i + j*DIM_X].color = new Color(c,c,c,1);
                }
                else
                {
                    float c = (Y[i + j*DIM_X]-miny)/(maxy-miny);
                    tiles[i + j*DIM_X].color = new Color(c,c,c,1);
                }
            }
        }
        tileMap.RefreshAllTiles();
    }
}
