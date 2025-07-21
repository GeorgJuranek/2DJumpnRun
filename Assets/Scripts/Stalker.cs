using UnityEngine;

public class Stalker : MonoBehaviour
{
    [SerializeField] GameObject target;

    void Update()
    {
        transform.position = new Vector3(target.transform.position.x, transform.position.y, transform.position.z);
    }
}
