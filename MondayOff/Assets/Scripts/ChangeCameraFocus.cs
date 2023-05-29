using System.Collections;
using UnityEngine;
using System;

public class ChangeCameraFocus : MonoBehaviour
{
    private bool firstBallReached;
    private bool gameEnded;
    private int ballsEntered;
    [SerializeField] private int goal;
    [SerializeField] private GameObject finalFloor;

    public event Action ChangeFocusBallToBox = delegate { };
    public event Action GameEndEvent = delegate { };

    [SerializeField] private Material material;

    private void Start()
    {
        goal = Mathf.Clamp((GameManager.Instance.MaximumBallCount % 10) * 5, 10, 50);
        Debug.Log("<color=red>Goal : </color>" + goal);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;

        if (!firstBallReached)
        {
            ChangeFocusBallToBox.Invoke();
            firstBallReached = true;
        }

        ballsEntered++;
        // Debug.Log("collect!");

        if (!gameEnded && ballsEntered >= goal)
        {
            Debug.Log("game ended!");
            gameEnded = true;
            StartCoroutine(Unboxing());
        }
    }

    private void Update()
    {
        // Debug.Log(material.HasFloat("_Dissolve"));
        // for (int i = 0; i < transform.childCount; i++)
        // {

        // }
    }

    private IEnumerator Unboxing()
    {
        // float currentTime = 0f;
        // float lerpTime = 5f;
        yield return new WaitForSeconds(0.5f);

        while (material.GetFloat("_Dissolve") < 0.8f)
        {
            // currentTime += Time.deltaTime;
            // if (currentTime > lerpTime) currentTime = lerpTime;

            // material.SetFloat("_Dissolve", Mathf.Lerp(material.GetFloat("_Dissolve"), 1, currentTime / lerpTime));
            material.SetFloat("_Dissolve", Mathf.Lerp(material.GetFloat("_Dissolve"), 1, Time.deltaTime));

            // yield return new WaitForEndOfFrame();
            yield return null;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(2f);

        GameEndEvent.Invoke();

        yield break;
    }

    private void OnApplicationQuit()
    {
        material.SetFloat("_Dissolve", 0);
    }
}
