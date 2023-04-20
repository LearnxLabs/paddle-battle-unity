using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Vector3 rotSpeed;
    [SerializeField]
    private Vector3 posSpeed;

    private Vector3 initialRot;
    [SerializeField]
    private Vector3 initialPos;
    // Start is called before the first frame update
    void Start()
    {
        initialRot = transform.localRotation.eulerAngles;
        initialPos = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<Camera>().enabled)
        {
            Vector3 mousePos = GetComponent<Camera>().ScreenToViewportPoint(Input.mousePosition);
            Vector2 screenPos = new Vector2((Mathf.Clamp(mousePos.x, 0, 1) - 0.5f), (Mathf.Clamp(mousePos.y, 0, 1) - 0.5f));
            transform.localRotation = Quaternion.Euler(initialRot + new Vector3(screenPos.y * rotSpeed.x, screenPos.x * rotSpeed.y, 0));
            transform.localPosition = new Vector3(initialPos.x + screenPos.x * posSpeed.x, initialPos.y + screenPos.y * posSpeed.y, initialPos.z + screenPos.x * posSpeed.z);
        }
    }
}
