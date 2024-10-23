using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace CharacterSystem
{
    public class Breakable : MonoBehaviour
    {
        // Start is called before the first frame update
        public enum BreakableType
        {
            Smash,
            Cut
        }
        public BreakableType breakableType;
        public void Break()
        {
            //temp
            GameObject.Destroy(gameObject);
        }
    }
}