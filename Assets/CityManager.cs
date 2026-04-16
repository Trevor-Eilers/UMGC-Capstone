using UnityEngine;
using UnityEngine.InputSystem;

public class CityManager : MonoBehaviour
{
    public DistrictSpawner[] districts;

    private bool batch1Done = false;
    private bool batch2Done = false;
    private bool batch3Done = false;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame && !batch1Done)
        {
            foreach (var d in districts)
                d.SpawnBatch1();

            batch1Done = true;
        }

        if (keyboard.digit2Key.wasPressedThisFrame && !batch2Done)
        {
            foreach (var d in districts)
                d.SpawnBatch2();

            batch2Done = true;
        }

        if (keyboard.digit3Key.wasPressedThisFrame && !batch3Done)
        {
            foreach (var d in districts)
                d.SpawnBatch3();

            batch3Done = true;
        }
    }
}
