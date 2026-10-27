using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inspection
{
    /// <summary>
    /// 自由视角：WASD 平移、Q/E 升降、Shift 加速、按住右键拖拽转视角。
    /// 用"按住右键才转视角"而不是锁定鼠标，是为了让左键能正常点 UI 和场景。
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("移动")]
        public float moveSpeed = 4f;
        public float boostMultiplier = 2.5f;

        [Header("视角")]
        public float lookSensitivity = 3f;
        public float minPitch = -80f;
        public float maxPitch = 80f;

        float yaw;
        float pitch;
        bool looking;

        void Start()
        {
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = euler.x;
        }

        void Update()
        {
            // 正在输入框里打字时，不要用 WASD 移动镜头
            if (IsTyping()) return;

            HandleLook();
            HandleMove();
        }

        static bool IsTyping()
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null) return false;

            var field = es.currentSelectedGameObject.GetComponent<InputField>();
            if (field == null) field = es.currentSelectedGameObject.GetComponentInParent<InputField>();
            return field != null && field.isFocused;
        }

        void HandleLook()
        {
            if (Input.GetMouseButtonDown(1))
            {
                looking = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            if (Input.GetMouseButtonUp(1))
            {
                looking = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (!looking) return;

            yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        void HandleMove()
        {
            float speed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) speed *= boostMultiplier;

            Vector3 dir = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) dir += transform.forward;
            if (Input.GetKey(KeyCode.S)) dir -= transform.forward;
            if (Input.GetKey(KeyCode.D)) dir += transform.right;
            if (Input.GetKey(KeyCode.A)) dir -= transform.right;
            if (Input.GetKey(KeyCode.E)) dir += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) dir -= Vector3.up;

            if (dir.sqrMagnitude > 0.001f)
            {
                transform.position += dir.normalized * (speed * Time.deltaTime);
            }
        }
    }
}
