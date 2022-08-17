using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraView : MonoBehaviour
{

    RawImage rawImage;
    AspectRatioFitter aspectRatioFitter;
    WebCamTexture webCamTexture;
    bool ratioSet;

    // Start is called before the first frame update
    void Start()
    {
        if (webCamTexture.width < 100 && !ratioSet)
        {
            ratioSet = true;
            SetAspectRatio();
        }
    }

    void SetAspectRatio()
    {
        aspectRatioFitter.aspectRatio = (float)webCamTexture.width / (float)webCamTexture.height;
    }

    void InitWebCam()
    {
        string camName = WebCamTexture.devices[0].name;
        webCamTexture = new WebCamTexture(camName, Screen.width, Screen.height, 30);
        rawImage.texture = webCamTexture;
        webCamTexture.Play();
    }
    public WebCamTexture GetCamImage()
    {
        return webCamTexture;
    }
}
