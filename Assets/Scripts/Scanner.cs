// Scanner.cs
using UnityEngine;
using UnityEngine.UI;

public class Scanner : MonoBehaviour
{
    [SerializeField] private float scanRange = 10f;
    [SerializeField] private float scanTime = 3f;
    
    // UI elements
    [SerializeField] private GameObject scanIndicatorIcon;
    [SerializeField] private Image scanProgressRadial;
    
    private Camera mainCamera;
    private float currentScanTime = 0f;
    private Creature currentTarget;

    private void Start()
    {
        mainCamera = Camera.main;
        scanIndicatorIcon.SetActive(false);
        scanProgressRadial.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Cast ray from camera center
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Use Physics.Raycast without layer mask
        if (Physics.Raycast(ray, out hit, scanRange))
        {
            // Check if hit object has "Creature" tag
            if (hit.collider.CompareTag("Creature"))
            {
                // Get the Creature component from the parent of the hit object
                Creature creature = hit.collider.GetComponentInParent<Creature>();
                if (creature != null)
                {
                    // Show scan indicator
                    scanIndicatorIcon.SetActive(true);
                    currentTarget = creature;

                    // Handle scanning
                    if (Input.GetMouseButton(0))
                    {
                        scanProgressRadial.gameObject.SetActive(true);
                        currentScanTime += Time.deltaTime;
                        scanProgressRadial.fillAmount = currentScanTime / scanTime;

                        // Complete scan
                        if (currentScanTime >= scanTime)
                        {
                            currentTarget.OnScanned();
                            ResetScanner();
                        }
                    }
                    else
                    {
                        ResetScanner();
                    }
                }
            }
            else
            {
                scanIndicatorIcon.SetActive(false);
                if (!Input.GetMouseButton(0))
                {
                    ResetScanner();
                }
            }
        }
        else
        {
            scanIndicatorIcon.SetActive(false);
            if (!Input.GetMouseButton(0))
            {
                ResetScanner();
            }
        }
    }

    private void ResetScanner()
    {
        currentScanTime = 0f;
        scanProgressRadial.gameObject.SetActive(false);
        scanProgressRadial.fillAmount = 0f;
    }
}