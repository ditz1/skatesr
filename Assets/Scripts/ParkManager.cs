using System.Collections.Generic;
using UnityEngine;

public class ParkManager : MonoBehaviour
{
    public GameObject[] parkPrefabs; // Changed to array
    public Transform skaterTransform;
    public GameObject initialPark; // Assign the park already in the scene
    public int maxParkRenders = 4;
    public float despawnDistance = 50f;

    private Queue<GameObject> activeParkQueue = new Queue<GameObject>();
    private int currentPrefabIndex = 0; // Track which prefab to spawn next

    void Start()
    {
        // Validate we have prefabs
        if (parkPrefabs == null || parkPrefabs.Length == 0)
        {
            Debug.LogError("No park prefabs assigned to ParkManager!");
            return;
        }

        // If there's an initial park, add it to the queue first
        if (initialPark != null)
        {
            activeParkQueue.Enqueue(initialPark);
            
            // Spawn the remaining parks
            for (int i = 1; i < maxParkRenders; i++)
            {
                ExtendPark();
            }
        }
        else
        {
            // No initial park, spawn all parks fresh
            for (int i = 0; i < maxParkRenders; i++)
            {
                ExtendPark();
            }
        }
    }

    void Update()
    {
        CheckIfNeedToExtendPark();
    }

    void CheckIfNeedToExtendPark()
    {
        if (activeParkQueue.Count == 0) return;

        // Get the first (oldest) park in the queue
        GameObject firstPark = activeParkQueue.Peek();
        
        // Check if skater has passed this park by the despawn distance
        Transform connection_point = firstPark.transform.Find("connection");
        if (skaterTransform.position.z > connection_point.position.z + despawnDistance)
        {
            // Remove the old park
            GameObject parkToRemove = activeParkQueue.Dequeue();
            Destroy(parkToRemove);
            
            // Spawn a new park at the end
            ExtendPark();
        }
    }

    void ExtendPark()
    {
        // Get the next prefab in rotation
        GameObject prefabToSpawn = parkPrefabs[currentPrefabIndex];
        
        // Increment and wrap around
        currentPrefabIndex = (currentPrefabIndex + 1) % parkPrefabs.Length;

        GameObject newPark;
        
        if (activeParkQueue.Count == 0)
        {
            // First park spawns at the manager's position
            newPark = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
        }
        else
        {
            // Get the last park's connection point
            GameObject lastPark = GetLastPark();
            Transform connectionPoint = lastPark.transform.Find("connection");
            
            if (connectionPoint != null)
            {
                // Spawn new park at the connection point
                newPark = Instantiate(prefabToSpawn, connectionPoint.position, connectionPoint.rotation);
            }
            else
            {
                Debug.LogError("Connection point 'connection' not found on park prefab!");
                return;
            }
        }
        
        activeParkQueue.Enqueue(newPark);
    }

    GameObject GetLastPark()
    {
        GameObject lastPark = null;
        foreach (GameObject park in activeParkQueue)
        {
            lastPark = park;
        }
        return lastPark;
    }
}