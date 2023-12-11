using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class Sand3D : MonoBehaviour
{
    public ComputeShader compute;
    public Image plotImage;
    Texture2D plotTexture;
    RenderTexture renderTexture;
    public int DIM;
    public int DIM_Z;
    public int loopCount;
    int Init, SpawnSand, Plot, Step;
    ComputeBuffer sandBuffer, particleTypeBuffer, variableBuffer;
    struct SandParticle
    {
        bool active;
        int x;
        int y;
        int z;
    };
    private void OnDestroy()
    {
        sandBuffer.Dispose();
        particleTypeBuffer.Dispose();
        variableBuffer.Dispose();
    }
    void Start()
    {
        plotTexture = new Texture2D(DIM, DIM);
        plotTexture.filterMode = FilterMode.Point;
        plotImage.sprite = Sprite.Create(plotTexture, new Rect(0, 0, DIM, DIM), Vector2.zero);
        renderTexture = new RenderTexture(DIM, DIM, 24);
        renderTexture.enableRandomWrite = true;
        compute.SetInt("DIM", DIM);
        compute.SetInt("DIM_Z", DIM_Z);

        sandBuffer = new ComputeBuffer(DIM * DIM * DIM_Z, Marshal.SizeOf(typeof(SandParticle)));
        particleTypeBuffer = new ComputeBuffer(DIM * DIM * DIM_Z, sizeof(int));
        variableBuffer = new ComputeBuffer(10, sizeof(int));

        Init = compute.FindKernel("Init");
        compute.SetBuffer(Init, "variableBuffer", variableBuffer);
        compute.SetBuffer(Init, "sandBuffer", sandBuffer);
        compute.SetBuffer(Init, "particleTypeBuffer", particleTypeBuffer);

        SpawnSand = compute.FindKernel("SpawnSand");
        compute.SetBuffer(SpawnSand, "variableBuffer", variableBuffer);
        compute.SetBuffer(SpawnSand, "sandBuffer", sandBuffer);
        compute.SetBuffer(SpawnSand, "particleTypeBuffer", particleTypeBuffer);

        Step = compute.FindKernel("Step");
        compute.SetBuffer(Step, "sandBuffer", sandBuffer);
        compute.SetBuffer(Step, "particleTypeBuffer", particleTypeBuffer);

        Plot = compute.FindKernel("Plot");
        compute.SetBuffer(Plot, "particleTypeBuffer", particleTypeBuffer);
        compute.SetTexture(Plot, "renderTexture", renderTexture);

        compute.Dispatch(Init, (DIM * DIM * DIM_Z + 63) / 64, 1, 1);
        compute.Dispatch(Plot, (DIM + 7) / 8, (DIM + 7) / 8, 1);
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            var touchPoint = MousePositionToCanvasPosition((Vector2)Input.mousePosition);
            if (PosOnCanvas(touchPoint))
            {
                int spawnX = (int)(touchPoint.x / 1080f * (float)DIM) + (Random.Range(-5, 6));
                int spawnY = (int)(touchPoint.y / 1080f * (float)DIM) + (Random.Range(-5, 6));
                int spawnZ = Random.Range(0, 2);
                if (spawnX < 0 || spawnX >= DIM || spawnY < 0 || spawnY >= DIM) return;
                compute.SetInt("spawnX", spawnX);
                compute.SetInt("spawnY", spawnY);
                compute.SetInt("spawnZ", spawnZ);
                compute.Dispatch(SpawnSand, 1, 1, 1);
            }
        }
    }
    void FixedUpdate()
    {
        for (int kk = 0; kk < loopCount; kk++)
        {
            int rndSeed = Random.Range(0, int.MaxValue);
            compute.SetInt("rndSeed", rndSeed);
            compute.Dispatch(Step, (DIM * DIM * DIM_Z + 63) / 64, 1, 1);
        }
        compute.Dispatch(Plot, (DIM + 7) / 8, (DIM + 7) / 8, 1);
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
    }
    Vector2 MousePositionToCanvasPosition(Vector2 mousePosition)
    {
        return ((Vector2)mousePosition - plotImage.GetComponent<RectTransform>().anchoredPosition);
    }
    bool PosOnCanvas(Vector2 pos)
    {
        if (pos.x < 0) return false;
        if (pos.x >= 1080) return false;
        if (pos.y < 0) return false;
        if (pos.y >= 1080) return false;
        return true;
    }
}
