using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{

    List<EffectBase> effectList = new List<EffectBase>();
    public List<EffectBase> EffectList { get => effectList; }

    public int EffectCount { get => effectList.Count; }

    void Awake()
    {
        for(int i=0; i<transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            string name = child.name;
            EffectBase effect = child.GetComponent<EffectBase>();
            if(effect == null)
            {
                Debug.LogError($"[{this.GetType()}] Can't find EffectBase in child: {name}");
            }
            else
            {
                effectList.Add(effect);
            }
        }
    }


    public void StartEffect(int effect_index)
    {
        if (effectList == null || effect_index < 0 || effect_index > EffectCount - 1)
            return;
        effectList[effect_index].StartEffect();
    }

    public void StopEffect(int effect_index)
    {
        if (effectList == null || effect_index < 0 || effect_index > EffectCount - 1)
            return;
        effectList[effect_index].StopEffect();
    }

    public void StopAllEffect()
    {
        foreach(var effect in effectList)
        {
            if (effect == null || effect.IsOn == false)
                continue;

            effect.StopEffect();
        }
    }

    public bool IsEffectOn(int effect_index)
    {
        if (effectList == null || effect_index < 0 || effect_index > EffectCount - 1)
            return false;

        return effectList[effect_index].IsOn;
    }
}
