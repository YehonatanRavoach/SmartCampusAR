using UnityEngine;

public class CanvasMover : MonoBehaviour
{
    [Header("Canvas Settings")]
    public RectTransform myCanvas;       // The RectTransform of the canvas to move
    public float moveUpDistance = 300f;   // Distance to move the canvas up
    public float moveSpeed = 5f;        // Speed of the canvas movement

    private Vector3 originalPosition;    // Original position of the canvas
    private Vector3 targetPosition;       // Target position to move the canvas to
    private bool isMoving = false;        // Flag to check if the canvas is currently moving
    void Start()
    {
        
        if (myCanvas == null)
            myCanvas = GetComponent<RectTransform>();

        originalPosition = myCanvas.localPosition;
        targetPosition = originalPosition;
    }

    void Update()
    {
        if (isMoving)
        {
            float step = Time.deltaTime * moveSpeed;
            myCanvas.localPosition = Vector3.Lerp(myCanvas.localPosition, targetPosition, step);

            if (Vector3.Distance(myCanvas.localPosition, targetPosition) < 1f)
            {
                myCanvas.localPosition = targetPosition;
                isMoving = false;
            }
        }
    }

   
    public void MoveCanvasUp()// Method to move the canvas up by a specified distance
    {
        targetPosition = originalPosition + new Vector3(0f, moveUpDistance, 0f);
        isMoving = true;
    }

    
    public void ResetCanvasPosition()// Method to reset the canvas position to its original position
    {
        targetPosition = originalPosition;
        isMoving = true;
    }
}
