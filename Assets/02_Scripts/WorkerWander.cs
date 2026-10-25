using UnityEngine;

namespace VibeXR
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class WorkerWander : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 1.25f;
        [SerializeField, Min(0.1f)] private float turnSpeed = 360f;
        [SerializeField] private Vector2 xBounds = new Vector2(-8.5f, 8.5f);
        [SerializeField] private Vector2 zBounds = new Vector2(-13.5f, 13.5f);
        [SerializeField, Min(0.1f)] private float arrivalDistance = 0.35f;
        [SerializeField, Min(0f)] private float pauseMin = 0.5f;
        [SerializeField, Min(0f)] private float pauseMax = 2f;
        [SerializeField] private LayerMask obstacleMask = ~0;

        private CharacterController characterController;
        private Animator animator;
        private Vector3 destination;
        private float pauseRemaining;
        private float blockedTime;
        private bool hasDestination;
        private float groundY;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            if (animator != null) animator.applyRootMotion = true;
            groundY = transform.position.y;
            ChooseDestination();
            pauseRemaining = Random.Range(0f, pauseMax);
            SetWalking(false);
        }

        private void Update()
        {
            if (pauseRemaining > 0f)
            {
                pauseRemaining -= Time.deltaTime;
                SetWalking(false);
                return;
            }

            if (!hasDestination || HorizontalDistance(GetAgentPosition(), destination) <= arrivalDistance)
            {
                ChooseDestination();
                pauseRemaining = Random.Range(pauseMin, pauseMax);
                SetWalking(false);
                return;
            }

            Vector3 direction = destination - GetAgentPosition();
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                ChooseDestination();
                return;
            }

            direction.Normalize();
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);

            SetWalking(true);
        }

        private void OnAnimatorMove()
        {
            if (animator == null || characterController == null || !hasDestination || pauseRemaining > 0f)
                return;

            Vector3 motion = animator.deltaPosition;
            motion.y = 0f;
            if (motion.sqrMagnitude < 0.000001f)
            {
                Vector3 direction = destination - GetAgentPosition();
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.0001f) return;
                motion = direction.normalized * moveSpeed * Time.deltaTime;
            }

            Vector3 beforeMove = transform.position;
            CollisionFlags flags = characterController.Move(motion);
            bool moved = (transform.position - beforeMove).sqrMagnitude > 0.000001f;

            if (!moved || (flags & CollisionFlags.Sides) != 0)
            {
                blockedTime += Time.deltaTime;
                if (blockedTime > 1.5f) ChooseDestination();
            }
            else
            {
                blockedTime = 0f;
            }
        }

        private void ChooseDestination()
        {
            Vector3 origin = GetAgentPosition();
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector3 candidate = new Vector3(
                    Random.Range(xBounds.x, xBounds.y),
                    groundY,
                    Random.Range(zBounds.x, zBounds.y));

                Vector3 offset = candidate - origin;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance < 1f) continue;

                Vector3 rayOrigin = origin;
                if (Physics.Raycast(rayOrigin, offset / distance, distance, obstacleMask,
                                    QueryTriggerInteraction.Ignore))
                    continue;

                destination = candidate;
                hasDestination = true;
                blockedTime = 0f;
                return;
            }

            hasDestination = false;
        }

        private void SetWalking(bool walking)
        {
            if (animator != null) animator.speed = walking ? 1f : 0f;
        }

        private Vector3 GetAgentPosition()
        {
            return characterController != null
                ? characterController.transform.TransformPoint(characterController.center)
                : transform.position;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
