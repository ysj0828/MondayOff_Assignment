using UnityEngine;

public class BallForce : MonoBehaviour
{
    private Rigidbody rigidbody;

    private void OnEnable()
    {
        Vector3 force = new Vector3(Random.Range(transform.position.x - 1, transform.position.x + 1),
                                    0,
                                    Random.Range(transform.position.z - 1, transform.position.z + 1)).normalized;

        rigidbody = GetComponent<Rigidbody>();

        rigidbody.AddForceAtPosition(force, transform.position, ForceMode.Impulse);

        GameManager.Instance.changeCamFocus.GameEndEvent += ForceOnFinish;
    }

    private void ForceOnFinish()
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere;
        randomDirection = new Vector3(randomDirection.x, Mathf.Abs(randomDirection.y), randomDirection.z) * 30;
        rigidbody.AddForce(randomDirection, ForceMode.Impulse);
    }
}
