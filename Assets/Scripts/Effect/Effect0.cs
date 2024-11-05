using UnityEngine;

public class Effect0 : EffectBase
{


    public override void StartEffect()
    {
        base.StartEffect();

        // initialize effect
        needPushBuffer = true;
        needPushHumanStencil = false;
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
