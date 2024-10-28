using UnityEngine;

/// <summary>
/// Unpacks all children of this GameObject by setting their parent to null,
/// and then destroys this GameObject.
/// </summary>
public class UnpackChildren : MonoBehaviour
{
    void Start()
    {
        Unpack();
    }

    public void Unpack()
    {
        Transform[] childTransforms = new Transform[transform.childCount];
        for (int i = 0; i < childTransforms.Length; i++)
        {
            childTransforms[i] = transform.GetChild(i);
        }

        // Unparent each child
        foreach (Transform child in childTransforms)
        {
            child.SetParent(null);
        }

        // Destroy this GameObject
        Destroy(gameObject);
    }
}