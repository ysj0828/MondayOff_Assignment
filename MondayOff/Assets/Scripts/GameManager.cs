using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Cinemachine;

public class GameManager : MonoBehaviour
{
    #region Observer Pattern Event
    public event Action ChangeColourEvent = delegate { };
    #endregion

    #region ContainerRelated
    [Header("Container Related")]
    [SerializeField] private GameObject container;
    [SerializeField] private GameObject containerLidRefPosition;
    [SerializeField] private GameObject containerLid;
    [SerializeField] private GameObject containerInstantiateRefPosition;
    [SerializeField] private Rigidbody containerRB;

    [SerializeField] private float containerLerpTime = 0.5f;
    [SerializeField] private float lidLerpTime = 0.25f;
    private float currentTime;
    #endregion

    #region Ball Related
    [Header("Ball Related")]
    [SerializeField] private GameObject initialBallPrefab;
    [SerializeField] private GameObject ObjPoolBallPrefab;
    [SerializeField] private GameObject objPoolHolder;
    private GameObject tempBall;
    private int poolSize = 250;
    private Queue<GameObject> objectPool;
    public int MaximumBallCount
    {
        get => _maximumBallCount;
        set
        {
            _maximumBallCount = value;

            int loopCountBound = Mathf.Min((_maximumBallCount - initialBallCount - objectPool.Count), poolSize);

            // Debug.LogFormat("_maximumBallCount : {0}, initialBallCount : {1}, objectPool.Count : {2}, loopCountBound : {3}", _maximumBallCount, initialBallCount, objectPool.Count, loopCountBound);

            if (loopCountBound < poolSize)
            {
                for (int i = 0; i < loopCountBound; i++)
                {
                    GameObject obj = Instantiate(ObjPoolBallPrefab);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }
            }

        }
    }
    private int _maximumBallCount = 1;
    private int initialBallCount;

    [SerializeField] private Material ballMaterial;

    public bool IsBlueBall
    {
        get => isBlueBall;
        set
        {
            isBlueBall = value;
            switch (isBlueBall)
            {
                case true:
                    Debug.Log("change to blue");
                    ballMaterial.SetColor("_Color", Constants.BlueColour);
                    break;

                case false:
                    Debug.Log("change to orange");
                    ballMaterial.SetColor("_Color", Constants.OrangeColour);
                    break;
            }
        }
    }
    private bool isBlueBall = true;

    #endregion

    #region Camera Related
    [Header("Camera Related")]
    public Camera cam;
    private CinemachineVirtualCamera camVirtual;
    private CinemachineTransposer camTransposer;

    private Vector3 lastPosition;
    private Vector3 currentPosition;
    private Vector3 delta;
    private Vector3 mouseOffset;

    public ChangeCameraFocus changeCamFocus;
    [SerializeField] Transform collectingBox;

    #endregion

    #region Game Related

    // [System.Serializable]
    // public class Pool
    // {
    //     public string Tag;
    //     public GameObject Prefab;
    //     public int PoolSize;
    // }

    // public Dictionary<string, Queue<GameObject>> poolDictionary;

    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (!_instance)
            {
                Debug.LogError("No GameManager Instance");
                return null;
            }
            return _instance;
        }
    }

    private bool gameStarted;
    #endregion

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }

        else
        {
            Debug.LogError("Destroying this");
            Destroy(this.gameObject);
        }

        // DontDestroyOnLoad(this.gameObject);


    }

    void Start()
    {
        cam = Camera.main;
        // containerRB = container.GetComponent<Rigidbody>();
        // container = GameObject.FindGameObjectWithTag("Container");
        // containerLid = container.transform.GetChild(container.transform.childCount - 1).gameObject;
        // containerLidRefPosition = container.transform.GetChild(container.transform.childCount - 3).gameObject;
        // containerInstantiateRefPosition = container.transform.GetChild(container.transform.childCount - 2).gameObject;

        camVirtual = cam.GetComponent<CinemachineVirtualCamera>();
        camTransposer = camVirtual.GetCinemachineComponent<CinemachineTransposer>();

        changeCamFocus.ChangeFocusBallToBox += ChangeFocus;

        objectPool = new Queue<GameObject>();
        initialBallCount = Random.Range(3, 7);
        MaximumBallCount = initialBallCount;
    }

    public GameObject ObjectFromPool(string tag, Vector3 pos, Quaternion rot)
    {
        try
        {
            GameObject objectToDequeue = objectPool.Dequeue();

            objectToDequeue.SetActive(true);
            objectToDequeue.transform.position = pos;
            objectToDequeue.transform.rotation = rot;

            objectPool.Enqueue(objectToDequeue);

            return objectToDequeue;
        }
        catch (System.Exception)
        {
            Debug.LogError("Tag input is not valid");
            throw;
        }
    }

    private void OnApplicationQuit()
    {
        ballMaterial.SetColor("_Color", Constants.BlueColour);
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && !gameStarted)
        {
            currentPosition = Input.mousePosition;
            if (lastPosition == Vector3.zero) lastPosition = currentPosition;
            delta = (currentPosition - lastPosition).normalized;

            // Vector3 mouseWorldPosition = cam.ScreenToWorldPoint(Input.mousePosition);
            // Vector3 diff = mouseWorldPosition - container.transform.position;

            // container.transform.position += container.transform.forward * delta.x * 0.03f;
            containerRB.AddForce(container.transform.forward * delta.x * 0.03f, ForceMode.Impulse);

            lastPosition = currentPosition;
        }

        else if (Input.GetMouseButtonUp(0) && !gameStarted)
        {
            containerRB.constraints = RigidbodyConstraints.FreezeAll;
            gameStarted = true;
            StartCoroutine(CloseContainer());
        }

        if (Input.GetMouseButtonDown(0) && gameStarted)
        {
            ChangeBallColour();
            // ChangeColourEvent.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.LogFormat("<color=red>ini : {0},   max : {1}</color>", initialBallCount, MaximumBallCount);
            Debug.LogWarning("child count :" + objPoolHolder.transform.childCount);
        }
    }

    private void InitialiseContainerBalls()
    {
        BoxCollider containerRefPosCollider = containerInstantiateRefPosition.GetComponent<BoxCollider>();
        Vector3 minPosition = containerRefPosCollider.bounds.min;
        Vector3 maxPosition = containerRefPosCollider.bounds.max;
        for (int i = 0; i < initialBallCount; i++)
        {
            Instantiate(initialBallPrefab, new Vector3(Random.Range(minPosition.x, maxPosition.x), Random.Range(minPosition.y, maxPosition.y), Random.Range(minPosition.z, maxPosition.z)), Quaternion.identity);
        }

        Vector3 originalCamPosition = cam.transform.position;

        Transform cameraTarget = GameObject.FindGameObjectWithTag("Ball").transform;
        camTransposer.m_FollowOffset = new Vector3(0f, originalCamPosition.y - cameraTarget.position.y, 4f * (cameraTarget.position.z - originalCamPosition.z));
        camVirtual.Follow = cameraTarget;
    }


    private void ChangeFocus()
    {
        camVirtual.Priority *= -1;
    }

    private void ChangeBallColour()
    {
        IsBlueBall = IsBlueBall ? false : true;
    }

    #region On Game Start Animation
    private IEnumerator RotateContainer()
    {
        currentTime = 0f;

        while (currentTime <= containerLerpTime)
        {
            float anglePerFrame = 180 * Time.deltaTime;

            container.transform.RotateAround(containerInstantiateRefPosition.transform.position, new Vector3(1, 0, 0), anglePerFrame / containerLerpTime);
            currentTime += Time.deltaTime;

            yield return null;
        }

        yield return StartCoroutine(OpenContainer());

        yield break;
    }

    private IEnumerator CloseContainer()
    {
        currentTime = 0f;

        while (currentTime <= lidLerpTime)
        {
            float anglePerFrame = 60f * Time.deltaTime;

            containerLid.transform.RotateAround(containerLidRefPosition.transform.position, new Vector3(1, 0, 0), anglePerFrame / lidLerpTime);
            currentTime += Time.deltaTime;

            yield return null;
        }

        yield return StartCoroutine(RotateContainer());

        yield break;
    }

    private IEnumerator OpenContainer()
    {
        currentTime = 0f;

        while (currentTime <= lidLerpTime)
        {
            float anglePerFrame = 60f * Time.deltaTime;

            containerLid.transform.RotateAround(containerLidRefPosition.transform.position, new Vector3(1, 0, 0), -anglePerFrame / lidLerpTime);
            currentTime += Time.deltaTime;

            yield return null;
        }

        InitialiseContainerBalls();

        yield break;
    }

    #endregion
}