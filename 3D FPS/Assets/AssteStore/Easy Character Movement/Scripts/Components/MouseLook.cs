using UnityEngine;

namespace ECM.Components
{
                                        
    public class MouseLook : MonoBehaviour
    {
        #region EDITOR EXPOSED FIELDS

        [Tooltip("Should the mouse cursor be locked (eg: hidden)?")]
        [SerializeField]
        private bool _lockCursor = true;

        [Tooltip("The keyboard key to unlock the mouse cursor.")]
        [SerializeField]
        private KeyCode _unlockCursorKey = KeyCode.Escape;

        [Tooltip("How fast the cursor moves in response to mouse lateral (x-axis) movement.")]
        [SerializeField]
        private float _lateralSensitivity = 2.0f;

        [Tooltip("How fast the cursor moves in response to mouse vertical (y-axis) movement.")]
        [SerializeField]
        private float _verticalSensitivity = 2.0f;

        [Tooltip("Should the rotation be smoothed (eg: interpolated)?")]
        [SerializeField]
        private bool _smooth;

        [Tooltip("Approximately the time (in seconds) it will take to reach the target.\n" +
                 "A smaller value will reach the target faster.")]
        [SerializeField]
        public float _smoothTime = 5.0f;

        [Tooltip("Should the rotation around x-axis be clamped?")]
        [SerializeField]
        private bool _clampPitch = true;

        [Tooltip("The minimum pitch angle (in degrees).")]
        [SerializeField]
        private float _minPitchAngle = -90.0f;

        [Tooltip("The maximum pitch angle (in degrees).")]
        [SerializeField]
        private float _maxPitchAngle = 90.0f;

        #endregion

        #region FIELDS

        protected bool _isCursorLocked = true;

        protected Quaternion characterTargetRotation;
        protected Quaternion cameraTargetRotation;

        #endregion

        #region PROPERTIES

                           
        public bool lockCursor
        {
            get { return _lockCursor; }
            set { _lockCursor = value; }
        }

                           
        public KeyCode unlockCursorKey
        {
            get { return _unlockCursorKey; }
            set { _unlockCursorKey = value; }
        }

                           
        public float lateralSensitivity
        {
            get { return _lateralSensitivity; }
            set { _lateralSensitivity = Mathf.Max(0.0f, value); }
        }

                           
        public float verticalSensitivity
        {
            get { return _verticalSensitivity; }
            set { _verticalSensitivity = Mathf.Max(0.0f, value); }
        }

                           
        public bool smooth
        {
            get { return _smooth; }
            set { _smooth = value; }
        }

                                    
        public float smoothTime
        {
            get { return _smoothTime; }
            set { _smoothTime = Mathf.Max(0.0f, value); }
        }

                           
        public bool clampPitch
        {
            get { return _clampPitch; }
            set { _clampPitch = value; }
        }

                           
        public float minPitchAngle
        {
            get { return _minPitchAngle; }
            set { _minPitchAngle = Mathf.Clamp(value, -180.0f, 180.0f); }
        }

                           
        public float maxPitchAngle
        {
            get { return _maxPitchAngle; }
            set { _maxPitchAngle = Mathf.Clamp(value, -180.0f, 180.0f); }
        }

        #endregion

        #region METHODS

        public virtual void Init(Transform characterTransform, Transform cameraTransform)
        {
            characterTargetRotation = characterTransform.localRotation;
            cameraTargetRotation = cameraTransform.localRotation;
        }

                                                      
        public virtual void LookRotation(CharacterMovement movement, Transform cameraTransform)
        {
            var yaw = Input.GetAxis("Mouse X") * lateralSensitivity;
            var pitch = Input.GetAxis("Mouse Y") * verticalSensitivity;

            var yawRotation = Quaternion.Euler(0.0f, yaw, 0.0f);
            var pitchRotation = Quaternion.Euler(-pitch, 0.0f, 0.0f);

            characterTargetRotation *= yawRotation;
            cameraTargetRotation *= pitchRotation;

            if (clampPitch)
                cameraTargetRotation = ClampPitch(cameraTargetRotation);

            if (smooth)
            {
                 
                if (movement.platformUpdatesRotation && movement.isOnPlatform && movement.platformAngularVelocity != Vector3.zero)
                {
                    characterTargetRotation *=
                        Quaternion.Euler(movement.platformAngularVelocity * Mathf.Rad2Deg * Time.deltaTime);
                }

                movement.rotation = Quaternion.Slerp(movement.rotation, characterTargetRotation,
                    smoothTime * Time.deltaTime);

                cameraTransform.localRotation = Quaternion.Slerp(cameraTransform.localRotation, cameraTargetRotation,
                    smoothTime * Time.deltaTime);
            }
            else
            {
                movement.rotation *= yawRotation;
                cameraTransform.localRotation *= pitchRotation;

                if (clampPitch)
                    cameraTransform.localRotation = ClampPitch(cameraTransform.localRotation);
            }

            UpdateCursorLock();
        }

        public virtual void SetCursorLock(bool value)
        {
            lockCursor = value;
            if (lockCursor)
                return;
            
             
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public virtual void UpdateCursorLock()
        {
             
            if (lockCursor)
                InternalLockUpdate();
        }

        protected virtual void InternalLockUpdate()
        {
            if (Input.GetKeyUp(unlockCursorKey))
                _isCursorLocked = false;
            else if (Input.GetMouseButtonUp(0))
                _isCursorLocked = true;

            if (_isCursorLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (!_isCursorLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        protected Quaternion ClampPitch(Quaternion q)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            var pitch = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

            pitch = Mathf.Clamp(pitch, minPitchAngle, maxPitchAngle);

            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * pitch);

            return q;
        }

        #endregion

        #region MONOBEHAVIOUR

                                                      
        public virtual void OnValidate()
        {
            lateralSensitivity = _lateralSensitivity;
            verticalSensitivity = _verticalSensitivity;

            smoothTime = _smoothTime;

            minPitchAngle = _minPitchAngle;
            maxPitchAngle = _maxPitchAngle;
        }
        
        #endregion
    }
}
