using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;


public class SwarmBehaviour : MonoBehaviour
{
    [Header("Movement")]
    public float speed;
    public float acceleration;
    public float turningSpeed;
    
    [Header("Swarm Params - Distances")]
    // min Distance from other Beings in the swarm
    public float separationDistance;
    // max Distance from other beings in the swarm
    public float cohesionDistance;
    // direction other beings are facing - so the swarm is going in one direction
    public float directionDistance;

    // Weights to prioritize different behaviours
    [Header("Swarm Params - Weights")]
    public float separationWeight;
    public float cohesionWeight;
    public float directionWeight;
    public float centerWeight;
    public float verticalNoiseFrequency = 0f;
    public float verticalNoiseAmplitude = 0f;
    
    [Header("Swarm Params - Neighbours")]
    public LayerMask neighbourMask;
    public int maxNeighbourColliders;
    
    
    Rigidbody rb;
    List<SwarmBehaviour> neighbours = new(64);
    Collider[] neighbourColliders;
    private Vector3 swarmCenter;
    float centerRadius;
    float noiseOffset;
    
    public Vector3 Velocity => rb ? rb.linearVelocity : Vector3.zero;
    float SeperationDistanceSq => separationDistance * separationDistance;
    float CohesionDistanceSq => cohesionDistance * cohesionDistance;
    float DirectionDistanceSq => directionDistance * directionDistance;
    float NeighbourScanRad => Math.Max(separationDistance, Math.Max(cohesionDistance, directionDistance));
    
    
    protected void Awake()
    {
        var bufferSize = maxNeighbourColliders;
        neighbourColliders =  new Collider[bufferSize];
        rb = GetComponent<Rigidbody>();
    }

    protected void Start()
    {
        if (rb.linearVelocity.magnitude <= 0.001f)
        {
            rb.linearVelocity = UnityEngine.Random.onUnitSphere * speed;
        }

        noiseOffset = UnityEngine.Random.Range(0f, 1000f);
    }

    public void ConfigureCenter(Vector3 center, float radius)
    {
        swarmCenter = center;
        centerRadius = radius;
    }

    private void FindNeighbours()
    {
        neighbours.Clear();
        var hits = Physics.OverlapSphereNonAlloc(transform.position, NeighbourScanRad, neighbourColliders, neighbourMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits; i++)
        {
            var hit = neighbourColliders[i];
            if(!hit) continue;
            
            var other = hit.attachedRigidbody ? hit.attachedRigidbody.GetComponent<SwarmBehaviour>() : hit.GetComponent<SwarmBehaviour>();

            if (!other || other == this) continue;
            neighbours.Add(other);
        }
    }

    public Vector3 ComputeSeperation()
    {
        var force =  Vector3.zero;
        var count = 0;
        var position = transform.position;

        for (int i = 0; i < neighbours.Count; i++)
        {
            var toOther = position - neighbours[i].transform.position;
            var distanceSq = toOther.sqrMagnitude;
            if (distanceSq > SeperationDistanceSq) continue;
            
            force += toOther.normalized / distanceSq;
            count++;
        }
        
        return count > 0 ? force / count : Vector3.zero;
    }

    public Vector3 ComputeDirection()
    {
        var avgVelocity = Vector3.zero;
        var count = 0;
        var position = transform.position;

        for (int i = 0; i < neighbours.Count; i++)
        {
            var offset = position -  neighbours[i].transform.position;
            if(offset.magnitude > directionDistance) continue;
            
            avgVelocity += neighbours[i].Velocity;
            count++;
        }
        
        return count > 0 ? avgVelocity / count : Vector3.zero;
    }

    public Vector3 ComputeCohesion()
    {
        var center = Vector3.zero;
        var count = 0;
        var position = transform.position;

        for (int i = 0; i < neighbours.Count; i++)
        {
            var otherPos = neighbours[i].transform.position;
            if((otherPos - position).sqrMagnitude > CohesionDistanceSq) continue;
            
            center += otherPos;
            count++;
        }
        
        return count > 0 ? (center / count - position).normalized : Vector3.zero;
    }

    public Vector3 ComputeCenter()
    {
        var offset = transform.position - swarmCenter;
        var distance = offset.magnitude;
        var innerRadius = centerRadius * 0.75f;
        
        if (distance <= innerRadius) return Vector3.zero;
        
        var strength = Mathf.InverseLerp(innerRadius, centerRadius, distance);
        
        return (swarmCenter - transform.position).normalized * strength;
    }
    protected void FixedUpdate()
    {
        FindNeighbours();
        
        var noise = (Mathf.PerlinNoise((transform.position.x + noiseOffset) * verticalNoiseFrequency, (transform.position.z + noiseOffset) * verticalNoiseFrequency) - 0.5f) * 2f;
        
        var direction =
            ComputeSeperation() * separationWeight +
            ComputeCohesion() * cohesionWeight +
            ComputeDirection() * directionWeight +
            ComputeCenter() * centerWeight +
            Vector3.up * (noise * verticalNoiseAmplitude);

        if (direction.magnitude <= 0.0001f)
        {
            direction = transform.forward;
        }
        
        var newAcceleration = direction.normalized * acceleration;
        var newVelocity = rb.linearVelocity + newAcceleration * Time.deltaTime;
        rb.linearVelocity = newVelocity.normalized * speed;

        if (rb.linearVelocity.magnitude >= 0.001f)
        {
            var lookDir = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up);
            var newRotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, turningSpeed * Time.deltaTime);
        }
    }
}
