using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//Invoke each UnityEvent when corresponding key pushed
public class InputManager : MonoBehaviour
{
    [SerializeField] private UnityEvent actionsW;
    [SerializeField] private UnityEvent actionsA;
    [SerializeField] private UnityEvent actionsS;
    [SerializeField] private UnityEvent actionsD;
    [SerializeField] private UnityEvent actionsSpace;
}
