using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

namespace GDD3400.Project01
{
    public class Dog : MonoBehaviour
    {
        
        private bool _isActive = true;
        public bool IsActive 
        {
            get => _isActive;
            set => _isActive = value;
        }

        // Required Variables (Do not edit!)
        private float _maxSpeed = 5f;
        private float _sightRadius = 7.5f;

        // Layers - Set In Project Settings
        private LayerMask _targetsLayer;
        private LayerMask _obstaclesLayer;

        // Tags - Set In Project Settings
        private string friendTag = "Friend";
        private string threatTag = "Threat";
        private string safeZoneTag = "SafeZone";


        // NEW ADDED FIELDS
        // Used in Behavior
        private Transform safeZone;
        private Transform targetSheep;
        private UnityEngine.Vector3 desiredPos;
        private UnityEngine.Vector3 patrolTarget;

        // Level Bounds
        public Transform xPlusBound;
        public Transform xMinusBound;
        public Transform zPlusBound;
        public Transform zMinusBound;

        private float minX, maxX, minZ, maxZ;

        private Rigidbody _rb;

        public void Awake()
        {
            // Find the layers in the project settings
            _targetsLayer = LayerMask.GetMask("Targets");
            _obstaclesLayer = LayerMask.GetMask("Obstacles");

            // GET RIDGIDBODY
            _rb = GetComponent<Rigidbody>();

        }

        // ADDED GIZMO TO SEE IN SCENEVIEW
        private void OnDrawGizmosSelected()
        {
            // Draw a yellow circle around the dog to show its sight radius
            Gizmos.color = Color.yellow;
            DrawCircleGizmo(transform.position, _sightRadius);
        }

        private void DrawCircleGizmo(UnityEngine.Vector3 center, float radius)
        {
            int segments = 64;
            UnityEngine.Vector3[] linePoints = new UnityEngine.Vector3[segments * 2];

            float angleStep = 2 * Mathf.PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float angleCurrent = i * angleStep;
                float angleNext = (i + 1) * angleStep;

                UnityEngine.Vector3 pointCurrent = new UnityEngine.Vector3(Mathf.Cos(angleCurrent), 0f, Mathf.Sin(angleCurrent)) * radius + center;
                UnityEngine.Vector3 pointNext = new UnityEngine.Vector3(Mathf.Cos(angleNext), 0f, Mathf.Sin(angleNext)) * radius + center;

                linePoints[i * 2] = pointCurrent;
                linePoints[i * 2 + 1] = pointNext;
            }

            Gizmos.DrawLine(linePoints[0], linePoints[1]);
            for (int i = 1; i < linePoints.Length / 2; i++)
            {
                Gizmos.DrawLine(linePoints[i * 2], linePoints[i * 2 + 1]);
            }
        }

        private void Start()
        {
            // Get started at current position 
            patrolTarget = transform.position;
            desiredPos = patrolTarget;

            // Calculate bounds from transforms
            /*minX = xMinusBound.position.x;
            maxX = xPlusBound.position.x;
            minZ = zMinusBound.position.z;
            maxZ = zPlusBound.position.z;*/

        }

        private void Update()
        {
            if (!_isActive) return;
            
            Perception();
            DecisionMaking();
        }

        private void Perception()
        {
            // #1 FIND THE SAFE ZONE
            // CHECK: Do we have a safe zone yet? If not assign it.
            if (safeZone == null)
            {
                var sz = GameObject.FindGameObjectWithTag(safeZoneTag);

                // Did we find the safe zone? If so get its transform location.
                if (sz != null)
                {
                    safeZone = sz.transform;
                } 
            }

            // CHECK: If we still didnt find it, skip.
            if (safeZone == null) return;

            // #2 COLLECT ALL SHEEP IN SIGHT RADIUS
            // Create an array (hits) of all sheep that collide with our sight radius
            Collider[] hits = Physics.OverlapSphere(transform.position, _sightRadius, _targetsLayer);

            // Create a list (sheepList)
            List<Transform> sheepList = new List<Transform>();

            // For each collider (c) add a new sheep to the list
            // List rebuilds itself every frame so it does not need to remove sheep from the list
            foreach (var c in hits)
            {
                // Identify sheep by checking if it has the Sheep component (script)
                if (c.GetComponentInParent<Sheep>() != null)
                {
                    // Store the transform location of the sheep in the list from the array
                    sheepList.Add(c.transform);
                }
            }

            // #3 PICK SHEEP FARTHEST FROM SAFE ZONE
            float maxDist = -Mathf.Infinity;

            // Reset each frame
            targetSheep = null;

            // For each sheep in the list find its distance from the safe zone
            foreach (var sheep in sheepList)
            {
                float d = UnityEngine.Vector3.Distance(sheep.position, safeZone.position);

                // Search list until the furthest sheep is found and store in targetSheep
                if (d > maxDist)
                {
                    maxDist = d;
                    targetSheep = sheep;
                }
            }
        }

        private void DecisionMaking()
        {
            // CHECK: Do the sheep and safe zone exist? If not then skip.
            if (safeZone == null) return;
           
            if (targetSheep != null)
            {
                // NORMAL HERDING BEHAVIOR
                // Find the vector from the target (furthest) sheep to goal
                UnityEngine.Vector3 sheepToGoal = (safeZone.position - targetSheep.position).normalized;

                // Dog moves behind the sheep (opposite of goal)
                float offset = 3f; // Distance behind the sheep to keep
                desiredPos = targetSheep.position - sheepToGoal * offset;
            }
            else
            {
                // BASIC SEARCH BEHAVIOR
                // If no sheep visible then pick random nearby point and head towards it, pick another if reached
                if ((transform.position - patrolTarget).magnitude < 0.5f)
                {
                    // Radius away it will patrol
                    float patrolRadius = 10f;

                    UnityEngine.Vector3 randomOffset
                        = new UnityEngine.Vector3(UnityEngine.Random.Range(-patrolRadius, patrolRadius),0, UnityEngine.Random.Range(-patrolRadius, patrolRadius));

                    // Generate new patrol target relative to current position
                    patrolTarget = transform.position + randomOffset;

                    // Clamp inside level bounds
                    //patrolTarget.x = Mathf.Clamp(patrolTarget.x, minX, maxX);
                    //patrolTarget.z = Mathf.Clamp(patrolTarget.z, minZ, maxZ);
                }

                desiredPos = patrolTarget;
            }
        }

        /// <summary>
        /// Make sure to use FixedUpdate for movement with physics based Rigidbody
        /// You can optionally use FixedDeltaTime for movement calculations, but it is not required since fixedupdate is called at a fixed rate
        /// </summary>
        private void FixedUpdate()
        {
            if (!_isActive) return;

            // NEW ADDED CODE
            //CHECK: Does a desired position exist? If not then skip.
            if (desiredPos == UnityEngine.Vector3.zero) return;

            // Move toward the desired position (desiredPos)
            UnityEngine.Vector3 dir = (desiredPos - transform.position);
            dir.y = 0; // stay level

            if (dir.magnitude > 0.1f)
            {
                dir = dir.normalized * _maxSpeed;
                _rb.linearVelocity = dir;

                // Face the movement direction
                UnityEngine.Quaternion targetRot = UnityEngine.Quaternion.LookRotation(dir);
                transform.rotation = UnityEngine.Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.fixedDeltaTime);
            }
            else
            {
                // Slow down when near
                _rb.linearVelocity = UnityEngine.Vector3.zero;
            }

        }
    }
}
