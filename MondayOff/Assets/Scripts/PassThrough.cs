using UnityEngine;
using Random = System.Random;
using TMPro;
using Cinemachine;

namespace PT
{
    public class PassThrough : MonoBehaviour
    {
        public int Multiplier
        {
            get => multiplier;
        }
        private int multiplier;
        private Transform tempBall;

        [SerializeField] private bool alternating;

        [SerializeField] private Material[] passMaterials;
        [SerializeField] private bool isBluePass = true;
        [SerializeField] private MeshRenderer meshRenderer;

        [SerializeField] private BoxCollider boxCollider;


        public TextMeshProUGUI text;
        private Vector3 offset = new Vector3(1f, 0f, 0f);

        private Camera cam;

        private bool firstBallPassed;

        private GameManager GMInstance;

        void Start()
        {
            Random r = new Random();
            multiplier = r.Next(2, 5);

            RandomisePassColour();

            GMInstance = GameManager.Instance;
            // GameManager.Instance.ChangeColourEvent += OnColourChange;
            GMInstance.MaximumBallCount *= multiplier;
            // Debug.Log("multiplier : " + multiplier);
            cam = Camera.main;
            text.text = "x" + multiplier;
            GameObject.FindGameObjectWithTag("FinalBox").GetComponent<ChangeCameraFocus>().GameEndEvent += DisableThis;
        }

        private void DisableThis()
        {
            boxCollider.enabled = false;
        }

        private void RandomisePassColour()
        {
            if (alternating) return;

            Random rand = new Random();
            int i = rand.Next(0, 2);
            switch (i)
            {
                case 0:
                    isBluePass = true;
                    meshRenderer.material = passMaterials[0];
                    break;

                case 1:
                    isBluePass = false;
                    meshRenderer.material = passMaterials[1];
                    break;
            }
        }

        void Update()
        {
            Vector3 textPos = cam.WorldToScreenPoint(transform.position + offset);
            if (text.transform.position != textPos) text.transform.position = textPos;
        }

        private void OnTriggerExit(Collider col)
        {
            if (col.CompareTag("Wall")) return;

            if (!firstBallPassed)
            {
                firstBallPassed = true;
                cam.GetComponent<CinemachineVirtualCamera>().Follow = col.transform;
            }

            if (GMInstance.IsBlueBall == isBluePass)
            {
                for (int i = 0; i < multiplier; i++)
                {
                    GMInstance.ObjectFromPool("Ball", col.transform.position, Quaternion.identity);
                }
            }

            else
            {
                col.gameObject.SetActive(false);
            }
        }
    }

}