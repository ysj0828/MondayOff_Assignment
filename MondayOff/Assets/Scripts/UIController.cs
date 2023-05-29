using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace PT
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private GameObject[] passThroughBlocks;
        [SerializeField] private GameObject[] obstacles;

        [SerializeField] private TextMeshProUGUI[] multiplierTexts;
        [SerializeField] private TextMeshProUGUI[] obstacleTexts;

        private Camera cam;

        // [SerializeField] private Vector3 offset = new Vector3(0.5f, 0f, 0f);

        // Start is called before the first frame update
        void Start()
        {
            cam = Camera.main;

            passThroughBlocks = GameObject.FindGameObjectsWithTag("PassThrough");
            obstacles = GameObject.FindGameObjectsWithTag("Obstacle");

            int i = 0;

            foreach (GameObject go in passThroughBlocks)
            {
                go.GetComponent<PassThrough>().text = multiplierTexts[i];
                multiplierTexts[i].gameObject.SetActive(true);
                // multiplierTexts[i].text = "x" + go.GetComponent<PassThrough>().Multiplier;
                // Vector3 pos = cam.WorldToScreenPoint(go.transform.position + offset);
                i++;
            }

            i = 0;

            foreach (GameObject go in obstacles)
            {
                Debug.Log("finding obstacle");
                go.GetComponent<Obstacle>().ballRequirementText = obstacleTexts[i];
                obstacleTexts[i].gameObject.SetActive(true);
                i++;
            }
        }


        void Update()
        {
            // Vector3 pos = cam.WorldToScreenPoint()
        }
    }

}
