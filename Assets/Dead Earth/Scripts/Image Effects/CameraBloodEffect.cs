using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode()]
public class CameraBloodEffect : MonoBehaviour {

    //Inspector Assigned
    [SerializeField] private float _bloodAmout = 0.0f;
    [SerializeField] private float _minBloodAmout = 0.0f;
    [SerializeField] private Shader _shader = null;
    [SerializeField] Texture2D _bloodTexture = null;
    [SerializeField] Texture2D _bloodNormalMap = null;
    [SerializeField] private float _distortion = 1.0f;
    [SerializeField] bool _autoFade = true;
    [SerializeField] float _fadeSpeed = 0.05f;

    //Private
    private Material _material = null;

    //Properties
    public float bloodAmount { get { return _bloodAmout; } set { _bloodAmout = value; } }
    public float minBloodAmount { get { return _minBloodAmout; } set { _minBloodAmout = value; } }
    public float fadeSpeed { get{ return _fadeSpeed; } set { _fadeSpeed = value; }  }
    public bool autoFade { get { return _autoFade; } set { _autoFade = value; } }


    private void Update()
    {
        if (autoFade)
        {
            _bloodAmout -= _fadeSpeed * Time.deltaTime;
            _bloodAmout = Mathf.Max(_bloodAmout, _minBloodAmout);
        }
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        //check if shader extists
        if (_shader == null) return;

        //create material
        if(_material == null)
        {
            _material = new Material(_shader);
        }

        //material could not be created eg shader did not compile
        if (_material == null) return;

        //send data into shader
        if(_bloodTexture !=null) _material.SetTexture("_BloodTex", _bloodTexture);
        if(_bloodNormalMap !=null) _material.SetTexture("_BloodBump", _bloodNormalMap);
        _material.SetFloat("_Distortion", _distortion);
        _material.SetFloat("_BloodAmount", _bloodAmout);

        //Perform Image Effect
        Graphics.Blit(src, dest, _material);
    }
}
