using UnityEngine;
using UnityEngine.UIElements;

public class PolicyValues : MonoBehaviour
{
    private UIDocument _doc;
    private VisualElement _root;

    public float taxRate = 0;
    public float eduRate = 0;
    public float infraRate = 0;
    public float housingRate = 0;
    public float envRate = 0;
    public float cityRate = 0;

    void Start()
    {
        _doc = GetComponent<UIDocument>();
        _root = _doc.rootVisualElement;

        _root.Q<Slider>("TaxSlider").RegisterValueChangedCallback(OnSliderValueChanged);
        _root.Q<Slider>("EduSlider").RegisterValueChangedCallback(OnSliderValueChanged);
        _root.Q<Slider>("InfraSlider").RegisterValueChangedCallback(OnSliderValueChanged);
        _root.Q<Slider>("HousingSlider").RegisterValueChangedCallback(OnSliderValueChanged);
        _root.Q<Slider>("EnvSlider").RegisterValueChangedCallback(OnSliderValueChanged);
        _root.Q<Slider>("CitySlider").RegisterValueChangedCallback(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(ChangeEvent<float> evt)
    {
        string slider = (evt.target as VisualElement)?.name;

        switch (slider)
        {
            case "TaxSlider": taxRate = evt.newValue; break;
            case "EduSlider": eduRate = evt.newValue; break;
            case "InfraSlider": infraRate = evt.newValue; break;
            case "HousingSlider": housingRate = evt.newValue; break;
            case "EnvSlider": envRate = evt.newValue; break;
            case "CitySlider": cityRate = evt.newValue; break;
            default: Debug.LogWarning($"Unknown element: {slider}"); break;
        }
    }
}