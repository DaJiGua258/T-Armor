using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunColor : MonoBehaviour
{
    [ColorUsage(true, true)]
    public Color sunColor;
    public Material mat;
    public Transform planetTrans;
    public Transform sunTrans;

    void Awake()
    {
        mat = transform.GetComponent<MeshRenderer>().sharedMaterial;
    }


    // Start is called before the first frame update
    void Start()
    {
        mat.SetColor("_Color", sunColor);
    }

    // Update is called once per frame
    void Update()
    {
        sunTrans.LookAt(planetTrans, Vector3.up);
    }

     private void OnValidate()
    {
        mat = transform.GetComponent<MeshRenderer>().sharedMaterial;
        sunTrans.LookAt(planetTrans, Vector3.up);
        mat.SetColor("_Color", sunColor);
    }
}
