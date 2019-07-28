using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shadow2DLight : MonoBehaviour
{
    public float range = 5.0f;
    public float intensity = 1.0f;
    public Color color = Color.white;

    //Internal fields, cache V and P matrix for this frame.
    internal Matrix4x4[] V = new Matrix4x4[4];  //Four matricies for right, down, left ,up
    internal Matrix4x4 P;

    public static List<Shadow2DLight> lights = new List<Shadow2DLight>();
    // Start is called before the first frame update
    private void OnEnable() {
        lights.Add(this);
    }

    private void OnDisable() {
        lights.Remove(this);
    }
}
