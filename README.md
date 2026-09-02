# Boids Simulation

Unity project simulating 3D fish flocking behavior using classic boid algorithms and Unity Physics.

## Key Scripts

• SwarmBehaviour.cs: Handles boid steering forces, neighbor detection, and physics movement.

• SwarmSpawner.cs: Spawns, initializes, and manages flock instances at runtime.

## Actual simulation

Each entity calculates a Vector3 based on the direction that other entities are going, the direction to the center of the boid, how far apart entities should be and added vertical noise (if wanted) to actually keep the swarm moving vertically.
This is adaptable for fish (the case for this project) but also for birds for example.

### Key code snippets

  Key snippets from SwarmBehaviour.cs:
  ### 1. Neighbor Detection (SwarmBehaviour.cs:73-88)

  Find fish near current fish without garbage collection allocation:

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

  ### 2. Flocking Forces

  • Separation (SwarmBehaviour.cs:90-107): Push away from close fish.

    for (int i = 0; i < neighbours.Count; i++)
    {
        var toOther = position - neighbours[i].transform.position;
        var distanceSq = toOther.sqrMagnitude;
        if (distanceSq > SeperationDistanceSq) continue;
        
        force += toOther.normalized / distanceSq;
        count++;
    }

  • Alignment (SwarmBehaviour.cs:109-125): Match velocity with neighbor fish.

    for (int i = 0; i < neighbours.Count; i++)
    {
        var offset = position - neighbours[i].transform.position;
        if(offset.magnitude > directionDistance) continue;
        
        avgVelocity += neighbours[i].Velocity;
        count++;
    }

  • Cohesion (SwarmBehaviour.cs:127-143): Steer toward group center.

    for (int i = 0; i < neighbours.Count; i++)
    {
        var otherPos = neighbours[i].transform.position;
        if((otherPos - position).sqrMagnitude > CohesionDistanceSq) continue;
        
        center += otherPos;
        count++;
    }

  ### 3. Force Blending & Movement (SwarmBehaviour.cs:157-185)

  Combine all forces, add noise, update physics velocity and rotation:

    var noise = (Mathf.PerlinNoise((transform.position.x + noiseOffset) * verticalNoiseFrequency, (transform.position.z + noiseOffset) * verticalNoiseFrequency) - 0.5f) * 2f;

    var direction =
        ComputeSeperation() * separationWeight +
        ComputeCohesion() * cohesionWeight +
        ComputeDirection() * directionWeight +
        ComputeCenter() * centerWeight +
        Vector3.up * (noise * verticalNoiseAmplitude);

    var newAcceleration = direction.normalized * acceleration;
    var newVelocity = rb.linearVelocity + newAcceleration * Time.deltaTime;
    rb.linearVelocity = newVelocity.normalized * speed;

## Quick Start

1. Open project in Unity (URP).
2. Open SampleScene.unity.
3. Press Play to run simulation. Adjust flock parameters on the spawner or boid prefab in the Inspector.

