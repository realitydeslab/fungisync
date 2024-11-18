using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.XR.ARFoundation;
using Xiaobo.UnityToolkit.Core;

public class EffectBase : MonoBehaviour
{
    [SerializeField]
    protected VisualEffect vfx = null;
    [SerializeField]
    protected Material mat = null;

    protected PlayerManager playerManager;
    protected Player player;

    [SerializeField] protected bool needPushBuffer = false;
    //MeshToBufferConvertor meshToBufferConverter;
    [SerializeField] protected bool needPushHitPoint = false;
    //MeshingRaycaster meshingRaycaster;
    [SerializeField] protected bool needPushHumanStencil = false;
    //DepthImageProcessor depthImageProcessor;
    [SerializeField] protected bool needPushAudioData = false;

    MeshingMaterialManager meshingMaterialManager;

    EnvironmentProbe environmentProbe;

    protected int effectIndex = -1;

    public bool IsOn { get => isOn; }
    protected bool isOn = false;
    protected float effectAlpha = 0;

    public Color effectColor = Color.white;

    protected Vector2 effectRange = Vector2.zero;    
    public float effectMaxRange = 30;
    public float effectWidth = 2;

    protected float marchingDistance = 0;
    public Remap.MapMode marchingMode = Remap.MapMode.Wrap;
    public float marchingSpeed = 1;

    void Awake()
    {
        playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find PlayerManager");
        }


        environmentProbe = FindFirstObjectByType<EnvironmentProbe>();
        if(environmentProbe == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find EnvironmentProbe");
        }

        meshingMaterialManager = FindFirstObjectByType<MeshingMaterialManager>();
        if (meshingMaterialManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find MeshingMaterialManager");
        }


        effectIndex = transform.GetSiblingIndex();

        if (vfx != null)
        {
            vfx.enabled = false;
        }
    }

    public virtual void StartEffect()
    {
        if (isOn)
            return;

        // initialize variables
        isOn = true;
        marchingDistance = 0;
        effectRange = Vector2.zero;

        // initialize local player
        player = playerManager.ActivePlayer;

        // start vfx
        StartVFXEffect();


        // start material
        StartMaterialEffect();



        Debug.Log($"Player(id={player.OwnerClientId})Start Effect:{effectIndex} ({this.GetType()})");
    }

    public virtual void StopEffect()
    {
        if (isOn == false)
            return;

        // initialize variables
        isOn = false;
        marchingDistance = 0;
        effectRange = Vector2.zero;

        // stop vfx
        StopVFXEffect();


        // stop material
        StopMaterialEffect();

        Debug.Log($"Player(id={player.OwnerClientId})Stop Effect:{effectIndex} ({this.GetType()})");

        player = null;
    }

    void StartVFXEffect()
    {
        if (vfx == null)
            return;

        vfx.enabled = true;
    }

    void StopVFXEffect()
    {
        if (vfx == null)
            return;

        vfx.enabled = false;
    }

    void StartMaterialEffect()
    {
        if (mat == null)
            return;

        meshingMaterialManager.RegisterMeshingMaterial(mat);
    }

    void StopMaterialEffect()
    {
        if (mat == null)
            return;

        meshingMaterialManager.UnregisterMeshingMaterial(mat);
    }

    void Update()
    {
        UpdateVFXParameter();
    }

    public virtual void UpdateVFXParameter()
    {
        if (isOn == false || GameManager.Instance.GameMode == GameMode.Undefined)
            return;

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null || NetworkManager.Singleton.LocalClient.PlayerObject.IsSpawned == false)
            return;

        if (player == null)
            return;

        if (player.currentEffectIndex.Value != effectIndex && player.targetEffectIndex.Value != effectIndex)
            return;


        // Update blending alpha
        effectAlpha = player.currentEffectIndex.Value == effectIndex ? 1 - player.effectLerp.Value : player.effectLerp.Value;


        // Update marching range
        marchingDistance += Time.deltaTime * marchingSpeed;
        effectRange.x = Remap.Map(marchingDistance, 0, effectMaxRange, 0, effectMaxRange, marchingMode);
        if(marchingMode == Remap.MapMode.Wrap || marchingMode == Remap.MapMode.Mirror)
            effectRange.y = Remap.Map(Mathf.Max(0, marchingDistance - effectWidth), 0, effectMaxRange, 0, effectMaxRange, marchingMode);
        else
            effectRange.y = Remap.Map(Mathf.Max(0, effectRange.x - effectWidth), 0, effectMaxRange, 0, effectMaxRange, marchingMode);

        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("EffectRangeX", effectRange.x.ToString());
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("EffectRangeY", effectRange.y.ToString());



        //// Update VFX
        //PushParametersToVFX();


        //// Update meshing material
        //PushParametersToMaterial();

        // Update both vfx and material
        PushParameters();
    }

    void PushParametersToVFX()
    {
        if (vfx != null)
        {
            if(vfx.HasBool("IsOn"))                 vfx.SetBool("IsOn", isOn);
            if (vfx.HasFloat("Alpha"))              vfx.SetFloat("Alpha", effectAlpha);
            if (vfx.HasVector4("EffectColor"))      vfx.SetVector4("EffectColor", effectColor);
            if (vfx.HasVector2("EffectRange"))      vfx.SetVector2("EffectRange", effectRange);
            if (vfx.HasFloat("EffectWidth"))        vfx.SetFloat("EffectWidth", effectWidth);
            if (vfx.HasVector3("Player_position"))  vfx.SetVector3("Player_position", player.Body.position);
            if (vfx.HasVector3("Player_angles"))    vfx.SetVector3("Player_angles", player.Body.eulerAngles);


            DepthImageProcessor depthImageProcessor = environmentProbe.DepthImageProcessor;
            if (needPushHumanStencil == true && depthImageProcessor != null && depthImageProcessor.HumanStencilTexture != null)
            {
                Texture2D human_tex = depthImageProcessor.HumanStencilTexture;
                human_tex.wrapMode = TextureWrapMode.Repeat;
                if (vfx.HasTexture("HumanStencilTexture")) vfx.SetTexture("HumanStencilTexture", human_tex);
                if (vfx.HasMatrix4x4("HumanStencilTextureMatrix")) vfx.SetMatrix4x4("HumanStencilTextureMatrix", depthImageProcessor.DisplayRotatioMatrix);
            }

            MeshToBufferConvertor meshToBufferConverter = environmentProbe.MeshToBufferConvertor;
            if (needPushBuffer == true && meshToBufferConverter != null)
            {
                if (vfx.HasInt("VertexCount"))
                {
                    vfx.SetInt("VertexCount", meshToBufferConverter.VertexCount);
                }                
                if (meshToBufferConverter.VertexBuffer != null && vfx.HasGraphicsBuffer("VertexBuffer"))
                {
                    vfx.SetGraphicsBuffer("VertexBuffer", meshToBufferConverter.VertexBuffer);
                }
                if (meshToBufferConverter.NormalBuffer != null && vfx.HasGraphicsBuffer("NormalBuffer"))
                {
                    vfx.SetGraphicsBuffer("NormalBuffer", meshToBufferConverter.NormalBuffer);
                }
            }


            MeshingRaycaster meshingRaycaster = environmentProbe.MeshingRaycaster;
            if (needPushHitPoint && meshingRaycaster != null)
            {
                if(vfx.HasBool("DidHit"))
                {
                    vfx.SetBool("DidHit", meshingRaycaster.DidHit);
                }                
                if (meshingRaycaster.HitPosition != null && vfx.HasVector3("HitPosition"))
                {
                    vfx.SetVector3("HitPosition", meshingRaycaster.HitPosition);
                }
                if (meshingRaycaster.HitNormal != null && vfx.HasVector3("HitNormal"))
                {
                    vfx.SetVector3("HitNormal", meshingRaycaster.HitNormal);
                }
            }

            HolokitAudioProcessor audioProcessor = environmentProbe.AudioProcessor;
            if (needPushAudioData && audioProcessor != null)
            {
                if (vfx.HasFloat("AudioVolume")) vfx.SetFloat("AudioVolume", audioProcessor.AudioVolume);

                if (vfx.HasFloat("AudioPitch")) vfx.SetFloat("AudioPitch", audioProcessor.AudioPitch);
            }
        }
    }

    void PushParametersToMaterial()
    {
        if (mat != null)
        {
            if (mat.HasInt("_IsOn")) mat.SetInt("_IsOn", isOn ? 1 : 0);
            if (mat.HasFloat("_Alpha")) mat.SetFloat("_Alpha", effectAlpha);
            if (mat.HasColor("_EffectColor")) mat.SetColor("_EffectColor", effectColor);
            if (mat.HasVector("_EffectRange")) mat.SetVector("_EffectRange", effectRange);
            if (mat.HasFloat("_EffectWidth")) mat.SetFloat("_EffectWidth", effectWidth);
            if (mat.HasVector("_Player_position")) mat.SetVector("_Player_position", player.Body.position);
            if (mat.HasVector("_Player_angles")) mat.SetVector("_Player_angles", player.Body.eulerAngles);
            
        }
    }

    void PushParameters()
    {
        PushSingleParameter("IsOn", isOn);
        PushSingleParameter("Alpha", effectAlpha);
        PushSingleParameter("EffectColor", effectColor);
        PushSingleParameter("EffectRange", effectRange);
        PushSingleParameter("EffectWidth", effectWidth);
        PushSingleParameter("Player_position", player.Body.position);
        PushSingleParameter("Player_angles", player.Body.eulerAngles);

        DepthImageProcessor depthImageProcessor = environmentProbe.DepthImageProcessor;
        if (needPushHumanStencil == true && depthImageProcessor != null && depthImageProcessor.HumanStencilTexture != null)
        {
            Texture2D human_tex = depthImageProcessor.HumanStencilTexture;
            human_tex.wrapMode = TextureWrapMode.Repeat;

            PushSingleParameter("HumanStencilTexture", human_tex);
            PushSingleParameter("HumanStencilTextureMatrix", depthImageProcessor.DisplayRotatioMatrix);
        }

        MeshToBufferConvertor meshToBufferConverter = environmentProbe.MeshToBufferConvertor;
        if (needPushBuffer == true && meshToBufferConverter != null)
        {
            PushSingleParameter("VertexCount", meshToBufferConverter.VertexCount);
            PushSingleParameter("VertexBuffer", meshToBufferConverter.VertexBuffer);
            PushSingleParameter("NormalBuffer", meshToBufferConverter.NormalBuffer);
        }


        MeshingRaycaster meshingRaycaster = environmentProbe.MeshingRaycaster;
        if (needPushHitPoint && meshingRaycaster != null)
        {
            PushSingleParameter("DidHit", meshingRaycaster.DidHit);
            PushSingleParameter("HitPosition", meshingRaycaster.HitPosition);
            PushSingleParameter("HitNormal", meshingRaycaster.HitNormal);
        }

        HolokitAudioProcessor audioProcessor = environmentProbe.AudioProcessor;
        if (needPushAudioData && audioProcessor != null)
        {
            PushSingleParameter("AudioVolume", audioProcessor.AudioVolume);
            PushSingleParameter("AudioPitch", audioProcessor.AudioPitch);
        }
    }

    void PushSingleParameter(string name, int value)
    {
        if (vfx != null && vfx.HasInt(name))
            vfx.SetInt(name, value);

        if(mat != null && mat.HasInt("_" + name))
            mat.SetFloat("_" + name, value);
    }

    void PushSingleParameter(string name, bool value)
    {
        if (vfx != null && vfx.HasBool(name))
            vfx.SetBool(name, value);

        if (mat != null && mat.HasInt("_" + name))
            mat.SetFloat("_" + name, value ? 1 : 0);
    }

    void PushSingleParameter(string name, float value)
    {
        if (vfx != null && vfx.HasFloat(name))
            vfx.SetFloat(name, value);

        if (mat != null && mat.HasFloat("_" + name))
            mat.SetFloat("_" + name, value);
    }

    void PushSingleParameter(string name, Color value)
    {
        if (vfx != null && vfx.HasVector4(name))
            vfx.SetVector4(name, value);

        if (mat != null && mat.HasColor("_" + name))
            mat.SetColor("_" + name, value);
    }

    void PushSingleParameter(string name, Vector2 value)
    {
        if (vfx != null && vfx.HasVector2(name))
            vfx.SetVector2(name, value);

        if (mat != null && mat.HasVector("_" + name))
            mat.SetVector("_" + name, value);
    }

    void PushSingleParameter(string name, Vector3 value)
    {
        if (vfx != null && vfx.HasVector3(name))
            vfx.SetVector3(name, value);

        if (mat != null && mat.HasVector("_" + name))
            mat.SetVector("_" + name, value);
    }
    void PushSingleParameter(string name, Vector4 value)
    {
        if (vfx != null && vfx.HasVector4(name))
            vfx.SetVector4(name, value);

        if (mat != null && mat.HasVector("_" + name))
            mat.SetVector("_" + name, value);
    }
    void PushSingleParameter(string name, Matrix4x4 value)
    {
        if (vfx != null && vfx.HasMatrix4x4(name))
            vfx.SetMatrix4x4(name, value);

        if (mat != null && mat.HasMatrix("_" + name))
            mat.SetMatrix("_" + name, value);
    }

    void PushSingleParameter(string name, Texture2D value)
    {
        if (vfx != null && vfx.HasTexture(name))
            vfx.SetTexture(name, value);

        if (mat != null && mat.HasTexture("_" + name))
            mat.SetTexture("_" + name, value);
    }

    void PushSingleParameter(string name, GraphicsBuffer value)
    {
        if (vfx != null && vfx.HasGraphicsBuffer(name))
            vfx.SetGraphicsBuffer(name, value);

        //if (mat != null && mat.HasBuffer("_" + name))
        //    mat.SetBuffer("_" + name, value);
    }

    
}
