using CosmicShore.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using CosmicShore.Utility.UI;

using URandom = UnityEngine.Random;

namespace CosmicShore.Game.Arcade {

	public class SegmentSpawner : MonoBehaviour
	{
		public static readonly Dictionary<PositioningScheme, Action<SegmentSpawner, GameObject>> Schemes = new() 
		{
			{ PositioningScheme.SphereUniform, (SegmentSpawner self, GameObject spawned) => 
			{
				spawned.transform.SetPositionAndRotation(URandom.insideUnitSphere * self.Radius + self.origin + self.transform.position, URandom.rotation);
			}},
			{ PositioningScheme.SphereSurface,  (SegmentSpawner self, GameObject spawned) => 
			{
					spawned.transform.position = Quaternion.Euler(0, 0, URandom.Range(self.spawnedItemCount * (360 / self.NumberOfSegments), self.spawnedItemCount * (360 / self.NumberOfSegments) + 20)) *
						(Quaternion.Euler(0, URandom.Range(Mathf.Max(self.DifficultyAngle - 20, 40), Mathf.Max(self.DifficultyAngle + 20, 40)), 0) *
						(self.Radius * Vector3.forward)) + self.origin + self.transform.position;
					spawned.transform.LookAt(Vector3.zero);
			}},
			{ PositioningScheme.StraightLineRandomOrientation, (SegmentSpawner self, GameObject spawned) => 
			{
				spawned.transform.position = new Vector3(0, 0, self.spawnedItemCount * self.StraightLineLength) + self.origin + self.transform.position;
				spawned.transform.Rotate(Vector3.forward, URandom.value * 180);
			}},
			{ PositioningScheme.KinkyLine, (SegmentSpawner self, GameObject spawned) => 
			{
					Quaternion rotation;
					spawned.transform.position = self.currentDisplacement += self.RandomVectorRotation(self.StraightLineLength * Vector3.forward, out rotation) ;
					spawned.transform.rotation = self.currentRotation = rotation;			
			}},
			{ PositioningScheme.ToroidSurface, (SegmentSpawner self, GameObject spawned) => 
			{
					// TODO: this is not a torus, it's ripped from the sphere
					int toroidDifficultyAngle = 90;
					spawned.transform.position = Quaternion.Euler(0, 0, URandom.Range(self.spawnedItemCount * (360 / self.NumberOfSegments), self.spawnedItemCount * (360 / self.NumberOfSegments) + 20)) *
						(Quaternion.Euler(0, URandom.Range(Mathf.Max(toroidDifficultyAngle - 20, 40), Mathf.Max(toroidDifficultyAngle - 20, 40)), 0) *
						(self.Radius * Vector3.forward)) + self.origin + self.transform.position;
					spawned.transform.LookAt(Vector3.zero);
			}},
			{ PositioningScheme.Cubic, (SegmentSpawner self, GameObject spawned) => 
			{
					// Volumetric Grid, looking at origin
					var volumeSideLength = 100;
					var voxelSideLength = 10;
					var x = URandom.Range(0, volumeSideLength/voxelSideLength) * voxelSideLength;
					var y = URandom.Range(0, volumeSideLength/voxelSideLength) * voxelSideLength;
					var z = URandom.Range(0, volumeSideLength/voxelSideLength) * voxelSideLength;
					spawned.transform.position = new Vector3(x, y, z) + self.origin + self.transform.position;
					spawned.transform.LookAt(Vector3.zero, Vector3.up);
			}},
			{ PositioningScheme.SphereEmanating, (SegmentSpawner self, GameObject spawned) => 
			{
					spawned.transform.SetPositionAndRotation(self.origin + self.transform.position, URandom.rotation);
			}},
			{ PositioningScheme.StraightLineConstantRotation, (SegmentSpawner self, GameObject spawned) => 
			{
					spawned.transform.position = new Vector3(0, 0, self.spawnedItemCount * self.StraightLineLength) + self.origin + self.transform.position;
					spawned.transform.Rotate(Vector3.forward, self.spawnedItemCount * self.RotationAmount);
			}},
			{ PositioningScheme.CylinderSurfaceWithAngle, (SegmentSpawner self, GameObject spawned) => 
			{
					spawned.transform.position = new Vector3(self.Radius * Mathf.Sin(self.spawnedItemCount),
															self.Radius * Mathf.Cos(self.spawnedItemCount),
															self.spawnedItemCount * self.StraightLineLength) + self.origin + self.transform.position;
					spawned.transform.Rotate(Vector3.forward + ((URandom.value - .4f) * Vector3.right)
															+ ((URandom.value - .4f) * Vector3.up), URandom.value * 180);
			}},
			{ PositioningScheme.KinkyLineBranching, (SegmentSpawner self, GameObject spawned) => 
			{
					// Check if the maximum total spawned objects limit is reached
					if (self.spawnedItemCount >= self.maxTotalSpawnedObjects)
						return;

					// Check if the current kink should branch
					if (URandom.value < self.branchProbability && self.maxDepth > 0)
					{
						// Determine the number of branches for the current kink
						int numBranches = URandom.Range(self.minBranches, self.maxBranches + 1);

						// Spawn branches
						for (int i = 0; i < numBranches; i++)
						{
							// Calculate the branch angle
							float branchAngle = URandom.Range(self.minBranchAngle, self.maxBranchAngle);
							float branchAngleRad = branchAngle * Mathf.Deg2Rad;

							// Calculate the direction vector for the branch
							Vector3 branchDirection = Quaternion.Euler(0f, branchAngleRad * Mathf.Rad2Deg, 0f) * self.currentRotation * Vector3.forward;

							// Calculate the branch length
							float branchLengthMultiplier = URandom.Range(self.minBranchLengthMultiplier, self.maxBranchLengthMultiplier);
							float branchLength = self.StraightLineLength * branchLengthMultiplier;

							// Spawn the branch object
							GameObject branch = self.SpawnRandomBranch();
							branch.transform.position = self.currentDisplacement + branchDirection * branchLength;
							branch.transform.rotation = Quaternion.LookRotation(branchDirection);

							// Recursively spawn branches for the current branch
							self.SpawnBranches(branch, self.maxDepth - 1, branchDirection, branchLength);
						}
					}
			}},
		};
		[Header("Weighted Spanables")]
       [SerializeField]  public List<WeightedSpawnable> spawnedSegment = new();
		[SerializeField] PositioningScheme positioningScheme = PositioningScheme.SphereUniform;
		[SerializeField] Transform parent;
		
		[SerializeField] public Vector3 origin = Vector3.zero;
		GameObject SpawnedSegmentContainer;
		List<Trail> trails = new();
		int spawnedItemCount;
		public float Radius = 250f;
		public float StraightLineLength = 400f;
		public float RotationAmount = 10f;
		[HideInInspector] public int DifficultyAngle = 90;

		[SerializeField] bool InitializeOnStart;
		[SerializeField] public int NumberOfSegments = 1;

		Vector3 currentDisplacement;
		Quaternion currentRotation;

		[Header("Branching Settings")]
		[SerializeField] float branchProbability = 0.2f;
		[SerializeField] int minBranchAngle = 20;
		[SerializeField] int maxBranchAngle = 20;
		[SerializeField] int minBranches = 1;
		[SerializeField] int maxBranches = 3;
		[SerializeField] float minBranchLengthMultiplier = 0.6f;
		[SerializeField] float maxBranchLengthMultiplier = 0.8f;
		[SerializeField] int maxDepth = 3;
		[SerializeField] int maxTotalSpawnedObjects = 100;
		[SerializeField] List<GameObject> branchPrefabs;

		void Start()
		{
			currentDisplacement = origin + transform.position;
			currentRotation = Quaternion.identity;
			SpawnedSegmentContainer = new GameObject();
			SpawnedSegmentContainer.name = "SpawnedSegments";

			if (InitializeOnStart)
				Initialize();
			if (parent != null) SpawnedSegmentContainer.transform.parent = parent;
		}

		public void Initialize()
		{
			// Clear out last run
			for (int i = 0; i < trails.Count; i++)
			{
				Trail trail = trails[i];
				for (int j = 0; j < trail.TrailList.Count; j++)
				{
					Destroy(trail.TrailList[j]);
				}
			}

			NukeTheTrails();

			normalizeWeights();

			for (int i=0; i < NumberOfSegments; i++)
			{
				var spawned = SpawnRandom();
				Schemes[positioningScheme](this, spawned);
				spawned.transform.parent = SpawnedSegmentContainer.transform;
				spawnedItemCount++;
			}
		}

		public void NukeTheTrails()
		{
			trails.Clear();
			spawnedItemCount = 0;
			if (SpawnedSegmentContainer == null) return;

			foreach (Transform child in SpawnedSegmentContainer.transform)
				Destroy(child.gameObject);
		}
		GameObject SpawnRandom()
		{
			float spawnWeight = URandom.value;
			var spawnIndex = 0;
			var totalWeight = 0f;
			for (int i = 0; i < spawnedSegment.Count && totalWeight < spawnWeight; i++)
			{
				spawnIndex = i;
				totalWeight += spawnedSegment[i].Weight;
			}

			return spawnedSegment[spawnIndex].Spawnable.Spawn();
		}

		void normalizeWeights()
		{
			float totalWeight = 0;
			foreach (var spawned in spawnedSegment)
				totalWeight += spawned.Weight;

			for (int i = 0; i < spawnedSegment.Count; i++)
				spawnedSegment[i].Weight *= 1 / totalWeight;
		}

		private Vector3 RandomVectorRotation(Vector3 vector, out Quaternion rotation)
		{
			float altitude = URandom.Range(70, 90);
			float azimuth = URandom.Range(0, 360);

			rotation = Quaternion.Euler(0f, 0f, azimuth) * Quaternion.Euler(0f, altitude, 0f);
			Vector3 newVector = rotation * vector;
			return newVector;
		}

		private void SpawnBranches(GameObject parent, int depth, Vector3 direction, float length)
		{
			if (depth <= 0 || spawnedItemCount >= maxTotalSpawnedObjects)
				return;

			// Check if the current branch should spawn more branches
			if (URandom.value < branchProbability)
			{
				// Determine the number of branches for the current branch
				int numBranches = URandom.Range(minBranches, maxBranches + 1);

				// Spawn branches
				for (int i = 0; i < numBranches; i++)
				{
					// Calculate the branch angle
					float branchAngle = URandom.Range(minBranchAngle, maxBranchAngle);
					float branchAngleRad = branchAngle * Mathf.Deg2Rad;

					// Calculate the direction vector for the branch
					Vector3 branchDirection = Quaternion.Euler(0f, branchAngleRad * Mathf.Rad2Deg, 0f) * direction;

					// Calculate the branch length
					float branchLengthMultiplier = URandom.Range(minBranchLengthMultiplier, maxBranchLengthMultiplier);
					float branchLength = length * branchLengthMultiplier;

					// Spawn the branch object
					GameObject branch = SpawnRandomBranch();
					branch.transform.position = parent.transform.position + branchDirection * branchLength;
					branch.transform.rotation = Quaternion.LookRotation(branchDirection);

					// Recursively spawn branches for the current branch
					SpawnBranches(branch, depth - 1, branchDirection, branchLength);
				}
			}
		}

		private GameObject SpawnRandomBranch()
		{
			// Randomly select a branch prefab from the pool
			int randomIndex = URandom.Range(0, branchPrefabs.Count);
			GameObject branchPrefab = branchPrefabs[randomIndex];

			// Spawn the branch object
			GameObject branch = Instantiate(branchPrefab);
			branch.transform.parent = SpawnedSegmentContainer.transform;
			spawnedItemCount++;

			return branch;
		}
	}
}