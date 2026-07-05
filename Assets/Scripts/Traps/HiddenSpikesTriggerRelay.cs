using UnityEngine;

public class HiddenSpikesTriggerRelay : MonoBehaviour
{
    [SerializeField] private HiddenSpikes owner;

    public void SetOwner(HiddenSpikes spikesOwner)
    {
        owner = spikesOwner;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (owner != null)
        {
            owner.HandlePotentialTrigger(collision);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (owner != null)
        {
            owner.HandlePotentialDamage(collision);
        }
    }
}
