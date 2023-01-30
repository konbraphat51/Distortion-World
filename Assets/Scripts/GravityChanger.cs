using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityChanger : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        GameObject opponent = other.gameObject;

        if (opponent.GetComponent<ClientPlayer>())
        {
            opponent.transform.rotation = transform.parent.rotation;
        }
    }
}
