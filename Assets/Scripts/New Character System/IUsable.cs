using CharacterSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUsable 
{
    // Start is called before the first frame update
    public void Interact(PlayerController player);
}
