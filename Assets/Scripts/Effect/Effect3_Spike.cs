using UnityEngine;

public class Effect0 : EffectBase
{


    public override void StartEffect()
    {
        base.StartEffect();

        // randomize color for testing
        if (player.OwnerClientId % 3 == 0) effectColor = Color.red;
        else if (player.OwnerClientId % 3 == 1) effectColor = Color.green;
        else effectColor = Color.blue;
    }

    public override void StopEffect()
    {
        base.StopEffect();
    }

    public override void UpdateVFXParameter()
    {
        base.UpdateVFXParameter();

        // set custom parameter
        // 
    }
}
