using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class EffectBase : MonoBehaviour
{
    [SerializeField]
    protected VisualEffect vfx = null;
    [SerializeField]
    protected Material mat = null;

    protected bool needPushHumanStencil = false;
    DepthImageProcessor depthImageProcessor;
    protected bool needPushBuffer = false;
    MeshToBufferConvertor meshToBufferConverter;

    protected int effectIndex = -1;

    protected bool isOn = false;
    protected float effectAlpha = 0;    
    protected Vector2 effectRange = new Vector2(0, float.MaxValue);

    void Awake()
    {
        depthImageProcessor = FindFirstObjectByType<DepthImageProcessor>();
        if(depthImageProcessor == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find DepthImageProcessor");
        }

        meshToBufferConverter = FindFirstObjectByType<MeshToBufferConvertor>();
        if (meshToBufferConverter == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find MeshToBufferConverter");
        }

        effectIndex = transform.GetSiblingIndex();
    }

    public virtual void StartEffect()
    {
        isOn = true;
    }

    public virtual void StopEffect()
    {
        isOn = false;
    }

    public virtual void UpdateVFXParameter()
    {
        if (isOn == false || GameManager.Instance.GameMode == GameMode.Undefined)
            return;

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null || NetworkManager.Singleton.LocalClient.PlayerObject.IsSpawned == false)
            return;


        Player player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();

        if (player.currentEffectIndex.Value != effectIndex && player.targetEffectIndex.Value != effectIndex)
            return;



        effectAlpha = player.currentEffectIndex.Value == effectIndex ? 1 - player.effectLerp.Value : player.effectLerp.Value;

        if(vfx != null)
        {
            vfx.SetBool("IsOn", isOn);
            vfx.SetFloat("Alpha", effectAlpha);
            vfx.SetVector2("Range", effectRange);
            vfx.SetVector3("Player_position", player.Body.position);
            vfx.SetVector3("Player_angles", player.Body.eulerAngles);

            if (needPushHumanStencil == true && depthImageProcessor != null && depthImageProcessor.HumanStencilTexture != null) 
            {
                Texture2D human_tex = depthImageProcessor.HumanStencilTexture;
                human_tex.wrapMode = TextureWrapMode.Repeat;
                if(vfx.HasTexture("HumanStencilTexture")) vfx.SetTexture("HumanStencilTexture", human_tex);
                if (vfx.HasMatrix4x4("HumanStencilTextureMatrix")) vfx.SetMatrix4x4("HumanStencilTextureMatrix", depthImageProcessor.DisplayRotatioMatrix);
            }

            if(needPushBuffer == true && meshToBufferConverter != null )
            {
                if (meshToBufferConverter.VertexBuffer != null && vfx.HasGraphicsBuffer("VertexBuffer"))
                {
                    vfx.SetGraphicsBuffer("VertexBuffer", meshToBufferConverter.VertexBuffer);
                }
                if (meshToBufferConverter.NormalBuffer != null && vfx.HasGraphicsBuffer("NormalBuffer"))
                {
                    vfx.SetGraphicsBuffer("NormalBuffer", meshToBufferConverter.NormalBuffer);
                }
            }
        }

        if(mat != null)
        {
            mat.SetInt("_IsOn", isOn ? 1 : 0);
            mat.SetFloat("_Alpha", effectAlpha);
        }
        
    }

    void Update()
    {
        UpdateVFXParameter();
    }
}
