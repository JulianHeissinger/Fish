using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BoidSpawner : MonoBehaviour {
    #region Fields
    
    [Header("Prefab")]
    [SerializeField] GameObject boidPrefab;
    
    [Header("Spawn")]
    [SerializeField, Min(1)] int boidCount = 60;
    [SerializeField] Vector3 spawnExtents = new(14f, 6f, 14f);
    
    [Header("Movement")]
    [SerializeField, Min(0.1f)] float baseSpeed = 7f;
    [SerializeField, Min(0f)] float speedVariance = 1.5f;
    [SerializeField, Min(0.5f)] float flockBoundsRadius = 30f;
    [SerializeField, Min(0.001f)] float verticalNoiseFrequency = 0.5f;
    
    readonly List<SwarmBehaviour> spawnedBoids = new();
    Transform runtimeRoot;
    
    #endregion
    
    protected void Start() => SpawnFlock();

    [ContextMenu("Spawn Flock")]
    public void SpawnFlock() {
        ClearFlock();
        EnsureRuntimeRoot();

        for (var i = 0; i < boidCount; i++) {
            var boid = CreateBoid(i);
            spawnedBoids.Add(boid);
        }
    }

    [ContextMenu("Clear Flock")]
    public void ClearFlock() {
        for (var i = spawnedBoids.Count - 1; i >= 0; i--) {
            var boid = spawnedBoids[i];
            if (boid) DestroyImmediate(boid.gameObject);
        }
        
        spawnedBoids.Clear();
        
        if (!runtimeRoot) return;
        
        DestroyImmediate(runtimeRoot.gameObject);
        runtimeRoot = null;
    }

    void EnsureRuntimeRoot() {
        if (runtimeRoot) return;
        
        var root = new GameObject("BoidsRuntime");
        root.transform.SetParent(transform, false);
        runtimeRoot = root.transform;
    }

    SwarmBehaviour CreateBoid(int index) {
        var spawnPosition = transform.position + GetRandomSpawnOffset();
        var boidObject = Instantiate(boidPrefab, spawnPosition, Quaternion.identity, runtimeRoot);
        boidObject.name = $"Boid_{index:000}";
        
        var capsuleCollider = boidObject.GetOrAddComponent<CapsuleCollider>();
        capsuleCollider.isTrigger = false;
        
        var body = boidObject.GetOrAddComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        
        var boid = boidObject.GetOrAddComponent<SwarmBehaviour>();
        boid.ConfigureCenter(transform.position, flockBoundsRadius);
        boid.speed = baseSpeed + Random.Range(-speedVariance, speedVariance);
        
        return boid;
    }
    
    Vector3 GetRandomSpawnOffset() => new(
        Random.Range(-spawnExtents.x, spawnExtents.x),
        Random.Range(-spawnExtents.y, spawnExtents.y),
        Random.Range(-spawnExtents.z, spawnExtents.z)
    );
}