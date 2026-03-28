using UnityEngine;

public class PlayerEffects : MonoBehaviour
{
    public GameObject attackEffect;

    public void PlayAttackEffect()
    {
        attackEffect.SetActive(true);
    }
    
    public void StopAttackEffect()
    {
        attackEffect.SetActive(false);
    }
}
