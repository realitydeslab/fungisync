using System.Collections.Generic;
using UnityEngine;

public class EffectManager : MonoBehaviour
{

    Dictionary<string, EffectBase> effectList = new Dictionary<string, EffectBase>();
    public Dictionary<string, EffectBase> EffectList { get => effectList; }

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
                effectList.Add(name, effect);
            }
        }
    }


    public void StartEffect(int effect_index)
    {
        
    }
    
}
