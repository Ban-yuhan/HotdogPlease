using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private CharacterController controller;

    void Start()
    {
        
    }

    void Update()
    {
        Move();
    }


    private void Move()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(x, 0f, z).normalized;

        Vector3 moveDir = Quaternion.Euler(0, 45, 0) * inputDir;

        if (inputDir.magnitude >= 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(moveDir);
        }
        controller.SimpleMove(moveDir * moveSpeed);

        
    }
}
