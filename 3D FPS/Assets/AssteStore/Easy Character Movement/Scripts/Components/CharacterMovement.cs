using ECM.Common;
using System;
using System.Collections;
using UnityEngine;

namespace ECM.Components
{
                                                                      
    public sealed class CharacterMovement : MonoBehaviour
    {
        #region EDITOR EXPOSED FIELDS

        [Header("Speed Limiters")]
        [Tooltip("The maximum lateral speed this character can move, " +
                 "including movement from external forces like sliding, collisions, etc.")]
        [SerializeField]
        private float _maxLateralSpeed = 10.0f;

        [Tooltip("The maximum rising speed, " +
                 "including movement from external forces like sliding, collisions, etc.")]
        [SerializeField]
        private float _maxRiseSpeed = 20.0f;

        [Tooltip("The maximum falling speed, " +
                 "including movement from external forces like sliding, collisions, etc.")]
        [SerializeField]
        private float _maxFallSpeed = 20.0f;

        [Header("Gravity")]
        [Tooltip("Enable / disable character's custom gravity." +
                 "If enabled the character will be affected by this gravity force.")]
        [SerializeField]
        private bool _useGravity = true;

        [Tooltip("The gravity applied to this character.")]
        [SerializeField]
        private Vector3 _gravity = new Vector3(0.0f, -30.0f, 0.0f);

        [Header("Slopes")]
        [Tooltip("Should the character slide down of a steep slope?")]
        [SerializeField]
        private bool _slideOnSteepSlope;

        [Tooltip("The maximum angle (in degrees) for a walkable slope.")]
        [SerializeField]
        private float _slopeLimit = 45.0f;

        [Tooltip("The amount of gravity to be applied when sliding off a steep slope.")]
        [SerializeField]
        private float _slideGravityMultiplier = 2.0f;

        [Header("Ground-Snap")]
        [Tooltip("When enabled, will force the character to safely follow the walkable 'ground' geometry.")]
        [SerializeField]
        private bool _snapToGround = true;

        [Tooltip("A tolerance of how close to the 'ground' maintain the character.\n" +
                 "0 == no snap at all, 1 == 100% stick to ground.")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float _snapStrength = 0.5f;

        #endregion

        #region FIELDS

         
        private static readonly Collider[] OverlappedColliders = new Collider[8];
        
        private Coroutine _lateFixedUpdateCoroutine;

        private Vector3 _normal;

        private float _referenceCastDistance;

        private bool _forceUnground;
        private float _forceUngroundTimer;
        private bool _performGroundDetection = true;

        private Vector3 _savedVelocity;
        private Vector3 _savedAngularVelocity;

        #endregion

        #region PROPERTIES

                                    
        public float maxLateralSpeed
        {
            get { return _maxLateralSpeed; }
            set { _maxLateralSpeed = Mathf.Max(0.0f, value); }
        }

                                    
        public float maxRiseSpeed
        {
            get { return _maxRiseSpeed; }
            set { _maxRiseSpeed = Mathf.Max(0.0f, value); }
        }

                                    
        public float maxFallSpeed
        {
            get { return _maxFallSpeed; }
            set { _maxFallSpeed = Mathf.Max(0.0f, value); }
        }

                                    
        public bool useGravity
        {
            get { return _useGravity; }
            set { _useGravity = value; }
        }

                                    
        public Vector3 gravity
        {
            get { return _gravity; }
            set { _gravity = value; }
        }

                           
        public bool slideOnSteepSlope
        {
            get { return _slideOnSteepSlope; }
            set { _slideOnSteepSlope = value; }
        }

                           
        public float slopeLimit
        {
            get { return _slopeLimit; }
            set { _slopeLimit = Mathf.Clamp(value, 0.0f, 89.0f); }
        }

                           
        public float slideGravityMultiplier
        {
            get { return _slideGravityMultiplier; }
            set { _slideGravityMultiplier = Mathf.Max(1.0f, value); }
        }

                                    
        public bool snapToGround
        {
            get { return _snapToGround; }
            set { _snapToGround = value; }
        }

                                    
        public float snapStrength
        {
            get { return _snapStrength; }
            set { _snapStrength = Mathf.Clamp01(value); }
        }

                           
        public CapsuleCollider capsuleCollider
        {
            get { return groundDetection.capsuleCollider; }
        }

                           
        private BaseGroundDetection groundDetection { get; set; }

                                    
        public Vector3 groundPoint
        {
            get { return groundDetection.groundPoint; }
        }

                                    
        public Vector3 groundNormal
        {
            get { return groundDetection.groundNormal; }
        }

                                                               
        public Vector3 surfaceNormal
        {
            get { return groundDetection.surfaceNormal; }
        }

                           
        public float groundDistance
        {
            get { return groundDetection.groundDistance; }
        }

                                    
        public Collider groundCollider
        {
            get { return groundDetection.groundCollider; }
        }

                                    
        public Rigidbody groundRigidbody
        {
            get { return groundDetection.groundRigidbody; }
        }

                           
        public bool isGrounded
        {
            get { return groundDetection.isOnGround && groundDetection.isValidGround; }
        }

                           
        public bool wasGrounded
        {
            get
            {
                return groundDetection.prevGroundHit.isOnGround && groundDetection.prevGroundHit.isValidGround;
            }
        }

                           
        public bool isOnGround
        {
            get { return groundDetection.isOnGround; }
        }

                           
        public bool wasOnGround
        {
            get { return groundDetection.prevGroundHit.isOnGround; }
        }

                           
        public bool isValidGround
        {
            get { return groundDetection.isValidGround; }
        }

                           
        public bool isOnPlatform { get; private set; }

                           
        public bool isOnLedgeSolidSide
        {
            get { return groundDetection.isOnLedgeSolidSide; }
        }

                           
        public bool isOnLedgeEmptySide
        {
            get { return groundDetection.isOnLedgeEmptySide; }
        }

                           
        public float ledgeDistance
        {
            get { return groundDetection.ledgeDistance; }
        }

                           
        public bool isOnStep
        {
            get { return groundDetection.isOnStep; }
        }

                           
        public float stepHeight
        {
            get { return groundDetection.stepHeight; }
        }

                           
        public bool isOnSlope
        {
            get { return groundDetection.isOnSlope; }
        }

                           
        public float groundAngle
        {
            get { return groundDetection.groundAngle; }
        }

                           
        public bool isValidSlope
        {
            get { return !slideOnSteepSlope || groundAngle < slopeLimit; }
        }

                           
        public bool isSliding { get; private set; }

                                    
        public Vector3 platformVelocity { get; private set; }

                                    
        public Vector3 platformAngularVelocity { get; private set; }

                           
        public bool platformUpdatesRotation { get; set; }

                                             
        public Vector3 velocity
        {
            get { return cachedRigidbody.velocity - platformVelocity; }
            set { cachedRigidbody.velocity = value + platformVelocity; }
        }

                           
        public float forwardSpeed
        {
            get { return Vector3.Dot(velocity, transform.forward); }
        }

                                    
        public Quaternion rotation
        {
            get { return cachedRigidbody.rotation; }
            set { cachedRigidbody.MoveRotation(value); }
        }

                           
        public GroundHit groundHit
        {
            get { return groundDetection.groundHit; }
        }

                           
        public GroundHit prevGroundHit
        {
            get { return groundDetection.prevGroundHit; }
        }

                           
        public LayerMask groundMask
        {
            get { return groundDetection.groundMask; }
        }

                                    
        public LayerMask overlapMask
        {
            get { return groundDetection.overlapMask; }
        }

                           
        public QueryTriggerInteraction triggerInteraction
        {
            get { return groundDetection.triggerInteraction; }
        }

                           
        public Rigidbody cachedRigidbody { get; private set; }

        #endregion

        #region METHODS

                                                              
        public void Pause(bool pause, bool restoreVelocity = true)
        {
            if (pause)
            {
                 
                _savedVelocity = cachedRigidbody.velocity;
                _savedAngularVelocity = cachedRigidbody.angularVelocity;

                cachedRigidbody.isKinematic = true;
            }
            else
            {
                 
                cachedRigidbody.isKinematic = false;

                if (restoreVelocity)
                {
                    cachedRigidbody.AddForce(_savedVelocity, ForceMode.VelocityChange);
                    cachedRigidbody.AddTorque(_savedAngularVelocity, ForceMode.VelocityChange);
                }
                else
                {
                     
                    var zero = Vector3.zero;

                    cachedRigidbody.AddForce(zero, ForceMode.VelocityChange);
                    cachedRigidbody.AddTorque(zero, ForceMode.VelocityChange);
                }

                cachedRigidbody.WakeUp();
            }
        }

                           
        public void SetCapsuleDimensions(Vector3 capsuleCenter, float capsuleRadius, float capsuleHeight)
        {
            capsuleCollider.center = capsuleCenter;
            capsuleCollider.radius = capsuleRadius;
            capsuleCollider.height = Mathf.Max(capsuleRadius * 0.5f, capsuleHeight);
        }

                           
        public void SetCapsuleDimensions(float capsuleRadius, float capsuleHeight)
        {
            capsuleCollider.center = new Vector3(0.0f, capsuleHeight * 0.5f, 0.0f);
            capsuleCollider.radius = capsuleRadius;
            capsuleCollider.height = Mathf.Max(capsuleRadius * 0.5f, capsuleHeight);
        }

                           
        public void SetCapsuleHeight(float capsuleHeight)
        {
            capsuleHeight = Mathf.Max(capsuleCollider.radius * 2.0f, capsuleHeight);

            capsuleCollider.center = new Vector3(0.0f, capsuleHeight * 0.5f, 0.0f);
            capsuleCollider.height = capsuleHeight;
        }

                                                                                                           
        private void OverlapCapsule(Vector3 bottom, Vector3 top, float radius, out int overlapCount,
            LayerMask overlappingMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            var colliderCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, OverlappedColliders,
                overlappingMask, queryTriggerInteraction);

            overlapCount = colliderCount;
            for (var i = 0; i < colliderCount; i++)
            {
                var overlappedCollider = OverlappedColliders[i];
                if (overlappedCollider != null && overlappedCollider != capsuleCollider)
                    continue;

                if (i < --overlapCount)
                    OverlappedColliders[i] = OverlappedColliders[overlapCount];
            }
        }

                                                                                          
        public Collider[] OverlapCapsule(Vector3 position, Quaternion rotation, out int overlapCount,
            LayerMask overlapMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
        {
            var center = capsuleCollider.center;
            var radius = capsuleCollider.radius;

            var height = capsuleCollider.height * 0.5f - radius;

            var topSphereCenter = center + Vector3.up * height;
            var bottomSphereCenter = center - Vector3.up * height;

            var top = position + rotation * topSphereCenter;
            var bottom = position + rotation * bottomSphereCenter;

            var colliderCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, OverlappedColliders, overlapMask,
                queryTriggerInteraction);

            overlapCount = colliderCount;
            for (var i = 0; i < colliderCount; i++)
            {
                var overlappedCollider = OverlappedColliders[i];
                if (overlappedCollider != null && overlappedCollider != capsuleCollider)
                    continue;

                if (i < --overlapCount)
                    OverlappedColliders[i] = OverlappedColliders[overlapCount];
            }

            return OverlappedColliders;
        }

                                             
        public bool ClearanceCheck(float clearanceHeight)
        {
            const float kTolerance = 0.01f;

             
            var radius = Mathf.Max(kTolerance, capsuleCollider.radius - kTolerance);

            var height = Mathf.Max(radius * 2.0f + kTolerance, clearanceHeight - kTolerance);
            var halfHeight = height * 0.5f;

            var center = new Vector3(0.0f, halfHeight, 0.0f);

            var p = transform.position;
            var q = transform.rotation;

            var up = q * Vector3.up;

            var localBottom = center - up * Mathf.Max(0.0f, halfHeight - kTolerance) + up * radius;
            var localTop = center + up * halfHeight - up * radius;

            var bottom = p + q * localBottom;
            var top = p + q * localTop;

             
            int overlapCount;
            OverlapCapsule(bottom, top, radius, out overlapCount, overlapMask, triggerInteraction);

             
            return overlapCount == 0;
        }

                           
        private void OverlapRecovery(ref Vector3 probingPosition, Quaternion probingRotation)
        {
            int overlapCount;
            var overlappedColliders = groundDetection.OverlapCapsule(probingPosition, probingRotation, out overlapCount);

            for (var i = 0; i < overlapCount; i++)
            {
                var overlappedCollider = overlappedColliders[i];

                var overlappedColliderRigidbody = overlappedCollider.attachedRigidbody;
                if (overlappedColliderRigidbody != null)
                    continue;

                var overlappedColliderTransform = overlappedCollider.transform;

                float distance;
                Vector3 direction;
                if (!Physics.ComputePenetration(capsuleCollider, probingPosition, probingRotation, overlappedCollider,
                    overlappedColliderTransform.position, overlappedColliderTransform.rotation, out direction,
                    out distance))
                    continue;

                probingPosition += direction * distance;
            }
        }

                                                                                 
        public bool ComputeGroundHit(Vector3 probingPosition, Quaternion probingRotation, out GroundHit groundHitInfo,
            float scanDistance = Mathf.Infinity)
        {
            groundHitInfo = new GroundHit();
            return groundDetection.ComputeGroundHit(probingPosition, probingRotation, ref groundHitInfo, scanDistance);
        }

                                                      
        public bool ComputeGroundHit(out GroundHit hitInfo, float scanDistance = Mathf.Infinity)
        {
            var p = transform.position;
            var q = transform.rotation;

            return ComputeGroundHit(p, q, out hitInfo, scanDistance);
        }

                                                      
        public void Rotate(Vector3 direction, float angularSpeed, bool onlyLateral = true)
        {
            if (onlyLateral)
                direction = Vector3.ProjectOnPlane(direction, transform.up);

            if (direction.sqrMagnitude < 0.0001f)
                return;
            
            var targetRotation = Quaternion.LookRotation(direction, transform.up);
            var newRotation = Quaternion.Slerp(cachedRigidbody.rotation, targetRotation,
                angularSpeed * Mathf.Deg2Rad * Time.deltaTime);

            cachedRigidbody.MoveRotation(newRotation);
        }

                                                      
        public void ApplyDrag(float drag, bool onlyLateral = true)
        {
            var up = transform.up;
            var v = onlyLateral ? Vector3.ProjectOnPlane(velocity, up) : velocity;

            var d = -drag * v.magnitude * v;

            cachedRigidbody.AddForce(d, ForceMode.Acceleration);
        }

                                             
        public void ApplyForce(Vector3 force, ForceMode forceMode = ForceMode.Force)
        {
            cachedRigidbody.AddForce(force, forceMode);
        }

                                             
        public void ApplyVerticalImpulse(float impulse)
        {
            Vector3 up = transform.up;
            cachedRigidbody.velocity = Vector3.ProjectOnPlane(cachedRigidbody.velocity, up) + up * impulse;
        }

                                    
        public void ApplyImpulse(Vector3 impulse)
        {
            cachedRigidbody.velocity += impulse - Vector3.Project(cachedRigidbody.velocity, transform.up);
        }

                                    
        public void DisableGrounding(float time = 0.1f)
        {
            _forceUnground = true;
            _forceUngroundTimer = time;

            groundDetection.castDistance = 0.0f;
        }

                                   
        public void DisableGroundDetection()
        {
            _performGroundDetection = false;
        }

                           
        public void EnableGroundDetection()
        {
            _performGroundDetection = true;
        }

                           
        private void ResetGroundInfo()
        {
            groundDetection.ResetGroundInfo();

            isSliding = false;

            isOnPlatform = false;
            platformVelocity = Vector3.zero;
            platformAngularVelocity = Vector3.zero;
            
            _normal = transform.up;
        }

                           
        private void DetectGround()
        {
             
            ResetGroundInfo();

             
            if (_performGroundDetection)
            {
                if (_forceUnground || _forceUngroundTimer > 0.0f)
                {
                    _forceUnground = false;
                    _forceUngroundTimer -= Time.deltaTime;
                }
                else
                {
                     
                    groundDetection.DetectGround();
                    groundDetection.castDistance = isGrounded ? _referenceCastDistance : 0.0f;
                }
            }

             
            if (!isOnGround)
                return;

             
            var up = transform.up;

            if (isValidGround)
                _normal = isOnLedgeSolidSide ? up : groundDetection.groundNormal;
            else
            {
                                 
                _normal = Vector3.Cross(Vector3.Cross(up, groundDetection.groundNormal), up).normalized;
            }

             
            var otherRigidbody = groundRigidbody;
            if (otherRigidbody == null)
                return;

            if (otherRigidbody.isKinematic)
            {
                 
                isOnPlatform = true;
                platformVelocity = otherRigidbody.GetPointVelocity(groundPoint);
                platformAngularVelocity = Vector3.Project(otherRigidbody.angularVelocity, up);
            }
            else
            {
                 
                _normal = up;
            }
        }

                                            
        private void PreventGroundPenetration()
        {
             
            if (isOnGround)
                return;

             
            var v = velocity;

            var speed = v.magnitude;

            var direction = speed > 0.0f ? v / speed : Vector3.zero;
            var distance = speed * Time.deltaTime;

            RaycastHit hitInfo;
            if (!groundDetection.FindGround(direction, out hitInfo, distance))
                return;

             
            var remainingDistance = distance - hitInfo.distance;
            if (remainingDistance <= 0.0f)
                return;

             
            var velocityToGround = direction * (hitInfo.distance / Time.deltaTime);

                          
            var up = transform.up;
            var remainingLateralVelocity = Vector3.ProjectOnPlane(v - velocityToGround, up);

             
            remainingLateralVelocity = MathLibrary.GetTangent(remainingLateralVelocity, hitInfo.normal, up) * remainingLateralVelocity.magnitude;

                          
            var newVelocity = velocityToGround + remainingLateralVelocity;

             
            cachedRigidbody.velocity = newVelocity;

             
            groundDetection.castDistance = _referenceCastDistance;
        }

                                                               
        private void ApplyMovement(Vector3 desiredVelocity, float maxDesiredSpeed, bool onlyLateral)
        {
             
            var up = transform.up;

            if (onlyLateral)
                desiredVelocity = Vector3.ProjectOnPlane(desiredVelocity, up);

             
            if (isGrounded)
            {
                if (!slideOnSteepSlope || groundAngle < slopeLimit)
                {
                     
                    desiredVelocity = MathLibrary.GetTangent(desiredVelocity, _normal, up) * Mathf.Min(desiredVelocity.magnitude, maxDesiredSpeed);

                    velocity += desiredVelocity - velocity;
                }
                else
                {
                     
                    isSliding = true;

                    velocity += gravity * (slideGravityMultiplier * Time.deltaTime);
                }
            }
            else
            {
                 
                if (isOnGround)
                {
                    var isBraking = desiredVelocity.sqrMagnitude < 0.000001f;
                    if (isBraking && onlyLateral)
                    {
                         
                        desiredVelocity = velocity;
                    }
                    else
                    {
                         
                        if (Vector3.Dot(desiredVelocity, _normal) <= 0.0f)
                        {
                            var speedLimit = Mathf.Min(desiredVelocity.magnitude, maxDesiredSpeed);

                            var lateralVelocity = Vector3.ProjectOnPlane(velocity, up);

                            desiredVelocity = Vector3.ProjectOnPlane(desiredVelocity, _normal) +
                                              Vector3.Project(lateralVelocity, _normal);

                            desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, speedLimit);
                        }
                    }
                }

                 
                velocity += onlyLateral
                    ? Vector3.ProjectOnPlane(desiredVelocity - velocity, up)
                    : desiredVelocity - velocity;

                 
                if (useGravity)
                    velocity += gravity * Time.deltaTime;
            }
            
                          
            if (!isOnStep)
                return;

            var dot = Vector3.Dot(velocity, groundPoint - transform.position);
            if (dot <= 0.0f)
                return;

            var angle = Mathf.Abs(90.0f - Vector3.Angle(up, velocity));
            if (angle < 75.0f)
                return;

            var factor = Mathf.Lerp(1.0f, 0.0f, Mathf.InverseLerp(75.0f, 90.0f, angle));
            factor = factor * (2.0f - factor);

            velocity *= factor;
        }

                           
        private void ApplyGroundMovement(Vector3 desiredVelocity, float maxDesiredSpeed, float acceleration,
            float deceleration, float friction, float brakingFriction)
        {
            var up = transform.up;
            var deltaTime = Time.deltaTime;

             
            if (!slideOnSteepSlope || groundAngle < slopeLimit)
            {
                 
                var v = wasGrounded ? velocity : Vector3.ProjectOnPlane(velocity, up);

                 
                var desiredSpeed = desiredVelocity.magnitude;
                var speedLimit = desiredSpeed > 0.0f ? Mathf.Min(desiredSpeed, maxDesiredSpeed) : maxDesiredSpeed;

                 
                var desiredDirection = MathLibrary.GetTangent(desiredVelocity, _normal, up);
                var desiredAcceleration = desiredDirection * (acceleration * deltaTime);

                if (desiredAcceleration.isZero() || v.isExceeding(speedLimit))
                {
                     
                    v = MathLibrary.GetTangent(v, _normal, up) * v.magnitude;

                     
                    v = v * Mathf.Clamp01(1f - brakingFriction * deltaTime);

                     
                    v = Vector3.MoveTowards(v, desiredVelocity, deceleration * deltaTime);
                }
                else
                {
                     
                    v = MathLibrary.GetTangent(v, _normal, up) * v.magnitude;

                     
                    v = v - (v - desiredDirection * v.magnitude) * Mathf.Min(friction * deltaTime, 1.0f);

                     
                    v = Vector3.ClampMagnitude(v + desiredAcceleration, speedLimit);
                }

                 
                velocity += v - velocity;
            }
            else
            {
                 
                isSliding = true;
                
                velocity += gravity * (slideGravityMultiplier * Time.deltaTime);
            }
        }

                                   
        private void ApplyAirMovement(Vector3 desiredVelocity, float maxDesiredSpeed, float acceleration,
            float deceleration, float friction, float brakingFriction, bool onlyLateral = true)
        {
                         
            var up = transform.up;
            var v = onlyLateral ? Vector3.ProjectOnPlane(velocity, up) : velocity;

             
            if (onlyLateral)
                desiredVelocity = Vector3.ProjectOnPlane(desiredVelocity, up);

             
            if (isOnGround)
            {
                 
                if (Vector3.Dot(desiredVelocity, _normal) <= 0.0f)
                {
                    var maxLength = Mathf.Min(desiredVelocity.magnitude, maxDesiredSpeed);

                    var lateralVelocity = Vector3.ProjectOnPlane(velocity, up);

                    desiredVelocity = Vector3.ProjectOnPlane(desiredVelocity, _normal) +
                                      Vector3.Project(lateralVelocity, _normal);

                    desiredVelocity = Vector3.ClampMagnitude(desiredVelocity, maxLength);
                }

            }

             
            var desiredSpeed = desiredVelocity.magnitude;
            var speedLimit = desiredSpeed > 0.0f ? Mathf.Min(desiredSpeed, maxDesiredSpeed) : maxDesiredSpeed;

             
            var deltaTime = Time.deltaTime;

            var desiredDirection = desiredSpeed > 0.0f ? desiredVelocity / desiredSpeed : Vector3.zero;
            var desiredAcceleration = desiredDirection * (acceleration * deltaTime);

            if (desiredAcceleration.isZero() || v.isExceeding(speedLimit))
            {
                 
                if (isOnGround && onlyLateral)
                {
                                     }
                else
                {
                     
                    v = v * Mathf.Clamp01(1f - brakingFriction * deltaTime);

                     
                    v = Vector3.MoveTowards(v, desiredVelocity, deceleration * deltaTime);
                }
            }
            else
            {
                 
                v = v - (v - desiredDirection * v.magnitude) * Mathf.Min(friction * deltaTime, 1.0f);

                 
                v = Vector3.ClampMagnitude(v + desiredAcceleration, speedLimit);
            }

             
            if (onlyLateral)
                velocity += Vector3.ProjectOnPlane(v - velocity, up);
            else
                velocity += v - velocity;

             
            if (useGravity)
                velocity += gravity * Time.deltaTime;
        }

                                                                                          
        private void ApplyMovement(Vector3 desiredVelocity, float maxDesiredSpeed, float acceleration,
            float deceleration, float friction, float brakingFriction, bool onlyLateral)
        {
            if (isGrounded)
            {
                ApplyGroundMovement(desiredVelocity, maxDesiredSpeed, acceleration, deceleration, friction,
                    brakingFriction);
            }
            else
            {
                ApplyAirMovement(desiredVelocity, maxDesiredSpeed, acceleration, deceleration, friction,
                    brakingFriction, onlyLateral);
            }

                          
            if (!isOnStep)
                return;

            var dot = Vector3.Dot(velocity, groundPoint - transform.position);
            if (dot <= 0.0f)
                return;

            var angle = Mathf.Abs(90.0f - Vector3.Angle(transform.up, velocity));
            if (angle < 75.0f)
                return;

            var factor = Mathf.Lerp(1.0f, 0.0f, Mathf.InverseLerp(75.0f, 90.0f, angle));
            factor = factor * (2.0f - factor);

            velocity *= factor;
        }

                           
        private void LimitLateralVelocity()
        {
            var lateralVelocity = Vector3.ProjectOnPlane(velocity, transform.up);
            if (lateralVelocity.sqrMagnitude > maxLateralSpeed * maxLateralSpeed)
                cachedRigidbody.velocity += lateralVelocity.normalized * maxLateralSpeed - lateralVelocity;
        }

                                    
        private void LimitVerticalVelocity()
        {
            if (isGrounded)
                return;

            var up = transform.up;
            
            var verticalSpeed = Vector3.Dot(velocity, up);
            if (verticalSpeed < -maxFallSpeed)
                cachedRigidbody.velocity += up * (-maxFallSpeed - verticalSpeed);
            if (verticalSpeed > maxRiseSpeed)
                cachedRigidbody.velocity += up * (maxRiseSpeed - verticalSpeed);
        }

                                                                                                  
        public void Move(Vector3 desiredVelocity, float maxDesiredSpeed, bool onlyLateral = true)
        {
             
            DetectGround();

             
            ApplyMovement(desiredVelocity, maxDesiredSpeed, onlyLateral);

             
            if (snapToGround && isOnGround)
                SnapToGround();

             
            LimitLateralVelocity();
            LimitVerticalVelocity();

                          
            PreventGroundPenetration();
        }
        
                                                                                                                                      
        public void Move(Vector3 desiredVelocity, float maxDesiredSpeed, float acceleration, float deceleration,
            float friction, float brakingFriction, bool onlyLateral = true)
        {
             
            DetectGround();

             
            ApplyMovement(desiredVelocity, maxDesiredSpeed, acceleration, deceleration, friction, brakingFriction, onlyLateral);

             
            if (snapToGround && isGrounded)
                SnapToGround();
            
             
            LimitLateralVelocity();
            LimitVerticalVelocity();

                          
            PreventGroundPenetration();
        }

                                   
        private void SnapToGround()
        {
             
            if (groundDistance < 0.001f)
                return;

             
            var otherRigidbody = groundRigidbody;
            if (otherRigidbody != null && otherRigidbody.isKinematic)
                return;

             
            const float groundOffset = 0.01f;

            var distanceToGround = Mathf.Max(0.0f, groundDistance - groundOffset);

             
            if (isOnLedgeSolidSide)
                distanceToGround = Mathf.Max(0.0f, Vector3.Dot(transform.position - groundPoint, transform.up) - groundOffset);

             
            var snapVelocity = transform.up * (-distanceToGround * snapStrength / Time.deltaTime);

            var newVelocity = velocity + snapVelocity;

            velocity = newVelocity.normalized * velocity.magnitude;
        }

                                             
        private void SnapToPlatform(ref Vector3 probingPosition, ref Quaternion probingRotation)
        {
             
            if (_performGroundDetection == false || _forceUnground || _forceUngroundTimer > 0.0f)
                return;

             
            GroundHit hitInfo;
            if (!ComputeGroundHit(probingPosition, probingRotation, out hitInfo, groundDetection.castDistance))
                return;

             
            var otherRigidbody = hitInfo.groundRigidbody;
            if (otherRigidbody == null || !otherRigidbody.isKinematic)
                return;

             
             
            var up = probingRotation * Vector3.up;
            var groundedPosition = probingPosition - up * hitInfo.groundDistance;

             
            var pointVelocity = otherRigidbody.GetPointVelocity(groundedPosition);
            cachedRigidbody.velocity = velocity + pointVelocity;

            var deltaVelocity = pointVelocity - platformVelocity;
            groundedPosition += Vector3.ProjectOnPlane(deltaVelocity, up) * Time.deltaTime;

             
            if (hitInfo.isOnLedgeSolidSide)
                groundedPosition = MathLibrary.ProjectPointOnPlane(groundedPosition, hitInfo.groundPoint, up);

             
            probingPosition = groundedPosition;

             
            if (platformUpdatesRotation == false || otherRigidbody.angularVelocity == Vector3.zero)
                return;

            var yaw = Vector3.Project(otherRigidbody.angularVelocity, up);
            var yawRotation = Quaternion.Euler(yaw * (Mathf.Rad2Deg * Time.deltaTime));

            probingRotation *= yawRotation;
        }

                           
        [Obsolete("Rolled back to velocity based snap as this can cause undesired effect under certain cases.")]
        private void SnapToGround_OBSOLETE(ref Vector3 probingPosition, ref Quaternion probingRotation)
        {
             
            if (_performGroundDetection == false || _forceUnground || _forceUngroundTimer > 0.0f)
                return;

             
            GroundHit hitInfo;
            if (!ComputeGroundHit(probingPosition, probingRotation, out hitInfo, groundDetection.castDistance) || !hitInfo.isValidGround)
                return;

             
            if (hitInfo.isOnLedgeSolidSide)
                return;

             
            var otherRigidbody = hitInfo.groundRigidbody;
            if (otherRigidbody)
                return;

             
            var up = probingRotation * Vector3.up;
            var groundedPosition = probingPosition - up * hitInfo.groundDistance;

            probingPosition = groundedPosition;
        }
        
                           
        private IEnumerator LateFixedUpdate()
        {
            var waitTime = new WaitForFixedUpdate();
            
            while (true)
            {
                yield return waitTime;

                 
                var p = transform.position;
                var q = transform.rotation;

                OverlapRecovery(ref p, q);
                
                 
                if (isOnGround && isOnPlatform)
                    SnapToPlatform(ref p, ref q);

                 
                cachedRigidbody.MovePosition(p);
                cachedRigidbody.MoveRotation(q);
            }
        }

        #endregion

        #region MONOBEHAVIOUR

        public void OnValidate()
        {
            maxLateralSpeed = _maxLateralSpeed;
            maxRiseSpeed = _maxRiseSpeed;
            maxFallSpeed = _maxFallSpeed;

            useGravity = _useGravity;
            gravity = _gravity;

            slideOnSteepSlope = _slideOnSteepSlope;
            slopeLimit = _slopeLimit;
            slideGravityMultiplier = _slideGravityMultiplier;

            snapToGround = _snapToGround;
            snapStrength = _snapStrength;
        }

        public void Awake()
        {
             
            groundDetection = GetComponent<BaseGroundDetection>();
            if (groundDetection == null)
            {
                Debug.LogError(
                    string.Format(
                        "CharacterMovement: No 'GroundDetection' found for '{0}' game object.\n" +
                        "Please add a 'GroundDetection' component to '{0}' game object",
                        name));

                return;
            }

            _referenceCastDistance = groundDetection.castDistance;

            cachedRigidbody = GetComponent<Rigidbody>();
            if (cachedRigidbody == null)
            {
                Debug.LogError(
                    string.Format(
                        "CharacterMovement: No 'Rigidbody' found for '{0}' game object.\n" +
                        "Please add a 'Rigidbody' component to '{0}' game object",
                        name));

                return;
            }
            
            cachedRigidbody.useGravity = false;
            cachedRigidbody.isKinematic = false;
            cachedRigidbody.freezeRotation = true;

             
            var aCollider = GetComponent<Collider>();
            if (aCollider == null)
                return;

            var physicMaterial = aCollider.sharedMaterial;
            if (physicMaterial != null)
                return;

            physicMaterial = new PhysicMaterial("Frictionless")
            {
                dynamicFriction = 0.0f,
                staticFriction = 0.0f,
                bounciness = 0.0f,
                frictionCombine = PhysicMaterialCombine.Multiply,
                bounceCombine = PhysicMaterialCombine.Average
            };

            aCollider.material = physicMaterial;

            Debug.LogWarning(
                string.Format(
                    "CharacterMovement: No 'PhysicMaterial' found for '{0}'s Collider, a frictionless one has been created and assigned.\n" +
                    "Please add a Frictionless 'PhysicMaterial' to '{0}' game object.",
                    name));
        }

        public void OnEnable()
        {
             
            if (_lateFixedUpdateCoroutine != null)
                StopCoroutine(_lateFixedUpdateCoroutine);

            _lateFixedUpdateCoroutine = StartCoroutine(LateFixedUpdate());
        }

        public void OnDisable()
        {
             
            if (_lateFixedUpdateCoroutine != null)
                StopCoroutine(_lateFixedUpdateCoroutine);
        }

        #endregion
    }
}
