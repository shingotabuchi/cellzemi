// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class CampKesslerLevine : MonoBehaviour
// {
//     public Image plotImage;
//     public int DIM;
//     public float initE,dt,D,gamma,a,dx,Cmin,Cmax,tau,tauar,taurr,eta,beta,alpha;
//     float dy;
//     public int loopCount;
//     public bool stateMode = false;
//     public float maxC,minC;
//     Texture2D plotTexture;
//     Color[] plotPixels;
//     float[] C,Ctmp,E;
//     int[] bion;
//     int[] state;
//     float[] time;
//     void Start()
//     {
//         dy = dx;
//         C = new float[DIM*DIM];
//         E = new float[DIM*DIM];
//         state = new int[DIM*DIM];
//         time = new float[DIM*DIM];
//         Ctmp = new float[DIM*DIM];
//         bion = new float[DIM*DIM];
//         plotTexture = new Texture2D(DIM,DIM);
//         plotTexture.filterMode = FilterMode.Point;
//         plotPixels = new Color[DIM*DIM];
//         // plotPixels = plotTexture.GetPixels();
//         for (int i = 0; i < DIM; i++)
//         {
//             for (int j = 0; j < DIM; j++)
//             {
//                 time[i + j*DIM] = 0;
//                 state[i + j*DIM] = 0;
//                 // C[i + j*DIM] = 0;
//                 C[i + j*DIM] = Random.Range(0f,1f);
//                 // E[i + j*DIM] = initE;
//                 E[i + j*DIM] = Random.Range(0f,1f);
//                 if(stateMode) plotPixels[i + j*DIM] = GrayScale(state[i + j*DIM]/2f);
//                 else plotPixels[i + j*DIM] = GrayScale(C[i + j*DIM]);
//             }
//         }
//         plotImage.sprite = Sprite.Create(plotTexture, new Rect(0,0,DIM,DIM),Vector2.zero);
//     }

//     // Update is called once per frame
//     void Update()
//     {
//         // compute.Dispatch(stepkernel,(DIM+7)/8,(DIM+7)/8,1);
//         // compute.Dispatch(copykernel,(DIM+7)/8,(DIM+7)/8,1);
//         // compute.Dispatch(minmaxkernel,1,1,1);
//         // compute.Dispatch(plotkernel,(DIM+7)/8,(DIM+7)/8,1);
//         // RenderTexture.active = renderTexture;
//         // plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
//         // plotTexture.Apply();
//         for (int kk = 0; kk < loopCount; kk++)
//         {
//             Step();
//         }
//         plotTexture.SetPixels(plotPixels);
//         plotTexture.Apply();
//     }

//     void Step()
//     {
//         float maxCtmp = -Mathf.Infinity;
//         float minCtmp = Mathf.Infinity;
//         Ctmp = (float[])C.Clone();
//         for (int i = 0; i < DIM; i++)
//         {
//             for (int j = 0; j < DIM; j++)
//             {
//                 float laplacian = (Ctmp[(i+1)%DIM + j*DIM] - 2*Ctmp[i + j*DIM] + Ctmp[(i-1 + DIM)%DIM + j*DIM])/(dx*dx)+
//                                 (Ctmp[i + ((j+1)%DIM)*DIM] - 2*Ctmp[i + j*DIM] + Ctmp[i + ((j-1 + DIM)%DIM)*DIM])/(dy*dy);
//                 C[i + j*DIM] += (D*laplacian - gamma*Ctmp[i + j*DIM])*dt;

//                 if(state[i + j*DIM] != 0)
//                 {
//                     time[i + j*DIM] += dt;
//                     float A = (taurr/(taurr-tauar))*(Cmax-Cmin);
//                     float Ct = (Cmax - A*((time[i + j*DIM] - tauar)/time[i + j*DIM]))*(1f-E[i + j*DIM]);
//                     if(state[i + j*DIM]==1)
//                     {
//                         C[i + j*DIM] += a*dt;
//                         if(time[i + j*DIM]>=tau)
//                         {
//                             state[i + j*DIM] = 2;
//                         }
//                     }
//                     else if(state[i + j*DIM]==2 && time[i + j*DIM]>tauar)
//                     {
//                         if(time[i + j*DIM]<taurr && C[i + j*DIM] > Ct)
//                         {
//                             time[i + j*DIM] = 0;
//                             state[i + j*DIM] = 1;
//                         }
//                         else if(time[i + j*DIM]>=taurr)
//                         {
//                             time[i + j*DIM] = 0;
//                             state[i + j*DIM] = 0;
//                         }
//                     }
//                 }
//                 else if(C[i + j*DIM]>=Cmin)
//                 {
//                     state[i + j*DIM] = 1;
//                 }

//                 if(E[i + j*DIM]<1) E[i + j*DIM] += (eta + beta*C[i + j*DIM]-alpha*E[i + j*DIM])*dt;
//                 else E[i + j*DIM] = 1f;

//                 maxCtmp = Mathf.Max(C[i + j*DIM],maxCtmp);
//                 minCtmp = Mathf.Min(C[i + j*DIM],minCtmp);
//                 if(stateMode) plotPixels[i + j*DIM] = GrayScale(state[i + j*DIM]/2f);
//                 else plotPixels[i + j*DIM] = GrayScale((C[i + j*DIM]-minC)/(maxC-minC));
//             }
//         }
//         maxC = maxCtmp;
//         minC = minCtmp;
//     }

//     Color GrayScale(float c)
//     {
//         return new Color(c,c,c,1);
//     }
// }
