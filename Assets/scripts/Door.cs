using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DoorScript
{
    [RequireComponent(typeof(AudioSource))]
    
    public class Door : MonoBehaviour
    {
        public enum LocalSideAxis
        {
            X,
            Z
        }

        [Header("Hinge Assignment")]
        [Tooltip("Assign a HingeJoint manually if you want to control which hinge is used. If left empty, the script will use or add a HingeJoint on this GameObject.")]
        public HingeJoint assignedHinge;
    
        [Header("Auto-Detect Hinge Anchor")]
        [Tooltip("If true, automatically set the hinge anchor to the right edge of the mesh bounds in local space.")]
        public bool autoDetectRightEdge = false;
        [Header("Manual Hinge Anchor Override")]
        [Tooltip("If set, this value overrides the automatic anchor calculation. Useful if your mesh pivot is not centered or you want precise control.")]
        public bool useManualAnchor = false;
        public Vector3 manualAnchor = Vector3.zero;
    
        [Tooltip("If true, flips the swing direction (inverts hinge axis and open/close angles). Useful if the door opens the wrong way.")]
        public bool flipSwingDirection = false;
    
        [Header("Hinge Settings")]
        [Tooltip("Width of the door in local units (distance from left to right edge in local X axis).")]
        public float doorWidth = 1.0f;
        [Tooltip("If true, hinge is on the right edge; if false, hinge is on the left edge.")]
        public bool hingeOnRight = false;
        [Tooltip("Which local axis represents the door width/side pivot direction.")]
        public LocalSideAxis sidePivotAxis = LocalSideAxis.X;
    
        [Header("Animation (Optional)")]
        [Tooltip("Animator for animated doors. Leave empty for hinge doors.")]
        public Animator doorAnimator;
        [Tooltip("Trigger name for opening/closing in Animator.")]
        public string openTriggerName = "OpenDoor";

        [Header("Audio (Optional)")]
        public AudioSource doorAudioSource;
        public AudioClip openDoorClip;
        public AudioClip closeDoorClip;

        [Header("Hinge System (Physics-based)")]
        [Tooltip("If true, use Unity's HingeJoint for a physical swinging door.")]
        public bool useHingeJoint = true;
        [Tooltip("If true, uses a guaranteed kinematic side-pivot swing instead of hinge physics.")]
        public bool forceReliableSwing = true;
        [Tooltip("The angle the door swings open to (degrees)")]
        public float DoorOpenAngle = 90.0f;
        [Tooltip("The angle the door swings closed to (degrees)")]
        public float DoorCloseAngle = 0.0f;
        [Tooltip("How fast the door swings (higher = faster)")]
        public float openSpeed = 10f;
        [Tooltip("Degrees per second for reliable kinematic swing mode.")]
        public float reliableSwingSpeed = 180f;
        [Tooltip("Base spring force used by the hinge drive.")]
        public float springStrength = 300f;
        [Tooltip("Damping for hinge spring. Higher values reduce wobble.")]
        public float springDamping = 20f;

        [Header("Dual Hinge Setup")]
        [Tooltip("Optional second hinge for smoother door movement.")]
        public HingeJoint secondaryHinge;
        [Tooltip("Optional: assign a transform on the hinge side. Reliable swing will rotate around this exact world position.")]
        public Transform pivotReference;

        [Header("Hinge Orientation")]
        [Tooltip("If true, hinges are placed vertically (top and bottom). If false, hinges are horizontal (left and right).")]
        public bool verticalHinges = false;

        [Tooltip("If true, the script will override the hinge anchor X to the selected edge. Leave off to keep anchors exactly as set in Inspector.")]
        public bool autoConfigureHingeAnchor = false;

        [Tooltip("If true, always force hinge pivot to the selected side edge so the door swings like a real door.")]
        public bool forceSidePivot = true;
        [Tooltip("If true, hinge axis is aligned to world up so the door always swings vertically even if the mesh is rotated.")]
        public bool useWorldUpForHingeAxis = true;

        private bool isOpen = false;
        private bool useAnimator = false;
        private HingeJoint hinge;
        private Vector3 reliablePivotWorld;
        private float currentSwingAngle;
        private bool reliableSwingInitialized;

        private Vector3 GetHingeAxis()
        {
            if (!useWorldUpForHingeAxis)
            {
                return Vector3.up;
            }

            Vector3 localUp = transform.InverseTransformDirection(Vector3.up).normalized;
            if (localUp.sqrMagnitude < 0.5f)
            {
                return Vector3.up;
            }

            return localUp;
        }

        private Vector3 ApplySidePivot(Vector3 current, float signedHalfWidth)
        {
            if (sidePivotAxis == LocalSideAxis.Z)
            {
                current.z = signedHalfWidth;
            }
            else
            {
                current.x = signedHalfWidth;
            }

            return current;
        }

        private Vector3 GetPivotAnchorLocal()
        {
            if (useManualAnchor)
            {
                return manualAnchor;
            }

            Vector3 baseAnchor = Vector3.zero;
            if (hinge != null)
            {
                baseAnchor = hinge.anchor;
            }
            else if (assignedHinge != null)
            {
                baseAnchor = assignedHinge.anchor;
            }

            // Prefer the real hinge anchor side from the component so pivot matches placed hinges.
            float detectedSide = sidePivotAxis == LocalSideAxis.Z ? baseAnchor.z : baseAnchor.x;
            bool hasDetectedSide = Mathf.Abs(detectedSide) > 0.0001f;

            if (hasDetectedSide)
            {
                float halfWidth = doorWidth * 0.5f;
                float signedHalf = Mathf.Sign(detectedSide) * halfWidth;
                return ApplySidePivot(baseAnchor, signedHalf);
            }

            // Fallback if hinge anchor is centered.
            float fallbackHalfWidth = doorWidth * 0.5f;
            float fallbackSigned = hingeOnRight ? fallbackHalfWidth : -fallbackHalfWidth;
            return ApplySidePivot(baseAnchor, fallbackSigned);
        }

        private bool TryGetReliablePivotWorld(out Vector3 pivotWorld)
        {
            if (pivotReference != null)
            {
                pivotWorld = pivotReference.position;
                return true;
            }

            int count = 0;
            Vector3 sum = Vector3.zero;

            if (assignedHinge != null)
            {
                sum += assignedHinge.transform.position;
                count++;
            }

            if (secondaryHinge != null)
            {
                sum += secondaryHinge.transform.position;
                count++;
            }

            if (count > 0)
            {
                pivotWorld = sum / count;
                return true;
            }

            pivotWorld = Vector3.zero;
            return false;
        }

        private void GetEffectiveAngles(out float openAngle, out float closeAngle)
        {
            openAngle = DoorOpenAngle;
            closeAngle = DoorCloseAngle;
            if (flipSwingDirection)
            {
                openAngle = -openAngle;
                closeAngle = -closeAngle;
            }
        }

        private void ConfigureHingeLimits(HingeJoint joint)
        {
            if (joint == null) return;

            GetEffectiveAngles(out float openAngle, out float closeAngle);
            JointLimits limits = joint.limits;
            limits.min = Mathf.Min(openAngle, closeAngle);
            limits.max = Mathf.Max(openAngle, closeAngle);
            joint.limits = limits;
            joint.useLimits = true;
        }

        private void SetupHinge(HingeJoint hinge, bool isPrimary)
        {
            if (hinge == null) return;

            ConfigureHingeLimits(hinge);

            // Keep the swing axis vertical in world space to avoid "top hinge" behavior.
            hinge.axis = GetHingeAxis();
            hinge.useSpring = true;

            // Keep hinge anchors from Inspector by default.
            // This is important for top/bottom hinge setups where anchors are intentionally placed.
            if (isPrimary)
            {
                if (useManualAnchor)
                {
                    hinge.anchor = manualAnchor;
                }
                else if (autoConfigureHingeAnchor || forceSidePivot)
                {
                    float halfWidth = doorWidth * 0.5f;
                    float anchorX = hingeOnRight ? halfWidth : -halfWidth;
                    Vector3 current = hinge.anchor;
                    hinge.anchor = ApplySidePivot(current, anchorX);
                }
            }
            else if (forceSidePivot)
            {
                float halfWidth = doorWidth * 0.5f;
                float anchorX = hingeOnRight ? halfWidth : -halfWidth;
                Vector3 current = hinge.anchor;
                hinge.anchor = ApplySidePivot(current, anchorX);
            }

            // Debug: Log hinge setup details
            JointLimits configured = hinge.limits;
            Debug.Log($"[Door] Hinge setup: Axis = {hinge.axis}, Anchor = {hinge.anchor}, Limits = ({configured.min}, {configured.max})");
        }

        void Awake()
        {
            // In reliable swing mode, keep hinge anchors exactly as authored in Inspector.
            // Overriding anchors can force knob-side pivots on some door setups.
            if (forceReliableSwing)
            {
                autoConfigureHingeAnchor = false;
                forceSidePivot = false;
            }

                    // Debug: Log anchor and pivot info
                    Vector3 anchorUsed = useManualAnchor ? manualAnchor : new Vector3(hingeOnRight ? (doorWidth / 2f) : -(doorWidth / 2f), 0f, 0f);
                    Debug.Log($"[Door] Hinge anchor (local): {anchorUsed}, (world): {transform.TransformPoint(anchorUsed)}");
                    Debug.Log($"[Door] Door pivot (world): {transform.position}");
                    Debug.Log($"[Door] DoorOpenAngle: {DoorOpenAngle}, DoorCloseAngle: {DoorCloseAngle}, flipSwingDirection: {flipSwingDirection}");

            useAnimator = (doorAnimator != null);
            if (doorAudioSource == null)
                doorAudioSource = GetComponent<AudioSource>();

            if (useHingeJoint)
            {
                // Ensure Rigidbody exists and is not kinematic
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb == null)
                    rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = false;
                // Keep the door anchored in place and only allow swing around Y.
                rb.constraints = RigidbodyConstraints.FreezePositionX |
                                 RigidbodyConstraints.FreezePositionY |
                                 RigidbodyConstraints.FreezePositionZ |
                                 RigidbodyConstraints.FreezeRotationX |
                                 RigidbodyConstraints.FreezeRotationZ;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.angularDamping = Mathf.Max(rb.angularDamping, 2f);

                // Setup primary hinge
                hinge = assignedHinge != null ? assignedHinge : GetComponent<HingeJoint>() ?? gameObject.AddComponent<HingeJoint>();
                SetupHinge(hinge, true);

                // Setup secondary hinge if assigned
                if (secondaryHinge != null)
                {
                    SetupHinge(secondaryHinge, false);
                }
            }

            if (forceReliableSwing)
            {
                InitializeReliableSwingMode();
            }
        }

        void FixedUpdate()
        {
            if (forceReliableSwing)
            {
                ApplyReliableSwing(Time.fixedDeltaTime);
                return;
            }

            if (useHingeJoint && hinge != null)
            {
                ApplySpringToHinge(hinge);
                // Secondary hinge is for alignment support only.
                // Driving both with springs can cause joints to fight.
            }
            else if (!useAnimator)
            {
                // fallback: legacy hinge logic (not physics-based)
                float openAngle = DoorOpenAngle;
                float closeAngle = DoorCloseAngle;
                if (flipSwingDirection)
                {
                    openAngle = -openAngle;
                    closeAngle = -closeAngle;
                }
                float targetAngle = isOpen ? openAngle : closeAngle;
                transform.localRotation = Quaternion.Euler(0, targetAngle, 0);
            }
        }

        private void InitializeReliableSwingMode()
        {
            if (reliableSwingInitialized)
            {
                return;
            }

            GetEffectiveAngles(out _, out float closeAngle);
            currentSwingAngle = closeAngle;

            if (!TryGetReliablePivotWorld(out reliablePivotWorld))
            {
                Vector3 localAnchor = GetPivotAnchorLocal();
                reliablePivotWorld = transform.TransformPoint(localAnchor);
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;

            if (hinge != null)
            {
                hinge.useSpring = false;
                hinge.useMotor = false;
            }
            if (secondaryHinge != null)
            {
                secondaryHinge.useSpring = false;
                secondaryHinge.useMotor = false;
            }

            reliableSwingInitialized = true;
            Debug.Log($"[Door] Reliable swing enabled. Pivot world: {reliablePivotWorld}");
        }

        private void ApplyReliableSwing(float dt)
        {
            if (!reliableSwingInitialized)
            {
                InitializeReliableSwingMode();
            }

            GetEffectiveAngles(out float openAngle, out float closeAngle);
            float targetAngle = isOpen ? openAngle : closeAngle;
            float newAngle = Mathf.MoveTowards(currentSwingAngle, targetAngle, reliableSwingSpeed * dt);
            float delta = newAngle - currentSwingAngle;

            if (Mathf.Abs(delta) > 0.001f)
            {
                transform.RotateAround(reliablePivotWorld, Vector3.up, delta);
                currentSwingAngle = newAngle;
            }
        }

        private void ApplySpringToHinge(HingeJoint joint)
        {
            if (joint == null) return;

            ConfigureHingeLimits(joint);
            GetEffectiveAngles(out float openAngle, out float closeAngle);

            JointSpring spring = joint.spring;
            spring.spring = Mathf.Max(springStrength * openSpeed, 100f);
            spring.damper = springDamping;
            spring.targetPosition = isOpen ? openAngle : closeAngle;
            joint.spring = spring;
            joint.useSpring = true;
        }

        /// <summary>
        /// Opens or closes the door. Called by DoorInteractor or other scripts.
        /// </summary>
        public void OpenDoor()
        {

            isOpen = !isOpen;
            Debug.Log($"[Door] OpenDoor called. New state: isOpen = {isOpen}");

            // Animator-based door
            if (useAnimator && doorAnimator != null)
            {
                doorAnimator.SetTrigger(openTriggerName);
            }

            // Audio
            if (doorAudioSource != null)
            {
                if (isOpen && openDoorClip != null)
                {
                    doorAudioSource.clip = openDoorClip;
                    doorAudioSource.Play();
                }
                else if (!isOpen && closeDoorClip != null)
                {
                    doorAudioSource.clip = closeDoorClip;
                    doorAudioSource.Play();
                }
            }

            // Update hinge spring for physics-based doors
            if (!forceReliableSwing && useHingeJoint && hinge != null)
            {
                ApplySpringToHinge(hinge);

                Debug.Log("[Door] Hinge spring updated.");
            }
        }
    }
}