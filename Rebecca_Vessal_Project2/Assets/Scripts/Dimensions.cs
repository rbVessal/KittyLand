using UnityEngine;
using System.Collections;

public class Dimensions : MonoBehaviour
{

	private float radius;
	private float height;

	// Use this for initialization
	void Start ()
	{
        // radius approach A
		MeshRenderer renderer = gameObject.GetComponentInChildren<MeshRenderer>();
		float x = renderer.bounds.extents.x;
		float z = renderer.bounds.extents.z;
		float renderRadius = Mathf.Sqrt (x * x + z * z);
		Debug.Log ("renderRadius = " + renderRadius);
		
        // radius approach B
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		x = mesh.bounds.extents.x * transform.localScale.x;
		z = mesh.bounds.extents.z * transform.localScale.z;
		float meshRadius = Mathf.Sqrt (x * x + z * z);
		Debug.Log ("meshRadius = " + meshRadius);

        // approaches A and B yeild different results
        // when using meshes imported from 3D Modelling programs
        // experiment to see which one is desirable for your application
        // (usually renderer!)
		
		height = mesh.bounds.size.y;
		radius = meshRadius;
	}


    // public getters do not show up in the Inspector
    // but will be available to other scripts
	public float Radius {
		get { return radius; }
	}
	public float Height {
		get { return height; }
	}
	
}
