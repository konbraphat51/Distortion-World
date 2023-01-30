using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravityChanger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnTriggerEnter(Collider other)
    {
        GameObject opponent = other.gameObject;

        if (opponent.GetComponent<ClientPlayer>())
        {
            opponent.transform.rotation = transform.parent.rotation;
        }
    }
}
