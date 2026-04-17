using UnityEngine;

public class DistrictController : MonoBehaviour
{
    [Header("Tier Values (1 = Low, 2 = Mid, 3 = High)")]
    public int residentialTier = 0;
    public int industrialTier = 0;
    public int commercialTier = 0;
    public int civicTier = 0;

    [Header("Residential Blocks")]
    public GameObject rTier1;
    public GameObject rTier2;
    public GameObject rTier3;

    [Header("Industrial Blocks")]
    public GameObject iTier1;
    public GameObject iTier2;
    public GameObject iTier3;

    [Header("Commercial Blocks")]
    public GameObject cTier1;
    public GameObject cTier2;
    public GameObject cTier3;

    [Header("Civic Blocks")]
    public GameObject civicTier1;
    public GameObject civicTier2;
    public GameObject civicTier3;

    void Start()
    {
        ApplyAll();
    }

    void Update()
    {
        ApplyAll(); // change values live in Inspector
    }

    void ApplyAll()
    {
        ApplyTier(residentialTier, rTier1, rTier2, rTier3);
        ApplyTier(industrialTier, iTier1, iTier2, iTier3);
        ApplyTier(commercialTier, cTier1, cTier2, cTier3);
        ApplyTier(civicTier, civicTier1, civicTier2, civicTier3);
    }

    void ApplyTier(int tier, GameObject t1, GameObject t2, GameObject t3)
    {
        if (t1 != null) t1.SetActive(tier >= 1);
        if (t2 != null) t2.SetActive(tier >= 2);
        if (t3 != null) t3.SetActive(tier >= 3);
    }
}