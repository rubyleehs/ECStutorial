using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;

public class PerlinScroller : MonoBehaviour {

    int cubeCount;
    public int width = 100;
    public int length = 100;
    public int layers = 2;

    GameObject[] cubes;
    Transform[] cubesTransforms;
    TransformAccessArray cubeTransformAccessArray;
    PositionUpdateJob cubeJob;
    JobHandle cubePositionJobHandle;
    // Use this for initialization

    private void Awake()
    {
        cubeCount = (int)(width * length * layers);
        cubes = new GameObject[cubeCount];
        cubesTransforms = new Transform[cubeCount];
    }
    void Start () {
        cubes = CreateCubes(cubeCount);

        for (int i = 0; i < cubeCount; i++)
        {
            GameObject obj = cubes[i];
            cubesTransforms[i] = obj.transform;
        }

        cubeTransformAccessArray = new TransformAccessArray(cubesTransforms);
	}

    public GameObject[] CreateCubes(int count)
    {
        cubes = new GameObject[count];
        GameObject cubeToCopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Renderer renderer = cubeToCopy.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        Collider collider = cubeToCopy.GetComponent<Collider>();
        collider.enabled = false;

        for (int i = 0; i < count; i++)
        {
            GameObject cube = Instantiate(cubeToCopy);
            int x = i / (width * length);
            int z = (i - x * width * length) / layers;
            cube.transform.position = new Vector3(x, 0, z);
            cubes[i] = cube;
        }

        Destroy(cubeToCopy);
        return cubes;
    }

    struct PositionUpdateJob : IJobParallelForTransform
    {
        public int width;
        public int length;
        public int layers;
        public int xOffset;
        public int zOffest;

        public void Execute(int i,TransformAccess transform)//be aware that all transform are independant. so canot chekc adjacent tiles.
        {
            int x = i / (width * layers);
            int z = (i - x * length * layers) / layers;
            int yOffset = i - x * width * layers - z * layers;


            //cubes[i] <- normally you do this, but in job system, they do it for multiple cubes at once so unity auto pass the values in Excute() and know which cube its refering to.
            transform.position = new Vector3(x, GeneratePerlinHeight(x + xOffset, z + zOffest) + yOffset, z + zOffest);
        }
    }


    int xOffset = 0;
	void Update () {
        cubeJob = new PositionUpdateJob()
        {
            xOffset = xOffset++,
            zOffest = (int)(this.transform.position.z - length/2f),
            length = length,
            width = width,
            layers = layers
        };

        //The Handle is simply so later on can check progress on the job
        cubePositionJobHandle = cubeJob.Schedule(cubeTransformAccessArray);
	}

    public void LateUpdate()
    {
        cubePositionJobHandle.Complete();
    }

    public void OnDestroy()
    {
        cubeTransformAccessArray.Dispose();
    }

    static float GeneratePerlinHeight(float x, float z)
    {
        float smooth = 0.03f;
        float heightMult = 5;
        float height = (Mathf.PerlinNoise(x * smooth, z * smooth * 2) * heightMult + Mathf.PerlinNoise(x * smooth, z * smooth * 2) * heightMult) * 0.5f;

        return height * 10f;
    }
}
