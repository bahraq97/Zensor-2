using Core.VinylSelect;
using UnityEngine;
using UnityEngine.UI;

public class RecordInsertsUI : MonoBehaviour
{
    [SerializeField] private VinylSelectController controller;
    [SerializeField] private Button openButton;
    [SerializeField] private GameObject panel;
    [SerializeField] private Image insertImage;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button closeButton;

    private int currentIndex;

    private void Start()
    {
        if (controller == null || openButton == null || panel == null ||
            insertImage == null || previousButton == null ||
            nextButton == null || closeButton == null)
        {
            Debug.LogError(
                "RecordInsertsUI: Bitte alle Felder im Inspector zuweisen.",
                this
            );
            enabled = false;
            return;
        }

        panel.SetActive(false);
        openButton.onClick.AddListener(Open);
        previousButton.onClick.AddListener(Previous);
        nextButton.onClick.AddListener(Next);
        closeButton.onClick.AddListener(Close);
        insertImage.preserveAspect = true;
    }

    private void Update()
    {
        RecordData data = controller.SelectedVinyl?.GetData();
        bool hasInserts = data != null &&
                          data.inserts != null &&
                          data.inserts.Count > 0;

        bool canOpen = controller.CurrentVinylState == VinylState.VinylSelected
                       && hasInserts;

        openButton.gameObject.SetActive(canOpen);

        if (controller.CurrentVinylState != VinylState.InsertsOpen &&
            panel.activeSelf)
        {
            panel.SetActive(false);
        }
    }

    private void Open()
    {
        RecordData data = controller.SelectedVinyl?.GetData();
        if (data == null || data.inserts == null || data.inserts.Count == 0)
            return;

        if (!controller.OpenInserts())
            return;

        currentIndex = 0;
        panel.SetActive(true);
        ShowCurrentImage();
    }

    private void Close()
    {
        if (controller.CloseInserts())
            panel.SetActive(false);
    }

    private void Previous()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            ShowCurrentImage();
        }
    }

    private void Next()
    {
        RecordData data = controller.SelectedVinyl?.GetData();
        if (data != null && currentIndex < data.inserts.Count - 1)
        {
            currentIndex++;
            ShowCurrentImage();
        }
    }

    private void ShowCurrentImage()
    {
        RecordData data = controller.SelectedVinyl?.GetData();
        if (data == null || data.inserts == null ||
            currentIndex < 0 || currentIndex >= data.inserts.Count)
        {
            Debug.LogError("Beilagenbild konnte nicht gefunden werden.", this);
            return;
        }

        Sprite sprite = data.inserts[currentIndex];
        if (sprite == null)
            Debug.LogError("In der Beilagenliste fehlt ein Bild.", data);

        insertImage.sprite = sprite;
        previousButton.interactable = currentIndex > 0;
        nextButton.interactable = currentIndex < data.inserts.Count - 1;
    }
}