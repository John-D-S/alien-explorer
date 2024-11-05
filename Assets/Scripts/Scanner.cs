using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using CharacterSystem;

public class Scanner : MonoBehaviour
{
    [SerializeField] private float scanRange = 10f;
    [SerializeField] private float scanTime = 3f;
    
    // UI elements
    [SerializeField] private GameObject scanIndicatorIcon;
    [SerializeField] private GameObject alreadyScannedIcon; // New icon for already scanned creatures
    [SerializeField] private Image scanProgressRadial;
    
    private Camera mainCamera;
    private float currentScanTime = 0f;
    private Creature currentTarget;
    public UpgradeManager upgradeManager;
    // List of scanned creature types
    private List<CreatureData> scannedCreatures = new List<CreatureData>();

    private void Start()
    {
        mainCamera = Camera.main;
        scanIndicatorIcon.SetActive(false);
        alreadyScannedIcon.SetActive(false);
        scanProgressRadial.gameObject.SetActive(false);
    }

    private void Update()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, scanRange))
        {
            if (hit.collider.CompareTag("Creature"))
            {
                Creature creature = hit.collider.GetComponentInParent<Creature>();
                if (creature != null)
                {
                    // Check if this creature type has already been scanned
                    if (HasScannedCreature(creature.creatureData))
                    {
                        // Show already scanned icon
                        alreadyScannedIcon.SetActive(true);
                        scanIndicatorIcon.SetActive(false);
                        scanProgressRadial.gameObject.SetActive(false);
                    }
                    else
                    {
                        // Show scan indicator and handle scanning
                        alreadyScannedIcon.SetActive(false);
                        scanIndicatorIcon.SetActive(true);
                        currentTarget = creature;

                        if (Input.GetMouseButton(0))
                        {
                            scanProgressRadial.gameObject.SetActive(true);
                            currentScanTime += Time.deltaTime;
                            scanProgressRadial.fillAmount = currentScanTime / scanTime;

                            if (currentScanTime >= scanTime)
                            {
                                CompleteScanning(creature);
                            }
                        }
                        else
                        {
                            ResetScanner();
                        }
                    }
                }
            }
            else
            {
                HideAllIcons();
            }
        }
        else
        {
            HideAllIcons();
        }
    }

    private void CompleteScanning(Creature creature)
    {
        if (!HasScannedCreature(creature.creatureData))
        {
            scannedCreatures.Add(creature.creatureData);
            creature.OnScanned();
            
            // Find and disable scan masks for all creatures of this type
            Creature[] allCreatures = FindObjectsOfType<Creature>();
            foreach (Creature c in allCreatures)
            {
                if (c.creatureData == creature.creatureData)
                {
                    c.OnScanned();
                }
            }
            upgradeManager.CollectToken(creature.creatureData.ability); 
        }
        ResetScanner();
    }

    private void HideAllIcons()
    {
        scanIndicatorIcon.SetActive(false);
        alreadyScannedIcon.SetActive(false);
        if (!Input.GetMouseButton(0))
        {
            ResetScanner();
        }
    }

    private void ResetScanner()
    {
        currentScanTime = 0f;
        scanProgressRadial.gameObject.SetActive(false);
        scanProgressRadial.fillAmount = 0f;
    }

    public bool HasScannedCreature(CreatureData creatureData)
    {
        return scannedCreatures.Contains(creatureData);
    }
}