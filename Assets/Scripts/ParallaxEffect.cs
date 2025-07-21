using UnityEngine;

public class Parallax : MonoBehaviour
{
    //NOTE: This script is a modified version of a script from the Internet

    [SerializeField] Camera cam;
    [SerializeField] float parallax_value;
    Vector2 length;
    Vector3 startPosition;

    void Awake()
    {
        startPosition = transform.position;
        length = GetComponentInChildren<SpriteRenderer>().bounds.size; // gets the size of Sprite
    }
    void Update()
    {
        Vector3 relativePosition = cam.transform.position * parallax_value;
        Vector3 distance = cam.transform.position - relativePosition;

        if (distance.x > startPosition.x + length.x) // has moved more to the right than image is wide
        {
            startPosition.x += length.x; // moves one width to the right
        }
        if (distance.x < startPosition.x - length.x) // has moved more to the left than image is wide
        {
            startPosition.x -= length.x; // moves one width to the left
        }

        transform.position = startPosition + relativePosition; // updates position
    }
}
