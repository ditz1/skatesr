using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public CinemachineCamera virtualCamera;
    public BoardController boardController;

    bool should_follow_player = false;

    Vector3 default_cam_offset = new Vector3(0.5f, 2.3f, -1f); 
    Vector3 lookat_player_offset = new Vector3(1.5f, 1.0f, 3f);
    float default_cam_rotation = 15f;
    float lookat_player_rotation = -150f;

    private CinemachineFollow followComponent;

    public Button start_button;
    public GameObject score_bug;

    void Start()
    {
        score_bug.SetActive(false);
        // Get the follow component (replaces Transposer in Cinemachine 3.x)
        followComponent = virtualCamera.GetComponent<CinemachineFollow>();
        
        // Set up the follow target
        virtualCamera.Follow = boardController.transform;
        
        // Start with the "look at player" offset (front view)
        followComponent.FollowOffset = lookat_player_offset;
        
        // Set initial rotation
        virtualCamera.transform.rotation = Quaternion.Euler(0, lookat_player_rotation, 0);
    }

    void Update()
    {
        if (should_follow_player)
        {
            // Smoothly transition to the default follow offset (behind player)
            followComponent.FollowOffset = Vector3.Lerp(
                followComponent.FollowOffset, 
                default_cam_offset, 
                Time.deltaTime * 2f
            );
            
            // Smoothly transition rotation
            virtualCamera.transform.rotation = Quaternion.Lerp(
                virtualCamera.transform.rotation,
                Quaternion.Euler(default_cam_rotation, 0, 0),
                Time.deltaTime * 2f
            );
        }
    }

    public void StartGame()
    {
        should_follow_player = true;
        boardController.can_play = true;
        start_button.gameObject.SetActive(false);
        score_bug.SetActive(true);
    }
}