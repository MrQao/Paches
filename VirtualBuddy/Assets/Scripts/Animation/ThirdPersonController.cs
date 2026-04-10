using UnityEditor;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 10f;
    public float jumpForce = 8f;
    public float gravity = -20f;

    [Header("Camera Settings")]
    public Transform cameraPivot;
    public float cameraDistance = 4f;
    public float cameraHeight = 1.5f;
    public float cameraRotationSpeed = 2f;
    public float cameraCollisionOffset = 0.2f;

    [Header("Animation Settings")]
    public Animator animator;
    public float animationSmoothTime = 0.1f;

    private CharacterController controller;
    private Vector3 moveDirection;
    private float verticalVelocity;
    private float cameraAngleX;
    private float cameraAngleY;
    private float currentSpeed;
    private float animationSpeedPercent;
    bool is_Ground = true;

    public Material[] biaoqing;
    public SkinnedMeshRenderer body;
    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        cameraAngleX = transform.eulerAngles.y;
        cameraAngleY = 15f;
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {

        HandleMovement();
        UpdateCameraPosition();
        HandleCameraRotation();
        UpdateAnimations();

    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = Vector3.ProjectOnPlane(cameraPivot.forward, Vector3.up).normalized;
        Vector3 right = cameraPivot.right;
        Vector3 moveInput = (forward * vertical + right * horizontal).normalized;
        currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        moveDirection = moveInput * currentSpeed;
        moveDirection.y = verticalVelocity;

        controller.Move(moveDirection * Time.deltaTime);

        if (moveInput != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveInput);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void HandleCameraRotation()
    {
        cameraAngleX += Input.GetAxis("Mouse X") * cameraRotationSpeed;
        cameraAngleY -= Input.GetAxis("Mouse Y") * cameraRotationSpeed;
        cameraAngleY = Mathf.Clamp(cameraAngleY, -30f, 70f);
        cameraPivot.rotation = Quaternion.Euler(cameraAngleY, cameraAngleX, 0);
    }

    void UpdateCameraPosition()
    {
        // 计算理想位置
        Vector3 targetHeight = transform.position + Vector3.up * cameraHeight;
        Vector3 idealPosition = targetHeight - cameraPivot.forward * cameraDistance;

        // 摄像机碰撞避免
        RaycastHit hit;
        if (Physics.Raycast(targetHeight,
                           -cameraPivot.forward,
                           out hit,
                           cameraDistance))
        {
            idealPosition = hit.point + cameraPivot.forward * cameraCollisionOffset;
        }

        // 平滑移动摄像机
        Camera.main.transform.position = Vector3.Lerp(
            Camera.main.transform.position,
            idealPosition,
            10f * Time.deltaTime
        );

        Camera.main.transform.LookAt(targetHeight);
    }

    void UpdateAnimations()
    {
        // 计算速度百分比 (0-1范围)
        float targetSpeedPercent = controller.velocity.magnitude / runSpeed;
        animationSpeedPercent = targetSpeedPercent;
        // 设置动画参数
        animator.SetFloat("speed", animationSpeedPercent);
       // animator.SetBool("isRuning", Input.GetKey(KeyCode.LeftShift));
    }
    public void SetIdle()
    {
        body.materials[0] = biaoqing[0];
    }
    //private void OnTriggerEnter(Collider other)
    //{
    //    Debug.Log("other" + other.gameObject.tag);
    //    if (other.gameObject.tag== "Ground")
    //    {
    //        is_Ground = true;
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    if (other.gameObject.tag == "Ground")
    //    {
    //        is_Ground = false;
    //    }
    //}
}