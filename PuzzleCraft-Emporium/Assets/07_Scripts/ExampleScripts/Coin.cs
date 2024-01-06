using UnityEngine;
using System;


public enum CoinType
{
    Regular,
    Key,
    Chest
}


public class Coin : MonoBehaviour
{
    [SerializeField]
    private int _value;
    public event Action<int> OnValueChanged;
    public int Value 
    {
        get { return _value; }
        private set 
        { 
            if (_value != value)
            {
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }
    }
    public void SetValue(int newValue)
    {
        Value = newValue;
    }
    private CoinType _type = CoinType.Regular;
    public CoinType Type
    { 
        get { return _type; }
        set { _type = value; }
    }
    public bool IsMoving { get; set; } = false;
}
