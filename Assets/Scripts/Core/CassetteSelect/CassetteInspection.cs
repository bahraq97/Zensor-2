using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.CassetteSelect
{
    public class CassetteInspection : MonoBehaviour
    {
        [Header("Verbindung zur Station")] 
        [SerializeField] private CameraStation parentStation;

        [Header("Inspection")] 
        [SerializeField] private Transform inspectPoint; // Empty vor der Main Camera

        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float rotateSpeed = 0.4f;

        [Header("Deckel & Kassette")] 
        [SerializeField] private Transform lidTransform;

        [SerializeField] private Transform cassetteTransform;
        [SerializeField] private Transform cassetteInspectPoint; // Optional: Näherer Punkt für die Kassette allein
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float openSpeed = 5f;

        private bool isInspecting = false;
        private bool isLidOpen = false;
        private bool isCassetteTaken = false;

        // Ursprungsdaten Case
        private Vector3 originalPos;
        private Quaternion originalRot;
        private Quaternion targetInspectRot;

        // Ursprungsdaten Deckel & Kassette
        private Quaternion closedLidRot;
        private Quaternion openLidRot;
        private Transform originalCassetteParent;
        private Vector3 originalCassetteLocalPos;
        private Quaternion originalCassetteLocalRot;
        private Quaternion targetCassetteRot;

        void Start()
        {
            originalPos = transform.position;
            originalRot = transform.rotation;

            if (lidTransform != null)
            {
                closedLidRot = lidTransform.localRotation;
                openLidRot = closedLidRot * Quaternion.Euler(0f, 0f, openAngle);
            }

            if (cassetteTransform != null)
            {
                // Speichert den exakten Platz der Kassette in den Halterungen des Deckels
                originalCassetteParent = cassetteTransform.parent;
                originalCassetteLocalPos = cassetteTransform.localPosition;
                originalCassetteLocalRot = cassetteTransform.localRotation;
            }
        }

        void Update()
        {
            if (Mouse.current == null) return;
            if (parentStation && !parentStation.IsAtStation) return;

            // 1. Linksklick-Logik
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    bool hitCassette = cassetteTransform != null &&
                                       (hit.transform == cassetteTransform ||
                                        hit.transform.IsChildOf(cassetteTransform));
                    bool hitCase = hit.transform == transform || hit.transform.IsChildOf(transform);

                    if (!isInspecting && hitCase)
                    {
                        StartInspection();
                    }
                    else if (isInspecting && !isCassetteTaken)
                    {
                        // Wenn der Deckel offen ist und direkt auf die Kassette geklickt wird -> Rausholen!
                        if (isLidOpen && hitCassette)
                        {
                            TakeOutCassette();
                        }
                        // Sonst Deckel auf- oder zuklappen
                        else if (hitCase)
                        {
                            isLidOpen = !isLidOpen;
                        }
                    }
                }
            }

            // 2. Skyrim-Rotation mit rechter Maustaste (dreht jeweils das aktive Objekt)
            if (isInspecting && Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                Transform cam = Camera.main.transform;

                if (isCassetteTaken)
                {
                    // Dreht nur die herausgenommene Kassette
                    targetCassetteRot = Quaternion.AngleAxis(-delta.x * rotateSpeed, cam.up) * targetCassetteRot;
                    targetCassetteRot = Quaternion.AngleAxis(delta.y * rotateSpeed, cam.right) * targetCassetteRot;
                }
                else
                {
                    // Dreht das komplette Case
                    targetInspectRot = Quaternion.AngleAxis(-delta.x * rotateSpeed, cam.up) * targetInspectRot;
                    targetInspectRot = Quaternion.AngleAxis(delta.y * rotateSpeed, cam.right) * targetInspectRot;
                }
            }

            // 3. Schrittweises Zurücklegen mit ESC
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (isCassetteTaken) PutCassetteBack();
                else if (isInspecting) StopInspection();
                else if (parentStation != null) parentStation.LeaveStation();
            }

            // 4. Bewegungen & Animationen ausführen
            UpdateTransforms();
        }

        private void UpdateTransforms()
        {
            // Case-Bewegung (weicht leicht nach unten/hinten aus, wenn die Kassette allein betrachtet wird)
            Vector3 goalPos = isInspecting ? inspectPoint.position : originalPos;
            if (isCassetteTaken) goalPos += -Camera.main.transform.up * 3.18f; // Case sinkt leicht ab

            Quaternion goalRot = isInspecting ? targetInspectRot : originalRot;

            transform.position = Vector3.Lerp(transform.position, goalPos, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, goalRot, Time.deltaTime * moveSpeed);

            // Deckel-Rotation
            if (lidTransform != null)
            {
                Quaternion goalLid = isLidOpen ? openLidRot : closedLidRot;
                lidTransform.localRotation =
                    Quaternion.Slerp(lidTransform.localRotation, goalLid, Time.deltaTime * openSpeed);
            }

            // Kassette schwebt vor die Kamera, wenn sie herausgenommen wurde
            if (isCassetteTaken && cassetteTransform != null)
            {
                Transform targetPoint = cassetteInspectPoint != null ? cassetteInspectPoint : inspectPoint;
                cassetteTransform.position = Vector3.Lerp(cassetteTransform.position, targetPoint.position,
                    Time.deltaTime * moveSpeed);
                cassetteTransform.rotation = Quaternion.Slerp(cassetteTransform.rotation, targetCassetteRot,
                    Time.deltaTime * moveSpeed);
            }
        }

        public void TakeOutCassette()
        {
            Debug.Log("Taking out cassette");
            isCassetteTaken = true;
            // Aus der Deckel-Hierarchie lösen, damit sie sich frei bewegen kann
            cassetteTransform.SetParent(null);

            Transform targetPoint = cassetteInspectPoint != null ? cassetteInspectPoint : inspectPoint;
            targetCassetteRot = targetPoint.rotation;
        }

        public void PutCassetteBack()
        {
            isCassetteTaken = false;
            // Wieder exakt in die Halterung des Deckels einhängen
            cassetteTransform.SetParent(originalCassetteParent);
            cassetteTransform.localPosition = originalCassetteLocalPos;
            cassetteTransform.localRotation = originalCassetteLocalRot;
        }

        private void StartInspection()
        {
            isInspecting = true;
            targetInspectRot = inspectPoint.rotation;
        }

        private void StopInspection()
        {
            if (isCassetteTaken) PutCassetteBack();
            isInspecting = false;
            isLidOpen = false;
        }
    }
}