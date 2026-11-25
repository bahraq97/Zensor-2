using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.CassetteSelect
{
    public class CameraStation : MonoBehaviour
    {
        [Header("Cinemachine")] [SerializeField]
        private CinemachineCamera stationCamera;

        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 5;

        [Header("Objekte auf diesem Tisch")] [SerializeField]
        private Collider stationCollider; // Der große Klick-Bereich des Tisches

        [SerializeField] private bool isAtStation = false;

        public bool IsAtStation => isAtStation;

        void Start()
        {
            if (stationCamera != null)
                stationCamera.Priority = inactivePriority;
        }

        void Update()
        {
            if (Mouse.current == null) return;

            // Klick auf den Tisch -> Kamera gleitet hin
            if (!isAtStation && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider == stationCollider)
                {
                    EnterStation();
                }
            }
        }

        public void EnterStation()
        {
            isAtStation = true;
            stationCamera.Priority = activePriority;

            // Tisch-Collider ausmachen, damit Klicks jetzt die Kassette darunter treffen!
            if (stationCollider != null) stationCollider.enabled = false;
        }

        public void LeaveStation()
        {
            isAtStation = false;
            stationCamera.Priority = inactivePriority;

            if (stationCollider != null) stationCollider.enabled = true;
        }
    }
}