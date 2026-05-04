using UnityEngine;

public class LODSetter : MonoBehaviour
{
    void Start()
    {
        // Get all LODGroup components in children (including inactive)
        LODGroup[] lodGroups = GetComponentsInChildren<LODGroup>(true);

        foreach (var lodGroup in lodGroups)
        {
            // Force the LODGroup to always use LOD index 1
            lodGroup.ForceLOD(1);
        }
    }
}
