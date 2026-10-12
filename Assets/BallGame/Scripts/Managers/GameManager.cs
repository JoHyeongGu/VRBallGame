using UnityEngine;
using Unity.XR.CoreUtils;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("<color=yellow>Player Settings</color>")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private float targetHeight = 1.7f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SetPlayerHeight();
    }

    private void SetPlayerHeight()
    {
        if (xrOrigin == null) return;

        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        xrOrigin.CameraYOffset = targetHeight;
    }
}