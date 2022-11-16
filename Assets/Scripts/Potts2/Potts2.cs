using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Potts2 : MonoBehaviour
{
    public Image plotImage;
    public int DIM;
    public int A;
    public int L;
    public float T,lambda,gamma;
    public int N;
    public int cellTypeCount = 1;
    public int loopCount;
    public float cellCellJ;
    public float mediumJ;
    Texture2D plotTexture;
    int[] cellIndices;
    int[] cellTypes;
    int[] cellAreas;
    int[] cellPerimeters;
    Color[] plotPixels;
    ColorHeatMap colorMap;
    Dictionary<int, int> perimeterChangeMap = new Dictionary<int, int>();
    void Start()
    {
        colorMap = new ColorHeatMap();
        plotTexture = new Texture2D(DIM,DIM);
        plotPixels = new Color[DIM*DIM];
        plotTexture.filterMode = FilterMode.Point;
        plotImage.sprite = Sprite.Create(plotTexture, new Rect(0,0,DIM,DIM),Vector2.zero);
        cellIndices = new int[DIM*DIM];
        cellTypes = new int[N];
        cellAreas = new int[N];
        cellPerimeters = new int[N];
        for (int i = 0; i < N; i++)
        {
            cellAreas[i] = 0;
            cellPerimeters[i] = 0;
            if(i!=N-1)cellTypes[i] = Random.Range(0,cellTypeCount);
            else cellTypes[i] = cellTypeCount;
        }
        for (int i = 0; i < DIM*DIM; i++)
        {
            cellIndices[i] = Random.Range(0,N);
            cellAreas[cellIndices[i]]++;
            float c = (float)(cellIndices[i])/(float)(N-1);
            plotPixels[i] = colorMap.GetColorForValue(1-c,1);
        }

        for (int i = 0; i < DIM; i++)
        {
            for (int j = 0; j < DIM; j++)
            {
                int[] nearby = new int[4]{
                    cellIndices[(i+1)%DIM + j*DIM],
                    cellIndices[(i-1+DIM)%DIM + j*DIM],
                    cellIndices[i + ((j+1)%DIM)*DIM],
                    cellIndices[i + ((j-1+DIM)%DIM)*DIM]
                };
                for (int l = 0; l < 4; l++)
                {
                    if(cellIndices[i + j*DIM] != nearby[l])
                    {
                        cellPerimeters[cellIndices[i]]++;
                        break;
                    }
                }
            }
        }
        
        plotTexture.SetPixels(plotPixels);
        plotTexture.Apply();
    }

    // Update is called once per frame
    void Update()
    {
        for (int kk = 0; kk < loopCount; kk++)
        {
            RandomStep();
        }
        plotTexture.SetPixels(plotPixels);
        plotTexture.Apply();
    }
    void RandomStep()
    {
        for (int kk = 0; kk < DIM*DIM; kk++)
        {
            int i = Random.Range(0,DIM);
            int j = Random.Range(0,DIM);
            int[] nearby = new int[4]{
                cellIndices[(i+1)%DIM + j*DIM],
                cellIndices[(i-1+DIM)%DIM + j*DIM],
                cellIndices[i + ((j+1)%DIM)*DIM],
                cellIndices[i + ((j-1+DIM)%DIM)*DIM]
            };
            
            int[] nearbyTypes = new int[4]{
                cellTypes[nearby[0]],
                cellTypes[nearby[1]],
                cellTypes[nearby[2]],
                cellTypes[nearby[3]]
            };

            if(i+1>=DIM)
            {
                nearby[0] = N-1;
                nearbyTypes[0] = cellTypeCount;
            }
            if(i-1<0)
            {
                nearby[1] = N-1;
                nearbyTypes[1] = cellTypeCount;
            }
            if(j+1>=DIM)
            {
                nearby[2] = N-1;
                nearbyTypes[2] = cellTypeCount;
            }
            if(j-1<0)
            {
                nearby[3] = N-1;
                nearbyTypes[3] = cellTypeCount;
            }

            int index = Random.Range(0,4);
            int s = nearby[index];
            int t = nearbyTypes[index];

            float dH = 0;

            float[] nearbyJbefore = new float[4]{0,0,0,0};
            float[] nearbyJafter = new float[4]{0,0,0,0};
            
            for (int l = 0; l < 4; l++)
            {
                if(cellTypes[cellIndices[i + j*DIM]]==cellTypeCount || nearbyTypes[l]==cellTypeCount)
                {
                    nearbyJbefore[l] = mediumJ;
                }
                else nearbyJbefore[l] = cellCellJ;

                if(t==cellTypeCount || nearbyTypes[l]==cellTypeCount)
                {
                    nearbyJafter[l] = mediumJ;
                }
                else nearbyJafter[l] = cellCellJ;

                if(s!=nearby[l]) dH += nearbyJafter[l];
                if(cellIndices[i + j*DIM]!=nearby[l]) dH -= nearbyJbefore[l];
            }

            if(s!=cellIndices[i + j*DIM])
            {
                if(s!=N-1)
                {
                    dH += lambda*(float)(
                        (cellAreas[s] + 1 - A)*(cellAreas[s] + 1 - A)
                        -(cellAreas[s] - A)*(cellAreas[s] - A)
                    );
                }
                if(cellIndices[i + j*DIM]!=N-1)
                {
                    dH += lambda*(float)(
                        +(cellAreas[cellIndices[i + j*DIM]] - 1 - A)*(cellAreas[cellIndices[i + j*DIM]] - 1 - A)
                        -(cellAreas[cellIndices[i + j*DIM]] - A)*(cellAreas[cellIndices[i + j*DIM]] - A)
                    );
                }
            }

            // int[] isPerimeterBefore = new int[5]{0,0,0,0,0};
            // int[] isPerimeterAfter = new int[5]{0,0,0,0,0};

            // int[] nearestSiteIs = new int[5]{0, 0, 0, 1,-1};
            // int[] nearestSiteJs = new int[5]{0, 1,-1, 0, 0};

            // for (int l = 0; l < 5; l++)
            // {
            //     int siteIndex = (i+nearestSiteIs[l]+DIM)%DIM + ((j+nearestSiteJs[l]+DIM)%DIM)*DIM;
            //     if(cellIndices[siteIndex] == N-1) continue;
            //     for (int m = 0; m < 4; m++)
            //     {
            //         int nearSiteIndex = (i+nearestSiteIs[l] + nearestSiteIs[m+1] +DIM)%DIM + ((j+nearestSiteJs[l] + nearestSiteJs[m+1] +DIM)%DIM)*DIM;
            //         if(cellIndices[siteIndex]!=cellIndices[nearSiteIndex])
            //         {
            //             isPerimeterBefore[l] = 1;
            //             if(siteIndex != i + j*DIM && nearSiteIndex != i + j*DIM)
            //             {
            //                 isPerimeterAfter[l] = 1;
            //             }
            //         }

            //         if(nearSiteIndex == i + j*DIM)
            //         {
            //             if(cellIndices[siteIndex]!=s) isPerimeterAfter[l] = 1;
            //         }

            //         if(siteIndex == i + j*DIM)
            //         {
            //             if(cellIndices[nearSiteIndex]!=s) isPerimeterAfter[l] = 1;
            //         }
            //     }

            //     if(l == 0)
            //     {
            //         if(perimeterChangeMap.ContainsKey(cellIndices[siteIndex]))
            //         {
            //             perimeterChangeMap[cellIndices[siteIndex]] += -isPerimeterBefore[l];
            //         }
            //         else
            //         {
            //             perimeterChangeMap.Add(cellIndices[siteIndex],-isPerimeterBefore[l]);
            //         }
            //         if(perimeterChangeMap.ContainsKey(s))
            //         {
            //             perimeterChangeMap[s] += isPerimeterAfter[l];
            //         }
            //         else
            //         {
            //             perimeterChangeMap.Add(s,isPerimeterAfter[l]);
            //         }
            //     }
            //     else
            //     {
            //         if(perimeterChangeMap.ContainsKey(cellIndices[siteIndex]))
            //         {
            //             perimeterChangeMap[cellIndices[siteIndex]] += isPerimeterAfter[l] - isPerimeterBefore[l];
            //         }
            //         else
            //         {
            //             perimeterChangeMap.Add(cellIndices[siteIndex],isPerimeterAfter[l] - isPerimeterBefore[l]);
            //         }
            //     }
            // }

            // foreach (var item in perimeterChangeMap)
            // {
            //     dH += gamma*(float)(
            //         (cellPerimeters[item.Key] + item.Value - L)*(cellPerimeters[item.Key] + item.Value - L)
            //         -(cellPerimeters[item.Key] - L)*(cellPerimeters[item.Key] - L)
            //     );
            // }

            if(s!=cellIndices[i + j*DIM])
            {
                if(s!=N-1)
                {
                    dH += lambda*(float)(
                        (cellAreas[s] + 1 - A)*(cellAreas[s] + 1 - A)
                        -(cellAreas[s] - A)*(cellAreas[s] - A)
                    );
                }
                if(cellIndices[i + j*DIM]!=N-1)
                {
                    dH += lambda*(float)(
                        +(cellAreas[cellIndices[i + j*DIM]] - 1 - A)*(cellAreas[cellIndices[i + j*DIM]] - 1 - A)
                        -(cellAreas[cellIndices[i + j*DIM]] - A)*(cellAreas[cellIndices[i + j*DIM]] - A)
                    );
                }
            }

            if(ShouldChange(dH))
            {
                cellAreas[s]++;
                cellAreas[cellIndices[i + j*DIM]]--;
                cellIndices[i + j*DIM] = s;

                // foreach (var item in perimeterChangeMap)
                // {
                //     cellPerimeters[item.Key] += item.Value;
                // }
            }
            // float c = 1;
            // if(plotMode==PlotMode.Energy)
            // {
            //     c = 4f;
            //     if(cellIndices[i + j*DIM]!=nearby[0]) c -= 1f;
            //     if(cellIndices[i + j*DIM]!=nearby[1]) c -= 1f;
            //     if(cellIndices[i + j*DIM]!=nearby[2]) c -= 1f;
            //     if(cellIndices[i + j*DIM]!=nearby[3]) c -= 1f;
            //     c/=4f;
            // }
            // else if(plotMode==PlotMode.CellType)
            // {
            //     c = (float)(cellIndices[i + j*DIM])/(float)(N-1);
            // }
            // else
            // {
            //     c = (float)(cellTypes[cellIndices[i + j*DIM]])/2f;
            // }
            float c = (float)(cellIndices[i + j*DIM])/(float)(N-1);
            plotPixels[i + j*DIM] = colorMap.GetColorForValue(1-c,1);

            // perimeterChangeMap.Clear();
        }
    }

    bool ShouldChange(float dH)
    {
        float r = Random.Range(0f,1f);

        if(T!=0)
        {
            if(Mathf.Exp(-dH/T)>=r) return true;
        }
        else
        {
            if(dH<0) return true;
            if(dH==0 && r <= 0.5) return true;
        }

        return false;
    }
}
