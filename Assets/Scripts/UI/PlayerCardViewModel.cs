using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Simulation;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    [CreateAssetMenu(fileName = "PlayerCardViewModel", menuName = "Player Card View Model")]
    public class PlayerCardViewModel : ScriptableObject, INotifyBindablePropertyChanged, IDistrictBoundViewModel
    {
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        [SerializeField] private string _playerName;
        [SerializeField] private float _gdp;
        [SerializeField] private float _happiness;
        [SerializeField] private float _population;
        [SerializeField] private float _infrastructure;
        [SerializeField] private float _sustainability;
        [SerializeField] private float _pollution;

        [CreateProperty] public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        [CreateProperty] public float Gdp
        {
            get => _gdp;
            set => SetProperty(ref _gdp, value);
        }

        [CreateProperty] public float Happiness
        {
            get => _happiness;
            set => SetProperty(ref _happiness, value);
        }

        [CreateProperty] public float Population
        {
            get => _population;
            set => SetProperty(ref _population, value);
        }

        [CreateProperty] public float Infrastructure
        {
            get => _infrastructure;
            set => SetProperty(ref _infrastructure, value);
        }

        [CreateProperty] public float Sustainability
        {
            get => _sustainability;
            set => SetProperty(ref _sustainability, value);
        }

        [CreateProperty] public float Pollution
        {
            get => _pollution;
            set => SetProperty(ref _pollution, value);
        }

        public void BindToPanel(VisualElement root) { }

        public void UpdateFromDistrictState(DistrictState state)
        {
            Gdp            = state.gdp;
            Happiness      = state.happiness;
            Population     = state.population;
            Infrastructure = state.infrastructure;
            Sustainability = state.sustainability;
            Pollution = state.pollution;
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Notify(name);
            return true;
        }

        private void Notify([CallerMemberName] string property = "")
        {
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property));
        }
    }
}
