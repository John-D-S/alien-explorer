using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Creature", menuName = "ScriptableObjects/Creature Data")]
public class CreatureData : ScriptableObject
{
	public Sprite creatureSprite;
    
	public enum Locomotion
	{
		Walking,
		Swimming,
		Flying
	}
    
	public enum Ability
	{
		Dashing,
		Jumping,
		Flying,
		Swimming,
		Hot,
		Cold,
		CutPlants,
		SmashRocks
	}
    
	public enum Habitat
	{
		Desert,
		Rain,
		Ice,
		Temperate,
		Tropical
	}
    
	public Locomotion locomotionType;
	public Ability ability;
	public Habitat habitat;
	[TextArea(3, 10)]
	public string infoBlurb;

	#if UNITY_EDITOR
	private Sprite lastSprite;

	private void OnValidate()
	{
		// Only proceed if the sprite has actually changed
		if (creatureSprite != lastSprite)
		{
			lastSprite = creatureSprite;
            
			if (creatureSprite != null)
			{
				string assetPath = AssetDatabase.GetAssetPath(this);
				if (!string.IsNullOrEmpty(assetPath))
				{
					string newName = creatureSprite.name;
                    
					// Prevent rename if it's already named correctly
					if (name != newName)
					{
						// Defer the rename to avoid validation issues
						EditorApplication.delayCall += () =>
						{
							if (this != null)  // Check if asset still exists
							{
								AssetDatabase.RenameAsset(assetPath, newName);
							}
						};
					}
				}
			}
		}
	}
	#endif
}